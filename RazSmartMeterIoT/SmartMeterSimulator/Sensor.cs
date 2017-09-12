using System;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;


namespace SmartMeterSimulator
{
    /// <summary>
    /// A sensor represents a Smart Meter in the simulator.
    /// </summary>
    class Sensor
    {
        private DeviceClient _DeviceClient;
        private string _IotHubUri { get; set; }
        public string DeviceId { get; set; }
        public string DeviceKey { get; set; }
        public DeviceState State { get; set; }
        public string StatusWindow { get; set; }
        public double CurrentTemperature
        {
            get
            {
                double avgTemperature = 70;
                Random rand = new Random();

                double currentTemperature = avgTemperature + rand.Next(-6, 6);

                if (currentTemperature <= 68)
                    TemperatureIndicator = SensorState.Cold;
                else if (currentTemperature > 68 && currentTemperature < 72)
                    TemperatureIndicator = SensorState.Normal;
                else if (currentTemperature >= 72)
                    TemperatureIndicator = SensorState.Hot;

                return currentTemperature;
            }
        }
        public SensorState TemperatureIndicator { get; set; }

        public Sensor(string iotHubUri, string deviceId, string deviceKey)
        {
            _IotHubUri = iotHubUri;
            DeviceId = deviceId;
            DeviceKey = deviceKey;
            State = DeviceState.Registered;
        }
        public void InstallDevice(string statusWindow)
        {
            StatusWindow = statusWindow;
            State = DeviceState.Installed;
        }

        /// <summary>
        /// Connect a device to the IoT Hub by instantiating a DeviceClient for that Device by Id and Key.
        /// </summary>
        public void ConnectDevice()
        {
            //TODO: 17. Connect the Device to Iot Hub by creating an instance of DeviceClient
            _DeviceClient = DeviceClient.Create(_IotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(DeviceId, DeviceKey));

            //Set the Device State to Ready
            State = DeviceState.Ready;
        }
        public void DisconnectDevice()
        {
            //Delete the local device client
            _DeviceClient = null;

            //Set the Device State to Activate
            State = DeviceState.Activated;
        }

        /// <summary>
        /// Send a message to the IoT Hub from the Smart Meter device
        /// </summary>
        public async void SendMessageAsync()
        {
            var telemetryDataPoint = new
            {
                id = DeviceId,
                time = DateTime.UtcNow.ToString("o"),
                temp = CurrentTemperature
            };

            //TODO: 18.Serialize the telemetryDataPoint to JSON
            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);

            //TODO: 19.Encode the JSON string to ASCII as bytes and create new Message with the bytes
            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            //TODO: 20.Send the message to the IoT Hub
            await _DeviceClient.SendEventAsync(message);
        }
    }

    public enum DeviceState
    { 
        Registered,
        Installed,
        Activated,
        Ready,
        Transmit
    }
    public enum SensorState
    {
        Cold,
        Normal,
        Hot
    }
}
