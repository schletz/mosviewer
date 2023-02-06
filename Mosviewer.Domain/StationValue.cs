using System;
using System.IO;
using System.Text;

namespace Mosviewer.Domain
{
    public class StationValue
    {
        private int _forecastDate;
        private int _value;

        public string StationId { get; set; } = "";
        public string Parameter { get; set; } = "";
        public DateTime ForecastDate
        {
            get => new DateTime(_forecastDate * TimeSpan.TicksPerMinute, DateTimeKind.Utc);
            set => _forecastDate = (int)(value.Ticks / TimeSpan.TicksPerMinute);
        }
        public decimal? Value
        {
            get => _value == int.MinValue ? null : (_value / 10000M);
            set => _value = value.HasValue ? (int)(value.Value * 10000) : int.MinValue;
        }
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(StationId);
            writer.Write(Parameter);
            writer.Write(_forecastDate);
            writer.Write(_value);
        }
        public static StationValue Deserialize(BinaryReader reader)
        {
            var stationValue = new StationValue();

            stationValue.StationId = reader.ReadString();
            stationValue.Parameter = reader.ReadString();
            stationValue._forecastDate = reader.ReadInt32();
            stationValue._value = reader.ReadInt32();
            return stationValue;
        }
    }
}
