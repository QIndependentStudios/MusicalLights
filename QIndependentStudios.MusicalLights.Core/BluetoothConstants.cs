using System;
using System.Collections.Generic;

namespace QIndependentStudios.MusicalLights.Core
{
    public class BluetoothConstants
    {
        public const string DefaultSequenceName = "Default";

        public static readonly Guid ServiceUuid = Guid.Parse("317379A5-418E-432F-8444-C04126C72659");
        public static readonly Guid StatusCharacteristicUuid = Guid.Parse("6E7C1EED-9F1E-477A-9397-8FC0658E3E3E");
        public static readonly Guid CommandCharacteristicUuid = Guid.Parse("DAC1C967-3C15-4366-AEA3-D7091795F0F0");

        private static readonly Dictionary<int, string> _sequenceIdToName = new Dictionary<int, string>
        {
            { 0, DefaultSequenceName },
            { 1, "Wizards In Winter" }
        };

        public static string GetSequenceName(int id)
        {
            return _sequenceIdToName.TryGetValue(id, out var name)
                ? name
                : DefaultSequenceName;
        }
    }
}
