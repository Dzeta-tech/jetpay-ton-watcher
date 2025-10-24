using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    [Length(32, 32)]
    [MinLength(32)]
    [MaxLength(32)]
    public byte[] Account { get; private set; } = null!;

    public bool IsTrackingActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static TrackedAddress Create(int workchain, byte[] account)
    {
        return new TrackedAddress
        {
            Id = Guid.NewGuid(),
            Workchain = workchain,
            Account = account,
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