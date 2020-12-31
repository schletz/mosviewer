using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Mosviewer.Domain
{
    public class Station
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
        public decimal Elevation { get; set; }
        public string HtmlLabel => $"{Id} - {Name} ({Elevation:0}&thinsp;m)";
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Name);
            writer.Write(Lat);
            writer.Write(Lng);
            writer.Write(Elevation);
        }
        public static Station Deserialize(BinaryReader reader)
        {
            return new Station
            {
                Id = reader.ReadString(),
                Name = reader.ReadString(),
                Lat = reader.ReadDecimal(),
                Lng = reader.ReadDecimal(),
                Elevation = reader.ReadDecimal(),
            };
        }

    }
}
