using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace QIndependentStudios.MusicalLights.Core
{
    public class KeyFrame
    {
        public KeyFrame() : this(new TimeSpan())
        { }

        public KeyFrame(TimeSpan time) : this(time, new Dictionary<int, Color>())
        { }

        public KeyFrame(TimeSpan time, IDictionary<int, Color> lightValues)
        {
            Time = time;
            LightValues = lightValues.ToDictionary(x => x.Key, x => x.Value);
        }

        public TimeSpan Time { get; set; }
        public Dictionary<int, Color> LightValues { get; }

        public override string ToString()
        {
            var values = LightValues.Select(x => x.ToString())
                .DefaultIfEmpty("No light values")
                .Aggregate((x, y) => $"{x}, {y}");
            return $"{Time:G} - {values}";
        }
    }
}
