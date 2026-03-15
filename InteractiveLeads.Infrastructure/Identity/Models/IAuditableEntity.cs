namespace InteractiveLeads.Infrastructure.Identity.Models
{
    /// <summary>
    /// Interface for entities that support comprehensive audit tracking.
    /// Provides complete audit information including who created/updated entities and when.
    /// </summary>
    public interface IAuditableEntity
    {
        /// <summary>
        /// The date and time when the entity was created.
        /// This value is automatically set when the entity is first saved to the database.
        /// </summary>
        DateTime CreatedAt { get; set; }

        /// <summary>
        /// The date and time when the entity was last updated.
        /// This value is automatically updated whenever the entity is modified and saved.
        /// </summary>
        DateTime UpdatedAt { get; set; }

        /// <summary>
        /// The ID of the user who created this entity.
        /// This value is set when the entity is first saved to the database.
        /// </summary>
        Guid? CreatedBy { get; set; }

        /// <summary>
        /// The ID of the user who last updated this entity.
        /// This value is updated whenever the entity is modified and saved.
        /// </summary>
        Guid? UpdatedBy { get; set; }
    }
}
