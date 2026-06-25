// Generated code - do not modify
// Source:

// struct TransactionMetaV4
// {
//     ExtensionPoint ext;
// 
//     LedgerEntryChanges txChangesBefore;  // tx level changes before operations
//                                          // are applied if any
//     OperationMetaV2 operations<>;        // meta for each operation
//     LedgerEntryChanges txChangesAfter;   // tx level changes after operations are
//                                          // applied if any
//     SorobanTransactionMetaV2* sorobanMeta; // Soroban-specific meta (only for
//                                            // Soroban transactions).
// 
//     TransactionEvent events<>; // Used for transaction-level events (like fee payment)
//     DiagnosticEvent diagnosticEvents<>; // Used for all diagnostic information
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
    public partial class TransactionMetaV4
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

        public LedgerEntryChanges txChangesBefore
        {
            get => _txChangesBefore;
            set
            {
                _txChangesBefore = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"Tx Changes Before")]
        #endif
        private LedgerEntryChanges _txChangesBefore;

        /// <summary>
        /// are applied if any
        /// </summary>
        public OperationMetaV2[] operations
        {
            get => _operations;
            set
            {
                _operations = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"Operations")]
        #endif
        private OperationMetaV2[] _operations;

        /// <summary>
        /// meta for each operation
        /// </summary>
        public LedgerEntryChanges txChangesAfter
        {
            get => _txChangesAfter;
            set
            {
                _txChangesAfter = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"Tx Changes After")]
        #endif
        private LedgerEntryChanges _txChangesAfter;

        /// <summary>
        /// applied if any
        /// </summary>
        public SorobanTransactionMetaV2 sorobanMeta
        {
            get => _sorobanMeta;
            set
            {
                _sorobanMeta = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"Soroban Meta")]
        #endif
        private SorobanTransactionMetaV2 _sorobanMeta;

        public TransactionEvent[] events
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
        private TransactionEvent[] _events;

        /// <summary>
        /// Used for transaction-level events (like fee payment)
        /// </summary>
        public DiagnosticEvent[] diagnosticEvents
        {
            get => _diagnosticEvents;
            set
            {
                _diagnosticEvents = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"Diagnostic Events")]
        #endif
        private DiagnosticEvent[] _diagnosticEvents;

        public TransactionMetaV4()
        {
        }
        /// <summary>Validates all fields have valid values</summary>
        public virtual void Validate()
        {
        }
    }
    public static partial class TransactionMetaV4Xdr
    {
        /// <summary>Encodes value to XDR base64 string</summary>
        public static string EncodeToBase64(TransactionMetaV4 value)
        {
            using (var memoryStream = new MemoryStream())
            {
                XdrWriter writer = new XdrWriter(memoryStream);
                TransactionMetaV4Xdr.Encode(writer, value);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
        /// <summary>Encodes struct to XDR stream</summary>
        public static void Encode(XdrWriter stream, TransactionMetaV4 value)
        {
            value.Validate();
            ExtensionPointXdr.Encode(stream, value.ext);
            LedgerEntryChangesXdr.Encode(stream, value.txChangesBefore);
            stream.WriteInt(value.operations.Length);
            foreach (var item in value.operations)
            {
                    OperationMetaV2Xdr.Encode(stream, item);
            }
            LedgerEntryChangesXdr.Encode(stream, value.txChangesAfter);
            if (value.sorobanMeta==null){
            	stream.WriteInt(0);
            }
            else
            {
                stream.WriteInt(1);
                SorobanTransactionMetaV2Xdr.Encode(stream, value.sorobanMeta);
            }
            stream.WriteInt(value.events.Length);
            foreach (var item in value.events)
            {
                    TransactionEventXdr.Encode(stream, item);
            }
            stream.WriteInt(value.diagnosticEvents.Length);
            foreach (var item in value.diagnosticEvents)
            {
                    DiagnosticEventXdr.Encode(stream, item);
            }
        }
        /// <summary>Decodes struct from XDR stream</summary>
        public static TransactionMetaV4 Decode(XdrReader stream)
        {
            var result = new TransactionMetaV4();
            result.ext = ExtensionPointXdr.Decode(stream);
            result.txChangesBefore = LedgerEntryChangesXdr.Decode(stream);
            {
                var length = stream.ReadInt();
                result.operations = new OperationMetaV2[length];
                for (var i = 0; i < length; i++)
                {
                    result.operations[i] = OperationMetaV2Xdr.Decode(stream);
                }
            }
            result.txChangesAfter = LedgerEntryChangesXdr.Decode(stream);
            if (stream.ReadInt()==1)
            {
                result.sorobanMeta = SorobanTransactionMetaV2Xdr.Decode(stream);
            }
            {
                var length = stream.ReadInt();
                result.events = new TransactionEvent[length];
                for (var i = 0; i < length; i++)
                {
                    result.events[i] = TransactionEventXdr.Decode(stream);
                }
            }
            {
                var length = stream.ReadInt();
                result.diagnosticEvents = new DiagnosticEvent[length];
                for (var i = 0; i < length; i++)
                {
                    result.diagnosticEvents[i] = DiagnosticEventXdr.Decode(stream);
                }
            }
            return result;
        }
    }
}
