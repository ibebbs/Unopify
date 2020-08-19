using SpotifyApi.NetCore;

namespace Unopify.Application.Events
{
    public class PlaybackStateChanged
    {
        public PlaybackStateChanged(CurrentPlaybackContext context)
        {
            Context = context;
        }

        public CurrentPlaybackContext Context { get; }
    }
}
