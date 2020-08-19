using SpotifyApi.NetCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace Unopify.Home
{
    public class ViewModel : IViewModel, INotifyPropertyChanged
    {
        private readonly Spotify.IFacade _spotifyFacade;

        private readonly MVx.Observable.Property<CurrentTrackPlaybackContext> _currentTrackContext;
        private readonly MVx.Observable.Property<string> _imageUri;
        private readonly MVx.Observable.Property<string> _artistName;

        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModel(Spotify.IFacade spotifyFacade)
        {
            _spotifyFacade = spotifyFacade;

            _currentTrackContext = new MVx.Observable.Property<CurrentTrackPlaybackContext>(nameof(CurrentTrackContext), args => PropertyChanged?.Invoke(this, args));
            _imageUri = new MVx.Observable.Property<string>(nameof(ImageUri), args => PropertyChanged?.Invoke(this, args));
            _artistName = new MVx.Observable.Property<string>(nameof(ArtistName), args => PropertyChanged?.Invoke(this, args));
        }

        public IDisposable ShouldSubscrbeToCurrentContextOnActivation()
        {
            return _spotifyFacade.CurrentPlaybackContext
                .Do(context => System.Diagnostics.Debug.WriteLine($"Received {context}"))
                .OfType<CurrentTrackPlaybackContext>()
                .ObserveOn(Platform.Schedulers.Dispatcher)
                .Subscribe(_currentTrackContext);
        }

        public IDisposable ShouldUpdateImageUriWhenContextChanges()
        {
            return _currentTrackContext
                .Select(context => context?.Item?.Album?.Images?.Select(image => image.Url).FirstOrDefault())
                .DistinctUntilChanged()
                .ObserveOn(Platform.Schedulers.Dispatcher)
                .Subscribe(_imageUri);
        }

        public IDisposable Activate()
        {
            return new CompositeDisposable(
                ShouldSubscrbeToCurrentContextOnActivation(),
                ShouldUpdateImageUriWhenContextChanges()
            );
        }

        public CurrentTrackPlaybackContext CurrentTrackContext => _currentTrackContext.Get();

        public string ImageUri => _imageUri.Get();

        public string ArtistName => _artistName.Get();
    }
}
