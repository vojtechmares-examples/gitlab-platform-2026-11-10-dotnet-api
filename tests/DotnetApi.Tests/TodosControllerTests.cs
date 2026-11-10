using DotnetApi.Controllers;
using DotnetApi.Data;
using DotnetApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotnetApi.Tests;

/// <summary>Unit tests of the controller against an in-memory database: no web server, no PostgreSQL.</summary>
public class TodosControllerTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Create_trims_the_title_and_returns_201()
    {
        using var db = NewDb();
        var controller = new TodosController(db);

        var result = await controller.Create(new TodoItem { Title = "  write tests  " }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var todo = Assert.IsType<TodoItem>(created.Value);
        Assert.Equal("write tests", todo.Title);
        Assert.Equal(1, await db.Todos.CountAsync());
    }

    [Fact]
    public async Task Get_returns_404_for_an_unknown_id()
    {
        using var db = NewDb();

        var result = await new TodosController(db).Get(42, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_changes_the_task()
    {
        using var db = NewDb();
        db.Todos.Add(new TodoItem { Title = "old" });
        await db.SaveChangesAsync();
        var id = (await db.Todos.SingleAsync()).Id;

        var result = await new TodosController(db).Update(id, new TodoItem { Title = "new", IsDone = true }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var todo = await db.Todos.SingleAsync();
        Assert.Equal("new", todo.Title);
        Assert.True(todo.IsDone);
    }

    [Fact]
    public async Task Delete_removes_the_task_and_404s_the_second_time()
    {
        using var db = NewDb();
        db.Todos.Add(new TodoItem { Title = "gone soon" });
        await db.SaveChangesAsync();
        var id = (await db.Todos.SingleAsync()).Id;
        var controller = new TodosController(db);

        Assert.IsType<NoContentResult>(await controller.Delete(id, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.Delete(id, CancellationToken.None));
        Assert.Empty(await db.Todos.ToListAsync());
    }

    [Fact]
    public async Task List_is_ordered_by_id()
    {
        using var db = NewDb();
        db.Todos.AddRange(new TodoItem { Title = "a" }, new TodoItem { Title = "b" });
        await db.SaveChangesAsync();

        var result = await new TodosController(db).List(CancellationToken.None);

        Assert.Equal(["a", "b"], result.Value!.Select(t => t.Title));
    }
}
