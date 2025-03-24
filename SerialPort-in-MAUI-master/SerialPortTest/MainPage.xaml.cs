using CommunityToolkit.Maui.Alerts;
using Android.AdServices.Common;

namespace SerialPortTest
{
	public partial class MainPage : ContentPage
	{
        private System.Timers.Timer _timer;
        private IUsbService _usbService;
		private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		public MainPage()
		{
			InitializeComponent();
			_usbService = DependencyService.Get<IUsbService>();

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

		private async void BSend_Clicked(System.Object sender, System.EventArgs e)
		{
			if (!string.IsNullOrEmpty(ENumber.Text))
			{
				await _usbService.SendMessageAsync(ENumber.Text);
				await DisplayAlert("Успешно", "Сообщение отправлено", "OK");
			}
			else
				await DisplayAlert("Ошибка", "Введите сообщение", "OK");
		}

		private async void OnTimedEvent(object sender, EventArgs e)
		{
			try
			{
                var message = await _usbService.ReadMessageAsync();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
						readData.Text += message;
						if (readData.Text.Length > 15)
							readData.Text = "";
                    });
                }
            }
			catch (Exception ex)
			{
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void BConnect_Clicked(System.Object sender, System.EventArgs e)
		{
			if (PComPorts.SelectedItem == null)
			{
				await DisplayAlert("Ошибка", "Выберите COM порт или Bluetooth устройство", "OK");
				return;
			}

			string selectedDevice = PComPorts.SelectedItem.ToString();
			bool success = false;

			var usbPorts = await _usbService.GetAvailablePortsAsync();
			var bluetoothDevices = await _usbService.GetAvailableBluetoothDevicesAsync();

			if (usbPorts.Contains(selectedDevice))
			{
				success = await _usbService.ConnectAsync(selectedDevice);

				if (success)
					await Toast.Make("Вы подключились к COM порту", ToastDuration.Long, 16).Show(cancellationTokenSource.Token);
			}
			else if (bluetoothDevices.Contains(selectedDevice))
			{
				success = await _usbService.ConnectBluetoothAsync(selectedDevice);

				if (success)
					await Toast.Make("Вы подключились к Bluetooth устройству", ToastDuration.Long, 16).Show(cancellationTokenSource.Token);
			}

			if (!success)
				await Toast.Make("Не удалось подключиться к устройству", ToastDuration.Long, 16).Show(cancellationTokenSource.Token);
		}

		private async void BGetPorts_Clicked(object sender, EventArgs e)
		{
			try
			{
				var ports = await _usbService.GetAvailablePortsAsync();
				var bluetoothDevices = await _usbService.GetAvailableBluetoothDevicesAsync();

				var allDevices = ports.Concat(bluetoothDevices).ToList();
				PComPorts.ItemsSource = allDevices;
			}
			catch (Exception ex)
			{
				await DisplayAlert("Ошибка", ex.Message, "OK");
			}
		}
	}
}