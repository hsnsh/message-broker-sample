using Microsoft.EntityFrameworkCore;
using ThreadSafe.Api.Models;

namespace ThreadSafe.Api.Data;

public class BookContext : DbContext
{
    public DbSet<Book> Books { get; set; }
    public DbSet<Author> Authors { get; set; }

    public BookContext(DbContextOptions<BookContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .ToTable("Book")
            .HasOne(b => b.Author).WithMany(a => a.Books);

        modelBuilder.Entity<Author>().ToTable("Author");
    }
}