// Copyright Ionburst Limited 2022
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace Ionburst.SDK.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class IonburstManifest
    {
        public IonburstManifest() { Chunks = new List<IonburstChunk>(); }

        [JsonProperty]
        public List<IonburstChunk> Chunks { get; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public long ChunkCount { get; set; }

        [JsonProperty]
        public long ChunkSize { get; set; }

        [JsonProperty]
        public long MaxSize { get; set; }

        [JsonProperty]
        public long Size { get; set; }

        [JsonProperty]
        public string Hash { get; set; }

        [JsonProperty]
        public string IV { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public IonburstChunk GetChunk(int ordinal)
        {
            if (Chunks.Exists(c => c.Ord == ordinal))
            {
                return Chunks.First(c => c.Ord == ordinal);
            }

            return null;
        }
    }
}
