using System;
using DOT.AGM.Client;
using DOT.AGM.Services;

namespace SimpleDeclarativeInterop
{
    // Let's define a simple contract interface.
    // Note the attributes.

    // add a method namespace - so all methods will be prefixed with Glue42.Samples.
    [ServiceContract(MethodNamespace = "Glue42.Samples.")]
    public interface IServiceContract : IDisposable
    {
        // since the method does not have an output, we can go with async invocation
        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        void StartDoing(string jobName);

        // since we have an output, we have to explicitly say that we want a single target, and not all
        [ServiceOperation(InvocationTargetType = MethodTargetType.Any)]
        bool GetUpdatedEntityType(SomeEntityType input, out SomeEntityType updated);

        // since we have an output, we have to explicitly say that we want a single target, and not all
        // Note - it's by-ref input
        [ServiceOperation(InvocationTargetType = MethodTargetType.Any)]
        bool UpdateEntityType(ref SomeEntityType input);
    }

    // Let's define a simple entity type.
    public class SomeEntityType
    {
        public string Name { get; set; }
        public double Price { get; set; }
    }
}