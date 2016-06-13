using MySensorTag.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.Xaml;


namespace MySensorTag
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            FindSensors();
            Console.WriteLine("FIND OK");
            
        }

        private async void FindSensors()
        {
            try
            {               
                foreach (SensorTag tag in await SensorTag.FindAllDevices())
                {
                    this.sensorTagName.Text = tag.DeviceName;
                    sensor = tag;
                }
                await ConnectSensors();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Find Device Failed");
            }
            
        }

        SensorTag sensor;
        bool registeredConnectionEvents;
        bool connecting;
        private async Task ConnectSensors()
        {
            try
            {
                if (sensor == null)
                {
                    // no paired SensorTag, tell the user 
                    Console.WriteLine("This page should be navigated to with a SensorTag parameter");
                    return;
                }
                if (connecting)
                {
                    return;
                }
                connecting = true;
                if (sensor.Connected || await sensor.ConnectAsync())
                {
                    Console.WriteLine("CONNECT OKKKKKKKK");
                    connected = true;
                    await RegisterEvents(true);
                    if (sensor.Accelerometer != null)
                    {
                        await sensor.Accelerometer.SetPeriod(1000); // save battery
                    }
                    if (sensor.Movement != null)
                    {
                        await sensor.Movement.SetPeriod(1000); // save battery
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Connect failed, please ensure sensor is on and is not in use on another machine.");
                Console.WriteLine(ex.Message);
            }
            connecting = false;
        }

        public async Task RegisterEvents(bool register)
        {
            // these ones we always listen to.
            if (!registeredConnectionEvents)
            {
                registeredConnectionEvents = true;
                sensor.ServiceError += OnServiceError;
                sensor.StatusChanged += OnStatusChanged;
                sensor.ConnectionChanged += OnConnectionChanged;
            }

            await RegisterBarometer(register);
            //await RegisterIRTemperature(register);
            //await RegisterHumidity(register);

            if (sensor.Version == 2)
            {
                await RegisterMovement(register);
                //await RegisterLightIntensity(register);
            }

        }

        void OnBarometerMeasurementValueChanged(object sender, BarometerMeasurementEventArgs e)
        {
            
                var m = e.Measurement;
                Console.WriteLine("Baro: "+m);
                //var unit = (PressureUnit)Settings.Instance.PressureUnit;

                //string caption = Math.Round(m.GetUnit(unit), 3) + " " + pressureSuffixes[(int)unit];

                //GetTile("Barometer").SensorValue = caption;
                connected = true;
           
        }

        public async Task RegisterBarometer(bool register)
        {
            try
            {
                if (register)
                {
                    await sensor.Barometer.StartReading();
                    sensor.Barometer.BarometerMeasurementValueChanged -= OnBarometerMeasurementValueChanged;
                    sensor.Barometer.BarometerMeasurementValueChanged += OnBarometerMeasurementValueChanged;
                   // AddTile(new TileModel() { Caption = "Barometer", Icon = new BitmapImage(new Uri("ms-appx:/Assets/Barometer.png")) });
                }
                else
                {
                    //RemoveTiles(from t in tiles where t.Caption == "Barometer" select t);
                    await sensor.Barometer.StopReading();
                    sensor.Barometer.BarometerMeasurementValueChanged -= OnBarometerMeasurementValueChanged;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("### Error registering barometer: " + ex.Message);
            }
        }

        public async Task RegisterMovement(bool register)
        {
            try
            {
                if (register)
                {
                    Console.WriteLine("Move1: "+sensor.DeviceName+" "+sensor.Connected);
                    await sensor.Movement.StartReading(MovementFlags.Accel2G | MovementFlags.AccelX | MovementFlags.AccelY | MovementFlags.AccelZ | MovementFlags.GyroX | MovementFlags.GyroY | MovementFlags.GyroZ | MovementFlags.Mag);

                    sensor.Movement.MovementMeasurementValueChanged -= OnMovementMeasurementValueChanged;
                    sensor.Movement.MovementMeasurementValueChanged += OnMovementMeasurementValueChanged;

              
                }
                else
                {

                    await sensor.Movement.StopReading();
                    sensor.Movement.MovementMeasurementValueChanged -= OnMovementMeasurementValueChanged;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("### Error registering Movement: " + ex.Message);
            }
        }

        void OnMovementMeasurementValueChanged(object sender, MovementEventArgs e)
        {
                        
                try
                {
                    var m = e.Measurement;
                    string caption = Math.Round(m.AccelX, 3) + "," + Math.Round(m.AccelY, 3) + "," + Math.Round(m.AccelZ, 3);
                   
                    Console.WriteLine("Accelerometer: "+caption);
                    caption = Math.Round(m.GyroX, 3) + "," + Math.Round(m.GyroY, 3) + "," + Math.Round(m.GyroZ, 3);
                    
                    Console.WriteLine("Gyroscope: " + caption);
                    caption = Math.Round(m.MagX, 3) + "," + Math.Round(m.MagY, 3) + "," + Math.Round(m.MagZ, 3);
                    
                    Console.WriteLine("Magnetometer: " + caption);
                    connected = true;
                }
                catch
                {
                }
            
        }


        void OnServiceError(object sender, string message)
        {
            Console.WriteLine(message);
        }

        void OnStatusChanged(object sender, string status)
        {
            Console.WriteLine(status);
        }

        bool active;
        bool connected;
        void OnConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
            if (e.IsConnected != connected)
            {
                string message = null;
                if (e.IsConnected)
                {
                    message = "connected";
                }
                else if (connected)
                {
                    message = "lost connection";
                }

                if (!e.IsConnected)
                {
                    Console.WriteLine("Not connected");
                }

                Console.WriteLine(message);
            }
            connected = e.IsConnected;
        }
    }
}
