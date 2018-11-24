using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace QIndependentStudios.MusicalLights.Core
{
    public abstract class SequencePlayer
    {
        public event EventHandler SequenceCompleted;
        public event EventHandler StateChanged;

        private const int TimerCallbackInterval = 20;
        protected readonly Dictionary<int, Color> _colors = new Dictionary<int, Color>();

        protected Timer _timer;
        protected DateTime? _startTime;
        protected DateTime? _pauseTime;
        protected List<InterpolationFrame> _frames = new List<InterpolationFrame>();
        protected InterpolationFrame _currentFrame;
        protected InterpolationFrame _lastFrame;
        protected Dictionary<int, InterpolationSpan> _inProgressInterpolations;

        private SequencePlayerState _state = SequencePlayerState.Stopped;

        public SequencePlayerState State
        {
            get => _state;
            protected set
            {
                if (value != _state)
                {
                    _state = value;
                    OnStateChanged();
                }
            }
        }

        public bool IsSequenceLooped { get; protected set; }

        public virtual void Play()
        {
            StopTimer();

            if (_startTime.HasValue && _pauseTime.HasValue)
                _startTime = _startTime.Value.AddTicks((DateTime.Now - _pauseTime.Value).Ticks);
            else
                _startTime = DateTime.Now;

            _pauseTime = null;
            _timer = new Timer(TimerCallback, null, 0, TimerCallbackInterval);
            State = SequencePlayerState.Playing;
        }

        public virtual void Pause()
        {
            _pauseTime = DateTime.Now;
            StopTimer();
            State = SequencePlayerState.Paused;
        }

        public virtual void Stop()
        {
            StopTimer();
            _startTime = null;
            _pauseTime = null;
            State = SequencePlayerState.Stopped;
        }

        protected void PrepareSequenceData(Sequence sequence)
        {
            IsSequenceLooped = sequence.IsLooped;
            _frames = InterpolationData.Create(sequence)?.OrderBy(f => f.Time).ToList() ?? new List<InterpolationFrame>();
            _lastFrame = _frames.LastOrDefault();
            _inProgressInterpolations = new Dictionary<int, InterpolationSpan>();
        }

        protected void StopTimer()
        {
            _timer?.Dispose();
            _timer = null;
        }

        protected virtual void TimerCallback(object state)
        {
            var elapsed = GetElapsedTime();

            foreach (var item in _inProgressInterpolations.ToList())
            {
                var progress = (elapsed - item.Value.Time).TotalMilliseconds / item.Value.Duration.TotalMilliseconds;
                if (progress >= 1)
                {
                    _inProgressInterpolations.Remove(item.Key);
                    continue;
                }

                var r = (int)Lerp(item.Value.Color.R, item.Value.NextSpan.Color.R, progress);
                var g = (int)Lerp(item.Value.Color.G, item.Value.NextSpan.Color.G, progress);
                var b = (int)Lerp(item.Value.Color.B, item.Value.NextSpan.Color.B, progress);
                _colors[item.Value.LightId] = Color.FromArgb(r, g, b);
            }

            var frame = _frames.LastOrDefault(f => f.Time <= elapsed);
            if (frame != null && _currentFrame != frame)
            {
                _currentFrame = frame;

                foreach (var interpolationSpanKvp in _currentFrame.InterpolationSpans)
                {
                    _colors[interpolationSpanKvp.Key] = interpolationSpanKvp.Value.Color;

                    if (interpolationSpanKvp.Value.CanInterpolate)
                        _inProgressInterpolations[interpolationSpanKvp.Key] = interpolationSpanKvp.Value;
                }
            }

            UpdateLightColor(_colors);

            if (_lastFrame != null && _currentFrame == _lastFrame)
                OnSequenceCompleted();
        }

        protected virtual TimeSpan GetElapsedTime()
        {
            return _startTime.HasValue
                ? DateTime.Now - _startTime.Value
                : new TimeSpan();
        }

        protected abstract void UpdateLightColor(IDictionary<int, Color> lightColors);

        protected virtual void OnSequenceCompleted()
        {
            SequenceCompleted?.Invoke(this, new EventArgs());
            Stop();

            if (IsSequenceLooped)
                Play();
        }

        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this, new EventArgs());
        }

        private static double Lerp(double startValue, double endValue, double progress)
        {
            return startValue + ((endValue - startValue) * progress);
        }
    }
}
