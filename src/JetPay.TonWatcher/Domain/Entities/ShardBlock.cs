using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JetPay.TonWatcher.Domain.Entities;

[Index(nameof(Seqno))]
public class ShardBlock
{
    ShardBlock()
    {
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; private set; }

    public int Workchain { get; private set; }

    public long Shard { get; private set; }

    public uint Seqno { get; private set; }

    public bool IsProcessed { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public static ShardBlock Create(int workchain, long shard, uint seqno)
    {
        return new ShardBlock
        {
            Id = Guid.NewGuid(),
            Workchain = workchain,
            Shard = shard,
            Seqno = seqno,
            IsProcessed = false
        };
    }

    public void MarkAsProcessed()
    {
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
    }
}