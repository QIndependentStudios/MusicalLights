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

        private static void Main(string[] args)
        {
            Console.WriteLine("Enter csv data file path:");
            var path = Console.ReadLine();

            Console.WriteLine("Reading csv data...");
            var text = File.ReadAllText(path);

            Console.WriteLine("Parsing data...");
            var data = ParseData(text);

            Console.WriteLine("Constructing sequence...");
            var sequence = ConvertToModel(data);

            Console.WriteLine("Writing sequence json data...");
            var outputPath = Path.ChangeExtension(path, ".json");
            File.WriteAllText($"{outputPath}", sequence.ToJson());

            Console.WriteLine($"Json data written to {outputPath}");
#if DEBUG
            Console.Write($"Press any key to continue . . .");
            Console.ReadKey();
#endif
        }

        private static Sequence ConvertToModel(IDictionary<(double, int), Color> data)
        {
            var frames = new List<KeyFrame>();
            foreach (var framePosition in data.Keys.Select(k => k.Item1).Distinct().OrderBy(x => x))
            {
                var lightValues = data.Where(p => p.Key.Item1 == framePosition)
                    .ToDictionary(x => x.Key.Item2, x => x.Value);
                var msPosition = framePosition / FramesPerSecond * MillisecondsPerSecond;
                frames.Add(new KeyFrame(TimeSpan.FromMilliseconds(msPosition), lightValues));
            }
            return new Sequence(frames);
        }

        private static IDictionary<(double, int), Color> ParseData(string text)
        {
            var sequenceData = new Dictionary<(double, int), Color>();

            foreach (var line in text.Split(Environment.NewLine))
            {
                var values = line.Split(',');
                if (values.Length >= 5
                    && double.TryParse(values[1], out var frame)
                    && int.TryParse(values[0], out var lightId)
                    && int.TryParse(values[2], out var r)
                    && int.TryParse(values[3], out var g)
                    && int.TryParse(values[4], out var b))
                    sequenceData[(frame, lightId)] = Color.FromArgb(r, g, b);
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
    }
}
