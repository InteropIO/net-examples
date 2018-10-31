using System;
using System.Collections.Generic;
using System.Drawing;
using DOT.AGM;
using DOT.AGM.Client;
using DOT.AGM.Core;
using DOT.AGM.Services;
using Tick42;
using Tick42.Entities;

namespace AdvancedDeclarativeInterop
{
    public enum SCIResultCode
    {
        Failed,
        Interrupted,
        Succeeded
    }

    // Let's define the service contract
    [ServiceContract]
    public interface IServiceContract : IDisposable
    {
        [ServiceOperation]
        void ShowClient(T42Contact contact, [AGMServiceOptions] IServiceOptions serviceOption);

        // asynchronously handle the result
        [ServiceOperation(AsyncIfPossible = true, ExceptionSafe = true)]
        void GetState([ServiceOperationResultHandler("state")]
            Action<ClientPortfolioDemoState> handleResult);

        // get some int value, but also get the underlying method invocation result, that holds more information
        // about the call
        [ServiceOperation(AsyncIfPossible = true)]
        void CheckAsyncClientMethodResult(ref int someInt,
            [ServiceOperationResultHandler] Action<IClientMethodResult> handleClientMethodResult);

        [ServiceOperation(ObjectTypeRestrictions = new[] {"Instrument"}, MethodMatch = MethodMatch.Full)]
        [return: ServiceOperationField(IgnoreInSignature = false, Name = "response")]
        CompositeType SetCurrentInstrument(
            [ServiceOperationField(Description = "list of symbol name types")]
            string[] symbolNames,
            [ServiceOperationField(Description = "symbol Type for InstrumentDefaultName")]
            string instrumentDefaultType,
            [ServiceOperationField(Description = "the Instrument symbol name in the SymbolType")]
            string instrumentDefaultName,
            [ServiceOperationField(IsRequired = false, Description = "the venue of the instrument")]
            string venue,
            [ServiceOperationField(IsRequired = false, Description = "Specifies what kind of view is currently active")]
            string instrumentContext,
            out SCIResultCode sciResultCode,
            out DateTime dt,
            out CompositeType outResponse);

        [ServiceOperation(MethodMatch = MethodMatch.Full)]
        [return: ServiceOperationField(AgmObjectSerializerType = typeof(RectangleSerializer))]
        Rectangle Offset([ServiceOperationField(AgmObjectSerializerType = typeof(RectangleSerializer))]
            Rectangle rect, int x, int y);

        [ServiceOperation(UnwrapCompositeReturnParameter = true)]
        UnwrappedComposite GetUnwrapped(string csv, int x);

        Rectangle? Bounds
        {
            [ServiceOperation(MethodMatch = MethodMatch.Full)]
            [return: ServiceOperationField(AgmObjectSerializerType = typeof(RectangleSerializer), Name = "Bounds")]
            get;
            [ServiceOperation(MethodMatch = MethodMatch.Full)]
            [param: ServiceOperationField(AgmObjectSerializerType = typeof(RectangleSerializer), Name = "Bounds")]
            set;
        }

        [ServiceOperation(Name = "CalculateSum", AsyncIfPossible = true)]
        void CalculateSumAndMulAsync([ServiceOperationField] int x, [ServiceOperationField] int y,
            [ServiceOperationField(IgnoreInSignature = true)]
            [ServiceOperationResultHandler("sum", "mul")] Action<int, int> handleResult);

        [ServiceOperation]
        void ComplexAsyncOutput(int input,
            [ServiceOperationField(IgnoreInSignature = true)]
            [ServiceOperationResultHandler("response")] Action<CompositeType> response);

        [ServiceOperation(AsyncIfPossible = true)]
        void TestAGMOptions(string s, [AGMServiceOptions] IServiceOptions options);
    }

    public class UnwrappedComposite
    {
        public string[] SomeStringArray;
        public int SomeInt;
    }


    [ServiceContractType]
    public class CompositeType
    {
        public List<int> IntList = new List<int> {10, 9, 8, 7, 6};

        public KeyValuePair<string, KeyValuePair<string, bool>> KVPair =
            new KeyValuePair<string, KeyValuePair<string, bool>>("One",
                new KeyValuePair<string, bool>("Number1", true));

        public Dictionary<int, string> Map = new Dictionary<int, string>
        {
            {5, "five"},
            {6, "six"},
            {7, "seven"}
        };

        public KeyValuePair<string, KeyValuePair<int, bool>>[] Pairs =
        {
            new KeyValuePair<string, KeyValuePair<int, bool>>("pk1", new KeyValuePair<int, bool>(5, false)),
            new KeyValuePair<string, KeyValuePair<int, bool>>("pk2", new KeyValuePair<int, bool>(6, true)),
            new KeyValuePair<string, KeyValuePair<int, bool>>("pk3", new KeyValuePair<int, bool>(7, false))
        };

        public bool Success { get; set; }
        public string Message { get; set; }

        [ServiceContractType(ObjectType = typeof(Instance))]
        public IInstance MdguiInstance { get; set; }

        public List<CompositeType> InnerResponses { get; set; }
    }

    internal class RectangleSerializer : IAGMObjectSerializer
    {
        public Value Serialize(object @object, ObjectSerializerSettings settings)
        {
            var rect = (Rectangle) @object;
            return new Value(new Value[] {rect.Top, rect.Left, rect.Width, rect.Height});
        }

        public T Deserialize<T>(Value value, ObjectSerializerSettings settings)
        {
            return (T) Deserialize(typeof(T), value, settings);
        }

        public object Deserialize(Type type, Value value, ObjectSerializerSettings settings)
        {
            return new Rectangle(value.AsTuple[0], value.AsTuple[1], value.AsTuple[2],
                value.AsTuple[3]);
        }

        public object Create(Type type, Value value, params object[] args)
        {
            throw new NotImplementedException();
        }

        public bool CanCreate(Type objectType, Value value, params object[] args)
        {
            throw new NotImplementedException();
        }

        public bool Build(IAGMServiceInstanceConfiguration instanceConfiguration,
            ServiceContractAttribute serviceContractAttribute,
            ServiceOperation serviceOperation, ServiceOperationFieldAttribute attribute, Type type,
            IMethodParameterBuilder paramBuilder)
        {
            paramBuilder.SetAgmType(AgmValueType.Tuple).SetSchema(schemaBuilder =>
                schemaBuilder
                    .AddParameter("top", AgmValueType.Int)
                    .AddParameter("left", AgmValueType.Int)
                    .AddParameter("width", AgmValueType.Int)
                    .AddParameter("height", AgmValueType.Int));
            return true;
        }
    }

    // Some custom type
    public class ClientPortfolioDemoState
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string SelectedClient { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(Left)}: {Left}, {nameof(Top)}: {Top}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height}, {nameof(SelectedClient)}: {SelectedClient}";
        }
    }
}