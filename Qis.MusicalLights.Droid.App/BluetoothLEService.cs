using Android.Bluetooth;
using Android.Content;
using Java.Util;
using QIndependentStudios.MusicalLights.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qis.MusicalLights.Droid.App
{
    public class BluetoothLEService
    {
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<CharacteristicNotificationReceivedEventArgs> CharacteristicNotificationReceived;

        private readonly Context _context;
        private readonly BluetoothDevice _bluetoothDevice;
        private readonly HashSet<(Guid service, Guid characteristic)> _subscriptions = new HashSet<(Guid, Guid)>();

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
            ConnectGattResult result = null;
            _connectGattCompletionSource = new TaskCompletionSource<ConnectGattResult>();
            _gatt = _bluetoothDevice.ConnectGatt(_context, false, new GattCallback(this));

            var completedTask = await Task.WhenAny(_connectGattCompletionSource.Task,
                CreateTimeoutTask(new ConnectGattResult(GattStatus.Failure, ProfileState.Disconnected)));
            result = completedTask.Result;

            if (result.NewStatus == ProfileState.Connected)
            {
                _discoverServicesCompletionSource = new TaskCompletionSource<DiscoverServicesResult>();
                var discoverTask = _discoverServicesCompletionSource.Task;
                _gatt.DiscoverServices();

                var completedDiscoverTask = await Task.WhenAny(_discoverServicesCompletionSource.Task,
                    CreateTimeoutTask(new DiscoverServicesResult(GattStatus.Failure), 10000));
                if (completedDiscoverTask != discoverTask)
                {
                    Disconnect();
                    return new ConnectGattResult(GattStatus.Failure, ProfileState.Disconnected);
                }
            }
            else
                Disconnect();

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
                return new CharacteristicReadResult(GattStatus.Failure, new byte[0]);

            var characteristic = service.GetCharacteristic(UUID.FromString(characteristicUuid.ToString()));
            if (characteristic == null)
                return new CharacteristicReadResult(GattStatus.Failure, new byte[0]);

            if (_gatt.ReadCharacteristic(characteristic))
            {
                var completedTask = await Task.WhenAny(_readCharacteristicCompletionSource.Task,
                    CreateTimeoutTask(new CharacteristicReadResult(GattStatus.Failure, new byte[0])));
                return completedTask.Result;
            }

            return new CharacteristicReadResult(GattStatus.Failure, new byte[0]);
        }

        public async Task<CharacteristicWriteResult> WriteCharacteristicAsync(Guid serviceUuid,
            Guid characteristicUuid,
            byte[] value)
        {
            EnsureConnected();

            _writeCharacteristicCompletionSource = new TaskCompletionSource<CharacteristicWriteResult>();

            var service = _gatt.GetService(UUID.FromString(serviceUuid.ToString()));
            if (service == null)
                return new CharacteristicWriteResult(GattStatus.Failure);

            var characteristic = service.GetCharacteristic(UUID.FromString(characteristicUuid.ToString()));
            if (characteristic == null)
                return new CharacteristicWriteResult(GattStatus.Failure);

            if (!characteristic.SetValue(value))
                return new CharacteristicWriteResult(GattStatus.Failure);

            characteristic.WriteType = GattWriteType.Default;
            if (_gatt.WriteCharacteristic(characteristic))
            {
                var completedTask = await Task.WhenAny(_writeCharacteristicCompletionSource.Task,
                    CreateTimeoutTask(new CharacteristicWriteResult(GattStatus.Failure)));
                return completedTask.Result;
            }

            return new CharacteristicWriteResult(GattStatus.Failure);
        }

        public CharacteristicSubscriptionChangeResult SubscribeCharacteristicNotification(Guid serviceUuid,
            Guid characteristicUuid)
        {
            return ChangeCharacteristicNotificationSubscription(serviceUuid, characteristicUuid, true);
        }

        public CharacteristicSubscriptionChangeResult UnsubscribeCharacteristicNotification(Guid serviceUuid,
            Guid characteristicUuid)
        {
            return ChangeCharacteristicNotificationSubscription(serviceUuid, characteristicUuid, false);
        }

        public void Disconnect()
        {
            try
            {
                foreach (var subscription in _subscriptions)
                {
                    UnsubscribeCharacteristicNotification(subscription.service, subscription.characteristic);
                }

                _gatt?.Disconnect();
                _gatt = null;
            }
            catch (Exception) { }
        }

        protected void OnConnectionStateChanged(GattStatus gattStatus, ProfileState newStatus)
        {
            _connectGattCompletionSource?.SetResult(new ConnectGattResult(gattStatus, newStatus));
            _connectGattCompletionSource = null;

            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(gattStatus, newStatus));
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

        protected void OnCharacteristicNotificationReceived(Guid uuid, byte[] value)
        {
            CharacteristicNotificationReceived?.Invoke(this, new CharacteristicNotificationReceivedEventArgs(uuid, value));
        }

        private void EnsureConnected()
        {
            if (_gatt == null)
                throw new InvalidOperationException("GATT connection must be established first.");
        }

        private CharacteristicSubscriptionChangeResult ChangeCharacteristicNotificationSubscription(Guid serviceUuid,
            Guid characteristicUuid,
            bool isEnabled)
        {
            var service = _gatt.GetService(UUID.FromString(serviceUuid.ToString()));
            if (service == null)
                return new CharacteristicSubscriptionChangeResult(GattStatus.Failure);

            var characteristic = service.GetCharacteristic(UUID.FromString(characteristicUuid.ToString()));
            if (characteristic == null)
                return new CharacteristicSubscriptionChangeResult(GattStatus.Failure);

            if (!_gatt.SetCharacteristicNotification(characteristic, isEnabled))
                return new CharacteristicSubscriptionChangeResult(GattStatus.Failure);

            var descriptor = characteristic.GetDescriptor(UUID.FromString(BluetoothConstants.ClientCharacteristicConfigDescriptorUuid.ToString()));
            var descriptorValue = isEnabled ? BluetoothGattDescriptor.EnableNotificationValue : BluetoothGattDescriptor.DisableNotificationValue;
            if (!descriptor.SetValue(descriptorValue.ToArray()))
                return new CharacteristicSubscriptionChangeResult(GattStatus.Failure);

            if (!_gatt.WriteDescriptor(descriptor))
                return new CharacteristicSubscriptionChangeResult(GattStatus.Failure);

            _subscriptions.Remove((serviceUuid, characteristicUuid));

            if (isEnabled)
                _subscriptions.Add((serviceUuid, characteristicUuid));

            return new CharacteristicSubscriptionChangeResult(GattStatus.Success);
        }

        private async Task<T> CreateTimeoutTask<T>(T response, int timeout = 5000)
        {
            await Task.Delay(timeout);
            return response;
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

            public override void OnCharacteristicRead(BluetoothGatt gatt,
                BluetoothGattCharacteristic characteristic,
                GattStatus status)
            {
                _bluetoothLEService.OnCharacteristicRead(status, characteristic.GetValue());
            }

            public override void OnCharacteristicWrite(BluetoothGatt gatt,
                BluetoothGattCharacteristic characteristic,
                GattStatus status)
            {
                _bluetoothLEService.OnCharacteristicWritten(status);
            }

            public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
            {
                _bluetoothLEService.OnCharacteristicNotificationReceived(Guid.Parse(characteristic.Uuid.ToString()),
                    characteristic.GetValue());
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

    public class CharacteristicSubscriptionChangeResult
    {
        public CharacteristicSubscriptionChangeResult(GattStatus gattStatus)
        {
            GattStatus = gattStatus;
        }

        public GattStatus GattStatus { get; }
    }

    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionStateChangedEventArgs(GattStatus gattStatus, ProfileState newStatus)
        {
            GattStatus = gattStatus;
            NewStatus = newStatus;
        }

        public GattStatus GattStatus { get; }
        public ProfileState NewStatus { get; }
    }

    public class CharacteristicNotificationReceivedEventArgs : EventArgs
    {
        public CharacteristicNotificationReceivedEventArgs(Guid characteristicUuid, byte[] value)
        {
            CharacteristicUuid = characteristicUuid;
            Value = value;
        }

        public Guid CharacteristicUuid { get; }
        public byte[] Value { get; }
    }
}