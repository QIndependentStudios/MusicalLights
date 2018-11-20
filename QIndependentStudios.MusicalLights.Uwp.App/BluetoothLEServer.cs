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

        private GattServiceProvider _gattServiceProvider;

        static BluetoothLEServer()
        {
            Current = new BluetoothLEServer();
        }

        protected BluetoothLEServer()
        { }

        public static BluetoothLEServer Current;

        public async Task StartAsync()
        {
            if (_gattServiceProvider == null)
            {
                var serviceProviderResult = await GattServiceProvider.CreateAsync(BluetoothConstants.ServiceUuid);
                if (serviceProviderResult.Error != BluetoothError.Success)
                    throw new InvalidOperationException($"Failed to create GATT service with error {serviceProviderResult.Error}");

                _gattServiceProvider = serviceProviderResult.ServiceProvider;
                _gattServiceProvider.AdvertisementStatusChanged += ServiceProvider_AdvertisementStatusChanged;

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

        protected void OnCommandReceived(CommandCode commandCode)
        {
            CommandReceived?.Invoke(this, new CommandReceivedEventArgs(commandCode));
        }

        private async Task CreateCharacteristics(GattServiceProvider gattServiceProvider)
        {
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

            commandCharacteristicResult.Characteristic.WriteRequested += CommandCharacteristic_WriteRequested;
        }

        private void ServiceProvider_AdvertisementStatusChanged(GattServiceProvider sender,
            GattServiceProviderAdvertisementStatusChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(args.Status);
        }

        private async void CommandCharacteristic_WriteRequested(GattLocalCharacteristic sender,
            GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();

            var request = await args.GetRequestAsync();
            if (request == null)
                return;

            if (request.Value.Length != 1)
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

            OnCommandReceived(commandCode);

            if (request.Option == GattWriteOption.WriteWithResponse)
                request.Respond();

            deferral.Complete();
        }
    }

    public class CommandReceivedEventArgs : EventArgs
    {
        public CommandReceivedEventArgs(CommandCode commandCode)
        {
            CommandCode = commandCode;
        }

        public CommandCode CommandCode { get; }
    }
}
