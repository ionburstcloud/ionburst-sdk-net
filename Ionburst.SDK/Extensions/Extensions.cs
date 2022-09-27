// Copyright Ionburst Limited 2022
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using Ionburst.SDK.Model;

namespace Ionburst.SDK.Extensions
{
    public class IonburstClientOptions
    {
        public string Uri { get; set; }
        public string Profile { get; set; }
        public IonburstCredential IonburstCredential { get; set; }
        public string Id { get; set; }
        public string Key { get; set; }
        public IConfiguration ExternalConfiguration { get; set; }
    }

    public static class Extensions
    {
        public static IServiceCollection AddIonburstClient(this IServiceCollection services, IonburstClientOptions clientOptions = null)
        {
            IonburstClient newClient = new IonburstClient();
            if (clientOptions != null)
            {
                if (!string.IsNullOrEmpty(clientOptions.Uri))
                {
                    newClient = newClient.WithIonburstUri(clientOptions.Uri);
                }
                if (!string.IsNullOrEmpty(clientOptions.Profile))
                {
                    newClient = newClient.WithProfile(clientOptions.Profile);
                }
                if (clientOptions.IonburstCredential != null)
                {
                    newClient = newClient.WithCredential(clientOptions.IonburstCredential);
                }
                if (!string.IsNullOrEmpty(clientOptions.Id))
                {
                    newClient = newClient.WithIonburstId(clientOptions.Id);
                }
                if (!string.IsNullOrEmpty(clientOptions.Key))
                {
                    newClient = newClient.WithIonburstKey(clientOptions.Key);
                }
                if (clientOptions.ExternalConfiguration != null)
                {
                    newClient = newClient.WithExternalConfiguration(clientOptions.ExternalConfiguration);
                }
            }
            newClient.Build();

            if (newClient != null)
            {
                services.AddSingleton<IIonburstClient>(newClient);
            }

            return services;
        }
    }
}
