using QIndependentStudios.MusicalLights.Core;
using QIndependentStudios.MusicalLights.Uwp.App.SequencePlayback;
using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace QIndependentStudios.MusicalLights.Uwp.App
{
    public sealed partial class MainPage : Page
    {
        private const long Interval = TimeSpan.TicksPerMillisecond * 50;

        private readonly IotSequencePlayer _player = new IotSequencePlayer();

        public MainPage()
        {
            InitializeComponent();
            StartBluetooth();
        }

        private async void StartBluetooth()
        {
            var serviceProviderResult = await GattServiceProvider.CreateAsync(BluetoothConstants.ServiceUuid);
            if (serviceProviderResult.Error != BluetoothError.Success)
                return;

            var serviceProvider = serviceProviderResult.ServiceProvider;
            var commandCharacteristicResult = await serviceProvider.Service.CreateCharacteristicAsync(BluetoothConstants.CommandCharacteristicUuid,
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = GattCharacteristicProperties.Write
                        | GattCharacteristicProperties.WriteWithoutResponse,
                    WriteProtectionLevel = GattProtectionLevel.Plain,
                    UserDescription = "Command Characteristic"
                });
            if (commandCharacteristicResult.Error != BluetoothError.Success)
                return;

            commandCharacteristicResult.Characteristic.WriteRequested += CommandCharacteristic_WriteRequested;

            var advertisingParameters = new GattServiceProviderAdvertisingParameters
            {
                IsConnectable = (await BluetoothAdapter.GetDefaultAsync())?.IsPeripheralRoleSupported ?? false,
                IsDiscoverable = true
            };

            serviceProvider.AdvertisementStatusChanged += ServiceProvider_AdvertisementStatusChanged; ;
            serviceProvider.StartAdvertising(advertisingParameters);
        }

        private void ServiceProvider_AdvertisementStatusChanged(GattServiceProvider sender, GattServiceProviderAdvertisementStatusChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(args.Status);
        }

        private async void CommandCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            using (args.GetDeferral())
            {
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

                var commandValue = reader.ReadByte();
                if (!Enum.IsDefined(typeof(CommandCode), commandValue))
                {
                    if (request.Option == GattWriteOption.WriteWithResponse)
                        request.RespondWithProtocolError(GattProtocolError.InvalidPdu);
                    return;
                }

                switch ((CommandCode)commandValue)
                {
                    case CommandCode.Play:
                        await PlayAsync();
                        break;
                    case CommandCode.Pause:
                        break;
                    case CommandCode.Stop:
                        _player.Stop();
                        break;
                    default:
                        break;
                }

                request.Respond();
            }
        }

        private async Task PlayAsync()
        {
            var sequenceFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///SequenceData/Wizards In Winter.json"));
            var sequence = Sequence.FromJson(await FileIO.ReadTextAsync(sequenceFile));

            await _player.LoadAsync(MediaSource.CreateFromUri(new Uri($"ms-appx:///Media/{sequence.Audio}")), sequence);
            _player.Play();
        }
    }
}
