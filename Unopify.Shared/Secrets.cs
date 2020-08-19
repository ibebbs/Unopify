using System;

namespace Unopify
{
    public static partial class Secrets
    {
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
