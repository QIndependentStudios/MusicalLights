using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QIndependentStudios.MusicalLights.Core
{
    public abstract class SequencePlayer
    {
        public event EventHandler SequenceCompleted;
        public event EventHandler StateChanged;

        private const int TimerCallbackInterval = 10;
        protected Timer _timer;
        protected DateTime? _startTime;
        protected DateTime? _pauseTime;
        protected List<KeyFrame> _frames = new List<KeyFrame>();
        protected KeyFrame _currentFrame;
        protected KeyFrame _lastFrame;
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

        protected void StopTimer()
        {
            _timer?.Dispose();
            _timer = null;
        }

        protected virtual void TimerCallback(object state)
        {
            var elapsed = GetElapsedTime();

            var frame = _frames.LastOrDefault(f => f.Time <= elapsed);
            if (_currentFrame == frame)
                return;

            _currentFrame = frame;

            if (_currentFrame != null)
                UpdateColor(_currentFrame);

            if (_lastFrame != null && _currentFrame == _lastFrame)
                OnSequenceCompleted();
        }

        protected virtual TimeSpan GetElapsedTime()
        {
            return _startTime.HasValue
                ? DateTime.Now - _startTime.Value
                : new TimeSpan();
        }

        protected abstract void UpdateColor(KeyFrame keyFrame);

        protected virtual void OnSequenceCompleted()
        {
            Stop();
            SequenceCompleted?.Invoke(this, new EventArgs());
        }

        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this, new EventArgs());
        }
    }
}
