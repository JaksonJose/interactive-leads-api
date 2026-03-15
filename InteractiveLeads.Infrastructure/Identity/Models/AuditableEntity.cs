namespace InteractiveLeads.Infrastructure.Identity.Models
{
    /// <summary>
    /// Base class for entities that support comprehensive audit tracking.
    /// Provides automatic audit functionality including creation and modification tracking
    /// with both timestamps and user identification.
    /// </summary>
    public abstract class AuditableEntity : IAuditableEntity
    {
        /// <summary>
        /// The date and time when the entity was created.
        /// This value is automatically set when the entity is first saved to the database.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The date and time when the entity was last updated.
        /// This value is automatically updated whenever the entity is modified and saved.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// The ID of the user who created this entity.
        /// This value is set when the entity is first saved to the database.
        /// Can be null for system-generated entities or when user context is not available.
        /// </summary>
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// The ID of the user who last updated this entity.
        /// This value is updated whenever the entity is modified and saved.
        /// Can be null for system-generated updates or when user context is not available.
        /// </summary>
        public Guid? UpdatedBy { get; set; }
    }
}
