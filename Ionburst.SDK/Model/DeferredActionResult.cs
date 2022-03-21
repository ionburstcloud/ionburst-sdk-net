// Copyright Ionburst Limited 2019
using System;

namespace Ionburst.SDK.Model
{
    public class DeferredActionResult: ObjectResult
    {
        public Guid DeferredToken { get; set; }
    }
}
