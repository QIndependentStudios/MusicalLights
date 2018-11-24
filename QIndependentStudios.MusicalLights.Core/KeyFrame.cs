using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QIndependentStudios.MusicalLights.Core
{
    public class KeyFrame
    {
        public KeyFrame() : this(new TimeSpan())
        { }

        public KeyFrame(TimeSpan time) : this(time, new Dictionary<int, LightData>())
        { }

        [JsonConstructor]
        public KeyFrame(TimeSpan time, IDictionary<int, LightData> lightValues)
        {
            Time = time;
            LightValues = new ReadOnlyDictionary<int, LightData>(lightValues);
        }

        public TimeSpan Time { get; }
        public ReadOnlyDictionary<int, LightData> LightValues { get; }

        public override string ToString()
        {
            var values = LightValues.Select(x => x.ToString())
                .DefaultIfEmpty("No light values")
                .Aggregate((x, y) => $"{x}, {y}");
            return $"{Time:G} - {values}";
        }
    }
}
