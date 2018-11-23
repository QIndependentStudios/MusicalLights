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
        private string _sequenceDescription = "No sequence loaded";

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            BluetoothLEServer.Current.CommandReceived += Current_CommandReceived;
            _player.StateChanged += Player_StateChanged;
            await PlayAsync(BluetoothConstants.DefaultSequenceName);
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
                    if (args.SequenceId.HasValue)
                        await PlayAsync(BluetoothConstants.GetSequenceName(args.SequenceId.Value));
                    else
                        _player.Play();
                    break;
                case CommandCode.Pause:
                    _player.Pause();
                    break;
                case CommandCode.Stop:
                    _player.Stop();
                    break;
                default:
                    break;
            }
        }

        private void Player_StateChanged(object sender, EventArgs e)
        {
            BluetoothLEServer.Current.UpdateStatus(_player.State, _sequenceDescription);
        }

        private async Task PlayAsync(string sequenceName)
        {
            var sequenceFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///SequenceData/{sequenceName}.json"));
            var sequence = Sequence.FromJson(await FileIO.ReadTextAsync(sequenceFile));

            await _player.LoadAsync(sequence);
            _sequenceDescription = sequenceName;
            _player.Play();
        }
    }
}
