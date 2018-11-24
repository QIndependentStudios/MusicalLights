using System.Collections.Generic;
using System.Linq;

namespace QIndependentStudios.MusicalLights.Core
{
    public class InterpolationData
    {
        public InterpolationData(int lightId, IEnumerable<InterpolationFrame> interpolationFrames)
        {
            LightId = lightId;
            InterpolationFrame = interpolationFrames.OrderBy(x => x.Time).ToList().AsReadOnly();
        }

        public int LightId { get; }
        public IReadOnlyList<InterpolationFrame> InterpolationFrame { get; }

        public static IEnumerable<InterpolationFrame> Create(Sequence sequence)
        {
            var spans = new Dictionary<int, List<InterpolationSpan>>();
            var interpolationFrames = new List<InterpolationFrame>();

            foreach (var keyFrame in sequence.KeyFrames.OrderByDescending(x => x.Time))
            {
                var interpolationFrameData = new List<InterpolationSpan>();
                foreach (var lightDataKvp in keyFrame.LightValues)
                {
                    if (!spans.ContainsKey(lightDataKvp.Key))
                        spans.Add(lightDataKvp.Key, new List<InterpolationSpan>());

                    var lightKeyFrameData = spans[lightDataKvp.Key];
                    var nextSpan = lightKeyFrameData.FirstOrDefault();
                    var interpolationSpan = new InterpolationSpan(lightDataKvp.Key,
                        keyFrame.Time,
                        lightDataKvp.Value.Color,
                        lightDataKvp.Value.InterpolationMode,
                        nextSpan);
                    lightKeyFrameData.Insert(0, interpolationSpan);

                    interpolationFrameData.Add(interpolationSpan);
                }

                interpolationFrames.Add(new InterpolationFrame(keyFrame.Time, interpolationFrameData.ToDictionary(x => x.LightId, x => x)));
            }

            return interpolationFrames;
        }
    }
}
