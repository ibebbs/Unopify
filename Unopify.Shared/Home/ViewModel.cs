﻿using SpotifyApi.NetCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;

namespace Unopify.Home
{
    public class ViewModel : IViewModel, INotifyPropertyChanged
    {
        private readonly Spotify.IFacade _spotifyFacade;
        private readonly Event.IBus _eventBus;

        private readonly MVx.Observable.Property<CurrentTrackPlaybackContext> _currentTrackContext;
        private readonly MVx.Observable.Property<string> _imageUri;
        private readonly MVx.Observable.Property<string> _artistName;
        private readonly MVx.Observable.Command _playPauseCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModel(Spotify.IFacade spotifyFacade, Event.IBus eventBus)
        {
            _spotifyFacade = spotifyFacade;
            _eventBus = eventBus;

            _currentTrackContext = new MVx.Observable.Property<CurrentTrackPlaybackContext>(nameof(CurrentTrackContext), args => PropertyChanged?.Invoke(this, args));
            _imageUri = new MVx.Observable.Property<string>(nameof(ImageUri), args => PropertyChanged?.Invoke(this, args));
            _artistName = new MVx.Observable.Property<string>(nameof(ArtistName), args => PropertyChanged?.Invoke(this, args));
            _playPauseCommand = new MVx.Observable.Command(true);
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

        private IDisposable ShouldRefreshArtistNameWhenContextChanges()
        {
            return _currentTrackContext
                .Select(context => string.Join(",", context?.Item?.Artists?.Select(artist => artist.Name) ?? Enumerable.Empty<string>()))
                .DistinctUntilChanged()
                .ObserveOn(Platform.Schedulers.Dispatcher)
                .Subscribe(_artistName);
        }

        private IDisposable ShouldPlayOrPauseWhenThePlayPauseCommandIsInvoked()
        {
            return _playPauseCommand
                .WithLatestFrom(_currentTrackContext, (_, context) => context)
                .Select(context => context.IsPlaying ? (Application.IEvent) new Application.Events.Pause() : (Application.IEvent) new Application.Events.Play())
                .Subscribe(_eventBus.Publish);
        }

        public IDisposable Activate()
        {
            return new CompositeDisposable(
                ShouldSubscrbeToCurrentContextOnActivation(),
                ShouldUpdateImageUriWhenContextChanges(),
                ShouldRefreshArtistNameWhenContextChanges(),
                ShouldPlayOrPauseWhenThePlayPauseCommandIsInvoked()
            );
        }

        public CurrentTrackPlaybackContext CurrentTrackContext => _currentTrackContext.Get();

        public string ImageUri => _imageUri.Get();

        public string ArtistName => _artistName.Get();

        public ICommand PlayPauseCommand => _playPauseCommand;
    }
}
