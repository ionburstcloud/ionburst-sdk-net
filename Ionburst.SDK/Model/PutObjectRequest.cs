// Copyright Ionburst Limited 2019
using System.IO;

namespace Ionburst.SDK.Model
{
    public class PutObjectRequest : ObjectRequest
    {
        public Stream DataStream { get; set; }
        public long StreamPosition { get; set; }
        public string PolicyClassification { get; set; }
        public int PolicyClassificationId { get; set; }
    }
}
