using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetPay.TonWatcher.Infrastructure.Persistence.Attributes;
using TonSdk.Core;

namespace JetPay.TonWatcher.Domain.Entities;

public class TrackedAddress
{
    TrackedAddress()
    {
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; private set; }

    [AddressConversion]
    [MaxLength(36)] // 4 bytes (int32 workchain) + 32 bytes (hash)
    public Address Address { get; private set; }

    public bool IsTrackingActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static TrackedAddress Create(Address address)
    {
        return new TrackedAddress
        {
            Id = Guid.NewGuid(),
            Address = address,
            IsTrackingActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsTrackingActive = false;
    }

    public void Activate()
    {
        IsTrackingActive = true;
    }
}