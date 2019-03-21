using System;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;

namespace Sipp_PC.Sensor
{
    abstract class SippThing: IDisposable
    {
        
        internal string name;
        internal int step;
        internal int calorie;
        internal int distance;
        internal int duration;

        internal int currentStep;
        internal int originalStep = -1;

        internal double aX;
        internal double aY;
        internal double aZ;
        internal double gX;
        internal double gY;
        internal double gZ;

        public delegate void PedometerDataHandler(object sender, PedometerData pedometerData);
        
        public class PedometerData : EventArgs
        {
            public readonly int step;
            public readonly int calorie;
            public readonly int distance;
            public readonly int duration;
            public readonly int currentStep;

            internal PedometerData(int step, int calorie, int distance, int duration, int currentStep)
            {
                this.step = step;
                this.calorie = calorie;
                this.distance = distance;
                this.duration = duration;
                this.currentStep = currentStep;
            }
        }

        public event PedometerDataHandler PedometerEvent;


        public class Acceleration : EventArgs
        {
            public readonly double x;
            public readonly double y;
            public readonly double z;

            internal Acceleration(double x, double y, double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        public class AngularVelocity : EventArgs
        {
            public readonly double x;
            public readonly double y;
            public readonly double z;

            internal AngularVelocity(double x, double y, double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        public delegate void AccelerationHandler(object sender, Acceleration acceleration);
        public delegate void AngularVelocityHandler(object sender, AngularVelocity angularVelocity);

        public event AccelerationHandler AccelerometerEvent;
        public event AngularVelocityHandler GyroEvent;
        protected SerialPort serialAPI;
        // Sipp system state
        public const Int16 STATE_IDLE = 0;
        public const Int16 STATE_SCANNING = 1;
        public const Int16 STATE_CONNECTING = 2;
        public const Int16 STATE_FINDING_SERVICES = 3;
        public const Int16 STATE_READING_DATA = 4;
        public const Int16 STATE_ENABLING_PEDOMETER = 5;
        public const Int16 STATE_FINDING_ATTRIBUTES = 6;
        public const Int16 STATE_ENABLE_NOTIFICATION = 7;
        public const Int16 STATE_READY = 8;
        public const Int16 STATE_DISCONNECT = 9;
        public double threshold = 3.5;
        private Int16 AppState {
            get {
                return app_state;
            }
            set {
                app_state = value;
                SystemEventHandler?.Invoke(this, new SystemState(app_state, null));
            }
        }

        private Int16 app_state = STATE_IDLE;        // current application state
        protected void SetState(Int16 state) {
            AppState = state;
        }

        public Int16 GetState() {
            return AppState;
        }

        private PedometerData pedometer_data;

        public PedometerData PedometerValue {
            get {
                return pedometer_data;
            }
            set {
                pedometer_data = value;
                PedometerEvent?.Invoke(this, pedometer_data);
            }
        }

        private Acceleration acceleration;

        public Acceleration AccelerationValue {
            get {
                return acceleration;
            }
            set {
                acceleration = value;
                AccelerometerEvent?.Invoke(this, acceleration);
            }
        }

        private AngularVelocity angularVelocity;

        public AngularVelocity AngularvelocityValue
        {
            get
            {
                return angularVelocity;
            }
            set
            {
                angularVelocity = value;
                GyroEvent?.Invoke(this, angularVelocity);
            }
        }

        private Byte connection_handle = 0;              // connection handle (will always be 0 if only one connection happens at a time)
        
        protected Byte GetConnectionHandle() {
            return connection_handle;
        }
        private UInt16 att_handlesearch_start = 0;       // "start" handle holder during search
        private UInt16 att_handlesearch_end = 0;         // "end" handle holder during search
        protected void SetHandleRange(UInt16 start, UInt16 end) {
            att_handlesearch_start = start;
            att_handlesearch_end = end;
        }

        protected UInt16 GetHandleStart()
        {
            return att_handlesearch_start;
        }

        protected UInt16 GetHandleEnd() {
            return att_handlesearch_end;
        }

        private Bluegiga.BGLib bglib = new Bluegiga.BGLib();
        protected Byte[] NewFindInfoCmd(Byte connection) {
            return bglib.BLECommandATTClientFindInformation(connection, att_handlesearch_start, att_handlesearch_end);
        }
        protected Byte[] NewWriteAttributeCmd(Byte connection, UInt16 attribute, Byte[] value) {
            return bglib.BLECommandATTClientAttributeWrite(connection, attribute, value);
        }
        protected void SendCmd(Byte[] cmd) {
            bglib.SendCommand(serialAPI, cmd);
        }
        protected void SetGroupFoundHandler(Bluegiga.BLE.Events.ATTClient.GroupFoundEventHandler handler) {
            bglib.BLEEventATTClientGroupFound += handler;
        }
        protected void UnsetGroupFoundHandler(Bluegiga.BLE.Events.ATTClient.GroupFoundEventHandler handler)
        {
            bglib.BLEEventATTClientGroupFound -= handler;
        }
        protected void SetInformationFoundHandler(Bluegiga.BLE.Events.ATTClient.FindInformationFoundEventHandler handler) {
            bglib.BLEEventATTClientFindInformationFound += handler;
        }
        protected void UnsetInformationFoundHandler(Bluegiga.BLE.Events.ATTClient.FindInformationFoundEventHandler handler)
        {
            bglib.BLEEventATTClientFindInformationFound -= handler;
        }
        protected void SetProcedureCompleteHandler(Bluegiga.BLE.Events.ATTClient.ProcedureCompletedEventHandler handler) {
            bglib.BLEEventATTClientProcedureCompleted += handler;
        }
        protected void UnsetProcedureCompleteHandler(Bluegiga.BLE.Events.ATTClient.ProcedureCompletedEventHandler handler)
        {
            bglib.BLEEventATTClientProcedureCompleted -= handler;
        }
        protected void SetAttributeValueHandler(Bluegiga.BLE.Events.ATTClient.AttributeValueEventHandler handler) {
            bglib.BLEEventATTClientAttributeValue += handler;
        }
        protected void UnsetAttributeValueHandler(Bluegiga.BLE.Events.ATTClient.AttributeValueEventHandler handler)
        {
            bglib.BLEEventATTClientAttributeValue -= handler;
        }

        protected Byte[] GetConnectCmd(Byte[] address, Byte addr_type, UInt16 conn_interval_min, UInt16 conn_interval_max, UInt16 timeout, UInt16 latency) {
            return bglib.BLECommandGAPConnectDirect(address, addr_type, conn_interval_min, conn_interval_max, timeout, latency);
        }

        public string address;

        private static volatile SippThing instance;
        private static object syncRoot = new Object();
        
        protected SippThing(SerialPort serialAPI) {
            this.serialAPI = serialAPI;
            serialAPI.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            // initialize BGLib events we'll need for this script
            bglib.BLEEventConnectionStatus += new Bluegiga.BLE.Events.Connection.StatusEventHandler(this.ConnectionStatusEvent);
            bglib.BLEEventConnectionDisconnected += new Bluegiga.BLE.Events.Connection.DisconnectedEventHandler(this.DisconnectStatusEvent);

            //InitSelf();
        }
        ~SippThing()
        {
            
        }

        public static SippThing Instance {
            get {
                lock (syncRoot) {
                    return instance;
                }
            }
        }
       
        public static void NewInstance(SerialPort serialApi, String thingType)
        {
            lock (syncRoot)
            {
                if (thingType.Equals("Sipp3XII"))
                {
                    instance = new Sipp3XII(serialApi);
                }
                else if (thingType.Equals("Sipp6X"))
                {
                    instance = new Sipp6X(serialApi);
                }
                else if (thingType.Equals("Sipp9X"))
                {
                    instance = new Sipp9X(serialApi);
                }
            }
        }   

        public delegate void SystemStatusHandler(object sender, SystemState state);
        public class SystemState : EventArgs
        {
            public readonly int val;
            public readonly string message; 
            internal SystemState(int val, string message)
            {
                this.val = val;
                this.message = message;
            }
        }
        public static event SystemStatusHandler SystemEventHandler;

        public abstract Byte[] MyConnectCommand(Byte[] addr);
        public abstract string ThingType();        
        private void DataReceivedHandler(
                                object sender,
                                System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            System.IO.Ports.SerialPort sp = (System.IO.Ports.SerialPort)sender;
            int size = sp.BytesToRead;
            Byte[] inData = new Byte[size];

            // read all available bytes from serial port in one chunk
            sp.Read(inData, 0, size);

            // DEBUG: display bytes read
             //Console.WriteLine(String.Format("<= RX ({0}) [ {1}]", inData.Length, ByteArrayToHexString(inData)) + Environment.NewLine);

            // parse all bytes read through BGLib parser
            for (int i = 0; i < inData.Length; i++)
            {
                bglib.Parse(inData[i]);
                //Console.WriteLine("isData : " + inData[i]);
            }
        }

        private void DisconnectStatusEvent(object sender, Bluegiga.BLE.Events.Connection.DisconnectedEventArgs e)
        {

            Console.WriteLine("Disconnected");
            AppState = STATE_DISCONNECT;
        }
        
        // the "connection_status" event occurs when a new connection is established
        public void ConnectionStatusEvent(object sender, Bluegiga.BLE.Events.Connection.StatusEventArgs e)
        {
            String log = String.Format("ble_evt_connection_status: connection={0}, flags={1}, address=[ {2}], address_type={3}, conn_interval={4}, timeout={5}, latency={6}, bonding={7}" + Environment.NewLine,
                e.connection,
                e.flags,
                ByteArrayToHexString(e.address),
                e.address_type,
                e.conn_interval,
                e.timeout,
                e.latency,
                e.bonding
                );
            Console.Write(log);

            if ((e.flags & 0x05) == 0x05)
            {
                // connected, now perform service discovery
                connection_handle = e.connection;
                Console.WriteLine(String.Format("Connected to {0}", ByteArrayToHexString(e.address)));

                //Owan,
                Byte[] cmd = bglib.BLECommandATTClientReadByGroupType(e.connection, 0x0001, 0xFFFF, new Byte[] { 0x00, 0x28 }); // "service" UUID is 0x2800 (little-endian for UUID uint8array)

                Console.WriteLine(String.Format("=> TX ({0}) [ {1}]", cmd.Length, ByteArrayToHexString(cmd)));                                                                                                                // DEBUG: display bytes written

                // update state
                AppState = STATE_FINDING_SERVICES;
                bglib.SendCommand(serialAPI, cmd);
                //while (bglib.IsBusy()) ;
                Console.WriteLine("Finding service");
            }
        }

        // Convert byte array to "00 11 22 33 44 55 " string
        public string ByteArrayToHexString(Byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2} ", b);
            return hex.ToString();
        }

        internal void Connect(string address)
        {
            
            try
            {
                var tmp = address.Split(':');
                Byte[] addr = new Byte[6];
                for (int i = 0; i < 6; i++)
                {
                    addr[i] = Convert.ToByte(tmp[5 - i], 16);
                }
                Console.WriteLine("Address byte {0}: [{1}]", addr.Length, ByteArrayToHexString(addr));
                Byte[] cmd = MyConnectCommand(addr);

                bglib.SendCommand(serialAPI, cmd);
                this.address = address;
            }
            catch (FormatException e)
            {
                MessageMAC();
                return;
            }
            catch (IndexOutOfRangeException e)
            {
                MessageMAC();
                return;
            }
            catch (ArgumentOutOfRangeException e)
            {
                MessageMAC();
                return;
            }

        }

        internal void MessageMAC()
        {
            MessageBox.Show("The MAC address only contains A ~ F and 0 ~ 9.\nPlease enter the correct MAC address like AA:BB:CC:DD:EE:FF");
        }
        internal void Disconnect()
        {
            if (GetState() == STATE_DISCONNECT)
                return;

            Byte[] cmd = bglib.BLECommandConnectionDisconnect(connection_handle);
            bglib.SendCommand(serialAPI, cmd);
        }

        

        internal void EndProcedure()
        {
            Byte[] cmd = bglib.BLECommandGAPEndProcedure();
            bglib.SendCommand(serialAPI, cmd);
        }

        public abstract void Dispose();
    }
}
