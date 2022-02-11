using System.Collections.Generic;

namespace HomeAssistantDataGenerator.Configuration
{
    public enum GeneratorType
    {
        File,
        Wave,
        BinaryRandom
    }

    public enum ValuesType
    {
        Integer,
        Double,
        String,
        Boolean
    }

    public enum DataFormat
    {
        Correct = 0,
        Invalid1,
        Invalid2,
        Invalid3,
        Invalid4,
        Invalid5
    }

    public class PresetGenerator
    {
        public GeneratorType GeneratorType { get; set; }

        public ValuesType ValuesType { get; set; }

        public string FileName { get; set; }

        public double Frequency { get; set; }

        public double Amplitude { get; set; }

        public double VerticalShift { get; set; }

        public double HorizontalShift { get; set; }

        public int ValuesInMinute { get; set; }

        public double ScatterValues { get; set; }

        public int DigitsAfterPoint { get; set; }
        
        public double OnProbability { get; set; }

        public double OffProbability { get; set; }
        
        public int MinutesBetweenSwitching { get; set; }
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
        public DataFormat DataFormat { get;set; }
    }

    public class ProgramConfiguration
    {
        public string ServiceName { get; set; }
        public List<VirtualDevice> Devices { get; set; }
    }
}
