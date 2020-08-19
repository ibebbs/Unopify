using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Unopify.Token
{
    public interface IService
    {
        IDisposable Activate();

        IObservable<string> Token { get; }
    }

    public class Service : IService
    {
        private readonly IConnectableObservable<string> _token;
        private readonly HubConnection _connection;

        public Service()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://unopifyauthrelay20200819113428.azurewebsites.net/Hub")
                //.WithUrl("http://localhost:5000/Hub")
                .WithAutomaticReconnect()
                .Build();

            _token = CreateTokenObservable();
        }

        private IConnectableObservable<string> CreateTokenObservable()
        {
            return Observable
                .Create<string>(
                    async observer =>
                    {
                        await _connection.StartAsync();

                        Action<string> onReceiveToken = token => observer.OnNext(token);

                        var subscription = _connection.On("Token", onReceiveToken);

                        await _connection.SendAsync("RequestToken");

                        return new CompositeDisposable(
                            subscription,
                            Disposable.Create(() => _ = _connection.StopAsync())
                        );
                    })
                .Do(token => System.Diagnostics.Debug.WriteLine($"Received token: '{token}'"))
                .Replay(1);
        }

        public IDisposable Activate()
        {
            return _token.Connect();
        }

        public IObservable<string> Token => _token;
    }
}
