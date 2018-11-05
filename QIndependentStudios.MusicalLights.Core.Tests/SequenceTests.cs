using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QIndependentStudios.MusicalLights.Core.Tests
{
    [TestClass]
    public class SequenceTests
    {
        [TestMethod]
        public void Test()
        {
            var sequence = Sequence.FromJson("{\"Version\":1,\"KeyFrames\":[{\"Time\":\"00:00:00\",\"LightValues\":{\"1\":\"255, 255, 255\",\"2\":\"254, 254, 254\",\"3\":\"253, 253, 253\"}},{\"Time\":\"00:00:00.0584000\",\"LightValues\":{\"1\":\"255, 255, 255\",\"2\":\"255, 255, 255\",\"3\":\"255, 255, 255\"}},{\"Time\":\"00:00:01\",\"LightValues\":{\"1\":\"255, 255, 255\",\"2\":\"255, 255, 255\",\"3\":\"255, 255, 255\"}}]}");
            var json = sequence.ToJson();
        }
    }
}
