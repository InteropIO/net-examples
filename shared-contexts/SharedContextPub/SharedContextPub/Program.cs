using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tick42;
using Tick42.Contexts;

namespace SharedContextPub
{
    class Program
    {
        public static Glue42 Glue;

        static void Main(string[] args)
        {
            Glue = new Glue42();
            Glue.Initialize("SharedContextPub");
            StartUpdatingContext();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static async void StartUpdatingContext()
        {
            int data = 0;

            // update TestContext every second incrementing the value of data
            var context = await Glue.Contexts.GetContext("TestContext");
            ThreadPool.QueueUserWorkItem((c) =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    context["data"] = data++;
                }
            });
        }
    }
}
