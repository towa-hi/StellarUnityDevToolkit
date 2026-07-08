using System;
using System.IO;

namespace Stellar.RPC
{
    /// <summary>
    /// Parameters for getLedgers method
    /// </summary>
    [ProtoBuf.ProtoContract] public partial class GetLedgersParams
    {
        /// <summary>
        /// Ledger sequence number to start fetching responses from (inclusive). Must be omitted when a cursor is provided in Pagination.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("startLedger", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(1)] public long?  StartLedger { get; set; }

        [Newtonsoft.Json.JsonProperty("pagination", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(2)] public Pagination  Pagination { get; set; }



        private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    [ProtoBuf.ProtoContract] public partial class GetLedgersResult
    {
        [Newtonsoft.Json.JsonProperty("ledgers", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(1)] public System.Collections.Generic.ICollection<Ledgers>  Ledgers { get; set; }

        /// <summary>
        /// The sequence number of the latest ledger known to Stellar RPC at the time it handled the request.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("latestLedger", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(2)] public long  LatestLedger { get; set; }

        /// <summary>
        /// The unix timestamp of the close time of the latest ledger known to Stellar RPC at the time it handled the request.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("latestLedgerCloseTime", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(3)] public long  LatestLedgerCloseTime { get; set; }

        /// <summary>
        /// The sequence number of the oldest ledger ingested by Stellar RPC at the time it handled the request.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("oldestLedger", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(4)] public long  OldestLedger { get; set; }

        /// <summary>
        /// The unix timestamp of the close time of the oldest ledger ingested by Stellar RPC at the time it handled the request.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("oldestLedgerCloseTime", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(5)] public long  OldestLedgerCloseTime { get; set; }

        /// <summary>
        /// A token which can be included in a subsequent request to obtain the next page of results.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("cursor", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(6)] public string  Cursor { get; set; }



        private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    /// <summary>
    /// Object containing information about a single ledger.
    /// </summary>
    [ProtoBuf.ProtoContract] public partial class Ledgers
    {
        /// <summary>
        /// The hash of the ledger header which was included in the chain.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("hash", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(1)] public string  Hash { get; set; }

        /// <summary>
        /// The sequence number of the ledger (sometimes called the 'block height').
        /// </summary>
        [Newtonsoft.Json.JsonProperty("sequence", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(2)] public long  Sequence { get; set; }

        /// <summary>
        /// The unix timestamp at which the ledger was closed.
        /// </summary>
        [Newtonsoft.Json.JsonProperty("ledgerCloseTime", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(3)] public long  LedgerCloseTime { get; set; }

        /// <summary>
        /// The LedgerHeaderHistoryEntry structure for this ledger (base64-encoded string).
        /// </summary>
        [Newtonsoft.Json.JsonProperty("headerXdr", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(4)] public string  HeaderXdr { get; set; }

        /// <summary>
        /// The LedgerCloseMeta union for this ledger (base64-encoded string).
        /// </summary>
        [Newtonsoft.Json.JsonProperty("metadataXdr", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [ProtoBuf.ProtoMember(5)] public string  MetadataXdr { get; set; }

        private LedgerHeaderHistoryEntry _header;
        [Newtonsoft.Json.JsonIgnore]
        public LedgerHeaderHistoryEntry Header
        {
            get
            {
                if (_header == null)
                {
                    if (HeaderXdr != null)
                    {
                        byte[] bytes = Convert.FromBase64String(HeaderXdr);
                        using (MemoryStream stream = new MemoryStream(bytes))
                        {
                            _header = LedgerHeaderHistoryEntryXdr.Decode(new XdrReader(stream));
                        }
                    }
                }
                return _header;
            }
        }

        private LedgerCloseMeta _metadata;
        [Newtonsoft.Json.JsonIgnore]
        public LedgerCloseMeta Metadata
        {
            get
            {
                if (_metadata == null)
                {
                    if (MetadataXdr != null)
                    {
                        byte[] bytes = Convert.FromBase64String(MetadataXdr);
                        using (MemoryStream stream = new MemoryStream(bytes))
                        {
                            _metadata = LedgerCloseMetaXdr.Decode(new XdrReader(stream));
                        }
                    }
                }
                return _metadata;
            }
        }



        private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }
}
