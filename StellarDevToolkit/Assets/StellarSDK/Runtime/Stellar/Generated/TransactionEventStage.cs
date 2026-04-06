// Generated code - do not modify
// Source:

// enum TransactionEventStage {
//     // The event has happened before any one of the transactions has its 
//     // operations applied.
//     TRANSACTION_EVENT_STAGE_BEFORE_ALL_TXS = 0,
//     // The event has happened immediately after operations of the transaction
//     // have been applied.
//     TRANSACTION_EVENT_STAGE_AFTER_TX = 1,
//     // The event has happened after every transaction had its operations 
//     // applied.
//     TRANSACTION_EVENT_STAGE_AFTER_ALL_TXS = 2
// };


using System;
using System.IO;
using System.ComponentModel.DataAnnotations;
#if UNITY
	using UnityEngine;
#endif

namespace Stellar {

    /// <summary>
    /// emitted.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("XdrGenerator", "1.0")]
    [System.Serializable]
    public enum TransactionEventStage
    {
        /// <summary>
        /// operations applied.
        /// </summary>
        TRANSACTION_EVENT_STAGE_BEFORE_ALL_TXS = 0,
        /// <summary>
        /// have been applied.
        /// </summary>
        TRANSACTION_EVENT_STAGE_AFTER_TX = 1,
        /// <summary>
        /// applied.
        /// </summary>
        TRANSACTION_EVENT_STAGE_AFTER_ALL_TXS = 2,
    }

    public static partial class TransactionEventStageXdr
    {
        /// <summary>Encodes value to XDR base64 string</summary>
        public static string EncodeToBase64(TransactionEventStage value)
        {
            using (var memoryStream = new MemoryStream())
            {
                XdrWriter writer = new XdrWriter(memoryStream);
                TransactionEventStageXdr.Encode(writer, value);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
        /// <summary>Encodes enum value to XDR stream</summary>
        public static void Encode(XdrWriter stream, TransactionEventStage value)
        {
            stream.WriteInt((int)value);
        }
        /// <summary>Decodes enum value from XDR stream</summary>
        public static TransactionEventStage Decode(XdrReader stream)
        {
            var value = stream.ReadInt();
            if (!Enum.IsDefined(typeof(TransactionEventStage), value))
              throw new InvalidOperationException($"Unknown TransactionEventStage value: {value}");
            return (TransactionEventStage)value;
        }
    }
}
