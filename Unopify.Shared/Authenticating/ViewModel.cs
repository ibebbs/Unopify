using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;

namespace Unopify.Authenticating
{
    public class ViewModel : IViewModel
    {
        public IDisposable Activate()
        {
            return Disposable.Empty;
        }
    }
}
