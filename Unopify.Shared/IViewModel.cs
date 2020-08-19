using System;

namespace Unopify
{
    public interface IViewModel
    {
        IDisposable Activate();
    }
}
