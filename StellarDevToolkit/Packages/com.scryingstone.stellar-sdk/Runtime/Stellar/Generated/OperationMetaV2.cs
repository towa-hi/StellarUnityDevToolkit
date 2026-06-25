// Generated code - do not modify
// Source:

// struct OperationMetaV2
// {
//     ExtensionPoint ext;
// 
//     LedgerEntryChanges changes;
// 
//     ContractEvent events<>;
// };


using System;
using System.IO;
using System.ComponentModel.DataAnnotations;
#if UNITY
	using UnityEngine;
#endif

namespace Stellar {

    [System.CodeDom.Compiler.GeneratedCode("XdrGenerator", "1.0")]
    [System.Serializable]
    public partial class OperationMetaV2
    {
        public ExtensionPoint ext
        {
            get => _ext;
            set
            {
                _ext = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"Ext")]
        #endif
        private ExtensionPoint _ext;

        public LedgerEntryChanges changes
        {
            get => _changes;
            set
            {
                _changes = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"Changes")]
        #endif
        private LedgerEntryChanges _changes;

        public ContractEvent[] events
        {
            get => _events;
            set
            {
                _events = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"Events")]
        #endif
        private ContractEvent[] _events;

        public OperationMetaV2()
        {
        }
        /// <summary>Validates all fields have valid values</summary>
        public virtual void Validate()
        {
        }
    }
    public static partial class OperationMetaV2Xdr
    {
        /// <summary>Encodes value to XDR base64 string</summary>
        public static string EncodeToBase64(OperationMetaV2 value)
        {
            using (var memoryStream = new MemoryStream())
            {
                XdrWriter writer = new XdrWriter(memoryStream);
                OperationMetaV2Xdr.Encode(writer, value);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
        /// <summary>Encodes struct to XDR stream</summary>
        public static void Encode(XdrWriter stream, OperationMetaV2 value)
        {
            value.Validate();
            ExtensionPointXdr.Encode(stream, value.ext);
            LedgerEntryChangesXdr.Encode(stream, value.changes);
            stream.WriteInt(value.events.Length);
            foreach (var item in value.events)
            {
                    ContractEventXdr.Encode(stream, item);
            }
        }
        /// <summary>Decodes struct from XDR stream</summary>
        public static OperationMetaV2 Decode(XdrReader stream)
        {
            var result = new OperationMetaV2();
            result.ext = ExtensionPointXdr.Decode(stream);
            result.changes = LedgerEntryChangesXdr.Decode(stream);
            {
                var length = stream.ReadInt();
                result.events = new ContractEvent[length];
                for (var i = 0; i < length; i++)
                {
                    result.events[i] = ContractEventXdr.Decode(stream);
                }
            }
            return result;
        }
    }
}
