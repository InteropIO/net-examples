using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DOT.AGM;
using DOT.AGM.Client;
using DOT.AGM.Server;
using DOT.Core.Extensions;
using Tick42;

namespace HelloGlue42Interop
{
    class Program
    {
        private static void Log(string logMessage)
        {
            Console.WriteLine(">>> " + logMessage);
        }

        static void Main(string[] args)
        {
            // initialize Tick42 Interop (AGM) and Metrics components

            string glueUserName = Environment.UserName;

            // these envvars are expanded in some configuration files
            Environment.SetEnvironmentVariable("PROCESSID", Process.GetCurrentProcess().Id + "");
            Environment.SetEnvironmentVariable("GW_USERNAME", glueUserName);
            //Environment.SetEnvironmentVariable("DEMO_MODE", Mode + "");

            // The Glue42 facade provides a simplified programming
            // interface over the core Glue42 components.
            var glue = new Glue42();

            // subscribe to interop events

            // subscribe to interop connection status event
            glue.Interop.ConnectionStatusChanged += InteropOnConnectionStatusChanged;

            Log("Initializing Glue42");

            glue.Initialize(
                "HelloGlue42Interop", // application name - required
                useAgm: true,
                useAppManager: true,
                useMetrics: true,
                useContexts: false,
                useStickyWindows: false,
                credentials: Tuple.Create(glueUserName, ""));

            // Register synchronous calling endpoint, called HelloGlue.
            // we could use the serverEndpoint returned by this method to later unregister it by calling glue.Interop.UnregisterEndpoint
            IServerMethod serverEndpoint =
                glue.Interop.RegisterSyncronousEndpoint(mdb => mdb.SetMethodName("HelloGlue"), OnHelloWorldInvoked);

            Log($"Registered endpoint called {serverEndpoint.Definition.Name}");

            Log("Initialized Glue.");

            string input;

            // start the main loop, take console inputs until '!q' is passed.
            // send each line as an invocation argument, if not empty

            while (!string.Equals(input = Console.ReadLine(), "!q", StringComparison.CurrentCultureIgnoreCase))
            {
                // search and invoke 'HelloGlue' endpoint

                Task<IClientMethodResult> invoke;
                if (string.IsNullOrEmpty(input))
                {
                    // simply execute, passing no arguments
                    invoke = glue.Interop.Invoke("HelloGlue");
                }
                else
                {
                    // we have some argument, let's pass it
                    invoke = glue.Interop.Invoke("HelloGlue", mib => mib.SetContext(cb => cb.AddValue("input", input)));
                }

                invoke.ContinueWith(InvocationResultHandler);
            }
        }

        private static void InvocationResultHandler(Task<IClientMethodResult> invocationResult)
        {
            IClientMethodResult cmr = invocationResult.Result;
            Log($"Invoked: {invocationResult.Status} - {cmr.Status} - {cmr.ResultMessage}");
        }

        // used to count invocations
        private static int invocationIndex_;

        private static IServerMethodResult OnHelloWorldInvoked(IServerMethod method,
            IMethodInvocationContext invocationContext, IInstance caller, IServerMethodResultBuilder resultBuilder,
            object cookie)
        {
            Log(
                $"{caller.ApplicationName} said hello with these arguments: {{{invocationContext.Arguments.AsString()}}}");

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
    }
}