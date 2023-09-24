using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Papst.EventStore.EntityFrameworkCore.Database;

public class DesigntimeEventStoreDbContextFactory : IDesignTimeDbContextFactory<EventStoreDbContext>
{
  public EventStoreDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<EventStoreDbContext>();
    optionsBuilder.UseSqlServer("Server=localhost;Database=EventStoreyourStrong(!)Password;User Id=sa;Password=myPassword;");
    
    return new EventStoreDbContext(optionsBuilder.Options);
  }
}
