using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Papst.EventStore.EntityFrameworkCore.Database;

public class EventStoreDbContext : DbContext
{
  public DbSet<EventStreamEntity> Streams { get; set; } = null!;
  public DbSet<EventStreamDocumentEntity> Documents { get; set; } = null!;
  public EventStoreDbContext(DbContextOptions options) : base(options)
  { }

  public EventStoreDbContext()
  { }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    EntityTypeBuilder<EventStreamEntity> stream = modelBuilder.Entity<EventStreamEntity>();
    stream.HasKey(s => s.StreamId);

    EntityTypeBuilder<EventStreamDocumentEntity> evt = modelBuilder.Entity<EventStreamDocumentEntity>();
    evt.HasKey(s => s.Id);
    evt.HasIndex(s => s.StreamId);
    evt.HasIndex(s => s.Version);
    evt.HasIndex(s => new { s.StreamId, s.Version }).IsUnique();
    evt.Property(s => s.Name).HasMaxLength(100);
    evt.Property(s => s.TargetType).HasMaxLength(100);
    evt.Property(s => s.DataType).HasMaxLength(100);
    evt.OwnsOne(
      s => s.Data,
      data => data.ToJson()
    );
    evt.OwnsOne(
      s => s.MetaData, 
      metaData =>
      {
        metaData.ToJson();
        metaData.Property(p => p.UserId).HasMaxLength(50);
        metaData.Property(p => p.UserName).HasMaxLength(50);
        metaData.Property(p => p.TenantId).HasMaxLength(50);
        metaData.Property(p => p.Comment).HasMaxLength(255);
        metaData.OwnsOne(
          meta => meta.Additional, 
          add => add.ToJson()
        );
      });
  }
}
