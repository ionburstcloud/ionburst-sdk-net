// Copyright Cyborn Limited 2022
using System;
using System.Collections.Generic;

namespace Ionburst.SDK.Model
{
    public class GetManifestResult : GetObjectResult
    {
        public List<Guid> ManifestActivities { get; set; }
    }
}
