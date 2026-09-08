using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence;

public sealed class OrderFlowDbContextFactory : IDesignTimeDbContextFactory<OrderFlowDbContext>
{
    public OrderFlowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderFlowDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=OrderFlow;User Id=sa;Password=OrderFlow_Dev_123;TrustServerCertificate=True");

        return new OrderFlowDbContext(optionsBuilder.Options);
    }
}
