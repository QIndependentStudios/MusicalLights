using System;
using System.Drawing;

namespace QIndependentStudios.MusicalLights.Core
{
    public class InterpolationSpan
    {
        public InterpolationSpan(int lightId,
            TimeSpan time,
            Color color,
            InterpolationMode interpolationMode,
            InterpolationSpan nextKeyFrameData)
        {
            LightId = lightId;
            Time = time;
            Color = color;
            InterpolationMode = interpolationMode;
            NextSpan = nextKeyFrameData;
        }

        public int LightId { get; }
        public TimeSpan Time { get; }
        public Color Color { get; }
        public InterpolationMode InterpolationMode { get; }
        public InterpolationSpan NextSpan { get; }

        public TimeSpan Duration
        {
            get
            {
                if (NextSpan == null)
                    return new TimeSpan();

                return NextSpan.Time - Time;
            }
        }

        public bool CanInterpolate
        {
            get
            {
                if (NextSpan == null)
                    return false;

                return InterpolationMode != InterpolationMode.None && NextSpan.Color.ToArgb() != Color.ToArgb();
            }
        }
    }
}
