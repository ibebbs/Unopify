using System;

namespace Unopify.AuthServer
{
    public static partial class Secrets
    {
        static partial void GetClientId(ref string clientId);

        public static string ClientId
        {
            get
            {
                string clientId = null;
                GetClientId(ref clientId);

                if (string.IsNullOrWhiteSpace(clientId))
                {
                    throw new Exception("You must implement the GetClientId partial method");
                }

                return clientId;
            }
        }

        static partial void GetClientSecret(ref string clientSecret);

        public static string ClientSecret
        {
            get
            {
                string clientSecret = null;
                GetClientSecret(ref clientSecret);

                if (string.IsNullOrWhiteSpace(clientSecret))
                {
                    throw new Exception("You must implement the GetClientSecret partial method");
                }

                return clientSecret;
            }
        }

        static partial void GetAuthRelayHubUri(ref string authRelayHubUri);

        public static string AuthRelayHubUri
        {
            get
            {
                string authRelayHubUri = "http://localhost:5000/Hub";

                GetAuthRelayHubUri(ref authRelayHubUri);

                return authRelayHubUri;
            }
        }
    }
}
