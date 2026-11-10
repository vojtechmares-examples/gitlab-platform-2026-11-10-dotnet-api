using DotnetApi.Data;
using DotnetApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotnetApi.Controllers;

/// <summary>The to-do list.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TodosController(AppDbContext db) : ControllerBase
{
    /// <summary>All tasks, oldest first.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> List(CancellationToken ct) =>
        await db.Todos.AsNoTracking().OrderBy(t => t.Id).ToListAsync(ct);

    /// <summary>One task.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<TodoItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoItem>> Get(long id, CancellationToken ct)
    {
        var todo = await db.Todos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return todo is null ? NotFound() : todo;
    }

    /// <summary>Add a task.</summary>
    [HttpPost]
    [ProducesResponseType<TodoItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TodoItem>> Create(TodoItem input, CancellationToken ct)
    {
        var todo = new TodoItem { Title = input.Title.Trim(), IsDone = input.IsDone, CreatedAt = DateTimeOffset.UtcNow };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = todo.Id }, todo);
    }

    /// <summary>Change a task.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, TodoItem input, CancellationToken ct)
    {
        var todo = await db.Todos.FindAsync([id], ct);
        if (todo is null) return NotFound();
        todo.Title = input.Title.Trim();
        todo.IsDone = input.IsDone;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Remove a task.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var todo = await db.Todos.FindAsync([id], ct);
        if (todo is null) return NotFound();
        db.Todos.Remove(todo);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
