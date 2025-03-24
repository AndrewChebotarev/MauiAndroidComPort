using Android.App;

[assembly: UsesFeature("android.hardware.usb.host")]

namespace SerialPortTest
{
	public class UsbServiceAndroid : IUsbService
	{
		private UsbManager _usbManager;
		private UsbSerialPort _connectedPort;
		private BluetoothAdapter _bluetoothAdapter;
		private BluetoothSocket _bluetoothSocket;
		private BluetoothDevice _connectedBluetoothDevice;

		public UsbServiceAndroid()
		{
			_usbManager = (UsbManager)Android.App.Application.Context.GetSystemService(Context.UsbService);
			_bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
		}

		public async Task<IEnumerable<string>> GetAvailablePortsAsync()
		{
			var deviceList = _usbManager.DeviceList.Values;
			return deviceList.Select(device => device.DeviceName).ToList();
		}

		public async Task<IEnumerable<string>> GetAvailableBluetoothDevicesAsync()
		{
			var pairedDevices = _bluetoothAdapter.BondedDevices;
			return pairedDevices.Select(device => device.Name).ToList();
		}

		public async Task<bool> ConnectAsync(string portName)
		{
			var deviceList = _usbManager.DeviceList.Values;
			var device = deviceList.FirstOrDefault(d => d.DeviceName == portName);

			if (device != null)
			{
				var permissionGranted = _usbManager.HasPermission(device);
				if (!permissionGranted)
				{
					var permissionIntent = PendingIntent.GetBroadcast(Android.App.Application.Context, 0, new Intent("com.example.USB_PERMISSION"), PendingIntentFlags.UpdateCurrent);
					_usbManager.RequestPermission(device, permissionIntent);

					await Task.Delay(1000);
				}

				var drivers = UsbSerialProber.GetDefaultProber().FindAllDrivers(_usbManager);
				var driver = drivers.FirstOrDefault(d => d.Device.Equals(device));
				if (driver != null)
				{
					_connectedPort = driver.Ports.FirstOrDefault();
					if (_connectedPort != null)
					{
						var connection = _usbManager.OpenDevice(device);
						if (connection != null)
						{
							_connectedPort.Open(connection);

							_connectedPort.SetParameters(115200, 8, StopBits.One, Parity.None);
							return true;
						}
					}
				}
			}
			return false;
		}

		public async Task<bool> ConnectBluetoothAsync(string deviceName)
		{
			var device = _bluetoothAdapter.BondedDevices.FirstOrDefault(d => d.Name == deviceName);
			if (device != null)
			{
				_connectedBluetoothDevice = device;
				var uuid = device.GetUuids().FirstOrDefault()?.Uuid;
				_bluetoothSocket = device.CreateRfcommSocketToServiceRecord(uuid);
				try
				{
					await _bluetoothSocket.ConnectAsync();
					return true;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка подключения к Bluetooth-устройству: {ex.Message}");
				}
			}
			return false;
		}

		public async Task SendMessageAsync(string message)
		{
			if (_connectedPort != null)
			{
				try
				{
					byte[] messageBytes = System.Text.Encoding.ASCII.GetBytes(message);
					await Task.Run(() => _connectedPort.Write(messageBytes, 1000));
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
				}
			}
			else if (_bluetoothSocket != null && _bluetoothSocket.IsConnected)
			{
				try
				{
					byte[] buffer = Encoding.ASCII.GetBytes(message);
					await _bluetoothSocket.OutputStream.WriteAsync(buffer, 0, buffer.Length);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка отправки сообщения через Bluetooth: {ex.Message}");
				}
			}

		}

		public async Task<string> ReadMessageAsync()
        {
            if (_connectedPort != null)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int numBytesRead = await Task.Run(() => _connectedPort.Read(buffer, 1000));

                    if (numBytesRead > 0)
                        return Encoding.ASCII.GetString(buffer, 0, numBytesRead);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка чтения сообщения: {ex.Message}");
                }
            }
			else if (_bluetoothSocket != null && _bluetoothSocket.IsConnected)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = await _bluetoothSocket.InputStream.ReadAsync(buffer, 0, buffer.Length);
                    return Encoding.ASCII.GetString(buffer, 0, bytesRead);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка чтения сообщения через Bluetooth: {ex.Message}");
                }
            }

            return string.Empty;
        }
    }
}