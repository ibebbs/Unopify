using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Unopify.Navigation
{
    public interface IService
    {
        Task InitializeAsync();
        Task<IDisposable> NavigateToAsync(IViewModel viewModel);
        Task<IDisposable> NavigateToAsync<T>(Action<T> configure = default) where T : IViewModel;
    }

    public class Service : IService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewLocator _viewLocator;

        private Frame _rootFrame;
        private CoreDispatcher _dispatcher;

        public Service(IServiceProvider serviceProvider, IViewLocator viewLocator)
        {
            _serviceProvider = serviceProvider;
            _viewLocator = viewLocator;
        }

        public Task InitializeAsync()
        {
            _rootFrame = Windows.UI.Xaml.Window.Current.Content as Frame;
            _dispatcher = Windows.UI.Xaml.Window.Current.Dispatcher;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (_rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                _rootFrame = new Frame();

                // Place the frame in the current Window
                Windows.UI.Xaml.Window.Current.Content = _rootFrame;
            }

            // Ensure the current window is active
            Windows.UI.Xaml.Window.Current.Activate();

            return Task.CompletedTask;
        }

        public Task<IDisposable> NavigateToAsync(IViewModel viewModel)
        {
            var view = _viewLocator.ViewFor(viewModel);
            view.DataContext = viewModel;

            var activation = viewModel.Activate();

            if (viewModel is IViewAware viewAware)
            {
                viewAware.CurrentView = view;
            }

            _rootFrame.Content = view;

            return Task.FromResult(activation);
        }

        public Task<IDisposable> NavigateToAsync<T>(Action<T> configure = default)
            where T : IViewModel
        {
            var viewModel = _serviceProvider.GetService<T>();

            configure?.Invoke(viewModel);

            return NavigateToAsync(viewModel);
        }
    }
}
