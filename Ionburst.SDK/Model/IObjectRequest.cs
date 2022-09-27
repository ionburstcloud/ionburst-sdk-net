// Copyright Ionburst Limited 2019
using System;

namespace Ionburst.SDK.Model
{
    public delegate void ResultDelegate(IObjectResult result);

    public interface IObjectRequest
    {
        string Server { get; set; }
        string Routing { get; set; }
        string Particle { get; set; }
        TimeSpan RequestTimeout { get; set; }
        bool TimeoutSpecified { get; set; }
        ResultDelegate RequestResult { get; set; }
        string DelegateTag { get; set; }
    }
}
