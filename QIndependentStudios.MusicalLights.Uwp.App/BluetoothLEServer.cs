using QIndependentStudios.MusicalLights.Core;
using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace QIndependentStudios.MusicalLights.Uwp.App
{
    public class BluetoothLEServer
    {
        public event TypedEventHandler<BluetoothLEServer, CommandReceivedEventArgs> CommandReceived;

        private const int GattPresentationFormatExponent = 0;
        private const int GattPresentationFormatUnitless = 0x2700;
        private const int GattPresentationFormatNamespaceId = 1;
        private const int GattPresentationFormatDescription = 0;

        private GattServiceProvider _gattServiceProvider;
        private GattLocalCharacteristic _statusCharacteristic;

        static BluetoothLEServer()
        {
            Current = new BluetoothLEServer();
        }

        protected BluetoothLEServer()
        { }

        public static BluetoothLEServer Current;

        public DeviceStatus CurrentStatus { get; protected set; }

        public async Task StartAsync()
        {
            if (_gattServiceProvider == null)
            {
                var serviceProviderResult = await GattServiceProvider.CreateAsync(BluetoothConstants.ServiceUuid);
                if (serviceProviderResult.Error != BluetoothError.Success)
                    throw new InvalidOperationException($"Failed to create GATT service with error {serviceProviderResult.Error}");

                _gattServiceProvider = serviceProviderResult.ServiceProvider;

                await CreateCharacteristics(_gattServiceProvider);
            }

            var advertisingParameters = new GattServiceProviderAdvertisingParameters
            {
                IsConnectable = (await BluetoothAdapter.GetDefaultAsync())?.IsPeripheralRoleSupported ?? false,
                IsDiscoverable = true
            };
            _gattServiceProvider.StartAdvertising(advertisingParameters);
        }

        public void Stop()
        {
            _gattServiceProvider.StopAdvertising();
        }

        public async Task UpdateStatusAsync(SequencePlayerState state, string sequenceDescription)
        {
            CurrentStatus = new DeviceStatus(state, sequenceDescription);

            if (_statusCharacteristic != null)
                await _statusCharacteristic.NotifyValueAsync(GetStatusBuffer());
        }

        protected void OnCommandReceived(CommandCode commandCode, int? sequenceId = null)
        {
            CommandReceived?.Invoke(this, new CommandReceivedEventArgs(commandCode, sequenceId));
        }

        private async Task CreateCharacteristics(GattServiceProvider gattServiceProvider)
        {
            var statusCharacteristicParams = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify,
                WriteProtectionLevel = GattProtectionLevel.Plain,
                UserDescription = "Status Characteristic"
            };
            statusCharacteristicParams.PresentationFormats.Add(GattPresentationFormat.FromParts(GattPresentationFormatTypes.Utf8,
                GattPresentationFormatExponent,
                GattPresentationFormatUnitless,
                GattPresentationFormatNamespaceId,
                GattPresentationFormatDescription));

            var statusCharacteristicResult = await gattServiceProvider.Service.CreateCharacteristicAsync(
                BluetoothConstants.StatusCharacteristicUuid,
                statusCharacteristicParams);
            if (statusCharacteristicResult.Error != BluetoothError.Success)
                throw new InvalidOperationException($"Failed to create GATT status characteristic with error {statusCharacteristicResult.Error}");

            _statusCharacteristic = statusCharacteristicResult.Characteristic;

            var commandCharacteristicResult = await gattServiceProvider.Service.CreateCharacteristicAsync(
                BluetoothConstants.CommandCharacteristicUuid,
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Write
                        | GattCharacteristicProperties.WriteWithoutResponse,
                    WriteProtectionLevel = GattProtectionLevel.Plain,
                    UserDescription = "Command Characteristic"
                });
            if (commandCharacteristicResult.Error != BluetoothError.Success)
                throw new InvalidOperationException($"Failed to create GATT command characteristic with error {commandCharacteristicResult.Error}");

            _statusCharacteristic.ReadRequested += StatusCharacteristic_ReadRequested;
            commandCharacteristicResult.Characteristic.WriteRequested += CommandCharacteristic_WriteRequested;
        }

        private async void StatusCharacteristic_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();

            var request = await args.GetRequestAsync();
            if (request == null)
                return;

            request.RespondWithValue(GetStatusBuffer());

            deferral.Complete();
        }

        private IBuffer GetStatusBuffer()
        {
            var statusValue = $"{(int)(CurrentStatus?.PlayerState ?? SequencePlayerState.Unknown)},{CurrentStatus?.SequenceDescription ?? "Unknown"}";

            var writer = new DataWriter
            {
                ByteOrder = ByteOrder.LittleEndian,
                UnicodeEncoding = UnicodeEncoding.Utf8
            };
            writer.WriteString(statusValue);

            return writer.DetachBuffer();
        }

        private async void CommandCharacteristic_WriteRequested(GattLocalCharacteristic sender,
            GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();

            var request = await args.GetRequestAsync();
            if (request == null)
                return;

            if (request.Value.Length != 1 && request.Value.Length != 5)
            {
                if (request.Option == GattWriteOption.WriteWithResponse)
                    request.RespondWithProtocolError(GattProtocolError.InvalidAttributeValueLength);
                return;
            }

            var reader = DataReader.FromBuffer(request.Value);
            reader.ByteOrder = ByteOrder.LittleEndian;

            var commandCode = (CommandCode)reader.ReadByte();
            if (!Enum.IsDefined(typeof(CommandCode), commandCode))
            {
                if (request.Option == GattWriteOption.WriteWithResponse)
                    request.RespondWithProtocolError(GattProtocolError.InvalidPdu);
                return;
            }

            if (commandCode == CommandCode.Play && request.Value.Length == 5)
                OnCommandReceived(commandCode, reader.ReadInt32());
            else
                OnCommandReceived(commandCode);

            if (request.Option == GattWriteOption.WriteWithResponse)
                request.Respond();

            deferral.Complete();
        }
    }

    public class DeviceStatus
    {
        public DeviceStatus(SequencePlayerState playerState, string sequenceDescription)
        {
            PlayerState = playerState;
            SequenceDescription = sequenceDescription;
        }

        public SequencePlayerState PlayerState { get; }
        public string SequenceDescription { get; }
    }

    public class CommandReceivedEventArgs : EventArgs
    {
        public CommandReceivedEventArgs(CommandCode commandCode, int? sequenceId)
        {
            CommandCode = commandCode;
            SequenceId = sequenceId;
        }

        public CommandCode CommandCode { get; }
        public int? SequenceId { get; }
    }
}
