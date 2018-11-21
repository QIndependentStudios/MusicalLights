using AdafruitClassLibrary;
using QIndependentStudios.MusicalLights.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace QIndependentStudios.MusicalLights.Uwp.App.SequencePlayback
{
    public class IotSequencePlayer : SequencePlayer, IDisposable
    {
        private const double Brightness = 0.125;
        private readonly MediaPlayer _player = new MediaPlayer();
        private DotStar _dotStar;
        private bool _hasMedia;

        public async Task LoadAsync(Sequence sequence)
        {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            _hasMedia = !string.IsNullOrWhiteSpace(sequence.Audio);

            if (_hasMedia)
                _player.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Media/{sequence.Audio}"));

            _frames = sequence.KeyFrames?.OrderBy(f => f.Time).ToList() ?? new List<KeyFrame>();
            _lastFrame = _frames.LastOrDefault();

            var lightValues = _frames.SelectMany(f => f.LightValues).ToList();
            var maxNumberOfLights = lightValues.Any() ? lightValues.Max(p => p.Key) : 0;

            if (_dotStar == null)
            {
                _dotStar = new DotStar((uint)maxNumberOfLights, DotStar.DOTSTAR_BGR)
                {
                    Brightness = (int)(Brightness * 256) - 1
                };
                await _dotStar.BeginAsync();
            }
            else
                _dotStar.UpdateLength((uint)maxNumberOfLights);
        }

        public override void Play()
        {
            base.Play();
            _player.Play();
        }

        public override void Pause()
        {
            base.Pause();
            _player.Pause();
        }

        public override void Stop()
        {
            base.Stop();
            _player.Pause();
            _player.PlaybackSession.Position = new TimeSpan();
            _dotStar?.Clear();
            _dotStar?.Show();
        }

        public void Dispose()
        {
            Stop();
            _player.Dispose();
            _dotStar?.Clear();
            _dotStar?.Show();
            _dotStar?.End();
            _dotStar = null;
        }

        protected override TimeSpan GetElapsedTime()
        {
            return _hasMedia ? _player.PlaybackSession.Position : base.GetElapsedTime();
        }

        protected override void UpdateColor(KeyFrame keyFrame)
        {
            foreach (var lightValue in keyFrame.LightValues)
            {
                var color = lightValue.Value;
                _dotStar?.SetPixelColor(lightValue.Key - 1, color.R, color.G, color.B);
            }
            _dotStar?.Show();
        }
    }
}
