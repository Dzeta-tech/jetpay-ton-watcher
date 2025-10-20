using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetPay.TonWatcher.Data.Models;

public class TrackedAddress
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Workchain ID (usually 0 for basechain, -1 for masterchain)
    public int Workchain { get; set; }
    
    // Account ID as 32 bytes (256 bits)
    [MaxLength(32)] 
    public byte[] Account { get; set; } = null!;

    // Address type: TON or Jetton
    public TrackedAddressType Type { get; set; } = TrackedAddressType.TON;

    public bool IsTrackingActive { get; set; } = true;
}

public enum TrackedAddressType
{
    TON,
    Jetton
}