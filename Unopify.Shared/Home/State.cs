using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Unopify.Application;

namespace Unopify.Home
{
    public class State : IState
    {
        private readonly Navigation.IService _navigationService;
        private readonly Event.IBus _eventBus;
        public State(Navigation.IService navigationService, Event.IBus eventBus)
        {
            _navigationService = navigationService;
            _eventBus = eventBus;
        }

        public IObservable<Application.State.ITransition> Enter()
        {
            return Observable.Create<Application.State.ITransition>(
                async observer =>
                {
                    var activation = await _navigationService.NavigateToAsync<ViewModel>();

                    var transitions = Observable
                        .Never<Application.State.ITransition>()
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
