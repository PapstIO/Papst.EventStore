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
  }
}
