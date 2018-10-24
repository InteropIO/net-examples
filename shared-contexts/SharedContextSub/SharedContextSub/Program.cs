using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tick42;
using Tick42.Contexts;

namespace SharedContextSub
{
    class Program
    {
        public static Glue42 Glue { get; private set; }

        static void Main(string[] args)
        {
            Glue = new Glue42();
            Glue.Initialize("SharedContextSub");

            ListenForContextUpdates();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static async void ListenForContextUpdates()
        {
            // Listen for updates in TestContext
            var context = await Glue.Contexts.GetContext("TestContext");
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
