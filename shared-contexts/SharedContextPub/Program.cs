using System;
using System.Threading;
using System.Threading.Tasks;
using Tick42;
using Tick42.StartingContext;

namespace SharedContextPub
{
    class Program
    {
        static void Main(string[] args)
        {
            var initializeOptions = new InitializeOptions
            {
                ApplicationName = "Shared Context Publisher",
                IncludedFeatures = GDFeatures.UseContexts,
                InitializeTimeout = TimeSpan.FromSeconds(5)
            };

            Glue42.InitializeGlue(initializeOptions)
                .ContinueWith(glue =>
                {
                    //unable to register glue
                    if (glue.Status == TaskStatus.Faulted)
                    {
                        Console.WriteLine("Unable to initialize Glue42!");
                        return;
                    }

                    var glueInstance = glue.Result;
                    StartUpdatingContext(glueInstance);

                    Console.WriteLine("Context publisher is started...");
                });

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async void StartUpdatingContext(Glue42 glue)
        {
            int data = 0;

            // update TestContext every second incrementing the value of data
            var context = await glue.Contexts.GetContext("TestContext");
            var _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    context["data"] = data++;
                    Console.WriteLine($"Updated with {data}");
                }
            });
        }
    }
}