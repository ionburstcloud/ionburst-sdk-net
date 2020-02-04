// Copyright Ionburst Limited 2019
namespace Ionburst.SDK
{
    public class IonburstClientFactory
    {
        public static IIonburstClient CreateIonBurstClient(string serverUri = "")
        {
            return new IonburstClient(serverUri);
        }

        public static IIonburstClient CreateIonBurstClient()
        {
            return new IonburstClient();
        }
    }
}
