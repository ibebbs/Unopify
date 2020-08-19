using System;

namespace Unopify.Application
{
    public interface IState
    {
        IObservable<State.ITransition> Enter();
    }
}
