using QIndependentStudios.MusicalLights.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace QIndependentStudios.MusicalLights.DataConverter
{
    internal class Program
    {
        private const int MillisecondsPerSecond = 1000;
        private const double FramesPerSecond = 39.467;

        private static readonly Random Rand = new Random();
        private static readonly TimeSpan GeneratedSeqeunceDuration = TimeSpan.FromMinutes(5);
        private static readonly Color WarmWhite = Color.FromArgb(255, 147, 41);

        private static readonly List<Color> _colors = new List<Color>
        {
            Color.FromArgb(255, 0, 0),
            Color.FromArgb(255, 78, 0),
            Color.FromArgb(255, 231, 0),
            Color.FromArgb(81, 255, 0),
            Color.FromArgb(0, 255, 140),
            Color.FromArgb(0, 140, 255),
            Color.FromArgb(81, 0, 255),
            Color.FromArgb(255, 0, 231),
            Color.FromArgb(255, 0, 78)
        };

        private static void Main(string[] args)
        {
            Console.WriteLine("Ready for command. Enter \"convert\" or \"gen\"");
            var command = Console.ReadLine().Trim();

            switch (command)
            {
                case "convert":
                case "c":
                    ConvertCsv();
                    break;
                case "gen":
                case "g":
                    Console.WriteLine("What type? Enter \"twinkle\" or \"rainbow\"");
                    var type = Console.ReadLine().Trim();
                    Generate(type, 27);
                    break;
                default:
                    break;
            }

#if DEBUG
            Console.Write($"Press any key to continue . . .");
            Console.ReadKey();
#endif
        }

        private static void ConvertCsv()
        {
            Console.WriteLine("Enter csv data file path:");
            var path = Console.ReadLine().Trim('"');

            Console.WriteLine("Reading csv data...");
            var text = File.ReadAllText(path);

            Console.WriteLine("Parsing data...");
            var data = ParseData(text);

            Console.WriteLine("Constructing sequence...");
            var sequence = ConvertToModel($"{Path.GetFileNameWithoutExtension(path)}.mp3", data);

            Console.WriteLine("Writing sequence json data...");
            var outputPath = Path.ChangeExtension(path, ".json");
            File.WriteAllText($"{outputPath}", sequence.ToJson());

            Console.WriteLine($"Json data written to {outputPath}");
        }

        private static void Generate(string type, int lightCount)
        {
            var sequenceData = new Dictionary<(TimeSpan, int), LightData>();
            var outputPath = "";
            switch (type)
            {
                case "twinkle":
                    sequenceData = GenerateTwinkle(lightCount);
                    outputPath = @"C:\Users\qngo\Documents\Twinkle.json";
                    break;
                case "rainbow":
                    sequenceData = GenerateRainbow(lightCount);
                    outputPath = @"C:\Users\qngo\Documents\Rainbow.json";
                    break;
                default:
                    break;
            }

            sequenceData.Add((TimeSpan.Zero, lightCount + 1), new LightData(InterpolationMode.None, WarmWhite));
            var sequence = ConvertToModel(null, sequenceData);

            Console.WriteLine("Writing sequence json data...");
            File.WriteAllText(outputPath, sequence.ToJson());

            Console.WriteLine($"Json data written to {outputPath}");
        }

        private static Dictionary<(TimeSpan, int), LightData> GenerateTwinkle(int lightCount)
        {
            Console.WriteLine("Generating twinkle sequence...");

            const double minSeparation = 0.33;
            const double maxSeparation = 1.5;
            var fadeInDuration = TimeSpan.FromSeconds(1);
            var fadeOutDuration = TimeSpan.FromSeconds(2);
            var sequenceData = new Dictionary<(TimeSpan Time, int LightId), LightData>();
            var recentLights = new List<int>();
            var recentColors = new List<int>();

            for (var i = 0; i < lightCount; i++)
            {
                sequenceData.Add((TimeSpan.Zero, i + 1), new LightData(InterpolationMode.None, Color.FromArgb(0, 0, 0)));
            }

            var nextTwinkleTime = TimeSpan.Zero;
            while (nextTwinkleTime < GeneratedSeqeunceDuration)
            {
                var twinkleCandidates = sequenceData
                    .GroupBy(x => x.Key.LightId)
                    .Select(x => (LightId: x.Key, EndTime: x.Max(y => y.Key.Time)))
                    .Where(x => x.EndTime < nextTwinkleTime)
                    .ToList();

                if (nextTwinkleTime != TimeSpan.Zero && !twinkleCandidates.Any())
                {
                    nextTwinkleTime += TimeSpan.FromSeconds(GetRandomDouble(minSeparation, maxSeparation));
                    continue;
                }

                var lightId = -1;
                while (lightId < 0 || recentLights.Any(x => x >= lightId - 2 && x <= lightId + 2))
                    lightId = !twinkleCandidates.Any()
                        ? Rand.Next(lightCount)
                        : twinkleCandidates[Rand.Next(twinkleCandidates.Count)].LightId;

                if (nextTwinkleTime == TimeSpan.Zero)
                    sequenceData.Remove((Time: TimeSpan.Zero, LightId: lightId));

                var colorIndex = -1;
                while (colorIndex < 0 || recentColors.Contains(colorIndex))
                    colorIndex = Rand.Next(_colors.Count);

                sequenceData.Add((nextTwinkleTime, lightId),
                    new LightData(InterpolationMode.Linear, Color.FromArgb(0, 0, 0)));
                sequenceData.Add((nextTwinkleTime + fadeInDuration, lightId),
                    new LightData(InterpolationMode.Linear, _colors[colorIndex]));
                sequenceData.Add((nextTwinkleTime + fadeInDuration + fadeOutDuration, lightId),
                    new LightData(InterpolationMode.None, Color.FromArgb(0, 0, 0)));

                if (recentLights.Contains(lightId))
                    recentLights.Remove(lightId);
                else if (recentLights.Count >= 5)
                    recentLights.RemoveAt(0);

                recentLights.Add(lightId);


                if (recentColors.Contains(colorIndex))
                    recentColors.Remove(colorIndex);
                else if (recentColors.Count >= 3)
                    recentColors.RemoveAt(0);

                recentColors.Add(colorIndex);

                nextTwinkleTime += TimeSpan.FromSeconds(GetRandomDouble(minSeparation, maxSeparation));
            }

            return sequenceData;
        }

        private static Dictionary<(TimeSpan, int), LightData> GenerateRainbow(int lightCount)
        {
            Console.WriteLine("Generating rainbow sequence...");

            const double brightnessModifier = 0.25;
            var transitionDuration = TimeSpan.FromSeconds(0.5);
            var sequenceData = new Dictionary<(TimeSpan Time, int LightId), LightData>();

            var time = TimeSpan.Zero;
            var offset = 0;
            while ((offset / lightCount) != 10)
            {
                for (var i = 0; i < lightCount; i++)
                {
                    var color = _colors[(i + offset) % _colors.Count];
                    color = Color.FromArgb(color.A,
                        (int)(color.R * brightnessModifier),
                        (int)(color.G * brightnessModifier),
                        (int)(color.B * brightnessModifier));
                    sequenceData.Add((Time: time, LightId: i + 1), new LightData(InterpolationMode.Linear, color));
                }

                time = time.Add(transitionDuration);
                offset++;
            }

            return sequenceData;
        }

        private static IDictionary<(double, int), LightData> ParseData(string text)
        {
            var sequenceData = new Dictionary<(double, int), LightData>();

            foreach (var line in text.Split(Environment.NewLine))
            {
                var values = line.Split(',');
                if (values.Length >= 5
                    && double.TryParse(values[1], out var frame)
                    && int.TryParse(values[0], out var lightId)
                    && int.TryParse(values[3], out var r)
                    && int.TryParse(values[4], out var g)
                    && int.TryParse(values[5], out var b)
                    && int.TryParse(values[6], out var interpolationMode))
                {
                    var color = Color.FromArgb(r, g, b);
                    if (color.ToArgb() == Color.White.ToArgb())
                        color = WarmWhite;

                    sequenceData[(frame, lightId)] = new LightData((InterpolationMode)interpolationMode, color);
                }
            }

            var frames = sequenceData.Keys.Select(k => k.Item1).Distinct().OrderBy(x => x);
            var previous = frames.First();
            foreach (var frame in frames.Skip(1))
            {
                var delta = frame - previous;
                if (delta < 0.5)
                    throw new ArgumentException($"{previous} and {frame} are too close together.");
                previous = frame;
            }

            return sequenceData;
        }

        private static Sequence ConvertToModel(string audio, IDictionary<(double, int), LightData> data)
        {
            var frames = new List<KeyFrame>();
            foreach (var framePosition in data.Keys.Select(k => k.Item1).Distinct().OrderBy(x => x))
            {
                var lightValues = data.Where(p => p.Key.Item1 == framePosition)
                    .ToDictionary(x => x.Key.Item2, x => x.Value);
                var msPosition = framePosition / FramesPerSecond * MillisecondsPerSecond;
                frames.Add(new KeyFrame(TimeSpan.FromMilliseconds(msPosition), lightValues));
            }
            return new Sequence(frames, audio, isLooped: false);
        }

        private static Sequence ConvertToModel(string audio, IDictionary<(TimeSpan, int), LightData> data)
        {
            var frames = new List<KeyFrame>();
            foreach (var framePosition in data.Keys.Select(k => k.Item1).Distinct().OrderBy(x => x))
            {
                var lightValues = data.Where(p => p.Key.Item1 == framePosition)
                    .ToDictionary(x => x.Key.Item2, x => x.Value);
                frames.Add(new KeyFrame(framePosition, lightValues));
            }
            return new Sequence(frames, audio, isLooped: true);
        }

        private static double GetRandomDouble(double min, double max)
        {
            return Rand.NextDouble() * (max - min) + min;
        }
    }
}
