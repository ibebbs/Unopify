using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace Unopify.Authenticating
{
    public class State : Application.IState
    {
        private readonly Navigation.IService _navigationService;
        private readonly Token.IService _tokenService;
        private readonly Event.IBus _eventBus;

        public State(Navigation.IService navigationService, Event.IBus eventBus, Token.IService tokenService)
        {
            _navigationService = navigationService;
            _tokenService = tokenService;
            _eventBus = eventBus;
        }

        public IObservable<Application.State.ITransition> Enter()
        {
            return Observable.Create<Application.State.ITransition>(
                async observer =>
                {
                    var activation = await _navigationService.NavigateToAsync<ViewModel>();

                    var transitions = _tokenService.Token
                        .Do(token => System.Diagnostics.Debug.WriteLine($"Authentication state recieved token: {token}"))
                        .Where(token => !string.IsNullOrEmpty(token))
                        .Select(_ => new Application.State.Transition.ToHome())
                        .Subscribe(observer);

                    return new CompositeDisposable(
                        activation,
                        transitions
                    );
                }
            );
        }
    }
}
