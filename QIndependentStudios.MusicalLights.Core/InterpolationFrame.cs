using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QIndependentStudios.MusicalLights.Core
{
    public class InterpolationFrame
    {
        public InterpolationFrame(TimeSpan time, IDictionary<int, InterpolationSpan> interpolationSpans)
        {
            Time = time;
            InterpolationSpans = new ReadOnlyDictionary<int, InterpolationSpan>(interpolationSpans);
        }

        public TimeSpan Time { get; }
        public ReadOnlyDictionary<int, InterpolationSpan> InterpolationSpans { get; }
    }
}
