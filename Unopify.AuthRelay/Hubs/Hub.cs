using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Unopify.AuthRelay.Hubs
{
    public class Hub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly ITokenStore _tokenStore;

        public Hub(ITokenStore tokenStore)
        {
            _tokenStore = tokenStore;
        }

        public async Task RequestToken()
        {
            await Clients.Caller.SendAsync("Token", _tokenStore.Token);
        }

        public async Task TokenChanged(string token)
        {
            _tokenStore.Token = token;

            await Clients.All.SendAsync("Token", token);
        }
    }
}
