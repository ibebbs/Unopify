using Windows.UI.Xaml;

namespace Unopify
{
    public interface IViewAware
    {
        FrameworkElement CurrentView { get; set; }
    }
}
