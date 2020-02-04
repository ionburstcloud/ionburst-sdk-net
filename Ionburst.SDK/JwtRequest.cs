﻿// Copyright Ionburst Limited 2019
using Ionburst.SDK.Model;

namespace Ionburst.SDK
{
    internal class JwtRequest : ObjectRequest
    {
        public string Id { get; set; }
        public string Key { get; set; }
    }
}
