namespace Unopify.AuthRelay
{
    public interface ITokenStore
    {
        string Token { get; set; }
    }

    public class TokenStore : ITokenStore
    {
        public string Token { get; set; }
    }
}
