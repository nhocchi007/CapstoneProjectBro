using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Buffer = Windows.Storage.Streams.Buffer;

namespace MySensorTag
{
    /// <summary>
    /// This class provides access to the SensorTag Humidity BLE data.  The driver for this sensor is using a state machine 
    /// so when the enable command is issued, the sensor starts to perform one measurements and the data is stored. To obtain 
    /// the data either use notifications or read the data directly. The humidity and temperature data in the sensor is issued 
    /// and measured explicitly where the humidity data takes ~64ms to measure. The update rate is one second.
    /// </summary>
    public class BleHumidityService : BleGenericGattService
    {

        public BleHumidityService()
        {
        }

        /// <summary>
        /// The version of the SensorTag device.  1=CC2541, 2=CC2650.
        /// </summary>
        public int Version { get; set; }

        public static Guid HumidityServiceUuid = Guid.Parse("f0001130-0451-4000-b000-000000000000");
        static Guid HumidityCharacteristicUuid = Guid.Parse("f0001131-0451-4000-b000-000000000000");
        static Guid HumidityCharacteristicConfigUuid = Guid.Parse("f0001132-0451-4000-b000-000000000000");

        // Period is only supported on version 2
        static Guid HumidityCharacteristicPeriodUuid = Guid.Parse("f0001133-0451-4000-b000-000000000000");

        Delegate _humidityValueChanged;

        public event EventHandler<HumidityMeasurementEventArgs> HumidityMeasurementValueChanged
        {
            add
            {
                if (_humidityValueChanged != null)
                {
                    _humidityValueChanged = Delegate.Combine(_humidityValueChanged, value);
                }
                else
                {
                    _humidityValueChanged = value;
                    RegisterForValueChangeEvents(HumidityCharacteristicUuid);
                }
            }
            remove
            {
                if (_humidityValueChanged != null)
                {
                    _humidityValueChanged = Delegate.Remove(_humidityValueChanged, value);
                    if (_humidityValueChanged == null)
                    {
                        UnregisterForValueChangeEvents(HumidityCharacteristicUuid);
                    }
                }
            }
        }

        private async Task<int> GetConfig()
        {
            var ch = GetCharacteristic(HumidityCharacteristicConfigUuid);
            if (ch != null)
            {
                var properties = ch.CharacteristicProperties;

                if ((properties & GattCharacteristicProperties.Read) != 0)
                {
                    byte value = await ReadCharacteristicByte(HumidityCharacteristicConfigUuid, Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached);
                    Debug.WriteLine("Humidity config = " + value);
                    return (int)value;
                }
            }
            return -1;
        }

        bool isReading;

        public async Task StartReading()
        {
            if (!isReading)
            {
                await WriteCharacteristicByte(HumidityCharacteristicConfigUuid, 1);
                isReading = true;
            }
        }

        public async Task StopReading()
        {
            if (isReading)
            {
                isReading = false;
                await WriteCharacteristicByte(HumidityCharacteristicConfigUuid, 0);
            }
        }


        /// <summary>
        /// Get the rate at which humidity is being polled, in milliseconds.  
        /// This is only supported on Version 2 of the sensor
        /// </summary>
        /// <returns>Returns the value read from the sensor or -1 if something goes wrong.</returns>
        public async Task<int> GetPeriod()
        {
            if (Version == 2)
            {
                byte v = await ReadCharacteristicByte(HumidityCharacteristicPeriodUuid, Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached);
                return (int)(v * 10);
            }
            return 1000;
        }

        /// <summary>
        /// Set the rate at which humidity is being polled, in milliseconds.  
        /// </summary>
        /// <param name="milliseconds">The delay between updates, accurate only to 10ms intervals. Maximum value is 2550.</param>
        public async Task SetPeriod(int milliseconds)
        {
            if (Version == 2)
            {
                int delay = milliseconds / 10;
                byte p = (byte)delay;
                if (p < 1)
                {
                    p = 1;
                }

                await WriteCharacteristicByte(HumidityCharacteristicPeriodUuid, p);
            }
        }

        private void OnHumidityMeasurementValueChanged(HumidityMeasurementEventArgs args)
        {
            if (_humidityValueChanged != null)
            {
                ((EventHandler<HumidityMeasurementEventArgs>)_humidityValueChanged)(this, args);
            }
        }

        public async Task<bool> ConnectAsync(string deviceContainerId)
        {
            return await this.ConnectAsync(HumidityServiceUuid, deviceContainerId);
        }

        protected override void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
        {
            if (sender.Uuid == HumidityCharacteristicUuid)
            {
                if (_humidityValueChanged != null)
                {
                    uint dataLength = eventArgs.CharacteristicValue.Length;
                    Console.WriteLine("DataLength:" +dataLength);
                    Console.WriteLine("Vo EMG ne");
                    using (DataReader reader = DataReader.FromBuffer(eventArgs.CharacteristicValue))
                    {
                        var measurement = new HumidityMeasurement();
                        if (dataLength>0)
                        {
                            for(int i=0; i<dataLength/2;i++)
                            {
                                ushort tmp = ReadBigEndianU16bit(reader);
                                measurement.EMGDataList.Add(tmp);
                            }
                            
                        }
                        OnHumidityMeasurementValueChanged(new HumidityMeasurementEventArgs(measurement, eventArgs.Timestamp));
                         
                    }
                }
            }
        }
    }
   

    public class HumidityMeasurement
    {
        /// <summary>
        /// Relative humidity (%RH)
        /// </summary>
        
        public List<double> EMGDataList { get; set; }

        public HumidityMeasurement()
        {
            EMGDataList = new List<double>();
        }


        
    }

    public class HumidityMeasurementEventArgs : EventArgs
    {
        public HumidityMeasurementEventArgs(HumidityMeasurement measurement, DateTimeOffset timestamp)
        {
            Measurement = measurement;
            Timestamp = timestamp;
        }

        public HumidityMeasurement Measurement
        {
            get;
            private set;
        }

        public DateTimeOffset Timestamp
        {
            get;
            private set;
        }
    }

}
