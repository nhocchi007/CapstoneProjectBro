﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace MySensorTag
{
    /// <summary>
    /// This class combines all of the GATT services provided by SensorTag into one helper class.
    /// See http://processors.wiki.ti.com/index.php/CC2650_SensorTag_User's_Guide#IR_Temperature_Sensor
    /// for details on GATT services.
    /// </summary>
    public class SensorTag : INotifyPropertyChanged
    {
        BleIRTemperatureService _tempService;   
        BleHumidityService _humidityService;
        BleBarometerService _barometerService;

        // Version 1 only
        BleButtonService _buttonService;
        BleAccelerometerService _accelService;
        BleGyroscopeService _gyroService;
        BleMagnetometerService _magService;

        // Version 2 only.
        BleMovementService _motionService;
        BleLightIntensityService _lightService;
        BleEMGNode2Service _emgNode2Service;

        // variables
        bool connected;
        bool connecting;
        bool disconnecting;
        BleGattDeviceInfo deviceInfo;
        int version;
        string deviceName;
        static SensorTag _selected;

        public int Version { get { return this.version; } }

        private SensorTag(BleGattDeviceInfo deviceInfo)
        {
            this.deviceInfo = deviceInfo;
            this.version = 2;
            string name = deviceInfo.DeviceInformation.Name;
            Debug.WriteLine("Found sensor tag: [{0}]", name);
            
        }

        /// <summary>
        /// Get or set the selected SensorTag instance.
        /// </summary>
        public static SensorTag SelectedSensor { get { return _selected; } set { _selected = value; } }


        private SensorTag()
        {
            throw new InvalidOperationException();
        }

        public string DeviceAddress
        {
            get { return deviceInfo.Address.ToString("x"); }
        }

        public string DeviceName
        {
            get { return this.deviceName; }
        }

        public bool Connected { get { return connected; } }
        
        /// <summary>
        /// Find all SensorTag devices that are paired with this PC.
        /// </summary>
        /// <returns></returns>
        public static async Task<IEnumerable<SensorTag>> FindAllDevices()
        {
            List<SensorTag> result = new List<SensorTag>();
            foreach (var device in await BleGenericGattService.FindMatchingDevices(BleMovementService.MovementServiceUUid))
            {
                string name = "" + device.DeviceInformation.Name;
                //if (name.Contains("SensorTag") || name.Contains("Sensor Tag"))
                //{
                //    result.Add(new SensorTag(device));
                //}
                result.Add(new SensorTag(device));
            }
            return result;
        }


        public BleAccelerometerService Accelerometer { get { return _accelService; } }
        public BleGyroscopeService Gyroscope { get { return _gyroService; } }
        public BleMagnetometerService Magnetometer { get { return _magService; } }
        public BleIRTemperatureService IRTemperature { get { return _tempService; } }
        public BleButtonService Buttons { get { return _buttonService; } }
        public BleHumidityService Humidity { get { return _humidityService; } }
        public BleBarometerService Barometer { get { return _barometerService; } }

        // Version 2 sensors.
        public BleMovementService Movement { get { return _motionService; } }
        public BleLightIntensityService LightIntensity { get { return _lightService; } }
        public BleEMGNode2Service EMGNode2 { get { return _emgNode2Service; } }


        public event EventHandler<string> StatusChanged;

        private void OnStatusChanged(string status)
        {
            if (StatusChanged != null)
            {
                StatusChanged(this, status);
            }
        }

        /// <summary>
        /// Connect or reconnect to the device.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ConnectAsync()
        {
            if (!connecting && !connected)
            {
                disconnecting = false;

                try
                {
                    OnStatusChanged("connecting...");

                    // since all this code is async, user could quit in the middle, hence all the checks
                    // on the "disconnecting" state.
                    
                    //if (!await ConnectHumidityService())
                    //{
                    //    return false;
                    //};
                    if (!await ConnectBarometerService())
                    {
                        return false;
                    };



                    await ConnectMovementService();
                    if (disconnecting) return false;
                    //await ConnectHumidityService();
                    //if (disconnecting) return false;
                    await ConnectEMGNode2Service();
                    if (disconnecting) return false;
                    //await ConnectBarometerService();
                    //if (disconnecting) return false;



                    connected = true;
                    OnStatusChanged("connected");
                }
                finally
                {
                    connecting = false;
                }
            }
            return true;
        }

        public async void DisconnectEMG()
        {
            if (_humidityService != null)
            {
                using (_humidityService)
                {
                    try
                    {
                        _humidityService.Error -= OnServiceError;
                        _humidityService.ConnectionChanged -= OnConnectionChanged;
                        await _humidityService.StopReading();
                    }
                    catch { }
                    _humidityService = null;
                    Console.WriteLine("Disconnect EMGService Success!");
                }
            }
        }

        public async void DisconnectMovement()
        {
            if (_motionService != null)
            {
                using (_motionService)
                {
                    try
                    {
                        _motionService.Error -= OnServiceError;
                        _motionService.ConnectionChanged -= OnConnectionChanged;
                        await _motionService.StopReading();
                    }
                    catch { }
                    _motionService = null;
                    Console.WriteLine("Disconnect MovementService Success!");
                }
            }
        }

        public async void DisconnectBarometer()
        {
            if (_barometerService != null)
            {
                using (_barometerService)
                {
                    try
                    {
                        _barometerService.Error -= OnServiceError;
                        _barometerService.ConnectionChanged -= OnConnectionChanged;
                        await _barometerService.StopReading();
                    }
                    catch { }
                    _barometerService = null;
                    Console.WriteLine("Disconnect BaroService Success!");
                }
            }
        }

        public async void Disconnect()
        {
            disconnecting = true;
            connected = false;

            if (_emgNode2Service != null)
            {
                using (_emgNode2Service)
                {
                    try
                    {
                        _emgNode2Service.Error -= OnServiceError;
                        _emgNode2Service.ConnectionChanged -= OnConnectionChanged;
                        await _emgNode2Service.StopReading();
                    }
                    catch { }
                    _emgNode2Service = null;
                }
            }

            if (_barometerService != null)
            {
                using (_barometerService)
                {
                    try
                    {
                        _barometerService.Error -= OnServiceError;
                        _barometerService.ConnectionChanged -= OnConnectionChanged;
                        await _barometerService.StopReading();
                    }
                    catch { }
                    _barometerService = null;
                }
            }

            if (_tempService != null)
            {
                using (_tempService)
                {
                    try
                    {
                        _tempService.Error -= OnServiceError;
                        _tempService.ConnectionChanged -= OnConnectionChanged;
                        await _tempService.StopReading();
                    }
                    catch { }
                    _tempService = null;
                }
            }
            if (_buttonService != null)
            {
                using (_buttonService)
                {
                    _buttonService.Error -= OnServiceError;
                    _buttonService.ConnectionChanged -= OnConnectionChanged;
                    _buttonService = null;
                }
            }
            if (_accelService != null)
            {
                using (_accelService)
                {
                    try
                    {
                        _accelService.Error -= OnServiceError;
                        _accelService.ConnectionChanged -= OnConnectionChanged;
                        await _accelService.StopReading();
                    }
                    catch { }
                    _accelService = null;
                }
            }

            if (_gyroService != null)
            {
                using (_gyroService)
                {
                    try
                    {
                        _gyroService.Error -= OnServiceError;
                        _gyroService.ConnectionChanged -= OnConnectionChanged;
                        await _gyroService.StopReading();
                    }
                    catch { }
                    _gyroService = null;
                }
            }
            if (_magService != null)
            {
                using (_magService)
                {
                    try
                    {
                        _magService.Error -= OnServiceError;
                        _magService.ConnectionChanged -= OnConnectionChanged;
                        await _magService.StopReading();
                    }
                    catch { }
                    _magService = null;
                }
            }

            if (_humidityService != null)
            {
                using (_humidityService)
                {
                    try
                    {
                        _humidityService.Error -= OnServiceError;
                        _humidityService.ConnectionChanged -= OnConnectionChanged;
                        await _humidityService.StopReading();
                    }
                    catch { }
                    _humidityService = null;
                }
            }

            if (_barometerService != null)
            {
                using (_barometerService)
                {
                    try
                    {
                        _barometerService.Error -= OnServiceError;
                        _barometerService.ConnectionChanged -= OnConnectionChanged;
                        await _barometerService.StopReading();
                    }
                    catch { }
                    _barometerService = null;
                }
            }


            if (_motionService != null)
            {
                using (_motionService)
                {
                    try
                    {
                        _motionService.Error -= OnServiceError;
                        _motionService.ConnectionChanged -= OnConnectionChanged;
                        await _motionService.StopReading();
                    }
                    catch { }
                    _motionService = null;
                }
            }

            if (_lightService != null)
            {
                using (_lightService)
                {
                    try
                    {
                        _lightService.Error -= OnServiceError;
                        _lightService.ConnectionChanged -= OnConnectionChanged;
                        await _lightService.StopReading();
                    }
                    catch { }
                    _lightService = null;
                }
            }

        }

        private async Task<bool> ConnectIRTemperatureService()
        {
            if (_tempService == null)
            {
                _tempService = new BleIRTemperatureService() { Version = this.version };
                _tempService.Error += OnServiceError;

                if (await _tempService.ConnectAsync(deviceInfo.ContainerId))
                {
                    _tempService.ConnectionChanged += OnConnectionChanged;
                    return true;
                }
                _tempService.Error -= OnServiceError;
                _tempService = null;
                return false;
            }
            return true;
        }


        private async Task<bool> ConnectMovementService()
        {
            if (_motionService == null)
            {
                _motionService = new BleMovementService() { Version = this.version };
                _motionService.Error += OnServiceError;

                if (await _motionService.ConnectAsync(deviceInfo.ContainerId))
                {
                    _motionService.ConnectionChanged += OnConnectionChanged;
                    return true;
                }
                _motionService.Error -= OnServiceError;
                _motionService = null;
                return false;
            }
            return true;
        }

        private async Task<bool> ConnectEMGNode2Service()
        {
            if (_emgNode2Service == null)
            {
                _emgNode2Service = new BleEMGNode2Service() { Version = this.version };
                _emgNode2Service.Error += OnServiceError;

                if (await _emgNode2Service.ConnectAsync(deviceInfo.ContainerId))
                {
                    _emgNode2Service.ConnectionChanged += OnConnectionChanged;
                    return true;
                }
                _emgNode2Service.Error -= OnServiceError;
                _emgNode2Service = null;
                return false;
            }
            return true;
        }


        private async Task<bool> ConnectLightIntensityService()
        {
            if (_lightService == null)
            {
                _lightService = new BleLightIntensityService() { Version = this.version };
                _lightService.Error += OnServiceError;

                if (await _lightService.ConnectAsync(deviceInfo.ContainerId))
                {
                    _lightService.ConnectionChanged += OnConnectionChanged;
                    return true;
                }
                _lightService.Error -= OnServiceError;
                _lightService = null;
                return false;
            }
            return true;
        }

        private async Task<bool> ConnectButtonService()
        {
            if (_buttonService == null)
            {
                _buttonService = new BleButtonService() { Version = this.version };
                _buttonService.Error += OnServiceError;

                if (await _buttonService.ConnectAsync(deviceInfo.ContainerId))
                {
                    _buttonService.ConnectionChanged += OnConnectionChanged;
                    return true;
                }
                _buttonService.Error -= OnServiceError;
                _buttonService = null;
                return false;
            }
            return true;
        }

        private async Task<bool> ConnectAccelerometerService()
        {
            if (_accelService == null)
            {
                _accelService = new BleAccelerometerService() { Version = this.version };
                _accelService.Error += OnServiceError;

                if (await _accelService.ConnectAsync(deviceInfo.ContainerId))
                {
                    _accelService.ConnectionChanged += OnConnectionChanged;
                    return true;
                }
                _accelService.Error -= OnServiceError;
                _accelService = null;
                return false;
            }
            return true;
        }

        private async Task<bool> ConnectGyroscopeService()
        {
            if (_gyroService == null)
            {
                _gyroService = new BleGyroscopeService() { Version = this.version };
                _gyroService.Error += OnServiceError;

                if (await _gyroService.ConnectAsync(deviceInfo.ContainerId))
                {
                    _gyroService.ConnectionChanged += OnConnectionChanged;
                    return true;
                }

                _gyroService.Error -= OnServiceError;
                _gyroService = null;
                return false;
            }
            return true;
        }

        private async Task<bool> ConnectMagnetometerService()
        {
            if (_magService == null)
            {
                _magService = new BleMagnetometerService() { Version = this.version };
                _magService.Error += OnServiceError;

                if (await _magService.ConnectAsync(deviceInfo.ContainerId))
                {
                    _magService.ConnectionChanged += OnConnectionChanged;
                    return true;
                }
                _magService.Error -= OnServiceError;
                _magService = null;
                return false;
            }
            return true;
        }

        private async Task<bool> ConnectHumidityService()
        {
            if (_humidityService == null)
            {
                _humidityService = new BleHumidityService() { Version = this.version };
                _humidityService.Error += OnServiceError;

                if (await _humidityService.ConnectAsync(deviceInfo.ContainerId))
                {
                    _humidityService.ConnectionChanged += OnConnectionChanged;
                    return true;
                }
                _humidityService.Error -= OnServiceError;
                _humidityService = null;
                return false;
            }
            return true;
        }


        private async Task<bool> ConnectBarometerService()
        {
            if (_barometerService == null)
            {
                _barometerService = new BleBarometerService() { Version = this.version };
                _barometerService.Error += OnServiceError;

                if (await _barometerService.ConnectAsync(deviceInfo.ContainerId))
                {
                    OnStatusChanged("calibrating barometer...");
                    _barometerService.ConnectionChanged += OnConnectionChanged;
                    await _barometerService.StartCalibration();
                    OnStatusChanged("calibrated");
                    return true;
                }
                _barometerService.Error -= OnServiceError;
                _barometerService = null;
                return false;
            }
            return true;
        }

        public event EventHandler<string> ServiceError;

        private void OnServiceError(object sender, string message)
        {
            if (ServiceError != null)
            {
                ServiceError(sender, message);
            }

        }


        public event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;

        void OnConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
            if (ConnectionChanged != null)
            {
                ConnectionChanged(sender, e);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
