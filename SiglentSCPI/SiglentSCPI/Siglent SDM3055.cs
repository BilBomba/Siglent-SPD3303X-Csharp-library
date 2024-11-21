using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Globalization;

namespace SDM3055
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

        public const double invalidResult=9999e100;

        private bool CheckResponse=false;  // es kommt kein reponse
        private const int ReceiveTimeout=1000;
        private static CultureInfo useDot = new CultureInfo("en-US");  // to make sure . is used as comma separator
        private const int SDM3055CommandDelay=100;
        private const int SDM3055TriggerDelay=200;
        private const int SDM3055SampleIntervalDelay=1;
        private const int SDM3055SampleStringLength=15;

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
            Thread.Sleep(SDM3055CommandDelay);  // the siglent device is not answering without this delay
            response=response.Trim('\0');
            return response.Trim();

        }
        private string send(string cmd, int responseLength=0)
        {
            if( SCPI==null ) return "Error not connected";
            string response = "";
            byte[] command = Encoding.ASCII.GetBytes(cmd + "\n");
            while( SCPI.Available >0 ) {
                byte[] flushBuffer = new byte[1024];
                SCPI.Receive(flushBuffer, SocketFlags.None);
            }
            SCPI.Send(command, SocketFlags.None);
            //Thread.Sleep(100);
            byte[] buffer;
            if( responseLength==0 ) buffer = new byte[1024];
            else buffer = new byte[responseLength];
            SCPI.Receive(buffer, SocketFlags.None);
            response = Encoding.ASCII.GetString(buffer);
            Thread.Sleep(SDM3055CommandDelay);  // the siglent device is not answering without this delay
            response=response.Trim('\0');
            return response.Trim();
        }

        ///<summary>
        /// Instrument Ididefication 
        ///</summary>
        public string getIDN()
        {           
            string ret = send("*IDN?");
            return ret;
        }

        ///<summary>
        /// Gets the IP Address
        ///</summary>
        public string getInstrumentIP()
        {
            string response = send("SYST:COMM:LAN:IPAD?");
            return response;
        }

        ///<summary>
        /// Gets the Subnet Mask
        ///</summary>
        public string getInstrumentMask()
        {
            string response = send("SYST:COMM:LAN:SMAS?");
            return response;
        }

        private int samplePerResult=0;
        private int resultCount=0;
        public double[] ReadSetup(int samplePerResult_, int resultCount_)
        {
            if( SCPI == null ) return [invalidResult];
            samplePerResult=samplePerResult_;
            resultCount=resultCount_;
            Console.WriteLine("Setup Read Samples=" +  samplePerResult + "; Trigger Count= " + resultCount);
            string resp=telnetCommand("TRIG:COUNT " + resultCount);
            resp=telnetCommand("SAMP:COUNT " + 1);
            return Read();
        }

        public double[] Read()
        {
            if( SCPI == null ) return [0];
            if( resultCount==0){
                Console.WriteLine("Call ReadSetup first");
                return [invalidResult];
            }
            int responseLength=resultCount*SDM3055SampleStringLength+resultCount-1;  // last part of sum to add the result seperators
            Console.WriteLine("Reading Multimeter " + responseLength);
            SCPI.ReceiveTimeout = (SDM3055TriggerDelay*resultCount+SDM3055SampleIntervalDelay*samplePerResult)*2; // +2 safety margin
            string response = send("READ?",responseLength);
            SCPI.ReceiveTimeout = SDM3055CommandDelay;
            Console.WriteLine("Read?: " + response);
            var resultsStrings=response.Split(',');
            double [] result = new double[resultsStrings.Length];
            for( int i=0; i<resultsStrings.Length; i++ ) {
                result[i] = double.Parse(resultsStrings[i].Trim(), useDot);
            }
            return result;
        }

        private double MeasureCommand(string cmd) {
            if( SCPI == null ) return invalidResult;
            SCPI.ReceiveTimeout = SDM3055TriggerDelay*4; // *4 safety margin
            string response = send(cmd,SDM3055SampleStringLength);
            SCPI.ReceiveTimeout = SDM3055CommandDelay;
            double result = double.Parse(response.Trim(), useDot);
            return result;
        }

        public double VoltageDC()
        {
            var response=MeasureCommand("MEAS:VOLT:DC?");
            Console.WriteLine("Reading VDC= " + response.ToString("N4"));
            return response;
        }

        public double VoltageAC()
        {
            var response=MeasureCommand("MEAS:VOLT:AC?");
            Console.WriteLine("Reading VAC= " + response.ToString("N4"));
            return response;
        }

        public double Continuity()
        {
            var response=MeasureCommand("MEAS:VOLT:CONT?");
            Console.WriteLine("Reading continuity= " + response.ToString("N4"));
            return response;
        }

        public double CurrentDC()
        {
            var response=MeasureCommand("MEAS:CURR:DC?");
            Console.WriteLine("Reading IDC= " + response.ToString("N4"));
            return response;
        }

        public double CurrentAC()
        {
            var response=MeasureCommand("MEAS:CURR:AC?");
            Console.WriteLine("Reading IAC= " + response.ToString("N4"));
            return response;
        }

        public double Diode()
        {
            var response=MeasureCommand("MEAS:DIOD?");
            Console.WriteLine("Reading diode= " + response.ToString("N4"));
            return response;
        }

        public double Frequency()
        {
            var response=MeasureCommand("MEAS:FREQ?");
            Console.WriteLine("Reading f= " + response.ToString("N4"));
            return response;
        }

        public double Resistance()
        {
            var response=MeasureCommand("MEAS:RESI?");
            Console.WriteLine("Reading R= " + response.ToString("N4"));
            return response;
        }

        public double Temperature()
        {
            var response=MeasureCommand("MEAS:TEMP?");
            Console.WriteLine("Reading Temp= " + response.ToString("N4"));
            return response;
        }

        public double Capacitance()
        {
            var response=MeasureCommand("MEAS:CAP?");
            Console.WriteLine("Reading C= " + response.ToString("N4"));
            return response;
        }

    }
}
