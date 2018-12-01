using AdafruitClassLibrary;
using QIndependentStudios.MusicalLights.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace QIndependentStudios.MusicalLights.Uwp.IoT
{
    internal sealed class IotSequencePlayer : SequencePlayer, IDisposable
    {
        private readonly MediaPlayer _player = new MediaPlayer();

        private DotStar _dotStar;
        private bool _hasMedia;
        private double _brightness = BluetoothConstants.MediumBrightness;

        public double Brightness
        {
            get => _brightness;
            set
            {
                if (_brightness == value)
                    return;

                _renderMutex.WaitOne();
                _brightness = value;

                if (_dotStar != null)
                {
                    _dotStar.Brightness = GetDotStarBrightnessValue(_brightness);
                    _dotStar.Show();
                }

                _renderMutex.ReleaseMutex();
            }
        }

        internal async Task LoadAsync(Sequence sequence)
        {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            Stop();

            _hasMedia = !string.IsNullOrWhiteSpace(sequence.Audio);

            if (_hasMedia)
                _player.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Media/{sequence.Audio}"));
            else
                _player.Source = null;

            PrepareSequenceData(sequence);

            var lightValues = sequence.KeyFrames.SelectMany(f => f.LightValues).ToList();
            var maxNumberOfLights = lightValues.Any() ? lightValues.Max(p => p.Key) : 0;

            if (_dotStar == null)
            {
                _dotStar = new DotStar((uint)maxNumberOfLights, DotStar.DOTSTAR_BGR)
                {
                    Brightness = GetDotStarBrightnessValue(Brightness)
                };
                await _dotStar.BeginAsync();
            }
            else
                _dotStar.UpdateLength((uint)maxNumberOfLights);
        }

        public sealed override void Play()
        {
            base.Play();
            _player.Play();
        }

        public sealed override void Pause()
        {
            base.Pause();
            _player.Pause();
        }

        public sealed override void Stop()
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

        protected override void UpdateLightColor(IDictionary<int, Color> lightColors)
        {
            foreach (var lightValue in lightColors)
            {
                var color = lightValue.Value;
                _dotStar?.SetPixelColor(lightValue.Key - 1, color.R, color.G, color.B);
            }

            _dotStar?.Show();
        }

        private byte GetDotStarBrightnessValue(double brightness)
        {
            return (byte)(brightness * 256 - 1);
        }
    }
}
