﻿using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Globalization;

namespace SPD3303X_E
{
    public enum CHANNELS
    {
        CH1,
        CH2,
        CH3
    }

    public enum SWITCH { ON, OFF }

    public enum CHANNEL_MODE { CV, CC }

    public enum TIMERS { TIMER1, TIMER2 }

    public enum DISPLAYS { DIGITAL, WAVEFORM }

    public enum MEMORIES { M1 = 1, M2, M3, M4, M5 }

    public enum CONNECTION_MODE { SERIES, PARALLEL, INDEPENDENT, NONE }


    public class SocketManagement
    {
        private IPAddress IP_ADDRESS;
        private int IP_PORT;
        private IPEndPoint endPoint;
        private Socket? SCPI = null;

        private bool CheckResponse=false;  // es kommt kein reponse
        private const int ReceiveTimeout=1000;
        private static CultureInfo useDot = new CultureInfo("en-US");  // to make sure . is used as comma separator
        private const int SPD3303XDelay=100;

        public SocketManagement(string IP_ADDRESS, int PORT)
        {
            this.IP_ADDRESS = IPAddress.Parse(IP_ADDRESS);
            this.IP_PORT = PORT;
            endPoint = new IPEndPoint(this.IP_ADDRESS, IP_PORT);
        }

        ///<summary>
        /// Connection initialzation 
        ///</summary>
        public void connect()
        {
            Debug.WriteLine("Connecting to " + endPoint);
            SCPI = new Socket(IP_ADDRESS.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            SCPI.ReceiveTimeout=ReceiveTimeout;
            SCPI.Connect(endPoint);//connect to socket 
        }
        ///<summary>
        /// Socket termination
        ///</summary>
        public void disconnect()
        {
            if (SCPI != null)
            {
                SCPI.Close();
                SCPI.Dispose();
            }
        }

        //Private Function for Coms 
        private string telnetCommand(string cmd)
        {
            if( SCPI==null ) return "Error not connected";
            byte[] command = Encoding.ASCII.GetBytes(cmd + "\n");

            // flush receivebuffer
            while( SCPI.Available >0 ) {
                byte[] buffer = new byte[1024];
                SCPI.Receive(buffer, SocketFlags.None);
            }
            string response="";
            SCPI.Send(command, SocketFlags.None);
            if (CheckResponse)
            {
                byte[] buffer = new byte[1024];
//                Thread.Sleep(ReceiveTimeout);//DataRace reasons
                SCPI.Receive(buffer, SocketFlags.None);
                response = Encoding.ASCII.GetString(buffer);
                return response;
            }
            Thread.Sleep(SPD3303XDelay);  // the siglent device is not answering without this delay
            response=response.Trim('\0');
            return response.Trim();

        }
        private string send(string cmd)
        {
            if( SCPI==null ) return "Error not connected";
            string response = "";
            byte[] command = Encoding.ASCII.GetBytes(cmd + "\n");
            SCPI.Send(command, SocketFlags.None);
            //Thread.Sleep(100);
            byte[] buffer = new byte[1024];
            SCPI.Receive(buffer, SocketFlags.None);
            response = Encoding.ASCII.GetString(buffer);
            Thread.Sleep(SPD3303XDelay);  // the siglent device is not answering without this delay
            response=response.Trim('\0');
            return response.Trim();
        }
        //"System Status " command parser
        private int[] getSystemStatus()
        {
            int input;
            int[] status = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //String receive = await telnetCommand("SYSTem:STATus?", true);
            String receive = send("SYSTem:STATus?");
            Debug.WriteLine("STATUS RECEIVE " + receive);           
            try
            {
                string[] inp = receive.Split('x');//data recived
                Debug.WriteLine(inp);
                for (int i = 0; i < 10; i++)//reset status variable
                {
                    status[i] = 0;
                }
                Debug.WriteLine(inp[1]);
                inp[1] = inp[1].Trim('\0');
                inp[1] = inp[1].Remove(inp[1].Length - 1);
                input = Convert.ToInt32(inp[1], 16);//hex to binary
                Debug.WriteLine(input);
                for (int i = 0; i < 10; i++)
                {
                    status[i] = input & 1;
                    input = input >> 1;
                }

                for (int i = 0; i < status.Length; i++)//transfer values to starus array for prosessing 
                {
                    Debug.Write(status[i]);

                }
                Debug.WriteLine("");
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe);

            }
            return status;
        }


        private string returnChannel(CHANNELS channel)
        {
            switch (channel)
            {
                case CHANNELS.CH1:
                    return "CH1";
                case CHANNELS.CH2:
                    return "CH2";
                case CHANNELS.CH3:
                    return "CH3";
                default:
                    return "";
            }
        }


        private string returnSwitch(SWITCH switch1)
        {
            switch (switch1)
            {
                case SWITCH.ON:
                    return "ON";
                case SWITCH.OFF:
                    return "OFF";
                default: return "OFF";
            }
        }


        private string returnConnectionMode(CONNECTION_MODE connection)
        {
            switch (connection)
            {
                case CONNECTION_MODE.INDEPENDENT:
                    return "0";
                case CONNECTION_MODE.SERIES:
                    return "1";
                case CONNECTION_MODE.PARALLEL:
                    return "2";
                default: return "";
            }
        }


        private string returnMemory(MEMORIES memory)
        {
            switch (memory)
            {
                case MEMORIES.M1:
                    return "1";

                case MEMORIES.M2:
                    return "2";

                case MEMORIES.M3:
                    return "3";

                case MEMORIES.M4:
                    return "4";

                case MEMORIES.M5:
                    return "5";

                default: return "";
            }
        }
        ///<summary>
        /// Instrument Ididefication 
        ///</summary>
        public string getIDN()
        {           
            string ret = send("*IDN?");
            return ret;
        }

        public CHANNELS getActiveChannel()
        {
            string ret = send("INSTrument?");
            int ch = Int32.Parse(ret.Substring(2));
            switch (ch)
            {
                case 1:
                    return CHANNELS.CH1;

                case 2:
                    return CHANNELS.CH2;

                default:
                    return CHANNELS.CH3;
            }
        }

        ///<summary>
        /// Gets the Voltage SetPoint
        ///</summary>
        public double getVoltage(CHANNELS channel)
        {
            double value = Double.Parse(send(returnChannel(channel) + ":VOLTage?"), CultureInfo.InvariantCulture);
            return value;
        }

        ///<summary>
        /// Gets the Current SetPoint
        ///</summary>
        public double getCurrent(CHANNELS channel)
        {
            double value = Double.Parse(send(returnChannel(channel) + ":CURRent?"), CultureInfo.InvariantCulture);
            return value;
        }

        ///<summary>
        /// Gets the Voltage output of channel
        ///</summary>
        public double getOutputVoltage(CHANNELS channel)
        {
            double value = Double.Parse(send("MEASure:VOLTage? " + returnChannel(channel)), CultureInfo.InvariantCulture);
            return value;
        }

        ///<summary>
        /// Gets the current output of channel
        ///</summary>
        public double getOutputCurrent(CHANNELS channel)
        {
            double value = Double.Parse(send("MEASure:CURRent? " + returnChannel(channel)), CultureInfo.InvariantCulture);
            return value;
        }

        ///<summary>
        /// Gets the Power reading from the ADC
        ///</summary>
        public double getOutputPower(CHANNELS channel)
        {
            double value = Double.Parse(send("MEASure:POWEr? " + returnChannel(channel)), CultureInfo.InvariantCulture);
            return value;
        }



        ///<summary>
        /// Gets the Mode (CV/CC) of the givven channel 
        ///</summary>
        public CHANNEL_MODE getChannelMode(CHANNELS channel)
        {
            int[] status = getSystemStatus();
            switch (channel)
            {
                case CHANNELS.CH1:
                    if (status[0] == 1)
                    {
                        return CHANNEL_MODE.CC;
                    }
                    else
                    {
                        return CHANNEL_MODE.CV;
                    }

                case CHANNELS.CH2:
                    if (status[1] == 1)
                    {
                        return CHANNEL_MODE.CC;
                    }
                    else
                    {
                        return CHANNEL_MODE.CV;
                    }

                default: return CHANNEL_MODE.CV;
            }
        }

        ///<summary>
        /// Gets the Output Stateof the givven channel 
        ///</summary>
        public SWITCH getChannelStatus(CHANNELS channel)
        {
            int[] status = getSystemStatus();
            switch (channel)
            {
                case CHANNELS.CH1:
                    if (status[4] == 1)
                    {
                        return SWITCH.ON;
                    }
                    else
                    {
                        return SWITCH.OFF;
                    }

                case CHANNELS.CH2:
                    if (status[5] == 1)
                    {
                        return SWITCH.ON;
                    }
                    else
                    {
                        return SWITCH.OFF;
                    }


                default: return SWITCH.ON;
            }
        }

        ///<summary>
        /// Gets the Output Stateof the givven channel 
        ///</summary>
        public SWITCH getTimerStatus(TIMERS timer)
        {
            int[] status = getSystemStatus();
            switch (timer)
            {
                case TIMERS.TIMER1:
                    if (status[6] == 1)
                    {
                        return SWITCH.ON;
                    }
                    else
                    {
                        return SWITCH.OFF;
                    }

                case TIMERS.TIMER2:
                    if (status[7] == 1)
                    {
                        return SWITCH.ON;
                    }
                    else
                    {
                        return SWITCH.OFF;
                    }


                default: return SWITCH.ON;
            }
        }

        ///<summary>
        /// Gets the State of the waveForm display of the givven channel 
        ///</summary>
        public DISPLAYS getDisplay(CHANNELS channel)
        {
            int[] status = getSystemStatus();
            switch (channel)
            {
                case CHANNELS.CH1:
                    if (status[8] == 1)
                    {
                        return DISPLAYS.WAVEFORM;
                    }
                    else
                    {
                        return DISPLAYS.DIGITAL;
                    }

                case CHANNELS.CH2:
                    if (status[9] == 1)
                    {
                        return DISPLAYS.WAVEFORM;
                    }
                    else
                    {
                        return DISPLAYS.DIGITAL;
                    }

                default: return DISPLAYS.DIGITAL;
            }
        }

        ///<summary>
        /// Gets the Connection Mode (Serial / Parallel)
        ///</summary>
        public CONNECTION_MODE getConnectionMode()
        {
            int[] status = getSystemStatus();
            if (status[2] == 1)
            {
                if (status[3] == 1)
                {
                    return CONNECTION_MODE.SERIES;
                }
                else
                {
                    return CONNECTION_MODE.INDEPENDENT;
                }
            }
            else
            {
                if (status[3] == 1)
                {
                    return CONNECTION_MODE.PARALLEL;
                }
                else
                {
                    return CONNECTION_MODE.NONE;
                }
            }
        }

        ///<summary>
        /// Gets the Network address mode (DHCP/static)
        ///</summary>
        public bool getInstrumentDHCP()
        {
            string response = send("DHCP?");

            bool dhcpStatus = false;
            if (response.Contains("DHCP:ON"))
            {
                dhcpStatus = true;
            }
            if (response.Contains("DHCP:OFF"))
            {
                dhcpStatus = false;
            }
            return dhcpStatus;
        }

        ///<summary>
        /// Gets the IP Address
        ///</summary>
        public string getInstrumentIP()
        {
            string response = send("IPaddr?");
            return response;
        }

        ///<summary>
        /// Gets the Subnet Mask
        ///</summary>
        public string getInstrumentMask()
        {
            string response = send("MASKaddr?");
            return response;
        }

        ///<summary>
        /// Gets the Gateway
        ///</summary>
        public string getInstrumentGateway()
        {
            string response = send("GATEaddr?");
            return response;
        }

        ///<summary>
        /// Sets the Current SetPoint: setCurrent(CHANNELS channel, double value)
        ///</summary>
        public void setCurrent(CHANNELS channel, double value)
        {
            Console.WriteLine("Set current of channel " +  (int)channel + " to " + value);
            string svalue=value.ToString("F2", useDot);
            string resp=telnetCommand(returnChannel(channel) + ":CURRent " + svalue);
        }

        ///<summary>
        /// Sets the Voltage SetPoint: setVoltage(CHANNELS channel, double value)
        ///</summary>
        public void setVoltage(CHANNELS channel, double value)
        {
            value=Math.Round(value, 2);
            Console.WriteLine("Set voltage of channel " +  (int)channel + " to " + value);
            string svalue=value.ToString("F2", useDot);
            string resp=telnetCommand(returnChannel(channel) + ":VOLTage " + svalue);
        }

        ///<summary>
        /// Sets the Voltage SetPoint, reads the result back and compares
        /// Returns true if set successfully: setVoltageAndCheck(CHANNELS channel, double value)
        ///</summary>
        public bool setVoltageAndCheck(CHANNELS channel, double value)
        {
            setVoltage(channel, value);
            double res=getVoltage(channel);
            if( res==value ) return true;
            return false;
        }

        ///<summary>
        /// Sets the Currentlimit SetPoint, reads the result back and compares
        /// Returns true if set successfully: setVoltageAndCheck(CHANNELS channel, double value)
        ///</summary>
        public bool setCurrentAndCheck(CHANNELS channel, double value)
        {
            setCurrent(channel, value);
            double res=getCurrent(channel);
            if( res==value ) return true;
            return false;
        }

        ///<summary>
        /// Sets the output status: (ON/OFF) setChannelStatus(CHANNELS channel, SWITCH status)
        ///</summary>
        public void setChannelStatus(CHANNELS channel, SWITCH status)
        {
            string resp=telnetCommand("OUTPut " + returnChannel(channel) + "," + returnSwitch(status));
        }

        ///<summary>
        /// Sets the output mode: setChannelConnection(CONNECTION_MODE mode)
        ///</summary>
        public void setChannelConnection(CONNECTION_MODE mode)
        {
            string resp=telnetCommand("OUTPut:TRACK " + returnConnectionMode(mode));
        }

        ///<summary>
        /// Sets the status of the waveForm Display: setWaveformDisplay(CHANNELS channel, SWITCH sWITCH)
        ///</summary>
        public void setWaveformDisplay(CHANNELS channel, SWITCH sWITCH)
        {
            string resp=telnetCommand("OUTPut:WAVE " + returnChannel(channel) + "," + returnSwitch(sWITCH));
        }


        public void setInstrumentDHCP(SWITCH dhcp)
        {
            string resp=telnetCommand("DHCP " + returnSwitch(dhcp));
        }


        public void setInstrumentIP(string IP)
        {
            string resp=telnetCommand("IPaddr " + IP);
        }


        public void setInstrumentMask(string address)
        {
            string resp=telnetCommand("MASKaddr " + address);
        }


        public void setInstrumentGateway(string gateway)
        {
            string resp=telnetCommand("GATEaddr " + gateway);
        }

        ///<summary>
        /// Saves macine`s current status to memory: saveCurrentState(MEMORIES memory)
        ///</summary>
        public void saveCurrentState(MEMORIES memory)
        {
            string resp=telnetCommand("*SAV " + returnMemory(memory));
        }

        ///<summary>
        /// Recal previusly saved macine status: recallState(MEMORIES memory)
        ///</summary>
        public void recallState(MEMORIES memory)
        {
            string resp= telnetCommand("*RCL " + returnMemory(memory));
        }

    }
}
