using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DOT.AGM;
using DOT.AGM.Client;
using DOT.AGM.Server;
using DOT.Core.Extensions;
using Tick42;
using Tick42.StartingContext;

namespace HelloGlue42Interop
{
    class Program
    {
        static void Main(string[] args)
        {
            Glue42 glue42 = null;

            var initializeOptions = new InitializeOptions()
            {
                ApplicationName = "Hello Glue42 Interop",
                InitializeTimeout = TimeSpan.FromSeconds(5)
            };

            Glue42.InitializeGlue(initializeOptions)
                .ContinueWith((glue) =>
                {
                    //unable to register glue
                    if (glue.Status == TaskStatus.Faulted)
                    {
                        Log("Unable to initialize Glue42");
                        return;
                    }

                    glue42 = glue.Result;

                    // subscribe to interop connection status event
                    glue42.Interop.ConnectionStatusChanged += InteropOnConnectionStatusChanged;

                    // Register synchronous calling endpoint, called HelloGlue.
                    // we could use the serverEndpoint returned by this method to later unregister it by calling glue.Interop.UnregisterEndpoint
                    IServerMethod serverEndpoint = glue42.Interop.RegisterSynchronousEndpoint(mdb => mdb.SetMethodName("HelloGlue"), OnHelloWorldInvoked);

                    Log($"Registered endpoint called {serverEndpoint.Definition.Name}");
                    Log("Initialized Glue.");
                });

            string input;

            // start the main loop, take console inputs until '!q' is passed.
            // send each line as an invocation argument, if not empty
            while (!string.Equals(input = Console.ReadLine(), "!q", StringComparison.CurrentCultureIgnoreCase))
            {
                if(glue42 == null)
                {
                    return;
                }
                InteropSendOperation(glue42, input);
            }
        }

        private static void InteropSendOperation(Glue42 glue, string data)
        {
            // search and invoke 'HelloGlue' endpoint
            Task<IClientMethodResult> invoke;
            if (string.IsNullOrEmpty(data))
            {
                // simply execute, passing no arguments
                invoke = glue.Interop.Invoke("HelloGlue");
            }
            else
            {
                // we have some argument, let's pass it
                invoke = glue.Interop.Invoke("HelloGlue", mib => mib.SetContext(cb => cb.AddValue("input", data)));
            }

            invoke.ContinueWith(InvocationResultHandler);
        }

        private static void InvocationResultHandler(Task<IClientMethodResult> invocationResult)
        {
            if (invocationResult.IsFaulted)
            {
                Log($"Invocation failed with {invocationResult.Exception?.Flatten()}");
                return;
            }

            IClientMethodResult cmr = invocationResult.Result;
            Log($"Invoked: {invocationResult.Status} - {cmr.Status} - {cmr.ResultMessage}");
        }

        // used to count invocations
        private static int invocationIndex_;

        private static IServerMethodResult OnHelloWorldInvoked(
            IServerMethod method,
            IMethodInvocationContext invocationContext,
            IInstance caller, 
            IServerMethodResultBuilder resultBuilder,
            object cookie)
        {
            Log($"{caller.ApplicationName} said hello with these arguments: {{{invocationContext.Arguments.AsString()}}}");

            // let's simulate a problem on the implementation side
            if (++invocationIndex_ % 3 == 0)
            {
                throw new Exception("Oops! Something happened!");
            }

            // return empty (void) result.
            return resultBuilder.Build();
        }

        private static void InteropOnConnectionStatusChanged(object sender, InteropStatusEventArgs args)
        {
            Log($"Glue connection is now {args.Status.State}");
        }

        private static void Log(string logMessage)
        {
            Console.WriteLine(">>> " + logMessage);
        }
    }
}
