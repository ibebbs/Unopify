using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Unopify.AuthServer
{
    public enum State
    {
        ConnectingToRelay,
        CheckingForTokens,
        PendingAuthentication,
        CodeExchange,
        PublishingToken,
        TokenPublished,
        RefreshingToken
    }

    public class Tokens
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class ReceivedTokens
    {
        private static readonly TimeSpan RefreshPadding = TimeSpan.FromMinutes(1);

        public Tokens Tokens { get; set; }

        public DateTimeOffset ReceivedAt { get; set; }

        private DateTimeOffset ExpiresAt
        {
            get { return ReceivedAt.Add(TimeSpan.FromSeconds(Tokens?.ExpiresIn ?? 0)); }
        }

        private DateTimeOffset ShouldRefreshAt
        {
            get { return ExpiresAt.Subtract(RefreshPadding); }
        }

        public TimeSpan ShouldRefreshIn
        {
            get 
            {
                var timeLeft = ShouldRefreshAt.Subtract(DateTimeOffset.UtcNow);

                return timeLeft < TimeSpan.Zero ? TimeSpan.Zero : timeLeft;
            }
        }

        public bool NeedsRefreshing
        {
            get { return DateTimeOffset.UtcNow > ExpiresAt.Subtract(RefreshPadding); }
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        private const string RedirectUri = "http://localhost:7791";
        private const string StateValue = "34fFs29kd09";

        private static readonly HttpClient HttpClient = new HttpClient();

        private readonly MVx.Observable.Property<State> _state;
        private readonly MVx.Observable.Property<HubConnectionState> _connectionState;
        private readonly MVx.Observable.Property<string> _exchangeCode;
        private readonly MVx.Observable.Property<Exception> _authenticationException;
        private readonly MVx.Observable.Property<ReceivedTokens> _tokens;
        private readonly MVx.Observable.Property<Exception> _exchangeException;
        private readonly MVx.Observable.Property<Exception> _publishTokenException;
        private readonly MVx.Observable.Property<Exception> _refreshingTokenException;

        private readonly HubConnection _connection;

        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModel()
        {
            _state = new MVx.Observable.Property<State>(nameof(State), args => PropertyChanged?.Invoke(this, args));
            _connectionState = new MVx.Observable.Property<HubConnectionState>(nameof(ConnectionState), args => PropertyChanged?.Invoke(this, args));
            _exchangeCode = new MVx.Observable.Property<string>(nameof(ExchangeCode), args => PropertyChanged?.Invoke(this, args));
            _authenticationException = new MVx.Observable.Property<Exception>(nameof(AuthenticationException), args => PropertyChanged?.Invoke(this, args));
            _tokens = new MVx.Observable.Property<ReceivedTokens>(nameof(Tokens), args => PropertyChanged?.Invoke(this, args));
            _exchangeException = new MVx.Observable.Property<Exception>(nameof(ExchangeException), args => PropertyChanged?.Invoke(this, args));
            _publishTokenException = new MVx.Observable.Property<Exception>(nameof(PublishTokenException), args => PropertyChanged?.Invoke(this, args));
            _refreshingTokenException = new MVx.Observable.Property<Exception>(nameof(RefreshingTokenException), args => PropertyChanged?.Invoke(this, args));

            _connection = new HubConnectionBuilder()
                .WithUrl(Secrets.AuthRelayHubUri)
                .WithAutomaticReconnect()
                .Build();
        }

        private IDisposable ShouldConnectToRelayWhenInConnectingToRelayState()
        {
            return _state
                .Where(state => state == State.ConnectingToRelay)
                .SelectMany(_ => Observable
                    .StartAsync(async () =>
                    {
                        await _connection.StartAsync();
                        return _connection.State;
                    }))
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_connectionState);
        }

        private IDisposable ShouldTransitionToCheckingForRefreshTokenWhenConnectionToRelayEstablished()
        {
            return _connectionState
                .Where(state => state == HubConnectionState.Connected)
                .Select(_ => State.CheckingForTokens)
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_state);
        }

        private IDisposable ShouldCheckForTokensWhenEnteringCheckingForRefreshTokenState()
        {
            var refreshToken = _state
                .Where(state => state == State.CheckingForTokens)
                .Select(_ => ApplicationData.Current.LocalSettings.Values.TryGetValue("Tokens", out object refreshToken) ? JsonConvert.DeserializeObject<ReceivedTokens>((string)refreshToken) : default)
                .Publish();

            var success = refreshToken
                .Where(token => token != null && !string.IsNullOrWhiteSpace(token.Tokens.RefreshToken))
                .Subscribe(_tokens);

            var failure = refreshToken
                .Where(token => token is null || string.IsNullOrWhiteSpace(token.Tokens.RefreshToken))
                .Select(_ => State.PendingAuthentication)
                .Subscribe(_state);

            return new CompositeDisposable(success, failure, refreshToken.Connect());
        }

        private async Task<string> Authenticate()
        {
            string startURL = $"https://accounts.spotify.com/authorize?client_id={Secrets.ClientId}&redirect_uri={RedirectUri}&scope=user-read-private%20user-read-email%20user-read-playback-state%20streaming&response_type=code&state={StateValue}";
            string endURL = "http://localhost:7791";

            System.Uri startURI = new System.Uri(startURL);
            System.Uri endURI = new System.Uri(endURL);

            var webAuthenticationResult =
                await Windows.Security.Authentication.Web.WebAuthenticationBroker.AuthenticateAsync(
                Windows.Security.Authentication.Web.WebAuthenticationOptions.None,
                startURI,
                endURI);

            return webAuthenticationResult.ResponseStatus switch
            {
                Windows.Security.Authentication.Web.WebAuthenticationStatus.Success when HasExchangeCode(webAuthenticationResult, out string code) => code,
                Windows.Security.Authentication.Web.WebAuthenticationStatus.ErrorHttp => throw new ApplicationException(webAuthenticationResult.ResponseErrorDetail.ToString()),
                _ => throw new ApplicationException(webAuthenticationResult.ResponseData.ToString())
            };
        }

        private bool HasExchangeCode(Windows.Security.Authentication.Web.WebAuthenticationResult webAuthenticationResult, out string code)
        {
            Uri uri = new Uri(webAuthenticationResult.ResponseData);
            var queryParams = uri.Query.TrimStart('?')
                .Split("&")
                .Select(part => part.Split("="))
                .Where(kvp => kvp.Length == 2)
                .ToDictionary(kvp => kvp[0], kvp => kvp[1]);

            return queryParams.TryGetValue("code", out code);
        }

        private IDisposable ShouldStartAuthenticationWhenEnteringPendingAuthenticationState()
        {
            var authentication = _state
                .Where(state => state == State.PendingAuthentication)
                .SelectMany(_ => Observable.StartAsync(Authenticate))
                .Materialize()
                .Publish();

            var success = authentication
                .Where(notification => notification.Kind == NotificationKind.OnNext && !string.IsNullOrWhiteSpace(notification.Value))
                .Select(notification => notification.Value)
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_exchangeCode);

            var failure = authentication
                .Where(notification => notification.Kind == NotificationKind.OnError)
                .Select(notification => notification.Exception)
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_authenticationException);

            return new CompositeDisposable(success, failure, authentication.Connect());
        }

        private IDisposable ShouldTransitionToCodeExchangeWhenExchangeCodeReceived()
        {
            return _exchangeCode
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(_ => State.CodeExchange)
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_state);
        }

        private async Task<Tokens> PerformCodeExchange(string code)
        {
            var formContent = new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", RedirectUri),
                    new KeyValuePair<string, string>("client_id", Secrets.ClientId),
                    new KeyValuePair<string, string>("client_secret", Secrets.ClientSecret)
                }
            );

            var response = await HttpClient.PostAsync("https://accounts.spotify.com/api/token", formContent);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Tokens>(json);
        }

        private IDisposable ShouldConductCodeExchangeWhenEnteringCodeExchangeState()
        {
            var tokens = _state
                .Where(state => state == State.CodeExchange)
                .WithLatestFrom(_exchangeCode, (_, code) => code)
                .SelectMany(code => Observable.StartAsync(() => PerformCodeExchange(code)))
                .Materialize()
                .Publish();

            var success = tokens
                .Where(notification => notification.Kind == NotificationKind.OnNext)
                .Select(notification => new ReceivedTokens { Tokens = notification.Value, ReceivedAt = DateTimeOffset.UtcNow })
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_tokens);

            var failure = tokens
                .Where(notification => notification.Kind == NotificationKind.OnError)
                .Select(notification => notification.Exception)
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_exchangeException);

            return new CompositeDisposable(success, failure, tokens.Connect());
        }

        private IDisposable ShouldTransitionToPublishingTokenWhenTokensRecieved()
        {
            return _tokens
                .Where(token => token != null)
                .Select(token => token.NeedsRefreshing ? State.RefreshingToken : State.PublishingToken)
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_state);
        }

        private IDisposable ShouldPublishTokensWhenEnteringPublishingTokenState()
        { 
            var publish = _state
                .Where(state => state == State.PublishingToken)
                .WithLatestFrom(_tokens, (_, tokens) => tokens)
                .SelectMany(tokens => Observable.StartAsync(() => _connection.InvokeAsync("TokenChanged", tokens.Tokens.AccessToken)))
                .Materialize()
                .Publish();

            var success = publish
                .Where(notification => notification.Kind == NotificationKind.OnNext)
                .Select(_ => State.TokenPublished)
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_state);

            var failure = publish
                .Where(notification => notification.Kind == NotificationKind.OnError)
                .Select(notification => notification.Exception)
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_publishTokenException);

            return new CompositeDisposable(success, failure, publish.Connect());
        }

        private IDisposable ShouldTransitionToRefreshingTokenAfterTokenExpiresInIntervalAfterEnteringTokenPublishedState()
        {
            return _state
                .Where(state => state == State.TokenPublished)
                .WithLatestFrom(_tokens, (_, tokens) => tokens)
                .SelectMany(tokens => Observable.Interval(tokens.ShouldRefreshIn).Take(1))
                .Select(_ => State.RefreshingToken)
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_state);
        }

        private async Task<Tokens> RefreshTokens(Tokens tokens)
        {

            var formContent = new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", tokens.RefreshToken),
                    new KeyValuePair<string, string>("client_id", Secrets.ClientId),
                    new KeyValuePair<string, string>("client_secret", Secrets.ClientSecret)
                }
            );

            var response = await HttpClient.PostAsync("https://accounts.spotify.com/api/token", formContent);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Tokens>(json);
        }

        private IDisposable ShouldRefreshTokensWhenEnteringRefreshTokenState()
        {
            var refreshing = _state
                .Where(state => state == State.RefreshingToken)
                .WithLatestFrom(_tokens, (_, tokens) => tokens)
                .SelectMany(tokens => Observable.StartAsync(() => RefreshTokens(tokens.Tokens)))
                .Materialize()
                .Publish();

            var success = refreshing
                .Where(notification => notification.Kind == NotificationKind.OnNext)
                .Select(notification => new ReceivedTokens { Tokens = notification.Value, ReceivedAt = DateTimeOffset.UtcNow })
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_tokens);

            var failure = refreshing
                .Where(notification => notification.Kind == NotificationKind.OnError)
                .Select(_ => State.PendingAuthentication)
                .ObserveOn(CoreDispatcherScheduler.Current)
                .Subscribe(_state);

            return new CompositeDisposable(success, failure, refreshing.Connect());
        }

        private IDisposable ShouldPersistTokensWhenNewTokensReceived()
        {
            return _tokens
                .Where(tokens => !(tokens?.NeedsRefreshing ?? true))
                .Select(tokens => JsonConvert.SerializeObject(tokens))
                .Subscribe(json => ApplicationData.Current.LocalSettings.Values["Tokens"] = json);
        }

        public IDisposable Activate()
        {
            return new CompositeDisposable(
                ShouldConnectToRelayWhenInConnectingToRelayState(),
                ShouldTransitionToCheckingForRefreshTokenWhenConnectionToRelayEstablished(),
                ShouldCheckForTokensWhenEnteringCheckingForRefreshTokenState(),
                ShouldStartAuthenticationWhenEnteringPendingAuthenticationState(),
                ShouldTransitionToCodeExchangeWhenExchangeCodeReceived(),
                ShouldConductCodeExchangeWhenEnteringCodeExchangeState(),
                ShouldTransitionToPublishingTokenWhenTokensRecieved(),
                ShouldPublishTokensWhenEnteringPublishingTokenState(),
                ShouldTransitionToRefreshingTokenAfterTokenExpiresInIntervalAfterEnteringTokenPublishedState(),
                ShouldRefreshTokensWhenEnteringRefreshTokenState(),
                ShouldPersistTokensWhenNewTokensReceived()
            );
        }

        public State State => _state.Get();

        public HubConnectionState ConnectionState => _connectionState.Get();

        public string ExchangeCode => _exchangeCode.Get();

        public Exception AuthenticationException => _authenticationException.Get();

        public ReceivedTokens Tokens => _tokens.Get();

        public Exception ExchangeException => _exchangeException.Get();

        public Exception PublishTokenException => _publishTokenException.Get();

        public Exception RefreshingTokenException => _refreshingTokenException.Get();
    }
}
