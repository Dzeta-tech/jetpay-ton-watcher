using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TonSdk.Core;
using TonSdk.Core.EntityFrameworkCore;

namespace JetPay.TonWatcher.Infrastructure.Persistence.Attributes;

/// <summary>
/// Attribute to mark Address properties for binary conversion in EF Core.
/// Stores Address as 36 bytes (4 bytes workchain + 32 bytes hash).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AddressConversionAttribute : Attribute
{
    /// <summary>
    /// Gets the value converter for Address to byte[] conversion.
    /// </summary>
    public static ValueConverter GetConverter() => new AddressValueConverter();
}

