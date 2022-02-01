using HomeAssistantDataGenerator.Configuration;
using System;

namespace HomeAssistantDataGenerator.Generators
{
    class WaveGenerator<T> : IDataGenerator
    {
        public double Frequency { get; set; } // частота в минуту

        public double Amplitude { get; set; } // амплитуда

        public double VerticalShift { get; set; } // вертикальный сдвиг

        public double HorizontalShift { get; set; } // горизонтальный сдвиг

        public int ValuesInMinute { get; set; } // количество значений в минуту

        public double ScatterValues { get; set; } // разброс значений функции

        public int DigitsAfterPoint { get; set; } // количество значащих цифр после запятой

        public WaveGenerator(PresetGenerator configuration)
        {
            Frequency = configuration.Frequency;
            Amplitude = configuration.Amplitude;
            VerticalShift = configuration.VerticalShift;
            HorizontalShift = configuration.HorizontalShift;
            ValuesInMinute = configuration.ValuesInMinute;
            ScatterValues = configuration.ScatterValues;
            DigitsAfterPoint = configuration.DigitsAfterPoint;
        }

        protected Random Random { get; set; } = new Random((int)DateTime.Now.Ticks);
        protected DateTime LastDateTimeValue { get; set; } = DateTime.MinValue;

        public bool GetValue(DateTime dateTime, out object val)
        {
            val = default(T);

            if ((dateTime - LastDateTimeValue).TotalSeconds > 60 / ValuesInMinute)
            {
                var seconds = dateTime.TimeOfDay.TotalSeconds;
                var piFrequency = Math.PI / 60 * Frequency;
                var value = Math.Round(
                        VerticalShift + Amplitude * Math.Sin(HorizontalShift + seconds * piFrequency) + ScatterValues * (Random.NextDouble() * 2 - 1)
                    , DigitsAfterPoint);
                val = (T)Convert.ChangeType(value, typeof(T));

                LastDateTimeValue = dateTime;

                return true;
            }

            return false;
        }
    }
}
