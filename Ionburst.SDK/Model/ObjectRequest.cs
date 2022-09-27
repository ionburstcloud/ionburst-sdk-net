// Copyright Ionburst Limited 2019
using System;

namespace Ionburst.SDK.Model
{
    public abstract class ObjectRequest: IObjectRequest
    {
        public string Server { get; set; }
        public string Routing { get; set; }
        public string Particle { get; set; }
        public TimeSpan RequestTimeout { get; set; }
        public bool TimeoutSpecified { get; set; }
        public ResultDelegate RequestResult { get; set; }
        public string DelegateTag { get; set; }
        public bool PhasedMode { get; set; }
        public Guid DeferredToken { get; set; }

        public void CheckTrailingCharacters()
        {
            if (Server != string.Empty)
            {
                if (!Server.EndsWith("/"))
                {
                    Server = $"{Server}/";
                }
            }
            if (Routing != string.Empty)
            {
                if (!Routing.EndsWith("/"))
                {
                    Routing = $"{Routing}/";
                }
            }
        }

        public void CheckValues(string serverUri, string routing)
        {
            if (Server == string.Empty || Server == null)
            {
                Server = serverUri;
            }
            if (Routing == string.Empty || Routing == null)
            {
                Routing = routing;
            }
            CheckTrailingCharacters();
        }

    }
}
