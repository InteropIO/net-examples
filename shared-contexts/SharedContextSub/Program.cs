using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tick42;
using Tick42.Contexts;
using Tick42.StartingContext;

namespace SharedContextSub
{
    class Program
    {
        static void Main(string[] args)
        {
            var initializeOptions = new InitializeOptions()
            {
                ApplicationName = "Shared Context Subscriber",
                IncludedFeatures = GDFeatures.UseContexts,
                InitializeTimeout = TimeSpan.FromSeconds(5)
            };

            Glue42.InitializeGlue(initializeOptions)
                .ContinueWith((glue) =>
                {
                    //unable to register glue
                    if (glue.Status == TaskStatus.Faulted)
                    {
                        Console.WriteLine("Unable to initialize Glue42!");
                        return;
                    }

                    var glueInstance = glue.Result;
                    ListenForContextUpdates(glueInstance);

                    Console.WriteLine("Context subscriber is started...");
                });

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async void ListenForContextUpdates(Glue42 glue)
        {
            // Listen for updates in TestContext
            var context = await glue.Contexts.GetContext("TestContext");
            context.ContextUpdated += OnContextUpdated;
        }

        private static void OnContextUpdated(object sender, Tick42.Contexts.ContextUpdatedEventArgs e)
        {
            Console.WriteLine("Context updated: " + new
            {
                (sender as IContext).ContextName,
                Added = "[" + string.Join(", ", e.Added.Select(x => x + "")) + "]",
                Updated = "[" + string.Join(", ", e.Updated.Select(x => x + "")) + "]",
                Removed = "[" + string.Join(", ", e.Removed.Select(x => x + "")) + "]",
            });
        }
    }
}
