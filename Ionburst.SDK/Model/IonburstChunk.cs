// Copyright Ionburst Limited 2022
using System;

using Newtonsoft.Json;

namespace Ionburst.SDK.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class IonburstChunk
    {
        public IonburstChunk() { Id = Guid.NewGuid(); }

        [JsonProperty]
        public Guid Id { get; set; }

        [JsonProperty]
        public int Ord { get; set; }

        [JsonProperty]
        public string Hash { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
