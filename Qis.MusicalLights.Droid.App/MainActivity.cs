using Android;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using QIndependentStudios.MusicalLights.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Qis.MusicalLights.Droid.App
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const int PermissionsRequestCode = 41484;
        private const int EnableBluetoothRequestCode = 17116;

        private BluetoothLEService _bluetoothLEService;
        private Button _defaultButton;
        private Button _retryConnectButton;
        private Button _rainbowButton;
        private Button _wiwButton;
        private FloatingActionButton _playButton;
        private FloatingActionButton _pauseButton;
        private FrameLayout _playPauseLayout;
        private TextView _statusTextView;
        private ProgressBar _progressBar;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            BluetoothLEScanner.Current.DeviceDiscovered -= BluetoothLEScanner_DeviceDiscovered;
            BluetoothLEScanner.Current.DeviceDiscovered += BluetoothLEScanner_DeviceDiscovered;
            BluetoothLEScanner.Current.StateChanged -= BluetoothLEScanner_StateChanged;
            BluetoothLEScanner.Current.StateChanged += BluetoothLEScanner_StateChanged;

            _defaultButton = FindViewById<Button>(Resource.Id.defaultButton);
            _defaultButton.Click += DefaultButton_Click;

            _rainbowButton = FindViewById<Button>(Resource.Id.rainbowButton);
            _rainbowButton.Click += RainbowButton_Click;

            _wiwButton = FindViewById<Button>(Resource.Id.wiwButton);
            _wiwButton.Click += WiwButton_Click;

            _retryConnectButton = FindViewById<Button>(Resource.Id.retryConnectButton);
            _retryConnectButton.Click += RetryConnectButton_Click;

            _playButton = FindViewById<FloatingActionButton>(Resource.Id.playButton);
            _playButton.Click += PlayButton_Click;

            _pauseButton = FindViewById<FloatingActionButton>(Resource.Id.pauseButton);
            _pauseButton.Click += PauseButton_Click;

            _playPauseLayout = FindViewById<FrameLayout>(Resource.Id.playPauseButtonLayout);

            _statusTextView = FindViewById<TextView>(Resource.Id.statusTextView);

            _progressBar = FindViewById<ProgressBar>(Resource.Id.progressbar);

            Init();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _bluetoothLEService?.UnsubscribeCharacteristicNotification(BluetoothConstants.ServiceUuid,
                BluetoothConstants.StatusCharacteristicUuid);
            _bluetoothLEService?.Disconnect();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            menu.GetItem(0).SetEnabled(_bluetoothLEService != null);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.action_stop)
            {
                SendCommandAsync(CommandCode.Stop).RunSynchronously();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            for (var i = 0; i < grantResults.Length; i++)
            {
                if (grantResults[i] != Permission.Granted)
                {
                    _retryConnectButton.Enabled = true;
                    ShowAlert("Insufficient Permissions", $"Failed to obtain permission for {permissions[i]}.");
                    return;
                }
            }

            var bluetoothManager = (BluetoothManager)GetSystemService(BluetoothService);
            var bluetoothAdapter = bluetoothManager.Adapter;
            if (bluetoothAdapter?.IsEnabled != true)
            {
                var enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                try
                {
                    StartActivityForResult(enableBtIntent, EnableBluetoothRequestCode);
                }
                catch (Exception)
                {
                    _retryConnectButton.Enabled = true;
                    ShowAlert("Bluetooth Error", "Failed to enable Bluetooth.");
                }
            }
            else
                StartBluetoothScan();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == EnableBluetoothRequestCode)
            {
                if (resultCode == Result.Ok)
                    StartBluetoothScan();
                else
                {
                    _retryConnectButton.Enabled = true;
                    ShowAlert("Bluetooth Error", "Bluetooth is not enabled.");
                }
            }
        }

        private void Init()
        {
            if (_bluetoothLEService != null)
                _bluetoothLEService.CharacteristicNotificationReceived -= BluetoothLEService_CharacteristicNotificationReceived;

            _bluetoothLEService = null;
            SetControlsEnabled(false);

            _retryConnectButton.Visibility = ViewStates.Visible;

            var permissions = new[]
            {
                Manifest.Permission.AccessCoarseLocation,
                Manifest.Permission.AccessFineLocation,
                Manifest.Permission.Bluetooth,
                Manifest.Permission.BluetoothAdmin
            };

            RequestPermissions(permissions, PermissionsRequestCode);
        }

        private void StartBluetoothScan()
        {
            var scanFilter = new ScanFilter.Builder()
                .SetServiceUuid(ParcelUuid.FromString(BluetoothConstants.ServiceUuid.ToString()))
                .Build();

            BluetoothLEScanner.Current.StartScan(new[] { scanFilter }, new ScanSettings.Builder().Build());
        }

        private void BluetoothLEScanner_StateChanged(object sender, StateChangedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                if (e.IsScanning == false)
                {
                    _progressBar.Visibility = ViewStates.Gone;
                    if (_bluetoothLEService == null)
                    {
                        _retryConnectButton.Enabled = true;
                        ShowAlert("Bluetooth Connection Error", "Could not find a device to connect to.");
                    }
                }
                else
                    _progressBar.Visibility = ViewStates.Visible;
            });
        }

        private async void BluetoothLEScanner_DeviceDiscovered(object sender, DeviceDiscoveredEventArgs e)
        {
            _bluetoothLEService = new BluetoothLEService(this, e.Device);
            BluetoothLEScanner.Current.StopScan();

            _progressBar.Visibility = ViewStates.Visible;
            var result = await _bluetoothLEService.ConnectGattAsync();

            if (result.NewStatus != ProfileState.Connected)
            {
                _retryConnectButton.Enabled = true;
                ShowAlert("Bluetooth Connection Error", "Could not connect to device.");
                return;
            }

            _bluetoothLEService.ConnectionStateChanged += BluetoothLEService_ConnectionStateChanged;

            var readResult = await _bluetoothLEService.ReadCharacteristicAsync(BluetoothConstants.ServiceUuid,
                BluetoothConstants.StatusCharacteristicUuid);

            _progressBar.Visibility = ViewStates.Gone;

            if (readResult.GattStatus != GattStatus.Success)
                ShowAlert("Get Status Failed", $"Failed to get current status.");

            _bluetoothLEService.CharacteristicNotificationReceived += BluetoothLEService_CharacteristicNotificationReceived;
            var subscribeResult = _bluetoothLEService.SubscribeCharacteristicNotification(BluetoothConstants.ServiceUuid,
                BluetoothConstants.StatusCharacteristicUuid);

            if (subscribeResult.GattStatus != GattStatus.Success)
                ShowAlert("Status Subscription Failed", $"Failed to subscribe for status changes.");

            UpdateStatus(readResult.Response);

            SetControlsEnabled(true);
            _retryConnectButton.Visibility = ViewStates.Gone;
        }

        private void BluetoothLEService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.NewStatus == ProfileState.Disconnected && _bluetoothLEService != null)
            {
                _bluetoothLEService.CharacteristicNotificationReceived -= BluetoothLEService_CharacteristicNotificationReceived;
                _bluetoothLEService = null;
                SetControlsEnabled(false);
                RunOnUiThread(() =>
                {
                    _retryConnectButton.Visibility = ViewStates.Visible;
                    _retryConnectButton.Enabled = true;
                });
                ShowAlert("Bluetooth Disconnected", "Device has disconnected or lost connection. Reconnect and try again.");
            }
        }

        private void BluetoothLEService_CharacteristicNotificationReceived(object sender,
            CharacteristicNotificationReceivedEventArgs e)
        {
            UpdateStatus(e.Value);
        }

        private void UpdateStatus(byte[] value)
        {
            RunOnUiThread(() =>
            {
                var data = new string(Encoding.UTF8.GetChars(value)).Split(',');

                if (data.Length != 2)
                {
                    _statusTextView.Text = "Bad data";
                    _playButton.Visibility = ViewStates.Visible;
                    _pauseButton.Visibility = ViewStates.Gone;
                    return;
                }

                _statusTextView.Text = data[1];

                if (int.TryParse(data[0], out var statusId) && (SequencePlayerState)statusId == SequencePlayerState.Playing)
                {
                    _playButton.Visibility = ViewStates.Invisible;
                    _pauseButton.Visibility = ViewStates.Visible;
                    return;
                }

                _playButton.Visibility = ViewStates.Visible;
                _pauseButton.Visibility = ViewStates.Gone;
            });
        }

        private void SetControlsEnabled(bool isEnabled)
        {
            RunOnUiThread(() =>
            {
                _defaultButton.Enabled = isEnabled;
                _rainbowButton.Enabled = isEnabled;
                _wiwButton.Enabled = isEnabled;
                _playPauseLayout.Visibility = isEnabled ? ViewStates.Visible : ViewStates.Gone;
                _statusTextView.Visibility = isEnabled ? ViewStates.Visible : ViewStates.Gone;
                _retryConnectButton.Enabled = isEnabled;
            });
        }

        private async void DefaultButton_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(CommandCode.Play, 0);
        }

        private async void RainbowButton_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(CommandCode.Play, 1);
        }

        private async void WiwButton_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(CommandCode.Play, 2);
        }

        private async void PlayButton_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(CommandCode.Play);
        }

        private async void PauseButton_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(CommandCode.Pause);
        }

        private void RetryConnectButton_Click(object sender, EventArgs e)
        {
            Init();
        }

        private async Task SendCommandAsync(CommandCode commandCode, int? sequenceId = null)
        {
            RunOnUiThread(() =>
            {
                _progressBar.Visibility = ViewStates.Visible;
            });
            SetControlsEnabled(false);

            var bytes = new List<byte>() { (byte)commandCode };

            if (commandCode != CommandCode.Play && sequenceId.HasValue)
                throw new ArgumentException($"Sequence id is not allowed for command code {commandCode}.", nameof(sequenceId));
            else if (sequenceId.HasValue)
                bytes.AddRange(BitConverter.GetBytes(sequenceId.Value));

            var result = await _bluetoothLEService.WriteCharacteristicAsync(BluetoothConstants.ServiceUuid,
                BluetoothConstants.CommandCharacteristicUuid,
                bytes.ToArray());
            RunOnUiThread(() =>
            {
                _progressBar.Visibility = ViewStates.Gone;
            });
            SetControlsEnabled(true);

            if (result.GattStatus != GattStatus.Success)
                ShowAlert("Command Failed", $"Failed to send {commandCode} command.");
        }

        private void ShowAlert(string title, string message)
        {
            RunOnUiThread(() =>
            {
                new Android.App.AlertDialog.Builder(this, Android.Resource.Style.ThemeMaterialDialogAlert)
                .SetTitle(title)
                .SetMessage(message)
                .SetIcon(Android.Resource.Drawable.IcDialogAlert)
                .SetPositiveButton(Android.Resource.String.Ok, (IDialogInterfaceOnClickListener)null)
                .Show();
            });
        }
    }
}
