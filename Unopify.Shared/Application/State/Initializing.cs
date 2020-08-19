using System;
using System.Reactive.Linq;

namespace Unopify.Application.State
{
    public partial class Initializing : IState
    {
        private readonly Navigation.IService _navigationService;
        private readonly Token.IService _tokenService;
        private readonly Spotify.IFacade _spotifyEndpoint;

        public Initializing(Navigation.IService navigationService, Token.IService tokenService, Spotify.IFacade spotifyEndpoint)
        {
            _navigationService = navigationService;
            _tokenService = tokenService;
            _spotifyEndpoint = spotifyEndpoint;
        }

        public IObservable<ITransition> Enter()
        {
            return Observable.Create<ITransition>(
                async observer =>
                {
                    await _navigationService.InitializeAsync();

                    _spotifyEndpoint.Activate();
                    _tokenService.Activate();


                    return Observable
                        .Return(new Transition.ToResuming())
                        .Subscribe(observer);
                }
            );
        }
    }
}
