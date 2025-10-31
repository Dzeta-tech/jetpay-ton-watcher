using System.Linq.Expressions;
using JetPay.TonWatcher.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Ton.Core.Addresses;

namespace JetPay.TonWatcher.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<TrackedAddress> TrackedAddresses { get; set; } = null!;
    public DbSet<ShardBlock> ShardBlocks { get; set; } = null!;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Address>()
            .HaveConversion<AddressStringConverter>();
    }


    public class AddressStringConverter()
        : ValueConverter<Address, string>(ToProviderExpression, FromProviderExpression)
    {
        static readonly Expression<Func<Address, string>> ToProviderExpression =
            v => ConvertToString(v);

        static readonly Expression<Func<string, Address>> FromProviderExpression =
            v => ParseAddress(v);

        static string ConvertToString(Address address)
        {
            return address.ToString(bounceable: false);
        }

        static Address ParseAddress(string? value)
        {
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException("Address cannot be null or empty");

            return Address.Parse(value);
        }
    }
}