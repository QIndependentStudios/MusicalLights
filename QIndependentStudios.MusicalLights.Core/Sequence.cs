using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QIndependentStudios.MusicalLights.Core
{
    public class Sequence
    {
        private List<KeyFrame> _keyFrames = new List<KeyFrame>();

        public Sequence(int version = 1) : this(new List<KeyFrame>(), null, version)
        { }

        [JsonConstructor]
        public Sequence(IEnumerable<KeyFrame> keyFrames, string audio = null, int version = 1)
        {
            if (keyFrames == null)
                throw new ArgumentNullException(nameof(keyFrames));

            Version = version;
            Audio = audio;

            foreach (var keyFrame in keyFrames)
            {
                if (keyFrame == null)
                    throw new ArgumentNullException(nameof(keyFrame));

                if (_keyFrames.Any(f => f.Time == keyFrame.Time))
                    throw new ArgumentException($"A KeyFrame already exists with time {keyFrame.Time}.", nameof(keyFrame));

                _keyFrames.Add(keyFrame);
            }
        }

        public int Version { get; }

        public string Audio { get; }

        public IReadOnlyCollection<KeyFrame> KeyFrames => _keyFrames.AsReadOnly();

        public static Sequence FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Sequence>(json);
        }

        public string ToJson()
        {
            _keyFrames = _keyFrames.OrderBy(f => f.Time).ToList();
            return JsonConvert.SerializeObject(this);
        }
    }
}
