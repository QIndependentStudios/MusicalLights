using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qis.MusicalLights.Droid.App
{
    public class BluetoothLEScanner
    {
        public event EventHandler<StateChangedEventArgs> StateChanged;
        public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered;

        protected const int _scanTimeout = 30000;

        private static readonly ScanCallback _scanCallback;

        protected readonly BluetoothManager _manager;
        protected readonly BluetoothAdapter _adapter;
        protected readonly List<BluetoothDevice> _discoveredDevices = new List<BluetoothDevice>();

        static BluetoothLEScanner()
        {
            Current = new BluetoothLEScanner();
            _scanCallback = new BluetoothLEScannerScanCallback(Current);
        }

        protected BluetoothLEScanner()
        {
            var appContext = Android.App.Application.Context;
            _manager = (BluetoothManager)appContext.GetSystemService(Context.BluetoothService);
            _adapter = _manager.Adapter;
        }

        public static BluetoothLEScanner Current { get; }

        public bool IsScanning { get; protected set; }
        public IReadOnlyList<BluetoothDevice> DiscoveredDevices => _discoveredDevices.AsReadOnly();

        public void StartScan()
        {
            StartScan(false, null, null);
        }

        public void StartScan(IList<ScanFilter> filters, ScanSettings settings)
        {
            StartScan(true, filters, settings);
        }

        protected async void StartScan(bool withParams, IList<ScanFilter> filters, ScanSettings settings)
        {
            if (withParams)
            {
                if (filters == null)
                    throw new ArgumentNullException(nameof(filters));
                if (settings == null)
                    throw new ArgumentNullException(nameof(settings));
            }

            _discoveredDevices.Clear();

            IsScanning = true;
            OnStateChanged();

            if (withParams)
                _adapter.BluetoothLeScanner.StartScan(filters, settings, _scanCallback);
            else
                _adapter.BluetoothLeScanner.StartScan(_scanCallback);

            await Task.Delay(_scanTimeout);

            StopScan();
        }

        public void StopScan()
        {
            if (IsScanning)
            {
                _adapter.BluetoothLeScanner.StopScan(_scanCallback);
                IsScanning = false;
                OnStateChanged();
            }
        }

        protected void OnDeviceDiscovered(BluetoothDevice device, ScanRecord scanRecord, int rssi)
        {
            if (!_discoveredDevices.Any(d => device.Address == d.Address))
            {
                _discoveredDevices.Add(device);
                DeviceDiscovered?.Invoke(this, new DeviceDiscoveredEventArgs(device, scanRecord, rssi));
            }
        }

        protected void OnStateChanged()
        {
            StateChanged?.Invoke(this, new StateChangedEventArgs(IsScanning));
        }

        protected void OnScanFailed(ScanFailure errorCode)
        {
            throw new InvalidOperationException($"Scan failed with error code {errorCode}.");
        }

        protected class BluetoothLEScannerScanCallback : ScanCallback
        {
            private readonly BluetoothLEScanner _manager;

            public BluetoothLEScannerScanCallback(BluetoothLEScanner manager)
            {
                _manager = manager;
            }

            public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
            {
                base.OnScanResult(callbackType, result);
                _manager.OnDeviceDiscovered(result.Device, result.ScanRecord, result.Rssi);
            }

            public override void OnScanFailed(ScanFailure errorCode)
            {
                base.OnScanFailed(errorCode);
            }
        }
    }

    public class DeviceDiscoveredEventArgs : EventArgs
    {
        public DeviceDiscoveredEventArgs(BluetoothDevice device, ScanRecord scanRecord, int rssi)
        {
            Device = device;
            Rssi = rssi;
            ScanRecord = scanRecord;
        }

        public BluetoothDevice Device { get; }
        public ScanRecord ScanRecord { get; }
        public int Rssi { get; }
    }

    public class StateChangedEventArgs : EventArgs
    {
        public StateChangedEventArgs(bool isScanning)
        {
            IsScanning = isScanning;
        }

        public bool IsScanning { get; }
    }
}
