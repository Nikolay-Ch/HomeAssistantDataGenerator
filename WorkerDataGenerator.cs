using HassSensorConfiguration;
using HomeAssistantDataGenerator.Configuration;
using HomeAssistantDataGenerator.Generators;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HomeAssistantDataGenerator
{
    public interface IWorkerDataGenerator
    {
        Task PublishAsync(string topic, string payload, MqttQualityOfServiceLevel mqttQosLevel);
        Task PublishAsync(string configurationTopic, string configurationPayload, MqttQualityOfServiceLevel mqttQosLevel, bool retain);
    }

    public class WorkerDataGenerator : BackgroundService, IWorkerDataGenerator
    {
        protected ILogger<WorkerDataGenerator> Logger { get; }
        protected ProgramConfiguration ProgramConfiguration { get; }
        protected MqttConfiguration MqttConfiguration { get; }

        public IManagedMqttClient MqttClient { get; }

        protected List<IHassComponent> ComponentList { get; } = new List<IHassComponent>();

        protected List<Task> GeneratorTasks { get; set; } = new();

        public WorkerDataGenerator(ILogger<WorkerDataGenerator> logger, IOptions<ProgramConfiguration> programConfiguration, IOptions<MqttConfiguration> mqttConfiguration)
        {
            Logger = logger;
            ProgramConfiguration = programConfiguration.Value;
            MqttConfiguration = mqttConfiguration.Value;

            MqttClient = new MqttFactory().CreateManagedMqttClient();
        }

        private static IDataGenerator GeneratorFactory(PresetGenerator presetGenerator)
        {
            switch (presetGenerator.GeneratorType)
            {
                case GeneratorType.File:
                    var gen1 = typeof(FileGenerator<>);
                    Type[] typeArgs1 = { GetValuesType(presetGenerator.ValuesType) };
                    var makeme1 = gen1.MakeGenericType(typeArgs1);
                    return (IDataGenerator)Activator.CreateInstance(makeme1, presetGenerator);
                case GeneratorType.Wave:
                    var gen2 = typeof(WaveGenerator<>);
                    Type[] typeArgs2 = { GetValuesType(presetGenerator.ValuesType) };
                    var makeme2 = gen2.MakeGenericType(typeArgs2);
                    return (IDataGenerator)Activator.CreateInstance(makeme2, presetGenerator);
                default: return null;

            }
        }

        private static Type GetValuesType(ValuesType valuesType) =>
            valuesType switch
            {
                ValuesType.Integer => typeof(Int32),
                ValuesType.Double => typeof(Double),
                ValuesType.String => typeof(String),
                _ => throw new ArgumentException($"Invalud type! {valuesType}"),
            };

        public async Task PublishAsync(string topic, string payload, MqttQualityOfServiceLevel mqttQosLevel) =>
            await MqttClient.PublishAsync(topic, payload, mqttQosLevel);

        public async Task PublishAsync(string topic, string payload, MqttQualityOfServiceLevel mqttQosLevel, bool retain) =>
            await MqttClient.PublishAsync(topic, payload, mqttQosLevel, retain);


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var task = ConnectToMqtt(stoppingToken);

                CreateComponents();

                CreateGeneratorTasks(stoppingToken);

                await task;

                // send device configuration with retain flag
                await SendDeviceConfiguration();
                Logger.LogInformation("Sent devices configuration at: {time}", DateTimeOffset.Now);

                //await PostSendConfigurationAsync(stoppingToken);

                Logger.LogInformation("Running at: {time}", DateTimeOffset.Now);

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                        await Task.Delay(30000, stoppingToken);

                    // wait to all work tasks finished
                    Task.WaitAll(GeneratorTasks.ToArray(), 5000, stoppingToken);
                }
                catch (TaskCanceledException) { }

                Logger.LogInformation("Stopping at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error at {time}", DateTimeOffset.Now);
            }
        }

        private async Task ConnectToMqtt(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Creating MqttClient at: {time}. Uri:{mqttUri}", DateTimeOffset.Now, MqttConfiguration.MqttUri);

            var messageBuilder = new MqttClientOptionsBuilder()
                .WithClientId(MqttConfiguration.ClientId.Replace("-", "").Replace(" ", ""))
                .WithCredentials(MqttConfiguration.MqttUser, MqttConfiguration.MqttUserPassword)
                .WithTcpServer(MqttConfiguration.MqttUri, MqttConfiguration.MqttPort)
                .WithCleanSession();

            var options = MqttConfiguration.MqttSecure ?
                messageBuilder.WithTls().Build() :
                messageBuilder.Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(options)
                .Build();

            await MqttClient.StartAsync(managedOptions);

            // wait for connection
            while (!MqttClient.IsConnected && !stoppingToken.IsCancellationRequested)
            {
                Logger.LogTrace("MqttClient not connected... Go to sleep for a second...");
                Thread.Sleep(1000);
            }

            Logger.LogInformation("Creating MqttClient done at: {time}", DateTimeOffset.Now);
        }

        private void CreateComponents()
        {
            foreach (var virtualDevice in ProgramConfiguration.Devices)
            {
                var device = new Device
                {
                    Name = virtualDevice.DeviceDescription.Name,
                    Model = virtualDevice.DeviceDescription.Model,
                    Manufacturer = virtualDevice.DeviceDescription.Manufacturer,
                    Identifiers = new List<string> { virtualDevice.DeviceDescription.Identifier }
                };

                var deviceDescription = GetDeviceClassDescriptionValue(virtualDevice.DeviceDescription.DeviceType);
                var componentFactory = deviceDescription.ComponentFactory;
                var sensorDescription = componentFactory.CreateSensorDescription();
                sensorDescription.DeviceClassDescription = deviceDescription;
                sensorDescription.Device = device;
                ComponentList.Add(componentFactory.CreateComponent(sensorDescription));
            }
        }

        protected static DeviceClassDescription GetDeviceClassDescriptionValue(string deviceClass) =>
            deviceClass switch
            {
                "Temperature" => DeviceClassDescription.Temperature,
                "Voltage" => DeviceClassDescription.Voltage,
                "PressureHpa" => DeviceClassDescription.PressureHpa,
                "Current" => DeviceClassDescription.Current,
                "FrequencyHz" => DeviceClassDescription.FrequencyHz,
                "Humidity" => DeviceClassDescription.Humidity,
                "Plug" => DeviceClassDescription.Plug,
                _ => DeviceClassDescription.None,
            };

        public void CreateGeneratorTasks(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Creating Generators at: {time}", DateTimeOffset.Now);

            foreach (var device in ProgramConfiguration.Devices)
            {
                var generatorExt = GeneratorFactory(device.PresetGenerator);

                var task = new Task(() =>
                {
                    var component = ComponentList.First(e => e.Device.Identifiers[0] == device.DeviceDescription.Identifier);
                    var generator = generatorExt;
                    var ct = cancellationToken;

                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            if (generator.GetValue(DateTime.Now, out var value))
                            {
                                object payload = "";

                                /*if (component.DeviceClassDescription.ComponentFactory is BinarySensorFactory)
                                {
                                    payload = value.ToString();
                                }
                                else*/
                                {
                                    var payloadJObj = JObject.FromObject(new
                                    {
                                        Id = component.Device.Identifiers[0],
                                        name = $"{component.Device.Identifiers[0]}"
                                    });

                                    payloadJObj.Add(new JProperty(component.DeviceClassDescription.ValueName, value.ToString()));

                                    payload = payloadJObj;
                                }

                                // send message
                                var t = MqttClient.PublishAsync(
                                    component.StateTopic
                                        .Replace("+/+", $"{MqttConfiguration.MqttHomeDeviceTopic}/{ProgramConfiguration.ServiceName}"),
                                    payload.ToString(),
                                    MqttConfiguration.MqttQosLevel);

                                t.Wait();

                                Logger.LogInformation("WorkerDataGenerator send message: '{payload}' at {time}", payload, DateTimeOffset.Now);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e, "WorkerDataGenerator send message error at {time}", DateTimeOffset.Now);
                        }

                        ct.WaitHandle.WaitOne(1000);
                    }
                }, cancellationToken);

                task.Start();

                GeneratorTasks.Add(task);
            }

            Logger.LogInformation("Generators created at: {time}", DateTimeOffset.Now);
        }

        /// <summary>
        /// publish configuration message with retain flag to HomeAssistant
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        protected async Task SendDeviceConfiguration()
        {
            try
            {
                foreach (var component in ComponentList)
                {
                    await MqttClient.PublishAsync(
                        string.Format(MqttConfiguration.ConfigurationTopic,
                            component.GetType().GetHassComponentTypeString(), component.UniqueId),
                        JsonConvert.SerializeObject(component),
                        MqttConfiguration.MqttQosLevel,
                        true);

                    Logger.LogInformation("Send configuration for component {uniqueId} at: {time}", component.UniqueId, DateTimeOffset.Now);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error at {time}", DateTimeOffset.Now);
            }
        }
    }
}
