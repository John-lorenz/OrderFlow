using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.ValueObjects;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(OrderFlowDbContext context, IPasswordHasher passwordHasher)
    {
        if (context.Database.IsSqlServer())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        if (!await context.Users.AnyAsync())
        {
            var admin = User.Create("OrderFlow Admin", "admin@orderflow.dev", passwordHasher.Hash("Admin@123"), UserRole.Admin);
            var manager = User.Create("OrderFlow Manager", "manager@orderflow.dev", passwordHasher.Hash("Manager@123"), UserRole.Manager);
            var op = User.Create("OrderFlow Operator", "operator@orderflow.dev", passwordHasher.Hash("Operator@123"), UserRole.Operator);
            await context.Users.AddRangeAsync(admin, manager, op);
        }

        if (!await context.Customers.AnyAsync())
        {
            await context.Customers.AddRangeAsync(
                Customer.Create(
                    "Atlantic Terminal Ltda",
                    "ops@atlantic-terminal.dev",
                    "11222333000181",
                    "4730001111",
                    Address.Create("Av. Portuaria 100", "Navegantes", "SC", "88370-000", "BR")),
                Customer.Create(
                    "South Bound Logistics",
                    "contato@southbound.dev",
                    "44555666000172",
                    "4730002222",
                    Address.Create("Rua das Docas 250", "Itajai", "SC", "88301-000", "BR")));
        }

        if (!await context.Products.AnyAsync())
        {
            await context.Products.AddRangeAsync(
                Product.Create("CNT-20", "Container 20ft Dry", "Standard dry container", 850.00m, 40),
                Product.Create("CNT-40", "Container 40ft High Cube", "High cube dry container", 1200.00m, 25),
                Product.Create("GEN-SET", "Reefer Genset", "Clip-on generator set", 430.00m, 12),
                Product.Create("SEAL-01", "High-security seal", "ISO 17712 barrier seal", 18.50m, 400));
        }

        await context.SaveChangesAsync();
    }
}
