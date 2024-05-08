using Microsoft.EntityFrameworkCore;
using ThreadSafe.Api.Data;
using ThreadSafe.Api.Models;
using ThreadSafe.Api.Services;

namespace ThreadSafe.Api;

public interface ISampleAppService
{
    Task<string> InsertOperation(int sampleInput, CancellationToken cancellationToken);
    Task<string> DeleteOperation(int sampleInput, CancellationToken cancellationToken);
}

public class SampleAppService : ISampleAppService
{
    private readonly NameService _nameService;
    private readonly BookContext _context;

    public SampleAppService(NameService nameService, BookContext context)
    {
        _nameService = nameService;
        _context = context;
        if (_context.Database.IsRelational() && !_context.Database.GetCommandTimeout().HasValue)
        {
            _context.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));
        }
    }

    public async Task<string> InsertOperation(int sampleInput, CancellationToken cancellationToken)
    {
        // Add a new book
        var author = await GetRandomAuthorAsync(_context);
        var book = GetRandomBook(author);
        var addedBook = await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();

        // Remove the book
        _context.Remove(addedBook.Entity);
        await _context.SaveChangesAsync();

        return "OK";
    }

    public async Task<string> DeleteOperation(int sampleInput, CancellationToken cancellationToken)
    {
        // Add a new book
        var author = await GetRandomAuthorAsync(_context);
        var book = GetRandomBook(author);
        var addedBook = await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();

        // Remove the book
        _context.Remove(addedBook.Entity);
        await _context.SaveChangesAsync();

        Thread.Sleep(5000);
        
        return "OK";
    }

    private Book GetRandomBook(Author author) =>
        new Book
        {
            Author = author,
            Title = _nameService.GetWords(3, 7),
            Summary = _nameService.GetWords(15, 100),
            YearPublished = _nameService.GetYear(author.BirthDate.Year + 15)
        };

    private async Task<Author> GetRandomAuthorAsync(BookContext context)
    {
        var firstName = _nameService.GetFirstName();
        var lastName = _nameService.GetLastName();

        var author = await context.Authors.Where(a => a.FirstName == firstName && a.LastName == lastName).FirstOrDefaultAsync();
        if (author != null)
        {
            return author;
        }

        return new Author
        {
            LastName = lastName,
            FirstName = firstName,
            BirthDate = _nameService.GetBirthDate(),
        };
    }
}