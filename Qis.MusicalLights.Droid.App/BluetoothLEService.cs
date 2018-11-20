using Android.Bluetooth;
using Android.Content;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Qis.MusicalLights.Droid.App
{
    public class BluetoothLEService
    {
        private readonly Context _context;
        private readonly BluetoothDevice _bluetoothDevice;

        private BluetoothGatt _gatt;
        private TaskCompletionSource<ConnectGattResult> _connectGattCompletionSource;
        private TaskCompletionSource<DiscoverServicesResult> _discoverServicesCompletionSource;
        private TaskCompletionSource<CharacteristicReadResult> _readCharacteristicCompletionSource;
        private TaskCompletionSource<CharacteristicWriteResult> _writeCharacteristicCompletionSource;

        public BluetoothLEService(Context context, BluetoothDevice bluetoothDevice)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
        }

        public async Task<ConnectGattResult> ConnectGattAsync()
        {
            _connectGattCompletionSource = new TaskCompletionSource<ConnectGattResult>();
            _gatt = _bluetoothDevice.ConnectGatt(_context, false, new GattCallback(this));
            var result = await _connectGattCompletionSource.Task;

            _discoverServicesCompletionSource = new TaskCompletionSource<DiscoverServicesResult>();
            _gatt.DiscoverServices();
            await _discoverServicesCompletionSource.Task;
            return result;
        }

        public IList<BluetoothGattService> GetServices()
        {
            EnsureConnected();
            return _gatt.Services;
        }

        public async Task<CharacteristicReadResult> ReadCharacteristicAsync(Guid serviceUuid, Guid characteristicUuid)
        {
            EnsureConnected();

            _readCharacteristicCompletionSource = new TaskCompletionSource<CharacteristicReadResult>();

            var service = _gatt.GetService(UUID.FromString(serviceUuid.ToString()));
            if (service == null)
                throw new ArgumentException("Invalid service for bluetooth device.", nameof(serviceUuid));

            var characteristic = service.GetCharacteristic(UUID.FromString(characteristicUuid.ToString()));
            if (characteristic == null)
                throw new ArgumentException("Invalid characteristic for service.", nameof(characteristicUuid));

            _gatt.ReadCharacteristic(characteristic);

            return await _readCharacteristicCompletionSource.Task;
        }

        public async Task<CharacteristicWriteResult> WriteCharacteristicAsync(Guid serviceUuid,
            Guid characteristicUuid,
            byte[] value)
        {
            EnsureConnected();

            _writeCharacteristicCompletionSource = new TaskCompletionSource<CharacteristicWriteResult>();

            var service = _gatt.GetService(UUID.FromString(serviceUuid.ToString()));
            if (service == null)
                throw new ArgumentException("Invalid service for bluetooth device.", nameof(serviceUuid));

            var characteristic = service.GetCharacteristic(UUID.FromString(characteristicUuid.ToString()));
            if (characteristic == null)
                throw new ArgumentException("Invalid characteristic for service.", nameof(characteristicUuid));

            if (!characteristic.SetValue(value))
                throw new InvalidOperationException("Failed to set value to write to characteristic.");

            characteristic.WriteType = GattWriteType.Default;
            _gatt.WriteCharacteristic(characteristic);

            return await _writeCharacteristicCompletionSource.Task;
        }

        public void Disconnect()
        {
            _gatt?.Disconnect();
            _gatt = null;
        }

        protected void OnConnectionStateChanged(GattStatus gattStatus, ProfileState newStatus)
        {
            _connectGattCompletionSource?.SetResult(new ConnectGattResult(gattStatus, newStatus));
            _connectGattCompletionSource = null;
        }

        protected void OnServicesDiscovered(GattStatus gattStatus)
        {
            _discoverServicesCompletionSource?.SetResult(new DiscoverServicesResult(gattStatus));
            _discoverServicesCompletionSource = null;
        }

        protected void OnCharacteristicRead(GattStatus gattStatus, byte[] value)
        {
            _readCharacteristicCompletionSource?.SetResult(new CharacteristicReadResult(gattStatus, value));
            _readCharacteristicCompletionSource = null;
        }

        protected void OnCharacteristicWritten(GattStatus gattStatus)
        {
            _writeCharacteristicCompletionSource?.SetResult(new CharacteristicWriteResult(gattStatus));
            _writeCharacteristicCompletionSource = null;
        }

        private void EnsureConnected()
        {
            if (_gatt == null)
                throw new InvalidOperationException("GATT connection must be established first.");
        }

        protected class GattCallback : BluetoothGattCallback
        {
            private readonly BluetoothLEService _bluetoothLEService;

            public GattCallback(BluetoothLEService bluetoothLEService)
            {
                _bluetoothLEService = bluetoothLEService;
            }

            public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
            {
                _bluetoothLEService.OnConnectionStateChanged(status, newState);
            }

            public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
            {
                _bluetoothLEService.OnServicesDiscovered(status);
            }

            public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
            {
                _bluetoothLEService.OnCharacteristicRead(status, characteristic.GetValue());
            }

            public override void OnCharacteristicWrite(BluetoothGatt gatt,
                BluetoothGattCharacteristic characteristic,
                GattStatus status)
            {
                _bluetoothLEService.OnCharacteristicWritten(status);
            }
        }
    }

    public class ConnectGattResult
    {
        public ConnectGattResult(GattStatus gattStatus, ProfileState newStatus)
        {
            GattStatus = gattStatus;
            NewStatus = newStatus;
        }

        public GattStatus GattStatus { get; }
        public ProfileState NewStatus { get; }
    }

    public class DiscoverServicesResult
    {
        public DiscoverServicesResult(GattStatus gattStatus)
        {
            GattStatus = gattStatus;
        }

        public GattStatus GattStatus { get; }
    }

    public class CharacteristicReadResult
    {
        public CharacteristicReadResult(GattStatus gattStatus, byte[] response)
        {
            GattStatus = gattStatus;
            Response = response;
        }

        public GattStatus GattStatus { get; }
        public byte[] Response { get; }
    }

    public class CharacteristicWriteResult
    {
        public CharacteristicWriteResult(GattStatus gattStatus)
        {
            GattStatus = gattStatus;
        }

        public GattStatus GattStatus { get; }
    }
}