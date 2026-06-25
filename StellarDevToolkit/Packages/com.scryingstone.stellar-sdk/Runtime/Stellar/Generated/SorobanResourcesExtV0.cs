// Generated code - do not modify
// Source:

// struct SorobanResourcesExtV0
// {
//     uint32 archivedSorobanEntries<>;
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
    public partial class SorobanResourcesExtV0
    {
        [ProtoMember(1)]
        public uint32[] archivedSorobanEntries
        {
            get => _archivedSorobanEntries;
            set
            {
                _archivedSorobanEntries = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"Archived Soroban Entries")]
        #endif
        private uint32[] _archivedSorobanEntries;

        public SorobanResourcesExtV0()
        {
        }
        /// <summary>Validates all fields have valid values</summary>
        public virtual void Validate()
        {
        }
    }
    public static partial class SorobanResourcesExtV0Xdr
    {
        /// <summary>Encodes value to XDR base64 string</summary>
        public static string EncodeToBase64(SorobanResourcesExtV0 value)
        {
            using (var memoryStream = new MemoryStream())
            {
                XdrWriter writer = new XdrWriter(memoryStream);
                SorobanResourcesExtV0Xdr.Encode(writer, value);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
        /// <summary>Decodes value from XDR base64 string</summary>
        public static SorobanResourcesExtV0 DecodeFromBase64(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            using (var memoryStream = new MemoryStream(bytes))
            {
                XdrReader reader = new XdrReader(memoryStream);
                return SorobanResourcesExtV0Xdr.Decode(reader);
            }
        }
        /// <summary>Encodes struct to XDR stream</summary>
        public static void Encode(XdrWriter stream, SorobanResourcesExtV0 value)
        {
            value.Validate();
            stream.WriteInt(value.archivedSorobanEntries.Length);
            for (var i = 0; i < value.archivedSorobanEntries.Length; i++)
            {
                uint32Xdr.Encode(stream, value.archivedSorobanEntries[i]);
            }
        }
        /// <summary>Decodes struct from XDR stream</summary>
        public static SorobanResourcesExtV0 Decode(XdrReader stream)
        {
            var result = new SorobanResourcesExtV0();
            var length = stream.ReadInt();
            result.archivedSorobanEntries = new uint32[length];
            for (var i = 0; i < length; i++)
            {
                result.archivedSorobanEntries[i] = uint32Xdr.Decode(stream);
            }
            return result;
        }
    }
}
