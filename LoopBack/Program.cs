// 


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using FTD2XX_NET;

namespace LoopBack
{
    class Program
    {
        static FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;  //1
        static FTDI myFtdiDevice = new FTDI();//2

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


        static void Main(string[] args)
        {
            UInt32 ftdiDeviceCount = 0;
            //1

            // Create new instance of the FTDI device class
            //2

            // Determine the number of FTDI devices connected to the machine
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
            // Check status
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                Console.WriteLine("Number of FTDI devices: " + ftdiDeviceCount.ToString());
                Console.WriteLine("");
            }
            else
            {
                // Wait for a key press
                Console.WriteLine("Failed to get number of devices (error " + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }

            // If no devices available, return
            if (ftdiDeviceCount == 0)
            {
                // Wait for a key press
                Console.WriteLine("Failed to get number of devices (error " + ftStatus.ToString() + ")");
                Console.ReadKey();
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
                    Console.WriteLine("Device Index: " + i.ToString());
                    Console.WriteLine("Flags: " + String.Format("{0:x}", ftdiDeviceList[i].Flags));
                    Console.WriteLine("Type: " + ftdiDeviceList[i].Type.ToString());
                    Console.WriteLine("ID: " + String.Format("{0:x}", ftdiDeviceList[i].ID));
                    Console.WriteLine("Location ID: " + String.Format("{0:x}", ftdiDeviceList[i].LocId));
                    Console.WriteLine("Serial Number: " + ftdiDeviceList[i].SerialNumber.ToString());
                    Console.WriteLine("Description: " + ftdiDeviceList[i].Description.ToString());
                    Console.WriteLine("");
                }
            }


            // Open first device in our list by serial number
            ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[0].SerialNumber);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                Console.WriteLine("Failed to open device (error " + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }
            else
            {
                // ZJ
                Console.WriteLine(ftdiDeviceList[0].Description.ToString());
                Console.WriteLine("Open OK: ");
            }


            ftStatus = myFtdiDevice.ResetDevice();
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                Console.WriteLine("Failed to Reset Device (error " + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }
            else
            {
                Console.WriteLine("Reset OK: ");
            }





            //3
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
            Console.WriteLine("Wait on calibration");


            ftStatus = myFtdiDevice.Write(cmd_res, 5, ref numBytesWritten);

            do
            {
                Console.Write(".");
                ftStatus = myFtdiDevice.Write(cmd_rd_s, cmd_rd_s.Length, ref numBytesWritten);
                ftStatus = myFtdiDevice.Read(read_buffer, 4, ref numBytesRead);
                data = BitConverter.ToUInt32(read_buffer, 0);
                if ((data & 0x800) == 0x800) { calibrated = true; }
            } while (!calibrated);
            Console.WriteLine("");
            Console.WriteLine("Calibration done");

            // read the offset value - the simpliest version without averaging (you should read more consecutive data to calculate average value of OFFSET, see page 2.7)

            Double Offset = 0.000000000000000;
            UInt32 Offset_F = 0;
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
            Console.WriteLine("Offset = " + Offset.ToString() + " ns");
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
            Console.WriteLine("Offset_F = " + Offset_F.ToString() + " Hz");
            //Offset_F = Offset_F / 1000000000;



            //TimeIntervalMeas();

            do {
                FrequencyMeas(Offset_F);
                //WriteToFile();
                ////////////////////////
                Console.ReadKey();
            }while(true);
            // Close our device
            ftStatus = myFtdiDevice.Close();

            return;
        }


        static void TimeIntervalMeas()
        {
            UInt32 numBytesRead = 0;
            UInt32 data = 0;

            UInt32 numBytesWritten = 5;




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


            Console.WriteLine("");
            Console.WriteLine("Press CTRL C to intrrupt");
            Console.WriteLine("");

            cmd_meas = new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00 }; // number of measurements to do = 1
            do
            {
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

                cmd_rd_f_data = new byte[] { 0xF0, 0x02, 0x00, 0x00, 0x00 }; // how many words do you want to read? (2) 
                ftStatus = myFtdiDevice.Write(cmd_rd_f_data, 5, ref numBytesWritten);
                do
                {
                    ftStatus = myFtdiDevice.GetRxBytesAvailable(ref numBytesRead);
                } while (numBytesRead < 8);
                numBytesRead = 0;

                ftStatus = myFtdiDevice.Read(read_buffer, 8, ref numBytesRead);
                data = BitConverter.ToUInt32(read_buffer, 0);   // the first 32-bit word

                Count = data >> 20;
                A = data & 0x3FF;
                B = (data >> 10) & 0x3FF;

                data = BitConverter.ToUInt32(read_buffer, 4);   //  the second 32-bit word

                Count = (data << 12) | Count;
                Meas = 4 * ((double)Count + (((double)A - (double)B) / 1024));
                Meas = Meas / 1000000000;
                Meas = Meas - Offset;
                Console.WriteLine("Time interval = " + Meas.ToString());

                // you should write something to exit the loop
            } while (true);


        }
        static void FrequencyMeas(UInt32 Offset_F)
        {
            UInt32 numBytesRead = 0;
            UInt32 data = 0;

            UInt32 numBytesWritten = 5;

            var cmd_ctrl=new byte [] { 0x04, 0xE0, 0x08, 0x00, 0x00 };
            var cmd_en  = new byte[] { 0x03, 0x05, 0x00, 0x00, 0x00 };
            var cmd_wr_s= new byte[] { 0x02, 0x8C, 0x15, 0x00, 0x00 };
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


            Console.WriteLine("");
            Console.WriteLine("Press CTRL C to intrrupt");
            Console.WriteLine("");
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
                
                Console.WriteLine("Frequency = " + Meas.ToString("0.000000000") + "МГц");

                // you should write something to exit the loop
            }
        }
    }
}
