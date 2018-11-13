using AdafruitClassLibrary;
using QIndependentStudios.MusicalLights.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace QIndependentStudios.MusicalLights.Uwp.App.SequencePlayback
{
    public class IotSequencePlayer : SequencePlayer, IDisposable
    {
        private readonly MediaPlayer _player = new MediaPlayer();
        private DotStar _dotStar;

        public async Task LoadAsync(IMediaPlaybackSource mediaSource, Sequence sequence)
        {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            _player.Source = mediaSource;
            _frames = sequence.KeyFrames?.OrderBy(f => f.Time).ToList() ?? new List<KeyFrame>();

            var maxNumberOfLights = _frames.SelectMany(f => f.LightValues).Max(p => p.Key);
            _dotStar = new DotStar((uint)maxNumberOfLights);
            await _dotStar.BeginAsync();
        }

        public override void Play()
        {
            Stop();
            base.Play();
            _player.Play();
        }

        public override void Stop()
        {
            base.Stop();
            _player.Pause();
            _player.PlaybackSession.Position = new TimeSpan();
            _dotStar?.End();
            _dotStar = null;
        }

        public void Dispose()
        {
            Stop();
            _player.Dispose();
        }

        protected override TimeSpan GetElapsedTime()
        {
            return _player.PlaybackSession.Position;
        }

        protected override void UpdateColor(KeyFrame keyFrame)
        {
            foreach (var lightValue in keyFrame.LightValues)
            {
                var color = lightValue.Value;
                _dotStar.SetPixelColor(lightValue.Key, color.R, color.G, color.B);
            }
            _dotStar.Show();
        }
    }
}
