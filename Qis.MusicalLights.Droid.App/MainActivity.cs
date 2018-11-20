using Android;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using QIndependentStudios.MusicalLights.Core;
using System;

namespace Qis.MusicalLights.Droid.App
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const int PermissionsRequestCode = 166277918;
        private const int EnableBluetoothRequestCode = 76357864;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            var fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            var permissions = new[]
            {
                Manifest.Permission.AccessCoarseLocation,
                Manifest.Permission.AccessFineLocation,
                Manifest.Permission.Bluetooth,
                Manifest.Permission.BluetoothAdmin
            };

            RequestPermissions(permissions, PermissionsRequestCode);

            var bluetoothManager = (BluetoothManager)GetSystemService(BluetoothService);
            var bluetoothAdapter = bluetoothManager.Adapter;
            if (bluetoothAdapter?.IsEnabled != true)
            {
                var enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableBtIntent, EnableBluetoothRequestCode);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            var id = item.ItemId;
            if (id == Resource.Id.action_settings)
                return true;

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            Snackbar.Make((View)sender, "Starting bluetooth stuff...", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null)
                .Show();

            StartBluetoothScan();
        }

        private void StartBluetoothScan()
        {
            BluetoothLEScanner.Current.DeviceDiscovered += BluetoothLEScanner_DeviceDiscovered;
            BluetoothLEScanner.Current.StateChanged += BluetoothLEScanner_StateChanged;

            var scanFilter = new ScanFilter.Builder()
                .SetServiceUuid(ParcelUuid.FromString(BluetoothConstants.ServiceUuid.ToString()))
                .Build();

            BluetoothLEScanner.Current.StartScan(new[] { scanFilter }, new ScanSettings.Builder().Build());
        }

        private void BluetoothLEScanner_StateChanged(object sender, StateChangedEventArgs e)
        {
            Toast.MakeText(BaseContext, e.IsScanning ? "Scanning started" : "Scanning stopped", ToastLength.Short).Show();
        }

        private async void BluetoothLEScanner_DeviceDiscovered(object sender, DeviceDiscoveredEventArgs e)
        {
            Toast.MakeText(BaseContext, $"{e.Device.Name} {e.Device.Address}", ToastLength.Short).Show();
            BluetoothLEScanner.Current.StopScan();

            var service = new BluetoothLEService(this, e.Device);
            await service.ConnectGattAsync();
            await service.WriteCharacteristicAsync(BluetoothConstants.ServiceUuid,
                BluetoothConstants.CommandCharacteristicUuid,
                new[] { (byte)CommandCode.Play });
            service.Disconnect();
        }
    }
}
