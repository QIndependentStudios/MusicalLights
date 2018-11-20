using QIndependentStudios.MusicalLights.Core;
using QIndependentStudios.MusicalLights.Uwp.App.SequencePlayback;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace QIndependentStudios.MusicalLights.Uwp.App
{
    public sealed partial class MainPage : Page
    {
        private const long Interval = TimeSpan.TicksPerMillisecond * 50;

        private readonly IotSequencePlayer _player = new IotSequencePlayer();

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            BluetoothLEServer.Current.CommandReceived += Current_CommandReceived;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            BluetoothLEServer.Current.CommandReceived -= Current_CommandReceived;
        }

        private async void Current_CommandReceived(BluetoothLEServer sender, CommandReceivedEventArgs args)
        {
            switch (args.CommandCode)
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
        }

        private async Task PlayAsync()
        {
            var sequenceFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///SequenceData/Wizards In Winter.json"));
            var sequence = Sequence.FromJson(await FileIO.ReadTextAsync(sequenceFile));

            await _player.LoadAsync(sequence);
            _player.Play();
        }
    }
}
