using Google.Apis.Firestore.v1beta1;
using Google.Apis.Firestore.v1beta1.Data;
using Google.Apis.Services;
using Microsoft.Owin.Hosting;
using Sipp_PC.Sensor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Google.Apis.Firestore.v1beta1.ProjectsResource.DatabasesResource;

namespace Sipp_PC
{

    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private Scratch scratch = new Scratch();
        private DongleDialog dlg;

        private BackgroundWorker queryWorker;
        private DocumentsResource data;

        private Timer connectTimer;

        private Boolean userDisconnect = false;
        private ManagementEventWatcher insertWatcher;
        private ManagementEventWatcher removeWatcher;
        private string thingType = "Sipp3XII";
        protected SerialPort serialAPI;
        
        private string port;
        private static readonly Guid BLED112 = new Guid("{4d36e978-e325-11ce-bfc1-08002be10318}");

        public MainWindow()
        {
            String thisprocessname = Process.GetCurrentProcess().ProcessName;

            if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1)
                Close();

            InitializeComponent();
            DataContext = new DataObject();
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.Closing += new CancelEventHandler(MainWindow_Closing);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e) {
            if(SippThing.Instance != null)
                SippThing.Instance.Disconnect();
            CloseSerial();
            insertWatcher.Stop();
            removeWatcher.Stop();
            
        }

        private void CloseSerial()
        {
            if (serialAPI == null || !serialAPI.IsOpen)
                return;

            serialAPI.Close();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string address = "http://localhost:8080";

            // Start OWIN host
            try
            {
                var proxy = WebApp.Start<SippServer>(url: address);
            }
            catch (TargetInvocationException ex) {
                System.Diagnostics.Process.Start("http://127.0.0.1:8080/");
                MessageBox.Show("Please close the other application that bind 8080 port.\n For example, NI application web server.");
                Environment.Exit(0);
            }

            // load MAC
            try
            {
                var MAC = "";
                using (CINI oTINI = new CINI(Path.Combine(System.Windows.Forms.Application.StartupPath, "Setup.ini")))
                {
                    string sResult = oTINI.getKeyValue("Sensor Data", "MAC"); //Test5： Section；1：Key
                    //this.listBox1.Items.Add(sResult);
                    //MessageBox.Show(sResult);
                    if (sResult != "")
                    {
                        SippDevice.Text = sResult;
                    }
                    string sResult2 = oTINI.getKeyValue("Sensor Data", "Type"); //Test5： Section；1：Key
                    //this.listBox1.Items.Add(sResult);
                    if (sResult2 != "")
                    {
                        //SippDevice.Text = sResult;
                        if (sResult2 == "3x")
                        {
                            Type3XII.IsChecked = true;
                        }
                        if (sResult2 == "6x")
                        {
                            Type6X.IsChecked = true;
                        }
                        if (sResult2 == "9x")
                        {
                            Type9X.IsChecked = true;
                        }
                    }
                }
            }
            catch (TargetInvocationException ex)
            {
                System.Diagnostics.Process.Start("http://127.0.0.1:8080/");
                MessageBox.Show("Please close the other application that bind 8080 port.\n For example, NI application web server.");
                Environment.Exit(0);
            }

            FirestoreService firestoreService = new FirestoreService(new BaseClientService.Initializer
                {
                    ApplicationName = "Sipp-PC",
                    ApiKey = "AIzaSyCpPmG-AYByk3hircLVnAY_MLcp5ytIsAI",
                }
            );

            data = firestoreService.Projects.Databases.Documents;
            queryWorker = new BackgroundWorker();
            queryWorker.DoWork += new DoWorkEventHandler(DoJob);
            
            scratch.Fetch();

            CheckSystemStatus();
            MonitorInsertEvent();
            MonitorRemoveEvent();
        }

        private void MonitorInsertEvent()
        {

            string queryStr =
                "SELECT * FROM __InstanceCreationEvent " +
                "WITHIN 2 "
              + "WHERE TargetInstance ISA 'Win32_PnPEntity'";

            insertWatcher = new ManagementEventWatcher(queryStr);
            insertWatcher.EventArrived += (sender, args) =>
            {

                ManagementBaseObject instance = (ManagementBaseObject)args.NewEvent["TargetInstance"];

                // we're only interested by USB devices, dump all props
                foreach (var property in instance.Properties)
                {
                    Console.WriteLine(property.Name + " = " + property.Value);
                }


                if (new Guid((string)instance["ClassGuid"]) != BLED112)
                    return;

                Console.WriteLine("Inserted");
                Application.Current.Dispatcher.BeginInvoke((Action)(() => { CheckSystemStatus(); }));
            };
            insertWatcher.Start();
        }

        //Owan, check dongle and connect
        private void CheckSystemStatus()
        {
            port = FindBled112();
            Console.WriteLine("BLED112 port: " + port);
            if (port != null)
            {
                
                
                CloseWarningDialog();
                EnableConnect();
                
                SystemStatus.Content = "Standby";
            }
            else
            {
                
                ShowNoDongleWarning();
                SystemStatus.Content = "Dongle is not present";
            }
        }

        private void CloseWarningDialog() {
            if (dlg != null)
            {
                Console.WriteLine("Close window");
                dlg.HasDongle();
                dlg.Close();
                dlg = null;
            }
        }

        private void ShowNoDongleWarning() {
            if (dlg != null)
                return;

            // Instantiate the dialog box
            dlg = new DongleDialog();

            // Configure the dialog box
            dlg.Owner = this;

            // Open the dialog box modally 
            dlg.Show();

        }
        private void EnableConnect() {
            ThingsSelector.Visibility = Visibility.Visible;
            ConnectBtn.IsEnabled = true;
        }

        private string FindBled112()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort"))
            {

                string device = @"USB\VID_2458";
                string[] portnames = SerialPort.GetPortNames();
                var ports = searcher.Get().Cast<ManagementBaseObject>().ToList();
                var tList = (from n in portnames
                             join p in ports on n equals p["DeviceID"].ToString()
                             where p["PNPDeviceID"].ToString().StartsWith(device)
                             select n).ToList();

                if (tList.Count > 0)
                    return tList[0];
                else
                    return null;
            }
        }

        private void InitSerial()
        {
            if (serialAPI != null && serialAPI.IsOpen)
                return;

            this.serialAPI = new SerialPort();
            if (serialAPI == null) {
                return;
            }
            serialAPI.PortName = port;
            try
            {
                serialAPI.Open();
            }
            catch (UnauthorizedAccessException uae)
            {
                MessageBox.Show(uae.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException ioe)
            {
                MessageBox.Show(ioe.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (InvalidOperationException ioe)
            {
                MessageBox.Show(ioe.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MonitorRemoveEvent()
        {
            removeWatcher = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");
            removeWatcher.EventArrived += (sender, args) =>
            {
               
                Console.WriteLine("Removed");
                Application.Current.Dispatcher.BeginInvoke((Action)(() => { CheckSystemStatus(); }));
            };
            removeWatcher.Query = query;
            removeWatcher.Start();
        }

        private void RegisterLister() {
            SippThing.SystemEventHandler += new SippThing.SystemStatusHandler(this.SippSystemStatus);
            SippThing.Instance.PedometerEvent += new SippThing.PedometerDataHandler(this.PedometerDataHandler);
            SippThing.Instance.AccelerometerEvent += new SippThing.AccelerationHandler(this.AccelerationHandler);
            SippThing.Instance.GyroEvent += new SippThing.AngularVelocityHandler(this.AngularVelocityHandler);
        }

        private void UnregisterListener()
        {
            SippThing.SystemEventHandler -= new SippThing.SystemStatusHandler(this.SippSystemStatus);
            SippThing.Instance.PedometerEvent -= new SippThing.PedometerDataHandler(this.PedometerDataHandler);
            SippThing.Instance.AccelerometerEvent -= new SippThing.AccelerationHandler(this.AccelerationHandler);
            SippThing.Instance.GyroEvent -= new SippThing.AngularVelocityHandler(this.AngularVelocityHandler);
        }

        private void AccelerationHandler(object sender, SippThing.Acceleration acceleration) {
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    aX.Content = Math.Round(acceleration.x, 2);
                    aY.Content = Math.Round(acceleration.y, 2);
                    aZ.Content = Math.Round(acceleration.z, 2);
                }
            ));
        }

        private void AngularVelocityHandler(object sender, SippThing.AngularVelocity angularVelocity) {
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    gX.Content = Math.Round(angularVelocity.x, 2);
                    gY.Content = Math.Round(angularVelocity.y, 2);
                    gZ.Content = Math.Round(angularVelocity.z, 2);
                }
            ));
        }

        private void PedometerDataHandler(object sender, SippThing.PedometerData pedometerData)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    Stored.Content = pedometerData.step;
                    Current.Content = pedometerData.currentStep;
                }
            ));

            //Double timestamp = DateTime.Now.Millisecond;
            //createThingData(pedometerData, timestamp, sipp3XII.address);
            
            
        }

        private Document GetThing(string address) {
            var getThingReq = data.Get("projects/sipp-1f4c4/databases/(default)/documents/data/step/things/" + address);
            try
            {
                Document doc = getThingReq.Execute();
                if (doc == null)
                {
                    MessageBox.Show("Can't find device");
                    return null;
                }

                return doc;
            }
            catch (Google.GoogleApiException e)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoadingCover.Visibility = Visibility.Hidden;
                    CoverText.Visibility = Visibility.Hidden;
                }));

                MessageBox.Show(e.ToString());
                return null;
            }

        }
        private void CreateThingData(Sipp3XII.PedometerData pedometerData, Double timestamp, string address) {
            Document document = GetThing(address);

            if (document == null) {
                document = new Document();
                Dictionary<string, Value> fields = new Dictionary<string, Value>();
                Value timestampValue = new Value();
                timestampValue.DoubleValue = timestamp;
                fields.Add("lastUpdate", timestampValue);

                Value addressValue = new Value();
                addressValue.StringValue = address;
                fields.Add("address", addressValue);

                document.Fields = fields;
                var createThingReq = data.CreateDocument(document, "projects/sipp-1f4c4/databases/(default)/documents/data/step", "things");
                createThingReq.DocumentId = SippThing.Instance.address;
                document = createThingReq.Execute();
            }

            CreateTimeSlot("step", timestamp, pedometerData.step, "number");
            CreateTimeSlot("calorie", timestamp, pedometerData.calorie, "calorie");
            CreateTimeSlot("distance", timestamp, pedometerData.distance, "meter");
            CreateTimeSlot("duration", timestamp, pedometerData.duration, "minute");
        }

        private void CreateTimeSlot(string type, Double timestamp, int val, string unit) {
            Document document = new Document();
            Dictionary<string, Value> fields = new Dictionary<string, Value>();
            Value timestampValue = new Value();
            timestampValue.DoubleValue =timestamp;
            fields.Add("timestamp", timestampValue);

            Value valueValue = new Value();
            valueValue.IntegerValue = val;
            fields.Add("value", valueValue);

            Value unitValue = new Value();
            unitValue.StringValue = unit;
            fields.Add("unit", unitValue);
            
            var createTimeSlotReq = data.CreateDocument(document, "projects/sipp-1f4c4/databases/(default)/documents/data/" + type + "/things/" + Sipp3XII.Instance.address, "timeSlot");
            createTimeSlotReq.DocumentId = timestamp.ToString();
            createTimeSlotReq.Execute();
        }

        private void SippSystemStatus(object sender, SippThing.SystemState state) {
           
            if (state.val == SippThing.STATE_READY)
            {
                if (connectTimer != null)
                {
                    connectTimer.Dispose();
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {

                    LoadingCover.Visibility = Visibility.Hidden;
                    CoverText.Visibility = Visibility.Hidden;
                    SystemStatus.Content = "Connected";
                    ThingsSelector.Visibility = Visibility.Hidden;
                }));

            }
            else if (state.val == SippThing.STATE_DISCONNECT)
            {
                UnregisterListener();
                SippThing.Instance.Dispose();
                
                if (!userDisconnect)
                {
                    MessageBox.Show("Disconnected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                                
                ShowStandby();
            }
            
        }

        private void ShowStandby() {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ConnectBtn.Content = "Connect";
                ConnectBtn.Background = new SolidColorBrush(Color.FromArgb(255, 129, 232, 129));
                SystemStatus.Content = "Standby";
                ThingsSelector.Visibility = Visibility.Visible;
            }));
        }

        private void DoJob(object sender, DoWorkEventArgs args) {
            var deviceName = ((string)args.Argument).ToUpper();
            var req = data.Get("projects/sipp-1f4c4/databases/(default)/documents/things/lookup/names/" + deviceName);
            try
            {
                Document doc = req.Execute();
                if (doc == null)
                {
                    MessageBox.Show("Can't find device");
                    return;
                }

                Value val = new Value();
                string address = doc.Fields.TryGetValue("address", out val) ? val.StringValue : "";

                if (address.Length == 0)
                {
                    return;
                }
                Console.WriteLine("Address: " + address);
                SippThing.Instance.Connect(address);
            }
            catch (Google.GoogleApiException e)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoadingCover.Visibility = Visibility.Hidden;
                    CoverText.Visibility = Visibility.Hidden;
                }));
                
                MessageBox.Show("Can not find " + deviceName, "Error");
            }
            
        }

        private void SippDevice_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (SippDevice.Text.Equals("Please input device name"))
            {
                SippDevice.Text = "";
            }
        }

        private void SippDevice_MouseDown(object sender, MouseButtonEventArgs e) {
            if (SippDevice.Text.Equals("Please input device name"))
            {
                SippDevice.Text = "";
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (SippThing.Instance == null) {
                return;
            }
            SippThing.Instance.originalStep = SippThing.Instance.step;
            SippThing.Instance.currentStep = 0;
            Current.Content = 0;
        }

        private void Connecting() {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadingCover.Visibility = Visibility.Visible;
                CoverText.Visibility = Visibility.Visible;
                ConnectBtn.Content = "Disconnect";
                ConnectBtn.Background = new SolidColorBrush(Color.FromArgb(255, 232, 138, 129));
            }));
        }

        private void ResetUI() {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadingCover.Visibility = Visibility.Hidden;
                CoverText.Visibility = Visibility.Hidden;
                ConnectBtn.Content = "Connect";
                ConnectBtn.Background = new SolidColorBrush(Color.FromArgb(255, 129, 232, 129));
                
            }));
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            
            if (ConnectBtn.Content.Equals("Connect"))
            {

                CloseSerial();
                InitSerial();
                SippThing.NewInstance(serialAPI, thingType);
                RegisterLister();
                
                Console.WriteLine("Connect to " + SippDevice.Text);
                userDisconnect = false;

                Connecting();
                
                if(SippThing.Instance.GetType() == typeof(Sipp3XII))
                {
                    queryWorker.RunWorkerAsync(SippDevice.Text);
                }
                else {
                    
                    SippThing.Instance.Connect(SippDevice.Text.ToLower());
                    SippThing.Instance.threshold = Threshold.Value;
                    if (th_val != null)
                    {
                        th_val.Content = (Threshold.Value);
                    }
                }

                SetTimer();

            }
            else
            {
                userDisconnect = true;
                UnregisterListener();
                SippThing.Instance.Disconnect();
                
                ResetUI();
            }
        }

       
        private void SetTimer() {
            
            connectTimer = new Timer((Object stateInfo) =>
            {
                MessageBox.Show("Can not connect to device.  Please check is device turned on.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                SippThing.Instance.EndProcedure();
                SippThing.Instance.Disconnect();
                ResetUI();
            }, null, 5000, Timeout.Infinite);
        }

        private void HandleCheck(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            
            if (rb.Name.Equals("Type3XII"))
            {
                thingType = "Sipp3XII";    
            }
            else if (rb.Name.Equals("Type6X"))
            {
                thingType = "Sipp6X";
            }
            else if (rb.Name.Equals("Type9X"))
            {
                thingType = "Sipp9X";
            }
            
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (th_val != null)
            {
                th_val.Content = Math.Round(Threshold.Value, 1);
            }
            if (SippThing.Instance != null) {
                SippThing.Instance.threshold = Threshold.Value;
            }
        }

        private void Scratch_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://scratchx.org/?url=https://nctutwtlab.github.io/scratch/horse/run_my_horse.sbx#scratch");
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

      

        private void ACC_FSR_spinner(object sender, SelectionChangedEventArgs e)
        {
            //ACC_FSR.Items.Add("ACC SCALE 2G");
            //ACC_FSR.Items.Add("ACC SCALE 4G");
            //ACC_FSR.Items.Add("ACC SCALE 8G");
            //ACC_FSR.Items.Add("ACC SCALE 16G");
        }

        private void GYRO_FSR_spinner(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DMP_INIT_spinner(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DMP_GYRO_CAL_spinner(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DMP_GYRO_DATA_spinner(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DMP_ACC_DATA_spinner(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DATA_RATE_spinner(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DATA_TYPE_spinner(object sender, SelectionChangedEventArgs e)
        {

        }

        private void EXEC_CMD_spinner(object sender, SelectionChangedEventArgs e)
        {

        }

        private void COUNT_spinner(object sender, SelectionChangedEventArgs e)
        {

        }

        private void B0_LED_spinner(object sender, SelectionChangedEventArgs e)
        {

        }

        private void B1_LED_spinner(object sender, SelectionChangedEventArgs e)
        {

        }

   
    }
}
