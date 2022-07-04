// Copyright Ionburst Limited 2022
namespace Ionburst.SDK.Model
{
    public class PutManifestRequest : PutObjectRequest
    {
        public long ChunkSize { get; set; }
    }
}
