// Generated code - do not modify
// Source:

// union SorobanTransactionDataExt switch (int v)
// {
// case 0:
//     void;
// case 1:
//     SorobanResourcesExtV0 resourceExt;
// };


using System;
using System.IO;
using System.ComponentModel.DataAnnotations;
using ProtoBuf;
#if UNITY
	using UnityEngine;
#endif

namespace Stellar {

    [System.CodeDom.Compiler.GeneratedCode("XdrGenerator", "1.0")]
    [System.Serializable]
    [ProtoContract]
    [ProtoInclude(100, typeof(case_0), DataFormat = DataFormat.Default)]
    [ProtoInclude(101, typeof(case_1), DataFormat = DataFormat.Default)]
    public abstract partial class SorobanTransactionDataExt
    {
        public abstract int Discriminator { get; }

        /// <summary>Validates the union case matches its discriminator</summary>
        public abstract void ValidateCase();

        [System.Serializable]
        [ProtoContract(Name = "SorobanTransactionDataExt_case_0")]
        public sealed partial class case_0 : SorobanTransactionDataExt
        {
            public override int Discriminator => 0;

            public override void ValidateCase() {}
        }
        [System.Serializable]
        [ProtoContract(Name = "SorobanTransactionDataExt_case_1")]
        public sealed partial class case_1 : SorobanTransactionDataExt
        {
            public override int Discriminator => 1;
            [ProtoMember(1)]
            public SorobanResourcesExtV0 resourceExt
            {
                get => _resourceExt;
                set
                {
                    _resourceExt = value;
                }
            }
            #if UNITY
            	[SerializeField]
            	[SerializeReference]
            	[InspectorName(@"Resource Ext")]
            #endif
            private SorobanResourcesExtV0 _resourceExt;

            public override void ValidateCase() {}
        }
    }
    public static partial class SorobanTransactionDataExtXdr
    {
        /// <summary>Encodes value to XDR base64 string</summary>
        public static string EncodeToBase64(SorobanTransactionDataExt value)
        {
            using (var memoryStream = new MemoryStream())
            {
                XdrWriter writer = new XdrWriter(memoryStream);
                SorobanTransactionDataExtXdr.Encode(writer, value);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
        /// <summary>Decodes value from XDR base64 string</summary>
        public static SorobanTransactionDataExt DecodeFromBase64(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            using (var memoryStream = new MemoryStream(bytes))
            {
                XdrReader reader = new XdrReader(memoryStream);
                return SorobanTransactionDataExtXdr.Decode(reader);
            }
        }
        public static void Encode(XdrWriter stream, SorobanTransactionDataExt value)
        {
            value.ValidateCase();
            stream.WriteInt((int)value.Discriminator);
            switch (value)
            {
                case SorobanTransactionDataExt.case_0 case_0:
                break;
                case SorobanTransactionDataExt.case_1 case_1:
                SorobanResourcesExtV0Xdr.Encode(stream, case_1.resourceExt);
                break;
            }
        }
        public static SorobanTransactionDataExt Decode(XdrReader stream)
        {
            var discriminator = (int)stream.ReadInt();
            switch (discriminator)
            {
                case 0:
                var result_0 = new SorobanTransactionDataExt.case_0();
                return result_0;
                case 1:
                var result_1 = new SorobanTransactionDataExt.case_1();
                result_1.resourceExt = SorobanResourcesExtV0Xdr.Decode(stream);
                return result_1;
                default:
                throw new Exception($"Unknown discriminator for SorobanTransactionDataExt: {discriminator}");
            }
        }
    }
}
