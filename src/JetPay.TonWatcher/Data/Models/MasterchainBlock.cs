using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JetPay.TonWatcher.Data.Models;

[Index(nameof(IsProcessed))]
public class MasterchainBlock
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Seqno { get; set; }

    public bool IsProcessed { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public void MarkAsProcessed()
    {
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
    }
}