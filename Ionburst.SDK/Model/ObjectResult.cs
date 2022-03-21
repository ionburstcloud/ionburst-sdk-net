// Copyright Ionburst Limited 2019
using System;

namespace Ionburst.SDK.Model
{
    public abstract class ObjectResult: IObjectResult
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public Guid ActivityToken { get; set; }
        public string DelegateTag { get; set; }
    }
}
