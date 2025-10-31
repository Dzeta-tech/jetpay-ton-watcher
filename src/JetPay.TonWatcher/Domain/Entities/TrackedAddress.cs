using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ton.Core.Addresses;

namespace JetPay.TonWatcher.Domain.Entities;

public class TrackedAddress
{
    TrackedAddress()
    {
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; private set; }

    [Length(66, 68)] public Address Address { get; private set; } = null!;

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
}