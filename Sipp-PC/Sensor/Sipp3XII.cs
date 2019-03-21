using Sipp_PC.Sensor;
using System;
using System.IO.Ports;
using System.Linq;

namespace Sipp_PC.Sensor
{

    internal class Sipp3XII: SippThing
    {
        // Sipp3XII command
        private const Byte SIPP_02_CMD_09_READ_DATA = 0x07;
        private const Byte SIPP_02_CMD_11_REALTIME = 0x09;

        private UInt16 att_handle_tx = 0;       // TX attribute handle
        private UInt16 att_handle_rx = 0;   // RX attribute handle
        private UInt16 att_handle_rx_ccc = 0;   // RX client characteristic configuration handle (to enable notifications)

        internal Sipp3XII(SerialPort serialApi):base(serialApi) {
            Console.WriteLine("Regiser handler");
            SetGroupFoundHandler(new Bluegiga.BLE.Events.ATTClient.GroupFoundEventHandler(this.ATTClientGroupFoundEvent));
            SetInformationFoundHandler(new Bluegiga.BLE.Events.ATTClient.FindInformationFoundEventHandler(this.ATTClientFindInformationFoundEvent));
            SetProcedureCompleteHandler(new Bluegiga.BLE.Events.ATTClient.ProcedureCompletedEventHandler(this.ATTClientProcedureCompletedEvent));
            SetAttributeValueHandler(new Bluegiga.BLE.Events.ATTClient.AttributeValueEventHandler(this.ATTClientAttributeValueEvent));
        }
        
        override public string ThingType() { return "Sipp3XII"; }
        public void ATTClientGroupFoundEvent(object sender, Bluegiga.BLE.Events.ATTClient.GroupFoundEventArgs e)
        {
            String log = String.Format("ble_evt_attclient_group_found: connection={0}, start={1}, end={2}, uuid=[ {3}]" + Environment.NewLine,
                e.connection,
                e.start,
                e.end,
                ByteArrayToHexString(e.uuid)
                );
            Console.Write("Sipp3XII: " + log);

            // found "service" attribute groups (UUID=0xfff0), check for heart rate measurement service
            if (e.uuid.SequenceEqual(new Byte[] { 0xf0, 0xff }))
            {
                Console.WriteLine(String.Format("Found attribute group for service w/UUID=0xfff0: start={0}, end={1}", e.start, e.end) + Environment.NewLine);

                SetHandleRange(e.start, e.end);
                
            }
        }
        
        public void ATTClientFindInformationFoundEvent(object sender, Bluegiga.BLE.Events.ATTClient.FindInformationFoundEventArgs e)
        {
            String log = String.Format("ble_evt_attclient_find_information_found: connection={0}, chrhandle={1}, uuid=[ {2}]" + Environment.NewLine,
                e.connection,
                e.chrhandle,
                ByteArrayToHexString(e.uuid)
                );
            Console.Write(log);

            // check for TX characteristic (UUID=0xfff6)
            if (e.uuid.SequenceEqual(new Byte[] { 0xf6, 0xff }))
            {
                Console.WriteLine(String.Format("Found attribute w/UUID=0xfff6: handle={0}", e.chrhandle) + Environment.NewLine);
                att_handle_tx = e.chrhandle;
            }
            else if (e.uuid.SequenceEqual(new Byte[] { 0xf7, 0xff }))
            {
                Console.WriteLine(String.Format("Found attribute w/UUID=0xfff7: handle={0}", e.chrhandle) + Environment.NewLine);
                att_handle_rx = e.chrhandle;
            }
            // check for subsequent client characteristic configuration (UUID=0x2902)
            else if (e.uuid.SequenceEqual(new Byte[] { 0x02, 0x29 }) && att_handle_rx > 0)
            {
                Console.WriteLine(String.Format("Found attribute w/UUID=0x2902: handle={0}", e.chrhandle) + Environment.NewLine);
                att_handle_rx_ccc = e.chrhandle;
            }
        }

        public void ATTClientProcedureCompletedEvent(object sender, Bluegiga.BLE.Events.ATTClient.ProcedureCompletedEventArgs e)
        {
            String log = String.Format("ble_evt_attclient_procedure_completed: connection={0}, result={1}, chrhandle={2}" + Environment.NewLine,
                e.connection,
                e.result,
                e.chrhandle
                );
            Console.WriteLine("Sipp3XII State: {0}", GetState());
            Console.Write(log);

            // check if we just finished searching for services
            if (GetState() == STATE_FINDING_SERVICES)
            {
                if (GetHandleEnd() > 0)
                {
                    //print "Found 'Heart Rate' service with UUID 0x180D"

                    // found the service, so now search for the attributes inside
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
                    Console.WriteLine("Could not find 'Heart Rate' service with UUID 0x180D" + Environment.NewLine);

                }
            }
            // check if we just finished searching for attributes within the heart rate service
            else if (GetState() == STATE_ENABLE_NOTIFICATION)
            {
                read_data(e.connection);
                SetState(STATE_READING_DATA);

            }
            else if (GetState() == STATE_READING_DATA)
            {
                enablePedometer(e.connection);
                // update state
                SetState(STATE_ENABLING_PEDOMETER);
            }
            else if (GetState() == STATE_FINDING_ATTRIBUTES)
            {
                if (att_handle_rx_ccc > 0)
                {
                    //print "Found 'Heart Rate' measurement attribute with UUID 0x2A37"

                    // found the measurement + client characteristic configuration, so enable notifications
                    // (this is done by writing 0x0001 to the client characteristic configuration attribute)
                    Byte[] cmd = NewWriteAttributeCmd(e.connection, att_handle_rx_ccc, new Byte[] { 0x01, 0x00 });
                    // DEBUG: display bytes written
                    Console.WriteLine(String.Format("=> TX ({0}) [ {1}]", cmd.Length, ByteArrayToHexString(cmd)) + Environment.NewLine);

                    SendCmd(cmd);
                    //while (bglib.IsBusy()) ;

                    // update state
                    SetState(STATE_ENABLE_NOTIFICATION);

                }
                else
                {
                    Console.WriteLine("Could not find 'Sipp3XII pedometer' attribute with UUID 0xfff7" + Environment.NewLine);
                }
            }
            else if (GetState() == STATE_ENABLING_PEDOMETER) {
                SetState(STATE_READY);
            }
        }

        public void ATTClientAttributeValueEvent(object sender, Bluegiga.BLE.Events.ATTClient.AttributeValueEventArgs e)
        {
            String log = String.Format("ble_evt_attclient_attribute_value: connection={0}, atthandle={1}, type={2}, value=[ {3}]" + Environment.NewLine,
                e.connection,
                e.atthandle,
                e.type,
                ByteArrayToHexString(e.value)
                );
            Console.Write(log);

            // check for a new value from the connected peripheral's heart rate measurement attribute
            if (e.connection == GetConnectionHandle() && e.atthandle == att_handle_rx)
            {

                if (e.value[0] == SIPP_02_CMD_11_REALTIME)
                {
                    step = e.value[1] << 16 & 0xff0000 | e.value[2] << 8 & 0xff00 | e.value[3] & 0xff;
                    calorie = e.value[7] << 16 & 0xff0000 | e.value[8] << 8 & 0xff00 | e.value[9] & 0xff;
                    distance = e.value[10] << 16 & 0xff0000 | e.value[11] << 8 & 0xff00 | e.value[12] & 0xff;
                    duration = e.value[13] << 8 & 0xff00 | e.value[14] & 0xff;
                    if (originalStep < 0)
                    {
                        originalStep = step;
                    }
                    currentStep = step - originalStep;
                    PedometerValue = new PedometerData(step, calorie, distance, duration, currentStep);
                    
                    // display actual measurement
                    Console.WriteLine(String.Format("Step: {0},  Calore: {1} K, Distance: {2} meter, Duration: {3} minute", step, calorie, distance, duration) + Environment.NewLine);
                }
                else if (e.value[0] == SIPP_02_CMD_09_READ_DATA)
                {
                    if (e.value[1] == 0x00)
                    {
                        step = e.value[6] << 16 & 0xff0000 | e.value[7] << 8 & 0xff00 | e.value[8] & 0xff;
                        calorie = e.value[12] << 16 & 0xff0000 | e.value[13] << 8 & 0xff00 | e.value[14] & 0xff;

                        if (originalStep < 0)
                        {
                            originalStep = step;
                        }
                        currentStep = step - originalStep;

                    }
                    else if (e.value[1] == 0x01)
                    {
                        distance = e.value[6] << 16 & 0xff0000 | e.value[7] << 8 & 0xff00 | e.value[8] & 0xff;
                        duration = e.value[9] << 8 & 0xff00 | e.value[10] & 0xff;

                        PedometerValue = new PedometerData(step, calorie, distance, duration, currentStep);
                        // display actual measurement
                        Console.WriteLine(String.Format("Step: {0},  Calore: {1} K, Distance: {2} meter, Duration: {3} minute", step, calorie, distance, duration) + Environment.NewLine);
                    }
                }
            }
        }

        private void enablePedometer(Byte connection)
        {
            Byte[] cmd = NewWriteAttributeCmd(connection, att_handle_tx, new Byte[] { SIPP_02_CMD_11_REALTIME & 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09 & 0xff });
            // DEBUG: display bytes written
            Console.WriteLine(String.Format("=> TX ({0}) [ {1}]", cmd.Length, ByteArrayToHexString(cmd)) + Environment.NewLine);

            SendCmd(cmd);
            //while (bglib.IsBusy()) ;
        }

        private void read_data(Byte connection)
        {
            Byte[] cmd = NewWriteAttributeCmd(connection, att_handle_tx, new Byte[] { SIPP_02_CMD_09_READ_DATA & 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07 & 0xff });
            // DEBUG: display bytes written
            Console.WriteLine(String.Format("=> TX ({0}) [ {1}]", cmd.Length, ByteArrayToHexString(cmd)) + Environment.NewLine);

            SendCmd(cmd);
            //while (bglib.IsBusy()) ;
        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
                
        override public Byte[] MyConnectCommand(Byte[] addr) {
            return GetConnectCmd(addr, 0, 0x20, 0x30, 0x100, 0);
        }

        public override void Dispose()
        {
            Console.WriteLine("Unregister handler");
            UnsetGroupFoundHandler(new Bluegiga.BLE.Events.ATTClient.GroupFoundEventHandler(this.ATTClientGroupFoundEvent));
            UnsetInformationFoundHandler(new Bluegiga.BLE.Events.ATTClient.FindInformationFoundEventHandler(this.ATTClientFindInformationFoundEvent));
            UnsetProcedureCompleteHandler(new Bluegiga.BLE.Events.ATTClient.ProcedureCompletedEventHandler(this.ATTClientProcedureCompletedEvent));
            UnsetAttributeValueHandler(new Bluegiga.BLE.Events.ATTClient.AttributeValueEventHandler(this.ATTClientAttributeValueEvent));
        }
    }
}
