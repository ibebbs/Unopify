using System;
using System.Reactive.Linq;

namespace Unopify.Application.State
{
    public class Resuming : IState
    {
        public Resuming()
        {
        }

        public IObservable<ITransition> Enter()
        {
            return Observable.Return<ITransition>(new Transition.ToAuthenticating());
        }
    }
}
