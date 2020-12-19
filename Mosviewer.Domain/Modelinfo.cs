using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Mosviewer.Domain
{
    public class Modelinfo
    {
        public DateTime IssueTime { get; set; }
        public Dictionary<string, DateTime> ModelReferenceTimes { get; set; } = new();
        public List<DateTime> TimeSteps { get; set; } = new();
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(IssueTime.Ticks);
            writer.Write(ModelReferenceTimes.Count);
            foreach (var m in ModelReferenceTimes)
            {
                writer.Write(m.Key);
                writer.Write(m.Value.Ticks);
            }
            writer.Write(TimeSteps.Count);
            foreach (var t in TimeSteps)
            {
                writer.Write(t.Ticks);
            }
        }

        public static Modelinfo Deserialize(BinaryReader reader)
        {
            var modelinfo = new Modelinfo();
            modelinfo.IssueTime = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);

            var modelReferenceTimesCount = reader.ReadInt32();
            var modelReferenceTimes = new Dictionary<string, DateTime>(modelReferenceTimesCount);
            for (int i = 0; i < modelReferenceTimesCount; i++)
            {
                modelReferenceTimes.Add(
                    reader.ReadString(),
                    new DateTime(reader.ReadInt64(), DateTimeKind.Utc));
            }
            modelinfo.ModelReferenceTimes = modelReferenceTimes;

            var timeStepsCount = reader.ReadInt32();
            var timeSteps = new List<DateTime>(timeStepsCount);
            for (int i = 0; i < timeStepsCount; i++)
            {
                timeSteps.Add(new DateTime(reader.ReadInt64(), DateTimeKind.Utc));
            }
            modelinfo.TimeSteps = timeSteps;
            return modelinfo;
        }
    }
}
