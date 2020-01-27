using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using FTD2XX_NET;
 
namespace LoopBack
{
    class Program
    {
        static void Main(string[] args)
        {
            UInt32 ftdiDeviceCount = 0;
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

            // Создание нового экземпляр класса устройства FTDI:
            FTDI myFtdiDevice = new FTDI();

            // Определение количества устройств FTDI, подключенных сейчас к компьютеру:
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
            // Проверка результата вызова GetNumberOfDevices:
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                Console.WriteLine("Количество устройств FTDI: "
                                + ftdiDeviceCount.ToString());
                Console.WriteLine("");
            }
            else
            {
                // Ожидание нажатия на клавишу:
                Console.WriteLine("Ошибка получения количества устройств (ошибка "
                                + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }

            // Если нет ни одного подключенного устройства, то возврат:
            if (ftdiDeviceCount == 0)
            {
                // Ожидание нажатия на клавишу:
                Console.WriteLine("Ошибка получения количества устройств (ошибка "
                                + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }

            // Создание хранилища для списка информации об устройствах 
            //  (device info list):
            FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList =
                  new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

            // Заполнение списка информацией:
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);

            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                for (UInt32 i = 0; i < ftdiDeviceCount; i++)
                {
                    Console.WriteLine("Device Index: " + i.ToString());
                    Console.WriteLine("Flags: "
                                 + String.Format("{0:x}", ftdiDeviceList[i].Flags));
                    Console.WriteLine("Type: "
                                 + ftdiDeviceList[i].Type.ToString());
                    Console.WriteLine("ID: "
                                 + String.Format("{0:x}", ftdiDeviceList[i].ID));
                    Console.WriteLine("Location ID: "
                                 + String.Format("{0:x}", ftdiDeviceList[i].LocId));
                    Console.WriteLine("Serial Number: "
                                 + ftdiDeviceList[i].SerialNumber.ToString());
                    Console.WriteLine("Description: "
                                 + ftdiDeviceList[i].Description.ToString());
                    Console.WriteLine("");
                }
            }

            // Открыть первое устройство в нашем списке по серийному номеру:
            ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[0].SerialNumber);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Ожидание нажатия на клавишу:
                Console.WriteLine("Не получилось открыть устройство (ошибка "
                                 + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }

            // Настройка параметров устройства.
            // Установить скорость на 9600 бод:
            ftStatus = myFtdiDevice.SetBaudRate(9600);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Ожидание нажатия на клавишу:
                Console.WriteLine("Проблема в установке скорости (ошибка "
                                + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }

            // Установить формат фрейма - сколько бит данных, стоп-битов, четность:
            ftStatus = myFtdiDevice.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8,
                                                           FTDI.FT_STOP_BITS.FT_STOP_BITS_1,
                                                           FTDI.FT_PARITY.FT_PARITY_NONE);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Ожидание нажатия на клавишу:
                Console.WriteLine("Не получилось настроить параметры фрейма (ошибка "
                                + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }

            // Настроить управление потоком (flow control) на использование
            //  сигналов RTS/CTS:
            //ftStatus = myFtdiDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS,
            //                                       0x11, 0x13);
            //if (ftStatus != FTDI.FT_STATUS.FT_OK)
            //{
            //   // Ожидание нажатия на клавишу:
            //   Console.WriteLine("Не получилось настроить flow control (ошибка "
            //                   + ftStatus.ToString() + ")");
            //   Console.ReadKey();
            //   return;
            //}

            // Установить таймаут чтения на 5 секунд, таймаут записи на бесконечность:
            /*ftStatus = myFtdiDevice.SetTimeouts(5000, 0);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Ожидание нажатия на клавишу:
                Console.WriteLine("Не получилось установить таймауты (ошибка "
                                + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }*/

            // Начало теста loop back - убедитесь, что вход устройства соединен с его
            //  выходом.
            // Запись строки в устройство:


            //запись байтов
            /*byte dataToWrite = "0x00";

            UInt32 numBytesWritten = 0;
            // Обратите внимание, что метод Write перезагружен, благодаря чему можно
            //  записать строку или массив данных:
            ftStatus = myFtdiDevice.Write(dataToWrite,
                                          dataToWrite.Length,
                                          ref numBytesWritten);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Ожидание нажатия на клавишу:
                Console.WriteLine("Неудачная запись в устройство (ошибка "
                                + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }
            Thread.Sleep(3000);*/


            

            //Запись из примера
            string dataToWrite = "0x00";
            
            UInt32 numBytesWritten = 0;
            // Обратите внимание, что метод Write перезагружен, благодаря чему можно
            //  записать строку или массив данных:
            ftStatus = myFtdiDevice.Write(dataToWrite,
                                          dataToWrite.Length,
                                          ref numBytesWritten);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Ожидание нажатия на клавишу:
                Console.WriteLine("Неудачная запись в устройство (ошибка "
                                + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }
            Thread.Sleep(3000);
            // Проверка количества данных, доступных для чтения.
            // В этом случае мы точно знаем, сколько данных должно поступить, 
            //  поэтому просто ждем, пока не получим все отправленные данные.
            /*UInt32 numBytesAvailable = 0;
            do
            {
                ftStatus = myFtdiDevice.GetRxBytesAvailable(ref numBytesAvailable);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    // Ожидание нажатия на клавишу:
                    Console.WriteLine("Не получилось проверить количество доступных "
                                     + "для чтения байт (ошибка "
                                     + ftStatus.ToString() + ")");
                    Console.ReadKey();
                    return;
                }
                Thread.Sleep(10);
            } while (numBytesAvailable < dataToWrite.Length);

            // Теперь, когда у нас есть нужное количество байт, прочитаем их:
            string readData;
            UInt32 numBytesRead = 0;
            // Обратите внимание, что метод Read перезагружен, так что можно
            //  прочитать строку или массив байт:
            ftStatus = myFtdiDevice.Read(out readData,
                                         numBytesAvailable,
                                         ref numBytesRead);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Ожидание нажатия на клавишу:
                Console.WriteLine("Не получилось прочитать данные (ошибка "
                                 + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }*/

            //UInt32 DeviceCount = 0;
            string readData;
            UInt32 numBytesRead = 0;
            UInt32 numBytesAvailable = 4;
            // Обратите внимание, что метод Read перезагружен, так что можно
            //  прочитать строку или массив байт:
            ftStatus = myFtdiDevice.Read(out readData,
                                         numBytesAvailable,
                                         ref numBytesRead);
            /*// Check status
            if (ftStatus != FTDI.FT_STATUS.FT_OK || DeviceCount == 0)
            {

                Console.WriteLine(ftStatus.ToString());
                Console.ReadKey();
                return;
            }*/


            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Ожидание нажатия на клавишу:
                Console.WriteLine("Не получилось прочитать данные (ошибка "
                                 + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }




           Console.WriteLine(readData);
            // Закроем наше устройство:
            ftStatus = myFtdiDevice.Close();
            // Ожидание нажатия на клавишу:
            Console.WriteLine("Нажмите любую клавишу для продолжения.");
            Console.ReadKey();
            return;
        }
        /*public static void WriteWord(string args)
        {
            string dataToWrite = args;

            UInt32 numBytesWritten = 0;
            // Обратите внимание, что метод Write перезагружен, благодаря чему можно
            //  записать строку или массив данных:
            ftStatus = myFtdiDevice.Write(dataToWrite,
                                          dataToWrite.Length,
                                          ref numBytesWritten);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Ожидание нажатия на клавишу:
                Console.WriteLine("Неудачная запись в устройство (ошибка "
                                + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }
        }*/

    }

}