using DotnetApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(todo =>
        {
            todo.ToTable("todos");
            todo.Property(t => t.Title).HasMaxLength(200).IsRequired();
            todo.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
            todo.HasIndex(t => t.IsDone).HasDatabaseName("ix_todos_is_done");
        });
    }
}
