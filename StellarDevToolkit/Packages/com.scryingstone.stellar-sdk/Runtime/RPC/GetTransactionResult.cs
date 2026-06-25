using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace Stellar.RPC
{

    public partial class GetTransactionResult
    {
        [Newtonsoft.Json.JsonProperty("diagnosticEventsXdr", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoMember(103)]
        public ICollection<string> DiagnosticEventsXdr { get; set; }

        private TransactionResult _transactionResult;
        [ProtoMember(100)]
        [Newtonsoft.Json.JsonIgnore]
        public TransactionResult TransactionResult
        {
            get
            {
                if (_transactionResult == null)
                {
                    if (ResultXdr != null)
                    {
                        byte[] bytes = Convert.FromBase64String(ResultXdr);
                        using (MemoryStream stream = new MemoryStream(bytes))
                        {
                            _transactionResult = TransactionResultXdr.Decode(new XdrReader(stream));
                        }
                    }
                }
                return _transactionResult;
            }
        }

        private TransactionMeta _transactionResultMeta;
        [ProtoMember(101)]
        [Newtonsoft.Json.JsonIgnore]
        public TransactionMeta TransactionResultMeta
        {
            get
            {
                if (_transactionResultMeta == null)
                {
                    if (ResultMetaXdr != null)
                    {
                        byte[] bytes = Convert.FromBase64String(ResultMetaXdr);
                        using (MemoryStream stream = new MemoryStream(bytes))
                        {
                            _transactionResultMeta = TransactionMetaXdr.Decode(new XdrReader(stream));
                        }
                    }
                }
                return _transactionResultMeta;
            }
        }

        private TransactionEnvelope _transactionEnvelope;
        [ProtoMember(102)]
        [Newtonsoft.Json.JsonIgnore]
        public TransactionEnvelope TransactionEnvelope
        {
            get
            {
                if (_transactionEnvelope == null)
                {
                    if (EnvelopeXdr != null)
                    {
                        byte[] bytes = Convert.FromBase64String(EnvelopeXdr);
                        using (MemoryStream stream = new MemoryStream(bytes))
                        {
                            _transactionEnvelope = TransactionEnvelopeXdr.Decode(new XdrReader(stream));
                        }
                    }
                }
                return _transactionEnvelope;
            }
        }
    }
}
