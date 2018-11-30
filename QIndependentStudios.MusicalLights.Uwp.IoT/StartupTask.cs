using QIndependentStudios.MusicalLights.Core;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace QIndependentStudios.MusicalLights.Uwp.IoT
{
    public sealed class StartupTask : IBackgroundTask
    {
        private const long Interval = TimeSpan.TicksPerMillisecond * 50;

        private readonly IotSequencePlayer _player = new IotSequencePlayer();

        private string _sequenceDescription = "No sequence loaded";
        private BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            await BluetoothLEServer.Current.StartAsync();

            BluetoothLEServer.Current.CommandReceived += BluetoothLEServer_CommandReceived;
            _player.StateChanged += Player_StateChanged;
            _player.SequenceCompleted += Player_SequenceCompleted;

            await PlayAsync(BluetoothConstants.DefaultSequenceName);
        }

        private async Task PlayAsync(string sequenceName)
        {
            _player.Stop();

            var sequenceFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///SequenceData/{sequenceName}.json"));
            var sequence = Sequence.FromJson(await FileIO.ReadTextAsync(sequenceFile));

            await _player.LoadAsync(sequence);
            _sequenceDescription = sequenceName;
            _player.Play();
        }

        private async void BluetoothLEServer_CommandReceived(BluetoothLEServer sender, CommandReceivedEventArgs args)
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

        private async void Player_StateChanged(object sender, EventArgs e)
        {
            await BluetoothLEServer.Current.UpdateStatusAsync(_player.State, _sequenceDescription);
        }

        private async void Player_SequenceCompleted(object sender, EventArgs e)
        {
            if (!_player.IsSequenceLooped)
                await PlayAsync(BluetoothConstants.DefaultSequenceName);
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            BluetoothLEServer.Current.Stop();
            _player.Stop();
        }
    }
}
