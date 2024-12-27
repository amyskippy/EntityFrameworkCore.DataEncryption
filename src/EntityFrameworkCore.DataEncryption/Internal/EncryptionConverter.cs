﻿using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using SoftFluent.ComponentModel.DataAnnotations;
using System.Text;

namespace SoftFluent.EntityFrameworkCore.DataEncryption.Internal;

/// <summary>
/// Defines the internal encryption converter for string values.
/// </summary>
/// <typeparam name="TModel"></typeparam>
/// <typeparam name="TProvider"></typeparam>
internal sealed class EncryptionConverter<TModel, TProvider> : ValueConverter<TModel, TProvider>
{
    /// <summary>
    /// Creates a new <see cref="EncryptionConverter{TModel,TProvider}"/> instance.
    /// </summary>
    /// <param name="encryptionProvider">Encryption provider to use.</param>
    /// <param name="storageFormat">Encryption storage format.</param>
    /// <param name="mappingHints">Mapping hints.</param>
    public EncryptionConverter(IEncryptionProvider encryptionProvider, StorageFormat storageFormat, ConverterMappingHints mappingHints = null)
        : base(
            x => Encrypt<TModel, TProvider>(x, encryptionProvider, storageFormat),
            x => Decrypt<TModel, TProvider>(x, encryptionProvider, storageFormat), 
            mappingHints)
    {
    }

    /// <summary>
    /// Creates a new <see cref="EncryptionConverter{TModel,TProvider}"/> instance where the
    /// <see cref="IEncryptionProvider"/> is provided at runtime via a Func.
    /// </summary>
    /// <param name="encryptionProviderFunc">Func to resolve the Encryption provider that will be used.</param>
    /// <param name="storageFormat">Encryption storage format.</param>
    /// <param name="mappingHints">Mapping hints.</param>
    public EncryptionConverter(Func<IEncryptionProvider> encryptionProviderFunc, StorageFormat storageFormat, ConverterMappingHints mappingHints = null)
        : base(
            x => Encrypt<TModel, TProvider>(x, encryptionProviderFunc, storageFormat),
            x => Decrypt<TModel, TProvider>(x, encryptionProviderFunc, storageFormat),
            mappingHints)
    {
    }

    private static TOutput Encrypt<TInput, TOutput>(TInput input, IEncryptionProvider encryptionProvider, StorageFormat storageFormat)
    {
        byte[] inputData = input switch
        {
            string => !string.IsNullOrEmpty(input.ToString()) ? Encoding.UTF8.GetBytes(input.ToString()) : null,
            byte[] => input as byte[],
            _ => null,
        };

        byte[] encryptedRawBytes = encryptionProvider.Encrypt(inputData);

        if (encryptedRawBytes is null)
        {
            return default;
        }

        object encryptedData = storageFormat switch
        {
            StorageFormat.Default or StorageFormat.Base64 => Convert.ToBase64String(encryptedRawBytes),
            _ => encryptedRawBytes
        };

        return (TOutput)Convert.ChangeType(encryptedData, typeof(TOutput));
    }

    private static TOutput Encrypt<TInput, TOutput>(TInput input, Func<IEncryptionProvider> encryptionProviderFunc, StorageFormat storageFormat)
    {
        return Encrypt<TInput, TOutput>(input, encryptionProviderFunc(), storageFormat);
    }

    private static TModel Decrypt<TInput, TOutput>(TProvider input, IEncryptionProvider encryptionProvider, StorageFormat storageFormat)
    {
        Type destinationType = typeof(TModel);
        byte[] inputData = storageFormat switch
        {
            StorageFormat.Default or StorageFormat.Base64 => Convert.FromBase64String(input.ToString()),
            _ => input as byte[]
        };
        byte[] decryptedRawBytes = encryptionProvider.Decrypt(inputData);
        object decryptedData = null;

        if (decryptedRawBytes != null && destinationType == typeof(string))
        {
            decryptedData = Encoding.UTF8.GetString(decryptedRawBytes).Trim('\0');
        }
        else if (destinationType == typeof(byte[]))
        {
            decryptedData = decryptedRawBytes;
        }

        return (TModel)Convert.ChangeType(decryptedData, typeof(TModel));
    }

    private static TModel Decrypt<TInput, TOutput>(TProvider input, Func<IEncryptionProvider> encryptionProviderFunc, StorageFormat storageFormat)
    {
        return Decrypt<TInput, TOutput>(input, encryptionProviderFunc(), storageFormat);
    }
}
