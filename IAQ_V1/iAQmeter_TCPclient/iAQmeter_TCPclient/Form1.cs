using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Xml;
using System.Windows.Forms.DataVisualization.Charting;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {

        TcpClient client;
        StreamReader streamReader;
        NetworkStream netStream;
        delegate void delegateDataReceived(string ServerReply);
        
        string FileXML = "App.config";   //file with configuration settings
        string FileToLogData = "AirQualityData_" + DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Year.ToString() + ".txt"; //file name to log data
        int LogFlag = 0;  //0 - no logging data, 1 - logging data
        int ConnectionFlag = 0;
        bool CheckFanFlag=false;
        bool FanState = false; //Fan mode
        int X_axis = 1;
        
        string strIP;
        int iPort;

        
        double[] Readings = new double[8];  // array for readings 0-VOC, 1-CO2, 2-temperature, 3-RH, 4- PM25, 5- PM10, 6-Noise, 7- Light
        
        delegate void delegateReadingsUpdate(double[] Readings);
        delegate void delegateChartUpdate(double[] Readings);
        delegate void delegateIndexesUpdate(double[] CalcIndexes);
        delegate void delegateMainIndexUpdate(double MainiAQIndex);
        delegate void delegateGetUserSettings();
        delegate void delegateRH_Temp_Clear();
                     

        public Form1()
        {
            InitializeComponent();
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
        }

        
        private void Connect_Click(object sender, EventArgs e)  //click by "connect to the iAQ meter" button
        {
            if (File.Exists(FileXML))
            {
                try
                {
                    XmlReader xmlreader = XmlReader.Create(FileXML); // reading a settings from file
                    while (xmlreader.Read())
                    {
                        if (xmlreader.NodeType == XmlNodeType.Element)
                        {
                            if (xmlreader.Name == "Port")
                            {
                                strIP = xmlreader.GetAttribute("IP");
                                iPort = Convert.ToInt32(xmlreader.GetAttribute("Port"));
                                try
                                {
                                    client = new TcpClient();
                                    client.Connect(strIP, iPort);
                                    streamReader = new StreamReader(client.GetStream());
                                    netStream = client.GetStream();
                                    Thread backt = new Thread(tcpread);
                                    backt.IsBackground = true;
                                    backt.Start();
                                    button1.Enabled = false;
                                    ConnectionFlag = 1;
                                    textBox10.Text = FileToLogData;
                                    button2.Enabled = true;
                                    button3.Enabled = true;
                                    //button4.Enabled = false;
                                    button5.Enabled = false;
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Can not connect to remote device" +Environment.NewLine+ ex.ToString(), "iAQ meter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    SomethingIsWrong();
                                }
                                

                            }
                            
                        }

                    }
                    
                }
                catch (Exception)
                {
                    MessageBox.Show("Device is not connected or port settings are wrong", "iAQ meter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SomethingIsWrong();
                }



            }
            else
            {

                MessageBox.Show("File App.config not found", "iAQ meter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SomethingIsWrong();

            }
        }

        void tcpread()
        {
            while (true)
            {
                if (netStream.CanRead)
                {
                    string fullServerReply = streamReader.ReadLine();
                    DataReceived(fullServerReply);
                }
            }
        }

        void SomethingIsWrong() //if there is no config file or device is not connected
        {
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
        }

        private void DataReceived(string ServerReply)
        {
            if (InvokeRequired)
            {
                Invoke(new delegateDataReceived(DataReceived), ServerReply);
            }
            else
            {
                textBox1.Text = ServerReply;
                Readings_Clear(); // clear array of readings
                Readings = SplitIncomString(ServerReply); //split an incomming string to values as double
                ReadingsUpdate(Readings);  // update readings in text boxes
                ChartUpdate(Readings);  //update chars 
                X_axis++;
                if (LogFlag == 1) // data logging
                {
                    DataLogging(Readings);
                }
                
            }
        }



        

        void Readings_Clear() //clear the array of readings
        {
            for (int i = 0; i < 8; i++)
            {
                Readings[i] = 0;
            }

        }

        double[] SplitIncomString(string Readings)  //split an incomming string from port into array of readings
        {
            double[] dReadings = new double[8];
            string strVOC;
            string strCO2;
            string strPM25;
            string strPM10;
            string strTemp;
            string strRH;
            string strNoise;
            string strLight;
            try
            {
                strVOC = Readings.Substring(Readings.IndexOf("V") + 2, ((Readings.Substring(Readings.IndexOf("V") + 2)).IndexOf(" ")));
            }
            catch (Exception)
            {
                strVOC = "0.0";
            }
            try
            {
                strCO2 = Readings.Substring(Readings.IndexOf("CO2") + 4, ((Readings.Substring(Readings.IndexOf("CO2") + 4)).IndexOf(" ")));  
            }
            catch (Exception)
            {
                strCO2 = "0.0";
            }
            try
            {
                strTemp = Readings.Substring(Readings.IndexOf("T") + 2, (Readings.Substring(Readings.IndexOf("T") + 2)).IndexOf(" "));
            }
            catch (Exception)
            {
                strTemp = "0.0";
            }
            try
            {
                strRH = Readings.Substring(Readings.IndexOf("H") + 2, (Readings.Substring(Readings.IndexOf("H") + 2)).IndexOf(" "));
            }
            catch (Exception)
            {
                strRH = "0.0";
            }
            try
            {
                strPM25 = Readings.Substring(Readings.IndexOf("PM25") + 5, (Readings.Substring(Readings.IndexOf("PM25") + 5)).IndexOf(" "));
            }
            catch (Exception)
            {
                strPM25 = "0.0";
            }
            try
            {
                strPM10 = Readings.Substring(Readings.IndexOf("PM10") + 5, (Readings.Substring(Readings.IndexOf("PM10") + 5)).IndexOf(" "));
            }
            catch (Exception)
            {
                strPM10 = "0.0";
            }
            try
            {
                strNoise = Readings.Substring(Readings.IndexOf("N") + 2, (Readings.Substring(Readings.IndexOf("N") + 2)).IndexOf(" "));
            }
            catch (Exception)
            {
                strNoise = "0.0";
            }
            try
            {
                strLight = Readings.Substring(Readings.IndexOf("L") + 2, (Readings.Substring(Readings.IndexOf("L") + 2)).IndexOf(" "));
            }
            catch (Exception)
            {
                strLight = "0.0";
            }

            if (!CheckFanFlag)
            {
                int f = 0;
                try
                {
                    f = Convert.ToInt16(Readings.Substring(Readings.Length - 1));
                }
                catch (Exception)
                {
                    
                }
                if (f == 1)
                {
                    FanState = !FanState;
                }
                CheckFanFlag = true;
                if (FanState)
                {
                    button5.Image = WindowsFormsApplication1.Properties.Resources.stopfan_small;
                }
                else
                {
                    button5.Image = WindowsFormsApplication1.Properties.Resources.playfan_small;
                }
                button5.Enabled = true;

            }
            /*
            dReadings[0] = Convert.ToDouble(strVOC.Replace(".", ","));
            dReadings[1] = Convert.ToDouble(strCO2.Replace(".", ","));
            dReadings[2] = Convert.ToDouble(strPM25.Replace(".", ","));
            dReadings[3] = Convert.ToDouble(strPM10.Replace(".", ","));
            dReadings[4] = Convert.ToDouble(strTemp.Replace(".", ","));
            dReadings[5] = Convert.ToDouble(strRH.Replace(".", ","));
            dReadings[6] = Convert.ToDouble(strNoise.Replace(".", ","));
            dReadings[6] = Convert.ToDouble(strNoise.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            dReadings[7] = Convert.ToDouble(strLight.Replace(".", ","));
             */
            double.TryParse(strVOC, NumberStyles.Number, CultureInfo.InvariantCulture, out dReadings[0]);
            double.TryParse(strCO2, NumberStyles.Number, CultureInfo.InvariantCulture, out dReadings[1]);
            double.TryParse(strPM25, NumberStyles.Number, CultureInfo.InvariantCulture, out dReadings[2]);
            double.TryParse(strPM10, NumberStyles.Number, CultureInfo.InvariantCulture, out dReadings[3]);
            double.TryParse(strTemp, NumberStyles.Number, CultureInfo.InvariantCulture, out dReadings[4]);
            double.TryParse(strRH, NumberStyles.Number, CultureInfo.InvariantCulture, out dReadings[5]);
            double.TryParse(strNoise, NumberStyles.Number, CultureInfo.InvariantCulture, out dReadings[6]);
            double.TryParse(strLight, NumberStyles.Number, CultureInfo.InvariantCulture, out dReadings[7]);

            return dReadings;

        }



        void DataLogging(double[] RawReadings)
        {
            string StringToLog = "";
            StringToLog = Environment.NewLine + DateTime.Now.ToString() + " VOC " + String.Format("{0:0.00}", RawReadings[0]) + " CO2 " + String.Format("{0:0}", RawReadings[1]) + " PM25 " + String.Format("{0:0.0}", RawReadings[2]) + " PM10 " + String.Format("{0:0.0}", RawReadings[3]) + " T,C " + String.Format("{0:0.0}", RawReadings[4]) + " RH% " + String.Format("{0:0.0}", RawReadings[5]) + " Noise " + String.Format("{0:0.0}", RawReadings[6]) + " Light " + String.Format("{0:0.0}", RawReadings[7]) ;
            File.AppendAllText(FileToLogData, StringToLog);
        }

        

        public void ReadingsUpdate(double[] Readings)  //update values in text boxes
        {

            if (InvokeRequired)
            {
                Invoke(new delegateReadingsUpdate(ReadingsUpdate), Readings);

            }
            else
            {
                textBox2.Text = Readings[0].ToString();
                textBox3.Text = Readings[1].ToString();
                textBox4.Text = Readings[2].ToString();
                textBox5.Text = Readings[3].ToString();
                textBox6.Text = Readings[4].ToString();
                textBox7.Text = Readings[5].ToString();
                textBox8.Text = Readings[6].ToString();
                textBox9.Text = Readings[7].ToString();

            }
        }

        void ChartUpdate(double[] Readings) //update values on charts
        {
            if (InvokeRequired)
            {
                Invoke(new delegateChartUpdate(ChartUpdate), Readings);

            }
            else
            {
                
                chart1.Series[0].Points.AddXY(X_axis, Readings[0]);
                chart2.Series[0].Points.AddXY(X_axis, Readings[1]);
                chart3.Series[0].Points.AddXY(X_axis, Readings[2]);
                chart3.Series[1].Points.AddXY(X_axis, Readings[3]);
                chart4.Series[0].Points.AddXY(X_axis, Readings[4]);
                chart4.Series[1].Points.AddXY(X_axis, Readings[5]);
                chart5.Series[0].Points.AddXY(X_axis, Readings[6]);
                chart6.Series[0].Points.AddXY(X_axis, Readings[7]);
                
                if (X_axis == 100)  //if number of readings is more than 600 (10 minutes)- clear charts
                {
                    
                    chart1.Series[0].Points.Clear();
                    chart2.Series[0].Points.Clear();
                    chart3.Series[0].Points.Clear();
                    chart3.Series[1].Points.Clear();
                    chart4.Series[0].Points.Clear();
                    chart4.Series[1].Points.Clear();
                    chart5.Series[0].Points.Clear();
                    chart6.Series[0].Points.Clear();
                    
                    X_axis = 1;

                }

                
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) //send command to reset Arduino MCU if the app is closing
        {
            if (ConnectionFlag == 1)
            {
                if (netStream.CanWrite)
                {
                    byte[] bts = new byte[1];
                    bts[0] = 0x63; //command byte to reset MCU
                    netStream.Write(bts, 0, bts.Length);

                }
            }
        }

        


        

        
        private void Browse_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = FileToLogData;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox10.Text = saveFileDialog1.FileName;
                FileToLogData = saveFileDialog1.FileName;
            }
        }
        
        private void StartLog_Click(object sender, EventArgs e) //start logging data
        {
            LogFlag = 1;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = true;
        }

        private void StopLog_Click(object sender, EventArgs e)  //stop logging data
        {
            LogFlag = 0;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = false;
        }

        private void FanState_Click(object sender, EventArgs e)
        {
            if (!FanState)
            {
                if (netStream.CanWrite)
                {
                    byte[] btsFan = new byte[1];
                    btsFan[0] = 0x23; //command byte to turn on the fan
                    netStream.Write(btsFan, 0, btsFan.Length);

                }
                button5.Image = WindowsFormsApplication1.Properties.Resources.stopfan_small;
                FanState = !FanState;
            }
            else
            {
                if (netStream.CanWrite)
                {
                    byte[] btsFan = new byte[1];
                    btsFan[0] = 0x22; //command byte to turn off the fan
                    netStream.Write(btsFan, 0, btsFan.Length);

                }
                button5.Image = WindowsFormsApplication1.Properties.Resources.playfan_small;
                FanState = !FanState;
            }

        }

        

        

    }
}
