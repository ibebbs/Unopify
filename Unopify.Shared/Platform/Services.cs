using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Unopify.Platform
{
    public partial class Services
    {
        public static readonly Services Service = new Services();

        private readonly ServiceCollection _serviceCollection;
        private readonly Lazy<IServiceProvider> _serviceProvider;

        private Services()
        {
            _serviceCollection = new ServiceCollection();
            _serviceProvider = new Lazy<IServiceProvider>(() => _serviceCollection.BuildServiceProvider());
        }

        private void RegisterGlobalServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddHttpClient();

            services.AddSingleton<IViewLocator, ViewLocator>();

            services.AddSingleton<Event.IBus, Event.Bus>();
            services.AddSingleton<Navigation.IService, Navigation.Service>();
            services.AddSingleton<Application.State.IFactory, Application.State.Factory>();
            services.AddSingleton<Application.State.IMachine, Application.State.Machine>();

            services.AddSingleton<Token.IService, Token.Service>();
            services.AddSingleton<Spotify.IFacade, Spotify.Facade>();

            var viewModels = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => typeof(IViewModel).IsAssignableFrom(type) && !type.IsInterface);

            foreach (var viewModel in viewModels)
            {
                services.AddTransient(viewModel);
            }
        }

        partial void RegisterPlatformServices(IServiceCollection services);

        public void PerformRegistration()
        {
            if (_serviceProvider.IsValueCreated) throw new InvalidOperationException("You cannot register services after the service provider has been created");

            RegisterGlobalServices(_serviceCollection);
            RegisterPlatformServices(_serviceCollection);
        }

        public IServiceProvider Provider => _serviceProvider.Value;
    }
}
