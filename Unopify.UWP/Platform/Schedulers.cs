using System.Reactive.Concurrency;
using Windows.UI.Xaml;

namespace Unopify.Platform
{
    public static partial class Schedulers
    {
        static partial void OverrideDispatchScheduler(ref IScheduler scheduler)
        {
            scheduler = new CoreDispatcherScheduler(Window.Current.Dispatcher);
        }
    }
}
