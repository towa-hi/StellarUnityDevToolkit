using System;
using UnityEngine;

namespace StellarSDK
{
    public enum StatusCode
    {
        SUCCESS,
        CONTRACT_ERROR,
        NETWORK_ERROR,
        RPC_ERROR,
        TIMEOUT,
        OTHER_ERROR,
        SERIALIZATION_ERROR,
        DESERIALIZATION_ERROR,
        TRANSACTION_FAILED,
        TRANSACTION_NOT_FOUND,
        TRANSACTION_TIMEOUT,
        ENTRY_NOT_FOUND,
        SIMULATION_FAILED,
        TRANSACTION_SEND_FAILED,
        WALLET_ERROR,
        WALLET_NOT_AVAILABLE,
        WALLET_ADDRESS_MISSING,
        WALLET_NETWORK_DETAILS_ERROR,
        WALLET_PARSING_ERROR,
        WALLET_SIGNING_ERROR,
        WALLET_SIGNING_CANCELLED,
    }

    public readonly struct Result<T>
    {
        public StatusCode Code { get; }
        public T Value { get; }
        public string Message { get; }

        public bool IsOk => Code == StatusCode.SUCCESS;
        public bool IsError => !IsOk;

        Result(StatusCode code, T value, string message)
        {
            Code = code;
            Value = value;
            Message = message;
        }

        static void LogError(StatusCode code, string message)
        {
            string safeMessage = string.IsNullOrEmpty(message) ? "No details provided." : message;
            Debug.LogError($"[{code} ({(int)code})] {safeMessage}");
        }

        public static Result<T> Ok(T value)
        {
            return new Result<T>(StatusCode.SUCCESS, value, null);
        }

        public static Result<T> Err(StatusCode code, string message = null)
        {
            if (code == StatusCode.SUCCESS)
            {
                throw new ArgumentException("Err cannot be created with SUCCESS code");
            }

            if (code == StatusCode.ENTRY_NOT_FOUND)
            {
                string safeMessage = string.IsNullOrEmpty(message) ? "No details provided." : message;
                Debug.LogWarning($"{code} {safeMessage}");
            }
            else
            {
                LogError(code, message);
            }
            return new Result<T>(code, default, message);
        }

        public static Result<T> Err(StatusCode code, T value, string message = null)
        {
            LogError(code, message);
            return new Result<T>(code, value, message);
        }

        public static Result<T> Err<TOther>(Result<TOther> errorResult)
        {
            if (errorResult.IsOk)
            {
                throw new ArgumentException("Error cannot be created with Ok code");
            }
            return new Result<T>(errorResult.Code, default, errorResult.Message);
        }

        public void Deconstruct(out StatusCode code, out T value)
        {
            code = Code;
            value = Value;
        }
    }
}
