namespace Cadence.Core.Models.Entities;

/// <summary>
/// Organization entity - parent container for users and exercises.
/// </summary>
public class Organization : BaseEntity
{
    /// <summary>
    /// Organization name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Organization description.
    /// </summary>
    public string? Description { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// Users belonging to this organization.
    /// </summary>
    public ICollection<User> Users { get; set; } = new List<User>();

    /// <summary>
    /// Exercises owned by this organization.
    /// </summary>
    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}
