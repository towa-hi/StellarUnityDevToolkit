// Stub attribute definitions for ProtoBuf and System.ServiceModel.
// These exist solely to satisfy the compiler for attributes used in the
// generated SorobanRPCSDK code. They carry no runtime behavior in Unity.
// Marked internal so they cannot conflict with real protobuf-net or
// ServiceModel assemblies in the consuming project.

namespace ProtoBuf
{
    internal enum DataFormat
    {
        Default,
        Group,
        FixedSize,
        TwosComplement,
        ZigZag
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct | System.AttributeTargets.Enum | System.AttributeTargets.Interface, Inherited = false)]
    internal class ProtoContractAttribute : System.Attribute
    {
        public string Name { get; set; }
    }

    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    internal class ProtoMemberAttribute : System.Attribute
    {
        public int Tag { get; }
        public DataFormat DataFormat { get; set; }
        public bool OverwriteList { get; set; }
        public ProtoMemberAttribute(int tag) { Tag = tag; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface, AllowMultiple = true)]
    internal class ProtoIncludeAttribute : System.Attribute
    {
        public int Tag { get; }
        public System.Type KnownType { get; }
        public DataFormat DataFormat { get; set; }
        public ProtoIncludeAttribute(int tag, System.Type knownType) { Tag = tag; KnownType = knownType; }
    }
}

namespace System.ServiceModel
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    internal class ServiceContractAttribute : Attribute
    {
        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal class OperationContractAttribute : Attribute { }
}
