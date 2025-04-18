﻿namespace SerialPortTest
{
	public interface IUsbService
	{
		Task<IEnumerable<string>> GetAvailablePortsAsync();
		Task<bool> ConnectAsync(string portName);
		Task SendMessageAsync(string message);
		Task<string> ReadMessageAsync();
		Task<IEnumerable<string>> GetAvailableBluetoothDevicesAsync();
		Task<bool> ConnectBluetoothAsync(string deviceName);
	}
}
