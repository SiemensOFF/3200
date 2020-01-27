using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FTD2XX_NET;

namespace FreqCount
{
    public partial class Form1 : Form
    {
        static FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;  //1
        static FTDI myFtdiDevice = new FTDI();//2
        public static UInt32 Offset_F = 0;

        // you can define control commands (see Programmer's Guide - page 4)
        static byte[] cmd_res = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 };
        static byte[] cmd_ctrl = new byte[] { 0x04, 0x23, 0x00, 0x00, 0x00 };  // time interval, A<->B, 1 s range
        static byte[] cmd_en = new byte[] { 0x03, 0x04, 0x00, 0x00, 0x00 };    // Ihibit
        static byte[] cmd_wr_s = new byte[] { 0x02, 0xee, 0x04, 0x00, 0x00 };   // set START A, STOP B, slope rise (both), internal clock ON
        static byte[] cmd_wr_dac = new byte[] { 0x05, 0x8f, 0x8f, 0x00, 0x00 };  // threshold 0.5 V to START/STOP.

        static byte[] cmd_rd_s = new byte[] { 0xF2, 0x01, 0x00, 0x00, 0x00 };

        static byte[] cmd_meas = new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00 };
        static byte[] cmd_rd_meas_no = new byte[] { 0xF1, 0x01, 0x00, 0x00, 0x00 };
        static byte[] cmd_rd_f_data = new byte[] { 0xF0, 0x02, 0x00, 0x00, 0x00 };

        static byte[] cmd_rd_offset_f = new byte[] { 0xF0, 0x01, 0x00, 0x00, 0x00 };// read frequency offset

        static byte[] read_buffer = new byte[4 * 1024];   // set the size of buffer 

        static Boolean Meas_Ready = false;

       

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            UInt32 ftdiDeviceCount = 0;
            // Create new instance of the FTDI device class
            // Determine the number of FTDI devices connected to the machine
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
            // Check status
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                textBox1.Text += "Number of FTDI devices: " + ftdiDeviceCount.ToString() +"\r\n\r\n";
                //Console.WriteLine("Number of FTDI devices: " + ftdiDeviceCount.ToString());
                //Console.WriteLine("");
            }
            else
            {
                // Wait for a key press
                //Console.WriteLine("Failed to get number of devices (error " + ftStatus.ToString() + ")");
                //Console.ReadKey();
                return;
            }

            // If no devices available, return
            if (ftdiDeviceCount == 0)
            {
                // Wait for a key press
               // Console.WriteLine("Failed to get number of devices (error " + ftStatus.ToString() + ")");
                //Console.ReadKey();
                return;
            }

            // Allocate storage for device info list
            FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

            // Populate our device list
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);

            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                for (UInt32 i = 0; i < ftdiDeviceCount; i++)
                {
                    //textBox1.Text += "Device Index: " + i.ToString() + "\r\n";
                    //textBox1.Text += "Flags: " + String.Format("{0:x}", ftdiDeviceList[i].Flags) + "\r\n";
                    textBox1.Text += "Type: " + ftdiDeviceList[i].Type.ToString() + "\r\n";
                    //textBox1.Text += "ID: " + String.Format("{0:x}", ftdiDeviceList[i].ID) + "\r\n";
                    //textBox1.Text += "Location ID: " + String.Format("{0:x}", ftdiDeviceList[i].LocId) + "\r\n";
                    textBox1.Text += "Serial Number: " + ftdiDeviceList[i].SerialNumber.ToString() + "\r\n";
                    //textBox1.Text += "Description: " + ftdiDeviceList[i].Description.ToString() + "\r\n";
                    //textBox1.Text += " \r\n"; 
                }
            }


            // Open first device in our list by serial number
            ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[0].SerialNumber);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                //Console.WriteLine("Failed to open device (error " + ftStatus.ToString() + ")");
                //Console.ReadKey();
                return;
            }
            else
            {
                textBox1.Text += ftdiDeviceList[0].Description.ToString() + "\r\n";
                //textBox1.Text += "Open OK: " + "\r\n";
            }


            ftStatus = myFtdiDevice.ResetDevice();
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                //Console.WriteLine("Failed to Reset Device (error " + ftStatus.ToString() + ")");
                //Console.ReadKey();
                return;
            }
            else
            {
                //textBox1.Text += "Reset OK: " + "\r\n";
            }

            UInt32 numBytesRead = 0;
            UInt32 data = 0;

            UInt32 numBytesWritten = 5;
            Boolean calibrated = false;
            //4//Boolean Meas_Ready = false;

            ftStatus = myFtdiDevice.Write(cmd_ctrl, cmd_ctrl.Length, ref numBytesWritten);
            ftStatus = myFtdiDevice.Write(cmd_en, cmd_en.Length, ref numBytesWritten);
            ftStatus = myFtdiDevice.Write(cmd_wr_s, cmd_wr_s.Length, ref numBytesWritten);
            ftStatus = myFtdiDevice.Write(cmd_wr_dac, cmd_wr_dac.Length, ref numBytesWritten);

            // wait on finish of calibration 
            //textBox1.Text += "Wait on calibration" + "\r\n";
            //Console.WriteLine("Wait on calibration");

            ftStatus = myFtdiDevice.Write(cmd_res, 5, ref numBytesWritten);
            do
            {
                //Console.Write(".");
                ftStatus = myFtdiDevice.Write(cmd_rd_s, cmd_rd_s.Length, ref numBytesWritten);
                ftStatus = myFtdiDevice.Read(read_buffer, 4, ref numBytesRead);
                data = BitConverter.ToUInt32(read_buffer, 0);
                if ((data & 0x800) == 0x800) { calibrated = true; }
            } while (!calibrated);
            //Console.WriteLine("");
            textBox1.Text += "Calibration done" + "\r\n";
            //Console.WriteLine("Calibration done");

            // read the offset value - the simpliest version without averaging
            Double Offset = 0.000000000000000;
            Double Meas = 0.000000000000000;
            UInt64 Count = 0;
            UInt32 A = 0;
            UInt32 B = 0;

            cmd_rd_f_data[1] = 0x02; // number of 32-bit data = 2 
            ftStatus = myFtdiDevice.Write(cmd_rd_f_data, 5, ref numBytesWritten);

            do
            {
                ftStatus = myFtdiDevice.GetRxBytesAvailable(ref numBytesRead);
            } while (numBytesRead < 8);

            ftStatus = myFtdiDevice.Read(read_buffer, 8, ref numBytesRead);
            data = BitConverter.ToUInt32(read_buffer, 0);   // first 32-bit word

            Count = data >> 20;
            A = data & 0x3FF;
            B = (data >> 10) & 0x3FF;

            data = BitConverter.ToUInt32(read_buffer, 4);   // second 32-bit word

            Count = (data << 12) | Count;
            Offset = 4 * ((double)Count + (((double)A - (double)B) / 1024));
            //Console.WriteLine("Offset = " + Offset.ToString() + " ns");
            Offset = Offset / 1000000000;

            ///////////////freq offset
            ftStatus = myFtdiDevice.Write(cmd_rd_offset_f, 5, ref numBytesWritten);

            do
            {
                ftStatus = myFtdiDevice.GetRxBytesAvailable(ref numBytesRead);
            } while (numBytesRead < 4);

            ftStatus = myFtdiDevice.Read(read_buffer, 4, ref numBytesRead);
            data = BitConverter.ToUInt32(read_buffer, 0);   // first 32-bit word

            //Count = (data << 12) | Count;
            Offset_F = data;

            //Console.WriteLine("Offset_F = " + Offset_F.ToString() + " Hz");
            //Offset_F = Offset_F / 1000000000;
            button2.Visible = true;
            button1.Visible = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string result=FrequencyMeas(Offset_F);
            label1.Text = result;
            File.AppendAllText("date.txt", DateTime.Now.ToString()+"\t"+result+"\r\n");
        }
        public static string FrequencyMeas(UInt32 Offset_F)
        {
            UInt32 numBytesRead = 0;
            UInt32 data = 0;
            UInt32 numBytesWritten = 5;

            var cmd_ctrl = new byte[] { 0x04, 0xE0, 0x08, 0x00, 0x00 };
            var cmd_en = new byte[] { 0x03, 0x05, 0x00, 0x00, 0x00 };
            var cmd_wr_s = new byte[] { 0x02, 0x8C, 0x15, 0x00, 0x00 };
            var cmd_wr_dac = new byte[] { 0x05, 0x80, 0x80, 0xD0, 0x3C };

            Double Offset = 0.000000000000000;
            Double Meas = 0.000000000000000;
            UInt64 Count = 0;
            UInt32 A = 0;
            UInt32 B = 0;

            // Measurement loop (in sequence: Start single measurement -> read the result)
            // refresh settings (the settings are not restored after calibration)
            ftStatus = myFtdiDevice.Write(cmd_ctrl, cmd_ctrl.Length, ref numBytesWritten);
            ftStatus = myFtdiDevice.Write(cmd_en, cmd_en.Length, ref numBytesWritten);
            ftStatus = myFtdiDevice.Write(cmd_wr_s, cmd_wr_s.Length, ref numBytesWritten);
            ftStatus = myFtdiDevice.Write(cmd_wr_dac, cmd_wr_dac.Length, ref numBytesWritten);
            Thread.Sleep(20);  //wait for DAC

            //Console.WriteLine("");
            //Console.WriteLine("Press CTRL C to intrrupt");
            //Console.WriteLine("");
            {
                cmd_meas = new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00 }; // number of measurements to do = 1

                ftStatus = myFtdiDevice.Write(cmd_meas, 5, ref numBytesWritten);    // start measuring process
                Meas_Ready = false;
                do        //check the value of RD_MEAS_NO register
                {
                    ftStatus = myFtdiDevice.Write(cmd_rd_meas_no, 5, ref numBytesWritten);
                    ftStatus = myFtdiDevice.Read(read_buffer, 4, ref numBytesRead);
                    data = BitConverter.ToUInt32(read_buffer, 0);
                    if (data > 0) { Meas_Ready = true; }
                } while (!Meas_Ready);


                // READ RESULTS

                cmd_rd_f_data = new byte[] { 0xF0, 0x03, 0x00, 0x00, 0x00 }; // how many words do you want to read? (3) 
                ftStatus = myFtdiDevice.Write(cmd_rd_f_data, 5, ref numBytesWritten);
                do
                {
                    ftStatus = myFtdiDevice.GetRxBytesAvailable(ref numBytesRead);
                } while (numBytesRead < 12);
                numBytesRead = 0;

                ftStatus = myFtdiDevice.Read(read_buffer, 12, ref numBytesRead);

                data = BitConverter.ToUInt32(read_buffer, 0);   // the first 32-bit word

                Count = data >> 20;
                A = data & 0x3FF;
                B = (data >> 10) & 0x3FF;

                data = BitConverter.ToUInt32(read_buffer, 4);   //  the second 32-bit word

                Count = (data << 12) | Count;

                data = BitConverter.ToUInt32(read_buffer, 8);   // the third 32-bit word

                UInt32 PERIODS_NO = data;

                //Meas = PERIODS_NO / ((4 * ((double)Count + (((double)A - (double)B) / 1024))) - Offset_F / 1024);
                Meas = 4 * ((double)Count + (((double)A - (double)B) / 1024));
                Meas = Meas - Offset_F / 1024;
                Meas = PERIODS_NO / Meas;
                Meas = Meas * 1000;

                //Console.WriteLine("Frequency = " + Meas.ToString("0.000000000") + "МГц");
                return Meas.ToString("0.000000000 МГц");
                
                // you should write something to exit the loop
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
