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
    /// <summary>
    /// Identifies that this property is encrypted using the <see cref="SoftFluent.EntityFrameworkCore.DataEncryption"/>
    /// package.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="builder">The <see cref="PropertyBuilder{TProperty}"/> to configure.</param>
    /// <param name="storageFormat">The storage format to use for the encrypted value.
    /// See <see cref="StorageFormat"/>, Defaults to <see cref="StorageFormat.Default"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="builder"/> is null.</exception>
    public static PropertyBuilder<TProperty> IsEncrypted<TProperty>(this PropertyBuilder<TProperty> builder,
        StorageFormat storageFormat = StorageFormat.Default)
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
