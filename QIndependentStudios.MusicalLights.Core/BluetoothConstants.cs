using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QIndependentStudios.MusicalLights.Core
{
    public class BluetoothConstants
    {
        public const string DefaultSequenceName = "Twinkle";
        public const double LowBrightness = 0.15;
        public const double MediumBrightness = 0.35;
        public const double HighBrightness = 0.7;

        // This is defined for Bluetooth and should never change.
        public static readonly Guid ClientCharacteristicConfigDescriptorUuid = Guid.Parse("00002902-0000-1000-8000-00805F9B34FB");

        public static readonly Guid ServiceUuid = Guid.Parse("317379A5-418E-432F-8444-C04126C72659");
        public static readonly Guid StatusCharacteristicUuid = Guid.Parse("6E7C1EED-9F1E-477A-9397-8FC0658E3E3E");
        public static readonly Guid CommandCharacteristicUuid = Guid.Parse("DAC1C967-3C15-4366-AEA3-D7091795F0F0");
        public static readonly Guid BrightnessCharacteristicUuid = Guid.Parse("793F69CD-B321-41FC-9608-A8D95FF0704A");

        public static readonly IReadOnlyDictionary<int, string> SequenceIdToName = new ReadOnlyDictionary<int, string>(new Dictionary<int, string>
        {
            { 0, DefaultSequenceName },
            { 1, "Rainbow" },
            { 2, "Wizards In Winter" }
        });

        public static string GetSequenceName(int id)
        {
            return SequenceIdToName.TryGetValue(id, out var name)
                ? name
                : DefaultSequenceName;
        }
    }
}
