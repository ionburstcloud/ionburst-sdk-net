// Copyright Ionburst Limited 2019
using Microsoft.Extensions.Configuration;
using System;

namespace Ionburst.SDK
{
    public class IonburstClientFactory
    {
        public static IIonburstClient CreateIonburstClient(string serverUri = "")
        {
            return new IonburstClient(serverUri);
        }

        [Obsolete("Use CreateIonburstClient")]
        public static IIonburstClient CreateIonBurstClient(string serverUri = "")
        {
            return new IonburstClient(serverUri);
        }

        public static IIonburstClient CreateIonburstClient(IConfiguration externalConfiguration, string serverUri = "")
        {
            return new IonburstClient(externalConfiguration, serverUri);
        }

        [Obsolete("Use CreateIonburstClient")]
        public static IIonburstClient CreateIonBurstClient(IConfiguration externalConfiguration, string serverUri = "")
        {
            return new IonburstClient(externalConfiguration, serverUri);
        }

        public static IIonburstClient CreateIonburstClient()
        {
            return new IonburstClient();
        }

        [Obsolete("Use CreateIonburstClient")]
        public static IIonburstClient CreateIonBurstClient()
        {
            return new IonburstClient();
        }
    }
}
