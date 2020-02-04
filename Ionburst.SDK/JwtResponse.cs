// Copyright Ionburst Limited 2019
using System;
using Ionburst.SDK.Model;

namespace Ionburst.SDK
{
    internal class JwtResponse : ObjectResult
    {
        public string JWT { get; set; }
        public Exception Exception { get; set; }
    }
}
