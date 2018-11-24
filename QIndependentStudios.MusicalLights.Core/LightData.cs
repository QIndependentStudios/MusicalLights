using System.Drawing;

namespace QIndependentStudios.MusicalLights.Core
{
    public class LightData
    {
        public LightData(InterpolationMode interpolationMode, Color color)
        {
            InterpolationMode = interpolationMode;
            Color = color;
        }

        public InterpolationMode InterpolationMode { get; }
        public Color Color { get; }
    }
}
