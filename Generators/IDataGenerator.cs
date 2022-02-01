using System;

namespace HomeAssistantDataGenerator.Generators
{
    public interface IDataGenerator
    {
        bool GetValue(DateTime datetIme, out object val);
    }
}