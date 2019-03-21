using System;
using System.IO.Ports;
using System.Linq;

namespace Sipp_PC.Sensor
{
    class Sipp9X : SippThing
    {
        public const int ACCELERATION_SCALE_2G = 0x00;
        public const int ACCELERATION_SCALE_4G = 0x01;
        public const int ACCELERATION_SCALE_8G = 0x02;

        private Byte[] serviceUuid = { 0x00, 0x16 };
        private Byte[] measurementUuid = { 0x01, 0x16 };
        private Byte[] currentChar;

        private UInt16 att_handle_measurement = 0;       // Measurement attribute handle
        private UInt16 att_handle_measurement_ccc = 0;   // Measurement characteristic configuration handle (to enable notifications)

        internal int accelerometerFSR = 16;
        internal int gyroFSR = 500;
        internal double x1 = 0;

        internal Sipp9X(SerialPort serialApi) : base(serialApi) {
            SetGroupFoundHandler(new Bluegiga.BLE.Events.ATTClient.GroupFoundEventHandler(this.ATTClientGroupFoundEvent));
            SetInformationFoundHandler(new Bluegiga.BLE.Events.ATTClient.FindInformationFoundEventHandler(this.ATTClientFindInformationFoundEvent));
            SetProcedureCompleteHandler(new Bluegiga.BLE.Events.ATTClient.ProcedureCompletedEventHandler(this.ATTClientProcedureCompletedEvent));
            SetAttributeValueHandler(new Bluegiga.BLE.Events.ATTClient.AttributeValueEventHandler(this.ATTClientAttributeValueEvent));
        }

        override public string ThingType() { return "Sipp9X"; }
        public void ATTClientAttributeValueEvent(object sender, Bluegiga.BLE.Events.ATTClient.AttributeValueEventArgs e)
        {

            // check for a new value from the connected peripheral's heart rate measurement attribute
            if (e.connection == GetConnectionHandle() && e.atthandle == att_handle_measurement)
            {
                aX = (e.value[1] * 256 + e.value[2]) * accelerometerFSR / Math.Pow(2, 16);
                aY = (e.value[3] * 256 + e.value[4]) * accelerometerFSR / Math.Pow(2, 16);
                aZ = (e.value[5] * 256 + e.value[6]) * accelerometerFSR / Math.Pow(2, 16);
                gX = (e.value[7] * 256 + e.value[8]) * gyroFSR / Math.Pow(2, 16);
                gY = (e.value[9] * 256 + e.value[10]) * gyroFSR / Math.Pow(2, 16);
                gZ = (e.value[11] * 256 + e.value[12]) * gyroFSR / Math.Pow(2, 16);
                AccelerationValue = new Acceleration(aX, aY, aZ);
                AngularvelocityValue = new AngularVelocity(gX, gY, gZ);
                double x0 = aX * aX + aY * aY + aZ * aZ;
              
                if (x0 - x1 > 290)
                {
                    if (originalStep < 0)
                    {
                        originalStep = step;
                    }
                    currentStep = step - originalStep;

                    PedometerValue = new PedometerData(step, calorie, distance, duration, currentStep);
                    step = step + 1;
                }
                x1 = x0;
            }
        }

        private void ATTClientProcedureCompletedEvent(object sender, Bluegiga.BLE.Events.ATTClient.ProcedureCompletedEventArgs e)
        {
            String log = String.Format("ble_evt_attclient_procedure_completed: connection={0}, result={1}, chrhandle={2}" + Environment.NewLine,
                e.connection,
                e.result,
                e.chrhandle
                );
            Console.WriteLine("Sipp9X State: {0}", GetState());
            Console.WriteLine(log);

            // check if we just finished searching for services
            if (GetState() == STATE_FINDING_SERVICES)
            {
                if (GetHandleEnd() > 0)
                {

                    Byte[] cmd = NewFindInfoCmd(e.connection); 
                    // DEBUG: display bytes written
                    Console.WriteLine(String.Format("=> TX ({0}) [ {1}]", cmd.Length, ByteArrayToHexString(cmd)) + Environment.NewLine);

                    SendCmd(cmd);
                    //while (bglib.IsBusy()) ;

                    // update state
                    SetState(STATE_FINDING_ATTRIBUTES);
                }
                else
                {
                    Console.WriteLine("Could not find service" + Environment.NewLine);

                }
            }
            // check if we just finished searching for attributes within the heart rate service
            else if (GetState() == STATE_ENABLE_NOTIFICATION)
            {
                SetState(STATE_READY);
            }
            else if (GetState() == STATE_FINDING_ATTRIBUTES)
            {
                if (att_handle_measurement_ccc > 0)
                {

                    // found the measurement + client characteristic configuration, so enable notifications
                    // (this is done by writing 0x0001 to the client characteristic configuration attribute)
                    Byte[] cmd = NewWriteAttributeCmd(e.connection, att_handle_measurement_ccc, new Byte[] { 0x01, 0x00 });
                    // DEBUG: display bytes written
                    Console.WriteLine(String.Format("=> TX ({0}) [ {1}]", cmd.Length, ByteArrayToHexString(cmd)) + Environment.NewLine);

                    SendCmd(cmd);
                    //while (bglib.IsBusy()) ;

                    // update state
                    SetState(STATE_ENABLE_NOTIFICATION);

                }
                else
                {
                    Console.WriteLine(String.Format("Could not find 'Sipp6X motion measurement' attribute with UUID {0}", att_handle_measurement_ccc) + Environment.NewLine);
                }
            }
        }

        private void ATTClientFindInformationFoundEvent(object sender, Bluegiga.BLE.Events.ATTClient.FindInformationFoundEventArgs e)
        {
            String log = String.Format("ble_evt_attclient_find_information_found: connection={0}, chrhandle={1}, uuid=[ {2}]" + Environment.NewLine,
                e.connection,
                e.chrhandle,
                ByteArrayToHexString(e.uuid)
                );
            Console.WriteLine(log);


            if (e.uuid.SequenceEqual(measurementUuid))
            {
                Console.WriteLine(String.Format("Found measurement attribute w/UUID={0}: handle={1}", ByteArrayToHexString(e.uuid), e.chrhandle) + Environment.NewLine);
                att_handle_measurement = e.chrhandle;
                currentChar = e.uuid;
            }
            // check for subsequent client characteristic configuration (UUID=0x2902)
            else if (e.uuid.SequenceEqual(new Byte[] { 0x02, 0x29 }) && currentChar != null && currentChar.SequenceEqual(measurementUuid))
            {
                Console.WriteLine(String.Format("Found measurement control point attribute w/UUID=0x2902: handle={0}", e.chrhandle) + Environment.NewLine);
                att_handle_measurement_ccc = e.chrhandle;
            }
            else
            {
                currentChar = null;
            }
        }

        private void ATTClientGroupFoundEvent(object sender, Bluegiga.BLE.Events.ATTClient.GroupFoundEventArgs e)
        {
            String log = String.Format("ble_evt_attclient_group_found: connection={0}, start={1}, end={2}, uuid=[ {3}]" + Environment.NewLine,
                e.connection,
                e.start,
                e.end,
                ByteArrayToHexString(e.uuid)
                );
            Console.WriteLine("Sipp9X: " + log);

            if (e.uuid.SequenceEqual(serviceUuid))
            {
                Console.WriteLine(String.Format("Found attribute group for service w/UUID={2}: start={0}, end={1}", e.start, e.end, e.uuid) + Environment.NewLine);

                SetHandleRange(e.start, e.end);
            }
        }

        public override byte[] MyConnectCommand(byte[] addr)
        {
            return GetConnectCmd(addr, 1, 0x006, 0x100, 0x0064, 0);
        }
        public override void Dispose()
        {
            UnsetGroupFoundHandler(new Bluegiga.BLE.Events.ATTClient.GroupFoundEventHandler(this.ATTClientGroupFoundEvent));
            UnsetInformationFoundHandler(new Bluegiga.BLE.Events.ATTClient.FindInformationFoundEventHandler(this.ATTClientFindInformationFoundEvent));
            UnsetProcedureCompleteHandler(new Bluegiga.BLE.Events.ATTClient.ProcedureCompletedEventHandler(this.ATTClientProcedureCompletedEvent));
            UnsetAttributeValueHandler(new Bluegiga.BLE.Events.ATTClient.AttributeValueEventHandler(this.ATTClientAttributeValueEvent));
        }
    }
}
