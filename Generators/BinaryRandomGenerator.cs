using HomeAssistantDataGenerator.Configuration;
using System;

namespace HomeAssistantDataGenerator.Generators
{
    class BinaryRandomGenerator : IDataGenerator
    {
        public int ValuesInMinute { get; set; } // количество значений в минуту

        public double OnProbability { get; set; } // вероятность включения

        public double OffProbability { get; set; } // вероятность отключения

        public int MinutesBetweenSwitching { get; set; } // минимальное количество минут между переключениями

        public BinaryRandomGenerator(PresetGenerator configuration)
        {
            ValuesInMinute = configuration.ValuesInMinute;
            OnProbability = configuration.OnProbability;
            OffProbability = configuration.OffProbability;
            MinutesBetweenSwitching = configuration.MinutesBetweenSwitching;
        }

        protected Random Random { get; set; } = new Random((int)DateTime.Now.Ticks);
        protected DateTime LastDateTimeValue { get; set; } = DateTime.MinValue;
        protected DateTime NextSwitchingDateTime { get; set; } = DateTime.MinValue;
        protected bool LastValue { get; set; } = false;

        public bool GetValue(DateTime dateTime, out object val)
        {
            val = default;

            if (
                ((dateTime - LastDateTimeValue).TotalSeconds > 60 / ValuesInMinute) &&
                (dateTime - NextSwitchingDateTime).TotalSeconds >= 0)
            {
                var randomValue = Random.NextDouble();

                if (randomValue > 1 - (LastValue ? OffProbability : OnProbability))
                {
                    LastDateTimeValue = dateTime;
                    NextSwitchingDateTime = dateTime.AddMinutes(MinutesBetweenSwitching);

                    LastValue = !LastValue;
                    val = LastValue ? "On" : "Off";

                    return true;
                }
            }

            return false;
        }
    }
}
