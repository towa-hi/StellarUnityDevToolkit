using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Stellar;
using Stellar.Utilities;
using UnityEngine;

namespace StellarSDK
{
    public static class SCUtility
    {
        public static bool EnableLogging;

        static readonly Dictionary<Type, Func<object, SCVal>> toScValRegistry = new();
        static readonly Dictionary<Type, Func<SCVal, object>> fromScValRegistry = new();

        public static void Register<T>(Func<T, SCVal> toScVal, Func<SCVal, T> fromScVal)
        {
            toScValRegistry[typeof(T)] = obj => toScVal((T)obj);
            fromScValRegistry[typeof(T)] = scVal => fromScVal(scVal);
        }

        static void DebugLog(string msg)
        {
            if (EnableLogging) Debug.Log(msg);
        }

        static string DescribeType(Type t)
        {
            return t == null ? "null" : t.FullName;
        }

        static string DescribeScVal(SCVal v)
        {
            if (v == null) return "null";
            try
            {
                return $"{v.GetType().Name} (Discriminator {v.Discriminator})";
            }
            catch
            {
                return v.GetType().Name;
            }
        }

        public static SCVal NativeToSCVal(object input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            Type type = input.GetType();

            if (toScValRegistry.TryGetValue(type, out var registeredConverter))
                return registeredConverter(input);

            switch (input)
            {
                case var _ when type.IsEnum:
                    uint raw = Convert.ToUInt32(input);
                    return new SCVal.ScvU32() { u32 = new uint32(raw) };
                case var _ when type == typeof(uint):
                    return new SCVal.ScvU32() { u32 = new uint32((uint)input) };
                case var _ when type == typeof(ulong):
                    return new SCVal.ScvU64() { u64 = new uint64((ulong)input) };
                case var _ when type == typeof(int):
                    return new SCVal.ScvI32 { i32 = new int32((int)input) };
                case var _ when type == typeof(string):
                    return new SCVal.ScvString { str = new SCString((string)input) };
                case var _ when type == typeof(bool):
                    return new SCVal.ScvBool { b = (bool)input };
                case byte[] byteArray:
                    return new SCVal.ScvBytes() { bytes = byteArray };
                case Vector2Int vector2Int:
                    return new SCVal.ScvMap
                    {
                        map = new SCMap(new[]
                        {
                            FieldToSCMapEntry("x", vector2Int.x),
                            FieldToSCMapEntry("y", vector2Int.y),
                        }),
                    };
                case SCVal.ScvAddress address:
                    return address;
                case Array inputArray:
                    SCVal[] scValArray = new SCVal[inputArray.Length];
                    for (int i = 0; i < inputArray.Length; i++)
                    {
                        try
                        {
                            scValArray[i] = NativeToSCVal(inputArray.GetValue(i));
                        }
                        catch (Exception ex)
                        {
                            object element = inputArray.GetValue(i);
                            string elementType = element == null ? "null" : DescribeType(element.GetType());
                            throw new InvalidOperationException($"NativeToSCVal: Failed converting array element at index {i} of type '{elementType}'.", ex);
                        }
                    }
                    return new SCVal.ScvVec() { vec = new SCVec(scValArray) };
                case System.Collections.IDictionary inputDict:
                    SCMapEntry[] dictEntries = new SCMapEntry[inputDict.Count];
                    int dictIdx = 0;
                    foreach (System.Collections.DictionaryEntry kvp in inputDict)
                    {
                        try
                        {
                            dictEntries[dictIdx] = new SCMapEntry()
                            {
                                key = NativeToSCVal(kvp.Key),
                                val = NativeToSCVal(kvp.Value),
                            };
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                $"NativeToSCVal: Failed converting dictionary entry at index {dictIdx} " +
                                $"(key type '{DescribeType(kvp.Key?.GetType())}', value type '{DescribeType(kvp.Value?.GetType())}').", ex);
                        }
                        dictIdx++;
                    }
                    return new SCVal.ScvMap { map = new SCMap(dictEntries) };
                case IScvMapCompatable inputStruct:
                    return inputStruct.ToScvMap();
                default:
                    throw new NotImplementedException($"NativeToSCVal: Type '{DescribeType(type)}' not implemented. Use SCUtility.Register<T>() to add support.");
            }
        }

        static object SCValToNative(SCVal scVal, Type targetType)
        {
            if (scVal == null)
            {
                throw new ArgumentNullException(nameof(scVal));
            }

            if (fromScValRegistry.TryGetValue(targetType, out var registeredConverter))
                return registeredConverter(scVal);

            DebugLog($"SCValToNative: Converting SCVal of discriminator {scVal.Discriminator} to native type {targetType}.");
            switch (targetType)
            {
                case var _ when targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>):
                    Type nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);
                    if (scVal is SCVal.ScvVec nullableVec)
                    {
                        SCVal[] items = nullableVec.vec.InnerValue;
                        if (items.Length == 0)
                        {
                            return null;
                        }
                        if (items.Length == 1)
                        {
                            object innerValue = SCValToNative(items[0], nullableUnderlyingType);
                            return Activator.CreateInstance(targetType, innerValue);
                        }
                        Debug.LogWarning($"SCValToNative: Nullable vector has {items.Length} elements. Using first.");
                        object firstValue = SCValToNative(items[0], nullableUnderlyingType);
                        return Activator.CreateInstance(targetType, firstValue);
                    }
                    else
                    {
                        object innerValue = SCValToNative(scVal, nullableUnderlyingType);
                        return Activator.CreateInstance(targetType, innerValue);
                    }
                case var _ when targetType.IsEnum:
                    if (scVal is SCVal.ScvU32 scvU32)
                    {
                        DebugLog($"SCValToNative: Attempting to convert {scvU32.u32.InnerValue} to {targetType}.");
                        return Enum.ToObject(targetType, scvU32.u32.InnerValue);
                    }
                    break;
                case var _ when targetType == typeof(uint):
                    DebugLog("SCValToNative: Target type is uint.");
                    if (scVal is SCVal.ScvU32 uintVal)
                    {
                        DebugLog($"SCValToNative: Found SCVal.ScvU32 with value {uintVal.u32.InnerValue}.");
                        return uintVal.u32.InnerValue;
                    }
                    break;
                case var _ when targetType == typeof(int):
                    DebugLog("SCValToNative: Target type is int.");
                    if (scVal is SCVal.ScvI32 i32Val)
                    {
                        DebugLog($"SCValToNative: Found SCVal.ScvI32 with value {i32Val.i32.InnerValue}.");
                        return i32Val.i32.InnerValue;
                    }
                    else if (scVal is SCVal.ScvU32 intU32Val)
                    {
                        Debug.LogWarning("SCValToNative: Expected SCVal.ScvI32 for int conversion, got SCVal.ScvU32. Converting anyway.");
                        return intU32Val.u32.InnerValue;
                    }
                    break;
                case var _ when targetType == typeof(ulong):
                    DebugLog("SCValToNative: Target type is ulong.");
                    if (scVal is SCVal.ScvU64 u64Val)
                    {
                        DebugLog($"SCValToNative: Found SCVal.ScvU64 with value '{u64Val.u64.InnerValue}'.");
                        return u64Val.u64.InnerValue;
                    }
                    break;
                case var _ when targetType == typeof(string):
                    DebugLog("SCValToNative: Target type is string.");
                    if (scVal is SCVal.ScvString strVal)
                    {
                        DebugLog($"SCValToNative: Found SCVal.ScvString with value '{strVal.str.InnerValue}'.");
                        return strVal.str.InnerValue;
                    }
                    break;
                case var _ when targetType == typeof(bool):
                    DebugLog("SCValToNative: Target type is bool.");
                    if (scVal is SCVal.ScvBool boolVal)
                    {
                        DebugLog($"SCValToNative: Found SCVal.ScvBool with value {boolVal.b}.");
                        return boolVal.b;
                    }
                    break;
                case var _ when targetType == typeof(byte[]):
                    DebugLog("SCValToNative: Target type is byte[].");
                    if (scVal is SCVal.ScvBytes scvBytes)
                    {
                        DebugLog($"SCValToNative: Found SCVal.ScvBytes with length {scvBytes.bytes.InnerValue.Length}.");
                        return scvBytes.bytes.InnerValue;
                    }
                    break;
                default:
                    switch (scVal)
                    {
                        case SCVal.ScvBytes scvBytes2:
                            DebugLog($"SCValToNative: Getting bytes with length '{scvBytes2.bytes.InnerValue.Length}'.");
                            return scvBytes2.bytes.InnerValue;

                        case SCVal.ScvVec scvVec:
                            DebugLog("SCValToNative: Target type is a collection. Using vector conversion branch.");
                            Type elementType = targetType.IsArray
                                ? targetType.GetElementType()
                                : (targetType.IsGenericType ? targetType.GetGenericArguments()[0] : typeof(object));
                            if (elementType == null)
                            {
                                Debug.LogError("SCValToNative: Unable to determine element type for collection conversion.");
                                throw new NotSupportedException("Unable to determine element type for collection conversion.");
                            }
                            SCVal[] vecInnerArray = scvVec.vec.InnerValue;
                            int len = vecInnerArray.Length;
                            object[] convertedElements = new object[len];
                            for (int i = 0; i < len; i++)
                            {
                                DebugLog($"SCValToNative: Converting collection element at index {i}.");
                                try
                                {
                                    convertedElements[i] = SCValToNative(vecInnerArray[i], elementType);
                                }
                                catch (Exception ex)
                                {
                                    throw new InvalidOperationException($"SCValToNative: Failed converting collection element at index {i} from {DescribeScVal(vecInnerArray[i])} to element type '{DescribeType(elementType)}' (target collection type '{DescribeType(targetType)}').", ex);
                                }
                            }
                            if (targetType.IsArray)
                            {
                                Array arr = Array.CreateInstance(elementType, len);
                                for (int i = 0; i < len; i++)
                                {
                                    arr.SetValue(convertedElements[i], i);
                                }
                                DebugLog("SCValToNative: Collection converted to array.");
                                return arr;
                            }
                            break;

                        case SCVal.ScvMap scvMap:
                            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                            {
                                DebugLog("SCValToNative: Target type is Dictionary<,>. Using dictionary conversion branch.");
                                Type keyType = targetType.GetGenericArguments()[0];
                                Type valueType = targetType.GetGenericArguments()[1];
                                var dictionary = (System.Collections.IDictionary)Activator.CreateInstance(targetType);
                                foreach (SCMapEntry entry in scvMap.map.InnerValue)
                                {
                                    object dictKey;
                                    object dictVal;
                                    try
                                    {
                                        dictKey = SCValToNative(entry.key, keyType);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new InvalidOperationException(
                                            $"SCValToNative: Failed converting dictionary key from {DescribeScVal(entry.key)} to '{DescribeType(keyType)}'.", ex);
                                    }
                                    try
                                    {
                                        dictVal = SCValToNative(entry.val, valueType);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new InvalidOperationException(
                                            $"SCValToNative: Failed converting dictionary value from {DescribeScVal(entry.val)} to '{DescribeType(valueType)}'.", ex);
                                    }
                                    dictionary.Add(dictKey, dictVal);
                                }
                                return dictionary;
                            }
                            DebugLog("SCValToNative: Target type is either a map or a structured type.");
                            if (targetType.IsValueType && !targetType.IsPrimitive)
                            {
                                object instance = Activator.CreateInstance(targetType);
                                DebugLog("SCValToNative: Target type is a struct");
                                Dictionary<string, SCMapEntry> dict = new Dictionary<string, SCMapEntry>();
                                foreach (SCMapEntry entry in scvMap.map.InnerValue)
                                {
                                    if (entry.key is SCVal.ScvSymbol sym)
                                    {
                                        dict[sym.sym.InnerValue] = entry;
                                        DebugLog($"SCValToNative: Found map key '{sym.sym.InnerValue}'.");
                                    }
                                    else
                                    {
                                        Debug.LogError("SCValToNative: Expected map key to be SCVal.ScvSymbol.");
                                        throw new NotSupportedException("Expected map key to be SCVal.ScvSymbol.");
                                    }
                                }
                                if (targetType == typeof(Vector2Int))
                                {
                                    if (dict.TryGetValue("x", out SCMapEntry xEntry) && dict.TryGetValue("y", out SCMapEntry yEntry))
                                    {
                                        int x;
                                        int y;
                                        try
                                        {
                                            x = (int)SCValToNative(xEntry.val, typeof(int));
                                        }
                                        catch (Exception ex)
                                        {
                                            throw new InvalidOperationException($"SCValToNative: Failed converting Vector2Int field 'x' from {DescribeScVal(xEntry.val)} to 'int'.", ex);
                                        }
                                        try
                                        {
                                            y = (int)SCValToNative(yEntry.val, typeof(int));
                                        }
                                        catch (Exception ex)
                                        {
                                            throw new InvalidOperationException($"SCValToNative: Failed converting Vector2Int field 'y' from {DescribeScVal(yEntry.val)} to 'int'.", ex);
                                        }
                                        return new Vector2Int(x, y);
                                    }
                                    else
                                    {
                                        Debug.LogError("SCValToNative: Vector2Int conversion requires 'x' and 'y' fields in SCVal map.");
                                        throw new NotSupportedException("Vector2Int conversion requires 'x' and 'y' fields in SCVal map.");
                                    }
                                }
                                foreach (FieldInfo field in targetType.GetFields(BindingFlags.Instance | BindingFlags.Public))
                                {
                                    if (dict.TryGetValue(field.Name, out SCMapEntry mapEntry))
                                    {
                                        DebugLog($"SCValToNative: Converting field '{field.Name}'.");

                                        bool isNullableField = field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>);

                                        if (isNullableField && mapEntry.val is SCVal.ScvVec scvVecNullable)
                                        {
                                            SCVal[] nullableInnerArray = scvVecNullable.vec.InnerValue;
                                            if (nullableInnerArray.Length == 1)
                                            {
                                                DebugLog($"SCValToNative: Unwrapping single-item Vec for nullable field '{field.Name}'.");
                                                Type underlyingType = Nullable.GetUnderlyingType(field.FieldType);
                                                try
                                                {
                                                    object fieldValue = SCValToNative(nullableInnerArray[0], underlyingType);
                                                    field.SetValue(instance, fieldValue);
                                                }
                                                catch (Exception ex)
                                                {
                                                    throw new InvalidOperationException($"SCValToNative: Failed converting nullable field '{field.Name}' from {DescribeScVal(nullableInnerArray[0])} to '{DescribeType(underlyingType)}'.", ex);
                                                }
                                            }
                                            else if (nullableInnerArray.Length == 0)
                                            {
                                                DebugLog($"SCValToNative: Empty Vec treated as null for nullable field '{field.Name}'.");
                                                field.SetValue(instance, null);
                                            }
                                            else
                                            {
                                                Debug.LogWarning($"SCValToNative: Vec for nullable field '{field.Name}' has {nullableInnerArray.Length} items, expected 0 or 1.");
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                object fieldValue = SCValToNative(mapEntry.val, field.FieldType);
                                                field.SetValue(instance, fieldValue);
                                            }
                                            catch (Exception ex)
                                            {
                                                throw new InvalidOperationException($"SCValToNative: Failed converting field '{field.Name}' from {DescribeScVal(mapEntry.val)} to '{DescribeType(field.FieldType)}'.", ex);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        DebugLog($"SCValToNative: Field '{field.Name}' not found in SCVal map.");
                                    }
                                }
                                return instance;
                            }
                            break;
                    }
                    break;
            }
            DebugLog("SCValToNative: SCVal type not supported for conversion.");
            throw new NotSupportedException($"SCValToNative: Unsupported conversion from {DescribeScVal(scVal)} to target type '{DescribeType(targetType)}'. Use SCUtility.Register<T>() to add support.");
        }

        public static T SCValToNative<T>(SCVal scVal)
        {
            try
            {
                return (T)SCValToNative(scVal, typeof(T));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"SCValToNative<{DescribeType(typeof(T))}>: Failed converting {DescribeScVal(scVal)} to '{DescribeType(typeof(T))}'.", ex);
            }
        }

        public static SCMapEntry FieldToSCMapEntry(string fieldName, object input)
        {
            bool isNullable = false;

            if (input != null)
            {
                Type inputType = input.GetType();
                isNullable = inputType.IsGenericType && inputType.GetGenericTypeDefinition() == typeof(Nullable<>);
            }
            else
            {
                isNullable = true;
            }

            if (isNullable)
            {
                if (input == null)
                {
                    return new SCMapEntry()
                    {
                        key = new SCVal.ScvSymbol() { sym = new SCSymbol(fieldName) },
                        val = new SCVal.ScvVec() { vec = new SCVec(Array.Empty<SCVal>()) },
                    };
                }
                else
                {
                    SCVal inner;
                    try
                    {
                        inner = NativeToSCVal(input);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"NativeToSCVal: Failed converting field '{fieldName}' (nullable) of type '{DescribeType(input.GetType())}' to SCVal.", ex);
                    }
                    return new SCMapEntry()
                    {
                        key = new SCVal.ScvSymbol() { sym = new SCSymbol(fieldName) },
                        val = new SCVal.ScvVec() { vec = new SCVec(new SCVal[] { inner }) },
                    };
                }
            }
            else
            {
                SCVal value;
                try
                {
                    value = NativeToSCVal(input);
                }
                catch (Exception ex)
                {
                    string inputType = input == null ? "null" : DescribeType(input.GetType());
                    throw new InvalidOperationException($"NativeToSCVal: Failed converting field '{fieldName}' of type '{inputType}' to SCVal.", ex);
                }
                return new SCMapEntry()
                {
                    key = new SCVal.ScvSymbol() { sym = new SCSymbol(fieldName) },
                    val = value,
                };
            }
        }

        public static T FromXdrString<T>(string xdrString)
        {
            using MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(xdrString));
            SCVal val = SCValXdr.Decode(new XdrReader(memoryStream));
            return SCValToNative<T>(val);
        }

        public static byte[] Get16ByteHash(IScvMapCompatable obj)
        {
            SCVal scVal = obj.ToScvMap();
            string xdrString = SCValXdr.EncodeToBase64(scVal);
            using SHA256 sha256 = SHA256.Create();
            byte[] fullHash = sha256.ComputeHash(Convert.FromBase64String(xdrString));
            byte[] truncatedHash = new byte[16];
            Array.Copy(fullHash, truncatedHash, 16);
            return truncatedHash;
        }

        public static bool HashEqual(SCVal a, SCVal b)
        {
            string encodedA = SCValXdr.EncodeToBase64(a);
            string encodedB = SCValXdr.EncodeToBase64(b);
            return encodedA == encodedB;
        }
    }
}
