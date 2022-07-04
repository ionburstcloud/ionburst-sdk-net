// Copyright Cyborn Limited 2022
using System;
using System.Collections.Generic;

namespace Ionburst.SDK.Model
{
    public class DeleteManifestResult : DeleteObjectResult
    {
        public List<Guid> ManifestActivities { get; set; }
    }
}
