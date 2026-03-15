using InteractiveLeads.Application.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Interfaces
{
    /// <summary>
    /// Service interface for cross-tenant operations.
    /// </summary>
    /// <remarks>
    /// Provides functionality to execute operations in different tenant contexts
    /// while maintaining proper isolation and authorization.
    /// </remarks>
    public interface ICrossTenantService
    {
        /// <summary>
        /// Executes an operation in the context of a specific tenant with proper DbContext management.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="tenantId">The ID of the tenant to execute the operation in.</param>
        /// <param name="operation">The operation to execute with access to the scoped service provider.</param>
        /// <returns>The result of the operation.</returns>
        /// <remarks>
        /// This method creates a new service scope for the target tenant context, executes the operation,
        /// and ensures proper disposal of resources. It also validates that the current
        /// user has permission to access the specified tenant.
        /// </remarks>
        Task<T> ExecuteInTenantContextAsync<T>(string tenantId, Func<IServiceProvider, Task<T>> operation);

        /// <summary>
        /// Executes an operation in the context of a specific tenant with proper DbContext management.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant to execute the operation in.</param>
        /// <param name="operation">The operation to execute with access to the scoped service provider.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method creates a new service scope for the target tenant context, executes the operation,
        /// and ensures proper disposal of resources. It also validates that the current
        /// user has permission to access the specified tenant.
        /// </remarks>
        Task ExecuteInTenantContextAsync(string tenantId, Func<IServiceProvider, Task> operation);

        /// <summary>
        /// Executes an operation in the context of a specific tenant (legacy method for backward compatibility).
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="tenantId">The ID of the tenant to execute the operation in.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The result of the operation.</returns>
        /// <remarks>
        /// This method is marked as obsolete. Use the overload that accepts IServiceProvider parameter for better DbContext management.
        /// </remarks>
        [Obsolete("Use the overload that accepts IServiceProvider parameter for better DbContext management")]
        Task<T> ExecuteInTenantContextAsync<T>(string tenantId, Func<Task<T>> operation);

        /// <summary>
        /// Executes an operation in the context of a specific tenant (legacy method for backward compatibility).
        /// </summary>
        /// <param name="tenantId">The ID of the tenant to execute the operation in.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method is marked as obsolete. Use the overload that accepts IServiceProvider parameter for better DbContext management.
        /// </remarks>
        [Obsolete("Use the overload that accepts IServiceProvider parameter for better DbContext management")]
        Task ExecuteInTenantContextAsync(string tenantId, Func<Task> operation);

        /// <summary>
        /// Logs a cross-tenant operation for audit purposes.
        /// </summary>
        /// <param name="userId">The ID of the user performing the operation.</param>
        /// <param name="originalTenantId">The original tenant context.</param>
        /// <param name="targetTenantId">The target tenant being accessed.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <param name="result">The result of the operation (success/failure).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LogCrossTenantOperationAsync(Guid userId, string? originalTenantId, string targetTenantId, string operation, bool result = true);
    }
}
