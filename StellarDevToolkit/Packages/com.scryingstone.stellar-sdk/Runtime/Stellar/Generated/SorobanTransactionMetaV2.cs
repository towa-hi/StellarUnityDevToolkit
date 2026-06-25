// Generated code - do not modify
// Source:

// struct SorobanTransactionMetaV2
// {
//     SorobanTransactionMetaExt ext;
// 
//     SCVal* returnValue;
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
    public partial class SorobanTransactionMetaV2
    {
        public SorobanTransactionMetaExt ext
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
        private SorobanTransactionMetaExt _ext;

        public SCVal returnValue
        {
            get => _returnValue;
            set
            {
                _returnValue = value;
            }
        }
        #if UNITY
        	[SerializeField]
        	[SerializeReference]
        	[InspectorName(@"Return Value")]
        #endif
        private SCVal _returnValue;

        public SorobanTransactionMetaV2()
        {
        }
        /// <summary>Validates all fields have valid values</summary>
        public virtual void Validate()
        {
        }
    }
    public static partial class SorobanTransactionMetaV2Xdr
    {
        /// <summary>Encodes value to XDR base64 string</summary>
        public static string EncodeToBase64(SorobanTransactionMetaV2 value)
        {
            using (var memoryStream = new MemoryStream())
            {
                XdrWriter writer = new XdrWriter(memoryStream);
                SorobanTransactionMetaV2Xdr.Encode(writer, value);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
        /// <summary>Encodes struct to XDR stream</summary>
        public static void Encode(XdrWriter stream, SorobanTransactionMetaV2 value)
        {
            value.Validate();
            SorobanTransactionMetaExtXdr.Encode(stream, value.ext);
            if (value.returnValue==null){
            	stream.WriteInt(0);
            }
            else
            {
                stream.WriteInt(1);
                SCValXdr.Encode(stream, value.returnValue);
            }
        }
        /// <summary>Decodes struct from XDR stream</summary>
        public static SorobanTransactionMetaV2 Decode(XdrReader stream)
        {
            var result = new SorobanTransactionMetaV2();
            result.ext = SorobanTransactionMetaExtXdr.Decode(stream);
            if (stream.ReadInt()==1)
            {
                result.returnValue = SCValXdr.Decode(stream);
            }
            return result;
        }
    }
}
