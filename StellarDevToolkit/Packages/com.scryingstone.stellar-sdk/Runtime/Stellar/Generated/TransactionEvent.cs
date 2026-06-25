// Generated code - do not modify
// Source:

// struct TransactionEvent {    
//     TransactionEventStage stage;  // Stage at which an event has occurred.
//     ContractEvent event;  // The contract event that has occurred.
// };


using System;
using System.IO;
using System.ComponentModel.DataAnnotations;
#if UNITY
	using UnityEngine;
#endif

namespace Stellar {

    /// <summary>
    /// refunded).
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("XdrGenerator", "1.0")]
    [System.Serializable]
    public partial class TransactionEvent
    {
        public TransactionEventStage stage
        {
            get => _stage;
            set
            {
                _stage = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"Stage")]
        #endif
        private TransactionEventStage _stage;

        /// <summary>
        /// Stage at which an event has occurred.
        /// </summary>
        public ContractEvent _event
        {
            get => __event;
            set
            {
                __event = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"_event")]
        #endif
        private ContractEvent __event;

        public TransactionEvent()
        {
        }
        /// <summary>Validates all fields have valid values</summary>
        public virtual void Validate()
        {
        }
    }
    public static partial class TransactionEventXdr
    {
        /// <summary>Encodes value to XDR base64 string</summary>
        public static string EncodeToBase64(TransactionEvent value)
        {
            using (var memoryStream = new MemoryStream())
            {
                XdrWriter writer = new XdrWriter(memoryStream);
                TransactionEventXdr.Encode(writer, value);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
        /// <summary>Encodes struct to XDR stream</summary>
        public static void Encode(XdrWriter stream, TransactionEvent value)
        {
            value.Validate();
            TransactionEventStageXdr.Encode(stream, value.stage);
            ContractEventXdr.Encode(stream, value._event);
        }
        /// <summary>Decodes struct from XDR stream</summary>
        public static TransactionEvent Decode(XdrReader stream)
        {
            var result = new TransactionEvent();
            result.stage = TransactionEventStageXdr.Decode(stream);
            result._event = ContractEventXdr.Decode(stream);
            return result;
        }
    }
}
