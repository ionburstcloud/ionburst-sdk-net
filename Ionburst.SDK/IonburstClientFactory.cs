// Copyright Ionburst Limited 2019
using Microsoft.Extensions.Configuration;

using Ionburst.SDK.Model;

namespace Ionburst.SDK
{
    public class IonburstClientFactory
    {
        public static IIonburstClient CreateIonburstClient(string serverUri = "")
        {
            if (string.IsNullOrEmpty(serverUri))
            {
                return new IonburstClient().Build();
            }

            return new IonburstClient()
                .WithIonburstUri(serverUri)
                .Build();
        }

        public static IIonburstClient CreateIonburstClient(IConfiguration externalConfiguration, string serverUri = "")
        {
            if (string.IsNullOrEmpty(serverUri))
            {
                return new IonburstClient()
                    .WithExternalConfiguration(externalConfiguration)
                    .Build();
            }

            return new IonburstClient()
                .WithIonburstUri(serverUri)
                .WithExternalConfiguration(externalConfiguration)
                .Build();
        }

        public static IIonburstClient CreateIonburstClient(IonburstCredential credential, string serverUri = "")
        {
            if (string.IsNullOrEmpty(serverUri))
            {
                return new IonburstClient()
                    .WithCredential(credential)
                    .Build();
            }

            return new IonburstClient()
                .WithIonburstUri(serverUri)
                .WithCredential(credential)
                .Build();
        }

        public static IIonburstClient CreateIonburstClient(IConfiguration externalConfiguration, IonburstCredential credential, string serverUri = "")
        {
            if (string.IsNullOrEmpty(serverUri))
            {
                return new IonburstClient()
                    .WithExternalConfiguration(externalConfiguration)
                    .WithCredential(credential)
                    .Build();
            }

            return new IonburstClient()
                .WithIonburstUri(serverUri)
                .WithExternalConfiguration(externalConfiguration)
                .WithCredential(credential)
                .Build();
        }

        public static IIonburstClient CreateIonburstClient()
        {
            return new IonburstClient().Build();
        }
    }
}
