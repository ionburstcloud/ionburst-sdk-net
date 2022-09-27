// Copright Ionburst Limited 2022
using System;
using System.Collections.Generic;

namespace Ionburst.SDK.Model
{
    public class PutManifestResult : PutObjectResult
    {
        public List<Guid> ManifestActivities { get; set; }
    }
}
