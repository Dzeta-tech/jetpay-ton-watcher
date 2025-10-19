using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetPay.TonWatcher.Data.Models;

public class TrackedAddress
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Address is TON address, 64 characters long + "0:" prefix
    // 66 characters long
    [StringLength(66)] public string Address { get; set; } = null!;

    public bool IsTrackingActive { get; set; } = true;
}