using System;
using Stellar;

namespace StellarSDK
{
    /// <summary>
    /// Resource fee breakdown from <see cref="SorobanTransactionMetaExtV1"/> (stroops).
    /// </summary>
    public readonly struct SorobanFees
    {
        public long TotalNonRefundableResourceFeeCharged { get; }
        public long TotalRefundableResourceFeeCharged { get; }
        public long RentFeeCharged { get; }
        public long ResourceFeeCharged => TotalNonRefundableResourceFeeCharged + TotalRefundableResourceFeeCharged;

        public SorobanFees(long totalNonRefundableResourceFeeCharged, long totalRefundableResourceFeeCharged, long rentFeeCharged)
        {
            TotalNonRefundableResourceFeeCharged = totalNonRefundableResourceFeeCharged;
            TotalRefundableResourceFeeCharged = totalRefundableResourceFeeCharged;
            RentFeeCharged = rentFeeCharged;
        }
    }

    /// <summary>
    /// Soroban transaction meta from ledger <see cref="TransactionMeta"/> (v3 or v4).
    /// </summary>
    public readonly struct SorobanInvocationMeta
    {
        internal object Meta { get; }

        internal SorobanInvocationMeta(object meta)
        {
            if (meta is not SorobanTransactionMeta and not SorobanTransactionMetaV2)
            {
                throw new ArgumentException(
                    $"Expected {nameof(SorobanTransactionMeta)} or {nameof(SorobanTransactionMetaV2)}, got {meta?.GetType().Name ?? "null"}.",
                    nameof(meta));
            }
            Meta = meta;
        }

        public SorobanTransactionMetaExt Ext => Meta switch
        {
            SorobanTransactionMeta m => m.ext,
            SorobanTransactionMetaV2 m => m.ext,
            _ => null,
        };
    }
}
