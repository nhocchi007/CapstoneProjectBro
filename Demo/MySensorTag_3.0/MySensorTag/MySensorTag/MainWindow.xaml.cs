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
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System.ComponentModel;
using System.Collections;
using System.Data;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace MySensorTag
{
    //Chart
    public class MeasureModel
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        private static double _node1threshold = 40000;
        private static double _node2threshold = 35000;
        private static double _node3threshold = 60000;
        private static int _interval = 1000;//MyTimer_Tick

        #region Notify
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private string _gesture;
        public string Gesture
        {
            get
            {
                return this._gesture;
            }

            set
            {
                if (value != this._gesture)
                {
                    this._gesture = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();



            #region Display on Chart

            Accelerate_XValues = new ChartValues<ObservableValue> { new ObservableValue(0) };
            Accelerate_YValues = new ChartValues<ObservableValue> { new ObservableValue(0) };
            Accelerate_ZValues = new ChartValues<ObservableValue> { new ObservableValue(0) };

            Gyroscope_XValues = new ChartValues<ObservableValue> { new ObservableValue(0) };
            Gyroscope_YValues = new ChartValues<ObservableValue> { new ObservableValue(0) };
            Gyroscope_ZValues = new ChartValues<ObservableValue> { new ObservableValue(0) };

            var A_blueLine = new LineSeries
            {
                Values = Accelerate_YValues,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                PointDiameter = 0,
                Stroke = System.Windows.Media.Brushes.Blue,
            };
            var A_redLine = new LineSeries
            {
                Values = Accelerate_XValues,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                PointDiameter = 0,
                Stroke = System.Windows.Media.Brushes.Red
            };
            var A_greenLine = new LineSeries
            {
                Values = Accelerate_ZValues,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                PointDiameter = 0,
                Stroke = System.Windows.Media.Brushes.Green
            };

            var G_blueLine = new LineSeries
            {
                Values = Gyroscope_YValues,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                PointDiameter = 0,
                Stroke = System.Windows.Media.Brushes.Blue,
            };

            var G_redLine = new LineSeries
            {
                Values = Gyroscope_XValues,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                PointDiameter = 0,
                Stroke = System.Windows.Media.Brushes.Red,
            };

            var G_greenLine = new LineSeries
            {
                Values = Gyroscope_ZValues,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                PointDiameter = 0,
                Stroke = System.Windows.Media.Brushes.Green,
            };

            AccelerateCollection = new SeriesCollection { A_blueLine, A_redLine, A_greenLine };
            GyroscopeCollection = new SeriesCollection { G_blueLine, G_greenLine, G_redLine };
            DataContext = this;
            #endregion

            FindSensors();

            Console.WriteLine("FIND OK");

            _timer = new Timer() { Interval = _interval, Enabled = true };
            _timer.Tick += new EventHandler(MyTimer_Tick);

            node1List = new List<double>();
            node2List = new List<double>();
            node3List = new List<double>();

            #region Pointer


            #endregion

        }

        #region Get Movement Data

        private async void FindSensors()
        {
            try
            {
                foreach (SensorTag tag in await SensorTag.FindAllDevices())
                {
                    //this.txtDeviceName.Text = tag.DeviceName;
                    //this.txtMacID.Text = tag.IRTemperature.MacAddress.ToString();
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
                        await sensor.Accelerometer.SetPeriod(100); // save battery
                    }
                    if (sensor.Movement != null)
                    {
                        await sensor.Movement.SetPeriod(100);
                        Console.WriteLine("Set period rui");// save battery
                    }
                    if (sensor.Barometer != null)
                    {
                        await sensor.Barometer.SetPeriod(100);
                        //await sensor.Humidity.ConnectAsync();
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

            //await RegisterBarometer(register);
            //await RegisterIRTemperature(register);
            //await RegisterHumidity(register);
            await RegisterEMGNode2(register);
            // await RegisterMovement(register);

            if (sensor.Version == 2)
            {
                //await RegisterMovement(register);
                //await RegisterLightIntensity(register);

            }

        }

        public async Task RegisterMovement(bool register)
        {
            try
            {
                if (register)
                {
                    Console.WriteLine("Move1: " + sensor.DeviceName + " " + sensor.Connected);
                    await sensor.Movement.StartReading(MovementFlags.Accel2G | MovementFlags.AccelX | MovementFlags.AccelY | MovementFlags.AccelZ | MovementFlags.GyroX | MovementFlags.GyroY | MovementFlags.GyroZ | MovementFlags.Mag);

                    sensor.Movement.MovementMeasurementValueChanged -= OnMovementMeasurementValueChanged;
                    sensor.Movement.MovementMeasurementValueChanged += OnMovementMeasurementValueChanged;
                    //sensor.Movement.MovementMeasurementValueChanged += TimerOnTick; 

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

        void OnBarometerMeasurementValueChanged(object sender, BarometerMeasurementEventArgs e)
        {

            var m = e.Measurement;
            //string caption = Math.Round(m.GetUnit(unit), 3);                 
            connected = true;
            Console.WriteLine("YYYYYYYYYYYYYYYYYYYYYYYYYYYYY BARO");
            Console.WriteLine(m.Temperature);
            baroDataCount++;
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

                }
                else
                {

                    await sensor.Barometer.StopReading();
                    sensor.Barometer.BarometerMeasurementValueChanged -= OnBarometerMeasurementValueChanged;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("### Error registering barometer: " + ex.Message);
            }
        }

        void OnMovementMeasurementValueChanged(object sender, MovementEventArgs e)
        {

            try
            {
                var m = e.Measurement;

                #region Accelerate Graph

                string caption = Math.Round(m.AccelX, 3) + "," + Math.Round(m.AccelY, 3) + "," + Math.Round(m.AccelZ, 3);

                Console.WriteLine("XXXXXXXXXXXXXXXXXXXXXXXXXxxxx");
                Console.WriteLine(caption);
                //    Accelerate_XValues.Add(new ObservableValue(Math.Round(m.AccelX, 3)));
                //    if(Accelerate_XValues.Count >10)
                //    {
                //        Accelerate_XValues.RemoveAt(0);
                //    }
                //    Accelerate_YValues.Add(new ObservableValue(Math.Round(m.AccelY, 3)));
                //    if (Accelerate_YValues.Count > 10)
                //    {
                //        Accelerate_YValues.RemoveAt(0);
                //    }
                //    Accelerate_ZValues.Add(new ObservableValue(Math.Round(m.AccelZ, 3)));
                //    if (Accelerate_ZValues.Count > 10)
                //    {
                //        Accelerate_ZValues.RemoveAt(0);
                //    }

                movementDataCount++;
                //Console.WriteLine("Accelerometer: "+caption);

                #endregion

                #region Pointer

                //double pitchrad = Math.Atan(m.AccelX / Math.Sqrt(m.AccelY * m.AccelY + m.AccelZ * m.AccelZ));
                //double rollrad = Math.Atan(m.AccelY / Math.Sqrt(m.AccelX * m.AccelX + m.AccelZ * m.AccelZ));

                //double pitchdeg = 180 * pitchrad / Math.PI;
                //double rolldeg = 180 * rollrad / Math.PI;
                //double min = -15;
                //double max = 15;
                //int mapX = (int)map((long)pitchdeg, (long)min, (long)max, (long)-6, (long)6);
                //int mapY = (int)map((long)rolldeg, (long)min, (long)max, (long)-6, (long)6);
                //SetCursorPos(System.Windows.Forms.Cursor.Position.X + mapX, System.Windows.Forms.Cursor.Position.Y + mapY);
                //Console.WriteLine("Cursor: " + mapX + ", " + mapY);
                #endregion

                #region Gyroscope
                //Gyroscope_XValues.Add(new ObservableValue(Math.Round(m.GyroX, 3)));
                //    if (Gyroscope_XValues.Count > 10)
                //    {
                //        Gyroscope_XValues.RemoveAt(0);
                //    }
                //    Gyroscope_YValues.Add(new ObservableValue(Math.Round(m.GyroY, 3)));
                //    if (Gyroscope_YValues.Count > 10)
                //    {
                //        Gyroscope_YValues.RemoveAt(0);
                //    }
                //    Gyroscope_ZValues.Add(new ObservableValue(Math.Round(m.GyroZ, 3)));
                //    if (Gyroscope_ZValues.Count > 10)
                //    {
                //        Gyroscope_ZValues.RemoveAt(0);
                //    } 
                //    caption = Math.Round(m.GyroX, 3) + "," + Math.Round(m.GyroY, 3) + "," + Math.Round(m.GyroZ, 3);

                // Console.WriteLine("Gyroscope: " + caption);
                #endregion


                caption = Math.Round(m.MagX, 3) + "," + Math.Round(m.MagY, 3) + "," + Math.Round(m.MagZ, 3);

                // Console.WriteLine("Magnetometer: " + caption);
                connected = true;
                //Console.WriteLine("----------------------------------------------------------");


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


        #endregion

        #region Display on Chart

        public ChartValues<ObservableValue> Accelerate_XValues { get; set; }
        public ChartValues<ObservableValue> Accelerate_YValues { get; set; }
        public ChartValues<ObservableValue> Accelerate_ZValues { get; set; }
        public SeriesCollection AccelerateCollection { get; set; }

        public ChartValues<ObservableValue> Gyroscope_XValues { get; set; }
        public ChartValues<ObservableValue> Gyroscope_YValues { get; set; }
        public ChartValues<ObservableValue> Gyroscope_ZValues { get; set; }
        public SeriesCollection GyroscopeCollection { get; set; }

        #endregion

        void MyTimer_Tick(object sender, EventArgs e)
        {

            //myCount++;
            //if(myCount==100)
            //{
            //Console.WriteLine("Time: 1000ms");
            //Console.WriteLine("Data Count EMG1:" + emg1DataCount);
            //Console.WriteLine("Data Count EMG2:" + emg2DataCount);
            //Console.WriteLine("Data Count movement:" + movementDataCount);
            //Console.WriteLine("Data Count baro:" + baroDataCount);
            //Console.WriteLine("COUNT--------------------------");
            //Console.WriteLine(emg1DataCount);
            int i = movementDataCount;
            int j = baroDataCount;
            emg1DataCount = 0;
            emg2DataCount = 0;
            movementDataCount = 0;
            baroDataCount = 0;
            //    myCount = 0;
            //}

            //if (node1List.Count>0)
            //{
            //    double node1Average = node1List.Average();

            //    node1List.Clear();

            //    Console.WriteLine("-----------------------");
            //    Console.WriteLine("Node 1: " + node1Average);


            //    Console.WriteLine("-----------------------");

            //    if (node1Average >= _node1threshold)
            //    {
            //        Gesture = "1";
            //    }

            //}

            //if (node2List.Count > 0)
            //{
            //    double node2Average = node2List.Average();
            //    node2List.Clear();
            //    Console.WriteLine("Node 2: " + node2Average);
            //}
            //if (node3List.Count > 0)
            //{
            //    double node3Average = node3List.Average();
            //    node3List.Clear();
            //    Console.WriteLine("Node 3: " + node3Average);
            //}
        }

        #region Humid

        public async Task RegisterEMGNode2(bool register)
        {
            try
            {
                if (register)
                {
                    await sensor.EMGNode2.StartReading();
                    sensor.EMGNode2.EMGNode2MeasurementValueChanged -= OnEMGNode2MeasurementValueChanged;
                    sensor.EMGNode2.EMGNode2MeasurementValueChanged += OnEMGNode2MeasurementValueChanged;
                    Console.WriteLine("EMGNode2 OK");
                }
                else
                {
                    await sensor.EMGNode2.StopReading();
                    sensor.EMGNode2.EMGNode2MeasurementValueChanged -= OnEMGNode2MeasurementValueChanged;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("### Error registering Humidity: " + ex.Message);
            }
        }


        public async Task RegisterHumidity(bool register)
        {
            try
            {
                if (register)
                {
                    await sensor.Humidity.StartReading();
                    sensor.Humidity.HumidityMeasurementValueChanged -= OnHumidityMeasurementValueChanged;
                    sensor.Humidity.HumidityMeasurementValueChanged += OnHumidityMeasurementValueChanged;
                    Console.WriteLine("Humid OK");
                }
                else
                {
                    await sensor.Humidity.StopReading();
                    sensor.Humidity.HumidityMeasurementValueChanged -= OnHumidityMeasurementValueChanged;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("### Error registering Humidity: " + ex.Message);
            }
        }


        void OnEMGNode2MeasurementValueChanged(object sender, EMGNode2MeasurementEventArgs e)
        {
            var m = e.Measurement;

            connected = true;

            Console.WriteLine("EMGNode2---------------------------");
            //double avg1 = (m.Data0 + m.Data1 + m.Data2 + m.Data3 + m.Data4 + m.Data5 + m.Data6 + m.Data7 + m.Data8 + m.Data9) / 10;
            //double avg2 = (m.Data3 + m.Data4 + m.Data5) / 3;
            //double avg3 = (m.Data5 + m.Data6 + m.Data7 + m.Data8 + m.Data9) / 5;
            //double ca3 = (avg1 + avg2 + avg3) / 3;

            double avg1 = (m.Data0 + m.Data1 + m.Data2 ) / 3;
            double avg2 = (m.Data3 + m.Data4 + m.Data5) / 3;
            double avg3 = (m.Data6 + m.Data7 + m.Data8) / 3;

            Console.WriteLine(avg1);
            Console.WriteLine(avg2);
            Console.WriteLine(avg3);

            //if (avg1 > 10000)
            //{
            //    Console.WriteLine("----------wave ra--------");
            //}
            //if (avg2 > 10000)
            //{
            //    Console.WriteLine("---------Nam tay---------");
            //}
            //if (avg3 > 10000)
            //{
            //    Console.WriteLine("---------Wave vao---------");
            //}
            //if (avg3 > 40000)
            //{
            //    Console.WriteLine("-------Banh tay----------");
            //}
            //Console.WriteLine(avg2);
            //Console.WriteLine(avg3);

            //Console.WriteLine(m.Data0);
            //Console.WriteLine(m.Data1);
            //Console.WriteLine(m.Data2);
            //Console.WriteLine(m.Data3);
            //Console.WriteLine(m.Data4);
            //Console.WriteLine(m.Data5);
            //Console.WriteLine(m.Data6);
            //Console.WriteLine(m.Data7);
            //Console.WriteLine(m.Data8);
            //Console.WriteLine(m.Data9);
            //Console.WriteLine(m.Data10);
            //Console.WriteLine(m.Data11);
            //Console.WriteLine(m.Data12);
            //Console.WriteLine(m.Data13);
            //Console.WriteLine(m.Data14);
            //Accelerate_XValues.Add(new ObservableValue(Math.Round(m.Data0, 3)));
            //Accelerate_XValues.Add(new ObservableValue(Math.Round(m.Data1, 3)));
            //Accelerate_XValues.Add(new ObservableValue(Math.Round(m.Data2, 3)));
            //Accelerate_XValues.Add(new ObservableValue(Math.Round(m.Data3, 3)));
            //Accelerate_XValues.Add(new ObservableValue(Math.Round(m.Data4, 3)));
            //Accelerate_XValues.Add(new ObservableValue(Math.Round(m.Data5, 3)));
            //Accelerate_XValues.Add(new ObservableValue(Math.Round(m.Data6, 3)));
            //Accelerate_XValues.Add(new ObservableValue(Math.Round(m.Data7, 3)));
            //Accelerate_XValues.Add(new ObservableValue(Math.Round(m.Data8, 3)));
            //Accelerate_XValues.Add(new ObservableValue(Math.Round(m.Data9, 3)));
            //if (Accelerate_XValues.Count > 100)
            //{
            //    Accelerate_XValues.RemoveAt(0);
            //    Accelerate_XValues.RemoveAt(1);
            //    Accelerate_XValues.RemoveAt(2);
            //    Accelerate_XValues.RemoveAt(3);
            //    Accelerate_XValues.RemoveAt(4);
            //    Accelerate_XValues.RemoveAt(5);
            //    Accelerate_XValues.RemoveAt(6);
            //    Accelerate_XValues.RemoveAt(7);
            //    Accelerate_XValues.RemoveAt(8);
            //    Accelerate_XValues.RemoveAt(9);
            //}
            emg2DataCount++;

        }

        void OnHumidityMeasurementValueChanged(object sender, HumidityMeasurementEventArgs e)
        {

            var m = e.Measurement;

            connected = true;
            Console.WriteLine("EMG NEK------------------------");
            for (int i = 0; i < m.EMGDataList.Count; i++)
            {

                Console.WriteLine(m.EMGDataList.ElementAt(i));

            }






            emg1DataCount++;




        }
        #endregion

        #region Pointer


        long map(long x, long in_min, long in_max, long out_min, long out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        #endregion

        private Timer _timer;
        int emg1DataCount = 0;
        int emg2DataCount = 0;
        int movementDataCount = 0;
        int baroDataCount = 0;
        List<double> node1List;
        List<double> node2List;
        List<double> node3List;

        private void btnDisconnectMovement_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}
