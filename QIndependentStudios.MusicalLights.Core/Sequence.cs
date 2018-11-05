using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QIndependentStudios.MusicalLights.Core
{
    public class Sequence
    {
        [JsonProperty(PropertyName = nameof(KeyFrames))]
        private List<KeyFrame> _keyFrames = new List<KeyFrame>();

        public Sequence(int version = 1)
        {
            Version = version;
        }

        public int Version { get; set; }

        [JsonIgnore]
        public IReadOnlyCollection<KeyFrame> KeyFrames => _keyFrames.AsReadOnly();

        public static Sequence FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Sequence>(json);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public void AddKeyFrame(KeyFrame keyFrame)
        {
            if (keyFrame == null)
                throw new ArgumentNullException(nameof(keyFrame));

            if (_keyFrames.Any(f => f.Time == keyFrame.Time))
                throw new ArgumentException($"A KeyFrame already exists with time {keyFrame.Time}.", nameof(keyFrame));

            _keyFrames.Add(keyFrame);
            _keyFrames = _keyFrames.OrderBy(f => f.Time).ToList();
        }

        public void AddKeyFrames(IEnumerable<KeyFrame> keyFrames)
        {
            if (keyFrames == null)
                throw new ArgumentNullException(nameof(keyFrames));

            foreach (var keyFrame in keyFrames)
            {
                if (_keyFrames.Any(f => f.Time == keyFrame.Time))
                    throw new ArgumentException($"A KeyFrame already exists with time {keyFrame.Time}.", nameof(keyFrame));

                _keyFrames.Add(keyFrame);
            }

            _keyFrames = _keyFrames.OrderBy(f => f.Time).ToList();
        }

        public void RemoveKeyFrame(KeyFrame keyFrame)
        {
            if (keyFrame == null)
                throw new ArgumentNullException(nameof(keyFrame));

            _keyFrames.Remove(keyFrame);
        }

        public void RemoveKeyFrame(TimeSpan time)
        {
            if (time == null)
                throw new ArgumentNullException(nameof(time));

            var match = _keyFrames.FirstOrDefault(f => f.Time == time);
            if (match == null)
                return;

            _keyFrames.Remove(match);
        }

        public void ClearKeyFrames()
        {
            _keyFrames.Clear();
        }
    }
}
