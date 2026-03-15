namespace InteractiveLeads.Infrastructure.Context.Tenancy.Interfaces
{
    public interface ITenantDbSeeder
    {
        Task InitializeDatabaseAsync(CancellationToken cancellationToken);
    }
}
