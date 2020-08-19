using System;
using System.Reactive.Linq;

namespace Unopify.Application.State
{
    public class Launching : IState
    {
        public IObservable<ITransition> Enter()
        {
            return Observable.Return(new Transition.ToInitializing());
        }
    }
}
