// Copyright Ionburst Limited 2019
using Microsoft.Extensions.Configuration;

namespace Ionburst.SDK
{
    public class IonburstClientFactory
    {
        public static IIonburstClient CreateIonBurstClient(string serverUri = "")
        {
            return new IonburstClient(serverUri);
        }

        public static IIonburstClient CreateIonBurstClient(IConfiguration externalConfiguration, string serverUri = "")
        {
            return new IonburstClient(externalConfiguration, serverUri);
        }

        public static IIonburstClient CreateIonBurstClient()
        {
            return new IonburstClient();
        }
    }
}
