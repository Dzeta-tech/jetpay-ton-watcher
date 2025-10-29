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

    public int Workchain { get; private set; }

    [Length(32, 32)] [MaxLength(32)] public byte[] Hash { get; private set; } = null!;

    public bool IsTrackingActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static TrackedAddress Create(Address address)
    {
        return new TrackedAddress
        {
            Id = Guid.NewGuid(),
            Workchain = address.Workchain,
            Hash = address.Hash,
            IsTrackingActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsTrackingActive = false;
    }
}