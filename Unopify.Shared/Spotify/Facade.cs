using SpotifyApi.NetCore;
using System;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Unopify.Spotify
{
    public interface IFacade
    {
        IDisposable Activate();

        IObservable<CurrentPlaybackContext> CurrentPlaybackContext { get; }
    }

    public class Facade : IFacade
    {
        private readonly Token.IService _tokenService;
        private readonly Event.IBus _eventBus;
        private readonly IPlayerApi _playerApi;

        private readonly IConnectableObservable<CurrentPlaybackContext> _context;

        public Facade(Token.IService tokenService, Event.IBus eventBus, IHttpClientFactory httpClientFactory)
        {
            _tokenService = tokenService;
            _eventBus = eventBus;
            _playerApi = new PlayerApi(httpClientFactory.CreateClient());

            _context = CreateContextObservable();
        }

        private IConnectableObservable<CurrentPlaybackContext> CreateContextObservable()
        {
            return _tokenService.Token
                .Select(token => Observable
                    .Interval(TimeSpan.FromSeconds(1))
                    .SelectMany(_ => Observable.StartAsync(() => _playerApi.GetCurrentPlaybackInfo(accessToken: token)))
                    .Do(context => System.Diagnostics.Debug.WriteLine($"Received: {context}")))
                .Switch()
                .Replay(1);
        }

        private IDisposable ShouldCallPauseWhenPauseEventReceived()
        {
            return _eventBus
                .GetEvent<Application.Events.Pause>()
                .WithLatestFrom(_tokenService.Token, (_, token) => token)
                .Subscribe(token => _playerApi.Pause(accessToken: token));
        }

        private IDisposable ShouldCallPlayWhenPlayEventReceived()
        {
            return _eventBus
                .GetEvent<Application.Events.Play>()
                .WithLatestFrom(_tokenService.Token, (_, token) => token)
                .Subscribe(token => _playerApi.Play(accessToken: token));
        }

        public IDisposable Activate()
        {
            return new CompositeDisposable(
                ShouldCallPlayWhenPlayEventReceived(),
                ShouldCallPauseWhenPauseEventReceived(),
                _context.Connect()
            );
        }

        public IObservable<CurrentPlaybackContext> CurrentPlaybackContext => _context;
    }
}
