
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;
using System.Threading;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;



class Program
{
    static List<int> manufacturerBuffer = new List<int>();
    static List<DataHandler> handlerBuffer = new List<DataHandler>();

    static object producerLock = new object();
    static object handlerLock = new object();

    static AutoResetEvent waitHandler = null;
    
    static bool flag = true;
    static bool flag1 = true;
    static Random random = new Random();

    static List<Thread> threads = new List<Thread>();

    static void Main()
    {
       
        while (flag1)
        {
            Console.WriteLine("Введите команды при необходимости: pause(p), resume(r), status(s), exit(ESC)");
            Console.WriteLine("---------------------------------------------------------------");
            flag = true;

            for (int i = 0; i < 3; i++)
            {
                int id = i+1;
                var handlerThread = new Thread(() => Handler(id));
                var manufacturerThread = new Thread(() => Manufacturer(id));
                var consumerThread = new Thread(() => Consumer(id));

                threads.Add(handlerThread);
                threads.Add(manufacturerThread);
                threads.Add(consumerThread);

                handlerThread.Start();
                manufacturerThread.Start();
                consumerThread.Start();
            }


            while (true)
            {
                var keyInfo = Console.ReadKey();
                string command = keyInfo.Key.ToString().ToLower();
                if(command == "p")
                {
                    flag = false;
                }
                else if(command == "r")
                {
                    flag = true;
                }
                else if (command == "s")
                {
                    Console.WriteLine("------------------------------------------");
                    Console.WriteLine($"Производитель: {manufacturerBuffer.Count}");
                    Console.WriteLine($"Обработчик: {handlerBuffer.Count}");
                    Console.WriteLine("------------------------------------------");
                    Thread.Sleep(5000);
                }
                else if (keyInfo.Key == ConsoleKey.Escape)
                {
                    flag = false;
                    flag1 = false;
                   
                    break;
                }
                Thread.Sleep(random.Next(1000, 2000));
            }
            
        }
        foreach (var thread in threads)
        {
            thread.Join(); 
        }
        Console.WriteLine("------------------------------------------");
        Console.WriteLine("Программа завершена.");
        Environment.Exit(0);
    }

    static void Manufacturer(int id)
    {
       
        while (flag)
        {
            int num = random.Next(1, 101); 
            bool acquiredLock = false;
            try
            {
                Monitor.Enter(producerLock, ref acquiredLock);
                {
                    Thread.Sleep(random.Next(500, 1001));
                    manufacturerBuffer.Add(num);
                    Console.WriteLine($"Производитель {id}: сгенерировал {num}");
                    Monitor.PulseAll(producerLock); 
                }
            }
            finally
            {
                if (acquiredLock) Monitor.Exit(producerLock);
            }
             
        }

    }


    static void Handler(int id)
    {
        while (true)
        {
            int data = 0;

            bool acquiredLock = false;
            if (!flag1) break;
            if (!flag) continue;
            try
            {
                Monitor.Enter(producerLock, ref acquiredLock);
                {
                    while (manufacturerBuffer.Count == 0 && flag)
                    {
                        Monitor.Wait(producerLock);
                    }

                    if (manufacturerBuffer.Count > 0)
                    {
                        data = manufacturerBuffer[0];
                        manufacturerBuffer.RemoveAt(0);
                        Monitor.PulseAll(producerLock);
                    }
                }
            }
            finally
            {
                if (acquiredLock) Monitor.Exit(producerLock);
            }
        
            if (data > 0)
            {
                var startTimeTotal = DateTime.Now;
                int processedData = data + random.Next(1, 11);
                acquiredLock = false;
                try
                {
                    Monitor.Enter(handlerLock, ref acquiredLock);
                    {
                        Thread.Sleep(random.Next(200, 501));
                        var endTimeTotal = DateTime.Now;
                        var dateFull = endTimeTotal.Subtract(startTimeTotal).TotalSeconds;

                        handlerBuffer.Add(new DataHandler(processedData, dateFull.ToString()));
                        
                        Console.WriteLine($"Обработчик {id}: изменил {data} на {processedData}");
                        Monitor.PulseAll(handlerLock);
                    }
                }
                finally
                {
                    if (acquiredLock) Monitor.Exit(handlerLock);
                }
            }
        }
    }


    static void Consumer(int id)
    {
        if (waitHandler == null)
            waitHandler = new AutoResetEvent(true);

        while (true)
        {
            if (!flag1) break; 
            if (!flag) continue;
            waitHandler.WaitOne();
            object resultsLock = new object();
           
            {
                if (handlerBuffer.Count > 0)
                {
                    Console.WriteLine($"Пользователь {id}: получил данные {handlerBuffer[0].Num},время обработки {handlerBuffer[0].DateFull} секунд");
                    handlerBuffer.RemoveAt(0);
                }
            }
            waitHandler.Set();
        }
    }

    record  class DataHandler(int Num, string DateFull);
}