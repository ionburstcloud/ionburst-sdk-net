// Copyright Ionburst Limited 2019
using System;

namespace Ionburst.SDK.Model
{
    public interface IObjectResult
    {
        int StatusCode { get; set; }
        string StatusMessage { get; set; }
        Guid ActivityToken { get; set; }
        string DelegateTag { get; set; }
    }
}
