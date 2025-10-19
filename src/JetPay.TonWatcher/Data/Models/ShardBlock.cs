using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JetPay.TonWatcher.Data.Models;

[Index(nameof(Seqno))]
public class ShardBlock
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Workchain { get; set; }

    public long Shard { get; set; }

    public long Seqno { get; set; }

    public string RootHash { get; set; } = null!;

    public string FileHash { get; set; } = null!;

    public bool IsProcessed { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public void MarkAsProcessed()
    {
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
    }
}