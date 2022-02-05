using HomeAssistantDataGenerator.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HomeAssistantDataGenerator.Generators
{
    public class DataValue<T>
    {
        public DateTime DateTime { get; set; }
        public T Value { get; set; }
    }

    public class FileGenerator<T> : IDataGenerator
    {
        protected Queue<DataValue<T>> DataGeneratorValues { get; } = new Queue<DataValue<T>>();

        public FileGenerator(PresetGenerator configuration)
        {
            var fileData = File.ReadAllText(configuration.FileName).Split(Environment.NewLine);
            foreach (var dataItem in fileData)
            {
                var values = dataItem.Split(";");

                if (values.Length < 2)
                    continue;

                DataGeneratorValues.Enqueue(
                    new DataValue<T>
                    {
                        DateTime = DateTime.Parse(values[0]),
                        Value = (T)Convert.ChangeType(values[1], typeof(T))
                    });
            }
        }

        public bool GetValue(DateTime datetIme, out object val)
        {
            val = default(T);

            var deltaTime = DataGeneratorValues
                .ToList()
                .Select(e => new { val = e, span = (e.DateTime.TimeOfDay - datetIme.TimeOfDay).Duration() })
                .Where(e => e.span.TotalSeconds <= 1)
                .OrderBy(e => e.span);

            if (deltaTime.Any())
            {
                val = deltaTime.First().val.Value;
                return true;
            }

            return false;
        }
    }
}