using System.Collections.Generic;

namespace HomeAssistantDataGenerator.Configuration
{
    public enum GeneratorType
    {
        Temperature,
        Voltage,
        Pressure,
        File
    }

    public enum ValuesType
    {
        Integer,
        Double,
        String,
        Boolean
    }

    public class PresetGenerator
    {
        public GeneratorType GeneratorType { get; set; }

        public double MinValue { get; set; }
        public double MaxValue { get; set; }

        public double RateOfChange { get; set; }
        public double ValuesShift { get; set; }

        public double VeluesShiftChangeRate { get; set; }

        public int ValuesInMinute { get; set; }

        public ValuesType ValuesType { get; set; }

        public string FileName { get; set; }
    }

    public class VirtualDevice
    {
        public DeviceDescription DeviceDescription { get; set; }
        public PresetGenerator PresetGenerator { get; set; }
    }

    public class DeviceDescription
    {
        public string Name { get; set; }
        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public string Identifier { get; set; }
        public string DeviceType { get; set; }
    }

    public class ProgramConfiguration
    {
        public string ServiceName { get; set; }
        public List<VirtualDevice> Devices { get; set; }
    }
}
