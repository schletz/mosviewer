using System;
using System.IO;

namespace Mosviewer.Domain
{
    public class StationValue
    {
        public string StationId { get; set; } = "";
        public string Parameter { get; set; } = "";
        public DateTime ForecastDate { get; set; }
        public decimal? Value { get; set; }
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(StationId);
            writer.Write(Parameter);
            writer.Write((int)(ForecastDate.Ticks / TimeSpan.TicksPerMinute));
            writer.Write(Value.HasValue);
            if (Value.HasValue) writer.Write(Value.Value);
        }
        public static StationValue Deserialize(BinaryReader reader)
        {
            var stationValue = new StationValue
            {
                StationId = reader.ReadString(),
                Parameter = reader.ReadString(),
                ForecastDate = new DateTime(reader.ReadInt32() * TimeSpan.TicksPerMinute, DateTimeKind.Utc),
                Value = reader.ReadBoolean() ? reader.ReadDecimal() : null
            };
            return stationValue;
        }
    }
}
