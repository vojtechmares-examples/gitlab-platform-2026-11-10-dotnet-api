using System.ComponentModel.DataAnnotations;

namespace DotnetApi.Models;

/// <summary>A task on the to-do list.</summary>
public class TodoItem
{
    public long Id { get; set; }

    /// <summary>What has to be done (1 to 200 characters).</summary>
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
