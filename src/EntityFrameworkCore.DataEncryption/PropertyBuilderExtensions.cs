using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftFluent.EntityFrameworkCore.DataEncryption.Internal;
using System;
using SoftFluent.ComponentModel.DataAnnotations;

namespace SoftFluent.EntityFrameworkCore.DataEncryption;

/// <summary>
/// Provides extensions for the <see cref="PropertyBuilder"/> type.
/// </summary>
public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<TProperty> IsEncrypted<TProperty>(this PropertyBuilder<TProperty> builder, StorageFormat storageFormat = StorageFormat.Default)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.HasAnnotation(PropertyAnnotations.IsEncrypted, true);
        builder.HasAnnotation(PropertyAnnotations.StorageFormat, storageFormat);

        return builder;
    }
}
