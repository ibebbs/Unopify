using SpotifyApi.NetCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace Unopify.Home
{
    public class ViewModel : IViewModel, INotifyPropertyChanged
    {
        private readonly Spotify.IFacade _spotifyFacade;

        private readonly MVx.Observable.Property<CurrentTrackPlaybackContext> _currentTrackContext;

        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModel(Spotify.IFacade spotifyFacade)
        {
            _spotifyFacade = spotifyFacade;

            _currentTrackContext = new MVx.Observable.Property<CurrentTrackPlaybackContext>(nameof(CurrentTrackContext), args => PropertyChanged?.Invoke(this, args));
        }

        public IDisposable ShouldSubscrbeToCurrentContextOnActivation()
        {
            return _spotifyFacade.CurrentPlaybackContext
                .Do(context => System.Diagnostics.Debug.WriteLine($"Received {context}"))
                .OfType<CurrentTrackPlaybackContext>()
                .ObserveOn(Platform.Schedulers.Dispatcher)
                .Subscribe(_currentTrackContext);
        }

        public IDisposable Activate()
        {
            return new CompositeDisposable(
                ShouldSubscrbeToCurrentContextOnActivation()
            );
        }

        public CurrentTrackPlaybackContext CurrentTrackContext => _currentTrackContext.Get();
    }
}
