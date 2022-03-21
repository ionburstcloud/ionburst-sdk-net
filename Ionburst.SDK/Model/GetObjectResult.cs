// Copyright Ionburst Limited 2019
using System.IO;

namespace Ionburst.SDK.Model
{
    public class GetObjectResult : ObjectResult
    {
        public Stream DataStream { get; set; }
    }
}
