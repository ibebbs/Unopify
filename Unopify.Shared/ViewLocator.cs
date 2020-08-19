using System;
using System.Reflection;
using Windows.UI.Xaml;

namespace Unopify
{
    public interface IViewLocator
    {
        FrameworkElement ViewFor(object viewModel);
    }

    public class ViewLocator : IViewLocator
    {
        public FrameworkElement ViewFor(object viewModel)
        {
            var viewName = $"{viewModel.GetType().Namespace}.View";

            var viewType = Assembly.GetExecutingAssembly().GetType(viewName, true);

            var view =  Activator.CreateInstance(viewType) as FrameworkElement;

            return view;
        }
    }
}
