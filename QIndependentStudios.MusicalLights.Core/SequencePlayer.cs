using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QIndependentStudios.MusicalLights.Core
{
    public abstract class SequencePlayer
    {
        protected Timer _timer;
        protected List<KeyFrame> _frames = new List<KeyFrame>();
        protected KeyFrame _currentFrame;

        public virtual void Play()
        {
            Stop();
            _timer = new Timer(TimerCallback, null, 0, 100);
        }

        public virtual void Stop()
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
        }

        protected abstract TimeSpan GetElapsedTime();
        protected abstract void UpdateColor(KeyFrame keyFrame);
    }
}
