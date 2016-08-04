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
    public class BleEMGNode2Service : BleGenericGattService
    {
        public BleEMGNode2Service()
        {

        }

        public int Version { get; set; }

        public static Guid EMGNode2ServiceUuid = Guid.Parse("f0001140-0451-4000-b000-000000000000");
        static Guid EMGNode2CharacteristicUuid = Guid.Parse("f0001141-0451-4000-b000-000000000000");
        static Guid EMGNode2CharacteristicConfigUuid = Guid.Parse("f0001142-0451-4000-b000-000000000000");

        // Period is only supported on version 2
        static Guid EMGNode2CharacteristicPeriodUuid = Guid.Parse("f0001143-0451-4000-b000-000000000000");

        Delegate _emgNode2ValueChanged;

        public event EventHandler<EMGNode2MeasurementEventArgs> EMGNode2MeasurementValueChanged
        {
            add
            {
                if (_emgNode2ValueChanged != null)
                {
                    _emgNode2ValueChanged = Delegate.Combine(_emgNode2ValueChanged, value);
                }
                else
                {
                    _emgNode2ValueChanged = value;
                    RegisterForValueChangeEvents(EMGNode2CharacteristicUuid);
                }
            }
            remove
            {
                if (_emgNode2ValueChanged != null)
                {
                    _emgNode2ValueChanged = Delegate.Remove(_emgNode2ValueChanged, value);
                    if (_emgNode2ValueChanged == null)
                    {
                        UnregisterForValueChangeEvents(EMGNode2CharacteristicUuid);
                    }
                }
            }
        }
        private async Task<int> GetConfig()
        {
            var ch = GetCharacteristic(EMGNode2CharacteristicConfigUuid);
            if (ch != null)
            {
                var properties = ch.CharacteristicProperties;

                if ((properties & GattCharacteristicProperties.Read) != 0)
                {
                    byte value = await ReadCharacteristicByte(EMGNode2CharacteristicConfigUuid, Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached);
                    Debug.WriteLine("EMGNode2 config = " + value);
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
                await WriteCharacteristicByte(EMGNode2CharacteristicConfigUuid, 1);
                isReading = true;
            }
        }
        public async Task StopReading()
        {
            if (isReading)
            {
                isReading = false;
                await WriteCharacteristicByte(EMGNode2CharacteristicConfigUuid, 0);
            }
        }
        private void OnHumidityMeasurementValueChanged(EMGNode2MeasurementEventArgs args)
        {
            if (_emgNode2ValueChanged != null)
            {
                ((EventHandler<EMGNode2MeasurementEventArgs>)_emgNode2ValueChanged)(this, args);
            }
        }
        public async Task<bool> ConnectAsync(string deviceContainerId)
        {
            return await this.ConnectAsync(EMGNode2ServiceUuid, deviceContainerId);
        }
        protected override void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
        {
            if (sender.Uuid == EMGNode2CharacteristicUuid)
            {
                if (_emgNode2ValueChanged != null)
                {
                    uint dataLength = eventArgs.CharacteristicValue.Length;
                    using (DataReader reader = DataReader.FromBuffer(eventArgs.CharacteristicValue))
                    {
                        if (dataLength == 18)
                        {
                            ushort n0 = ReadBigEndianU16bit(reader);
                            ushort n1 = ReadBigEndianU16bit(reader);
                            ushort n2 = ReadBigEndianU16bit(reader);
                            ushort n3 = ReadBigEndianU16bit(reader);
                            ushort n4 = ReadBigEndianU16bit(reader);
                            ushort n5 = ReadBigEndianU16bit(reader);
                            ushort n6 = ReadBigEndianU16bit(reader);
                            ushort n7 = ReadBigEndianU16bit(reader);
                            ushort n8 = ReadBigEndianU16bit(reader);
                            //ushort n9 = ReadBigEndianU16bit(reader);
                            //ushort n10 = ReadBigEndianU16bit(reader);
                            //ushort n11 = ReadBigEndianU16bit(reader);
                            //ushort n12 = ReadBigEndianU16bit(reader);
                            //ushort n13 = ReadBigEndianU16bit(reader);
                            //ushort n14 = ReadBigEndianU16bit(reader);

                            var measurement = new EMGNode2Measurement();


                            measurement.Data0 = n0;
                            measurement.Data1 = n1;
                            measurement.Data2 = n2;
                            measurement.Data3 = n3;
                            measurement.Data4 = n4;
                            measurement.Data5 = n5;
                            measurement.Data6 = n6;
                            measurement.Data7 = n7;
                            measurement.Data8 = n8;
                            //measurement.Data9 = n9;
                            //measurement.Data10 = n10;
                            //measurement.Data11 = n11;
                            //measurement.Data12 = n12;
                            //measurement.Data13 = n13;
                            //measurement.Data14 = n14;



                            OnHumidityMeasurementValueChanged(new EMGNode2MeasurementEventArgs(measurement, eventArgs.Timestamp));
                        }
                    }
                }
            }
        }
    }


    public class EMGNode2Measurement
    {
        public double Data0 { get; set; }

        /// <summary>
        /// Temperature in Celcius
        /// </summary>
        public double Data1 { get; set; }

        public double Data2 { get; set; }
        public double Data3 { get; set; }
        public double Data4 { get; set; }
        public double Data5 { get; set; }
        public double Data6 { get; set; }
        public double Data7 { get; set; }
        public double Data8 { get; set; }
        public double Data9 { get; set; }
        public double Data10 { get; set; }
        public double Data11 { get; set; }
        public double Data12 { get; set; }
        public double Data13 { get; set; }
        public double Data14 { get; set; }
    
    }

    public class EMGNode2MeasurementEventArgs: EventArgs
    {
        public EMGNode2MeasurementEventArgs(EMGNode2Measurement measurement, DateTimeOffset timestamp)
        {
            Measurement = measurement;
            Timestamp = timestamp;
        }

        public EMGNode2Measurement Measurement
        {
            get; private set;
        }

        public DateTimeOffset Timestamp
        {
            get; private set;
        }
    }
}

