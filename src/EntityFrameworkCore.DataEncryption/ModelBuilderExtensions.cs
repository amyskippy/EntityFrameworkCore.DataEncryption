﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SoftFluent.EntityFrameworkCore.DataEncryption.Internal;
using System;
using System.Collections.Generic;
using SoftFluent.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace SoftFluent.EntityFrameworkCore.DataEncryption;

/// <summary>
/// Provides extensions for the <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Enables encryption on this model using an encryption provider.
    /// </summary>
    /// <param name="modelBuilder">
    /// The <see cref="ModelBuilder"/> instance.
    /// </param>
    /// <param name="encryptionProvider">
    /// The <see cref="IEncryptionProvider"/> to use, if any.
    /// </param>
    /// <returns>
    /// The updated <paramref name="modelBuilder"/>.
    /// </returns>
    public static ModelBuilder UseEncryption(this ModelBuilder modelBuilder, IEncryptionProvider encryptionProvider)
    {
        if (modelBuilder is null)
        {
            throw new ArgumentNullException(nameof(modelBuilder));
        }

        if (encryptionProvider is null)
        {
            throw new ArgumentNullException(nameof(encryptionProvider));
        }

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            IEnumerable<EncryptedProperty> encryptedProperties = GetEntityEncryptedProperties(entityType);

            foreach (EncryptedProperty encryptedProperty in encryptedProperties)
            {
#pragma warning disable EF1001 // Internal EF Core API usage.
                if (encryptedProperty.Property.FindAnnotation(CoreAnnotationNames.ValueConverter) is not null)
                {
                    continue;
                }
#pragma warning restore EF1001 // Internal EF Core API usage.

                ValueConverter converter = GetValueConverter(encryptedProperty.Property.ClrType, encryptionProvider, encryptedProperty.StorageFormat);

                if (converter != null)
                {
                    encryptedProperty.Property.SetValueConverter(converter);
                }
            }
        }

        return modelBuilder;
    }

    private static ValueConverter GetValueConverter(Type propertyType, IEncryptionProvider encryptionProvider, StorageFormat storageFormat)
    {
        if (propertyType == typeof(string))
        {
            return storageFormat switch
            {
                StorageFormat.Default or StorageFormat.Base64 => new EncryptionConverter<string, string>(encryptionProvider, StorageFormat.Base64),
                StorageFormat.Binary => new EncryptionConverter<string, byte[]>(encryptionProvider, StorageFormat.Binary),
                _ => throw new NotImplementedException()
            };
        }
        else if (propertyType == typeof(byte[]))
        {
            return storageFormat switch
            {
                StorageFormat.Default or StorageFormat.Binary => new EncryptionConverter<byte[], byte[]>(encryptionProvider, StorageFormat.Binary),
                StorageFormat.Base64 => new EncryptionConverter<byte[], string>(encryptionProvider, StorageFormat.Base64),
                _ => throw new NotImplementedException()
            };
        }

        throw new NotImplementedException($"Type {propertyType.Name} does not support encryption.");
    }

    /// <summary>
    /// Enables encryption on this model using a function that provides an encryption provider.
    /// This is useful to provide an encryption provider from your dependency injection service collection,
    /// allowing encryption providers to have access to the current request state, or other dependencies.
    /// </summary>
    /// <param name="modelBuilder">
    /// The <see cref="ModelBuilder"/> instance.
    /// </param>
    /// <param name="encryptionProviderFunc">
    /// A function that returns an <see cref="IEncryptionProvider"/> to use for encryption.
    /// </param>
    /// <returns>
    /// The updated <paramref name="modelBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="modelBuilder"/> or <paramref name="encryptionProviderFunc"/> is null.
    /// </exception>
    public static ModelBuilder UseEncryption(this ModelBuilder modelBuilder, Func<IEncryptionProvider> encryptionProviderFunc)
    {
        if (modelBuilder is null)
        {
            throw new ArgumentNullException(nameof(modelBuilder));
        }

        if (encryptionProviderFunc == null)
        {
            throw new ArgumentNullException(nameof(encryptionProviderFunc));
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var encryptedProperties = GetEntityEncryptedProperties(entityType);

            foreach (var encryptedProperty in encryptedProperties)
            {
                if (encryptedProperty.Property == null)
                {
                    // Skip invalid properties
                    continue;
                }

#pragma warning disable EF1001 // Internal EF Core API usage.
                if (encryptedProperty.Property.FindAnnotation(CoreAnnotationNames.ValueConverter) is not null)
                {
                    // Skip properties that already have a converter
                    continue;
                }
#pragma warning restore EF1001 // Internal EF Core API usage.

                // Create and assign the value converter
                var converter = GetValueConverter(encryptedProperty.Property.ClrType, encryptionProviderFunc, encryptedProperty.StorageFormat);

                if (converter != null)
                {
                    encryptedProperty.Property.SetValueConverter(converter);
                }
            }
        }

        return modelBuilder;
    }

    private static ValueConverter GetValueConverter(Type propertyType, Func<IEncryptionProvider> encryptionProviderFunc, StorageFormat storageFormat)
    {
        if (propertyType == typeof(string))
        {
            return storageFormat switch
            {
                StorageFormat.Default or StorageFormat.Base64 => new EncryptionConverter<string, string>(encryptionProviderFunc, StorageFormat.Base64),
                StorageFormat.Binary => new EncryptionConverter<string, byte[]>(encryptionProviderFunc, StorageFormat.Binary),
                _ => throw new NotImplementedException()
            };
        }

        if (propertyType == typeof(byte[]))
        {
            return storageFormat switch
            {
                StorageFormat.Default or StorageFormat.Binary => new EncryptionConverter<byte[], byte[]>(encryptionProviderFunc, StorageFormat.Binary),
                StorageFormat.Base64 => new EncryptionConverter<byte[], string>(encryptionProviderFunc, StorageFormat.Base64),
                _ => throw new NotImplementedException()
            };
        }

        throw new NotImplementedException($"Type {propertyType.Name} does not support encryption.");
    }

    private static IEnumerable<EncryptedProperty> GetEntityEncryptedProperties(IMutableEntityType entity)
    {
        return entity.GetProperties()
            .Select(x => EncryptedProperty.Create(x))
            .Where(x => x is not null);
    }

    internal class EncryptedProperty
    {
        public IMutableProperty Property { get; }

        public StorageFormat StorageFormat { get; }

        private EncryptedProperty(IMutableProperty property, StorageFormat storageFormat)
        {
            Property = property;
            StorageFormat = storageFormat;
        }

        public static EncryptedProperty Create(IMutableProperty property)
        {
            StorageFormat? storageFormat = null;

            var encryptedAttribute = property.PropertyInfo?.GetCustomAttribute<EncryptedAttribute>(false);

            if (encryptedAttribute != null)
            {
                storageFormat = encryptedAttribute.Format;
            }

            IAnnotation encryptedAnnotation = property.FindAnnotation(PropertyAnnotations.IsEncrypted);

            if (encryptedAnnotation != null && (bool)encryptedAnnotation.Value == true)
            {
                storageFormat = (StorageFormat)property.FindAnnotation(PropertyAnnotations.StorageFormat)?.Value;
            }

            return storageFormat.HasValue ? new EncryptedProperty(property, storageFormat.Value) : null;
        }
    }
}
