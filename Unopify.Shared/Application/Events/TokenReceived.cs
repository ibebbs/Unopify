namespace Unopify.Application.Events
{
    public class TokenReceived
    {
        public TokenReceived(string token)
        {
            Token = token;
        }

        public string Token { get; }
    }
}
