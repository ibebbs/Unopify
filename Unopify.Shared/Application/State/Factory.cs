using System;

namespace Unopify.Application.State
{
    public interface IFactory
    {
        IState Initializing();
        IState Launching();
        IState Resuming();
        IState Authenticating();
        IState Home();
        IState Suspending();
        IState Suspended();
    }

    public class Factory : IFactory
    {
        private readonly Navigation.IService _navigationService;
        private readonly Event.IBus _eventBus;
        private readonly Token.IService _tokenService;
        private readonly Spotify.IFacade _spotifyEndpoint;

        public Factory(Navigation.IService navigationService, Event.IBus eventBus, Token.IService tokenService, Spotify.IFacade spotifyEndpoint)
        {
            _navigationService = navigationService;
            _eventBus = eventBus;
            _tokenService = tokenService;
            _spotifyEndpoint = spotifyEndpoint;
        }

        public IState Initializing()
        {
            return new Initializing(_navigationService, _tokenService, _spotifyEndpoint);
        }

        public IState Launching()
        {
            return new Launching();
        }

        public IState Resuming()
        {
            return new Resuming();
        }

        public IState Authenticating()
        {
            return new Authenticating.State(_navigationService, _eventBus, _tokenService);
        }

        public IState Home()
        {
            return new Home.State(_navigationService, _eventBus);
        }

        public IState Suspending()
        {
            //return new Suspending();
            throw new NotImplementedException();
        }

        public IState Suspended()
        {
            //return new Suspended();
            throw new NotImplementedException();
        }
    }
}
