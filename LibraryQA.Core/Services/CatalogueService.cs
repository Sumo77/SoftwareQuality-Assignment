using LibraryQA.Core.Database;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LibraryQA.Core.Services
{
    public class BookInput // Raw text exactly as entered on the form - parsing and validation happen in CatalogueService
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Isbn { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string PublicationYear { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CatalogueResult // Stored result of a catalogue add, edit or remove
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? Field { get; init; } // Which field failed validation, so the view can focus it
        public int? BookId { get; init; }
    }

    // Catalogue Service handles staff adding, editing and removing catalogue records (REQ-1)
    // and validates every field at the point of entry (REQ-9, REQ-12). The rules live here only (REQ-13).
    public class CatalogueService
    {
        public const string FieldTitle = "Title";
        public const string FieldAuthor = "Author";
        public const string FieldIsbn = "ISBN";
        public const string FieldPublicationYear = "Publication year";
        public const string FieldPublisher = "Publisher";
        public const string FieldGenre = "Genre";
        public const string FieldDescription = "Description";

        public const int MaxTitleLength = 200;
        public const int MaxAuthorLength = 200;
        public const int MaxPublisherLength = 100;
        public const int MaxGenreLength = 100;
        public const int MaxDescriptionLength = 1000;

        // Negative years are allowed on purpose: the catalogue already holds The Odyssey as -800.
        public const int MinPublicationYear = -3000;

        private readonly string _connectionString;

        public CatalogueService(string connectionString) // Initialise database access
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public CatalogueResult AddBook(BookInput input) // Validates the details, then adds the book as Available
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                var problem = Validate(input, db, null, out var clean);
                if (problem != null)
                {
                    return problem;
                }

                int? bookId = db.AddBook(clean.Title, clean.Author, clean.Isbn, clean.Publisher, clean.Year, clean.Genre, clean.Description);

                if (bookId == null)
                {
                    return Fail(FieldIsbn, "Unable to add the book. Its ISBN may already be in use.");
                }

                return new CatalogueResult
                {
                    Success = true,
                    Message = $"'{clean.Title}' added to the catalogue (Book ID {bookId}).",
                    BookId = bookId
                };
            }
        }

        public CatalogueResult UpdateBook(int bookId, BookInput input) // Validates the details, then updates the book. Status is never changed here
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                if (db.GetBookById(bookId) == null)
                {
                    return Fail(null, "Book not found. It may have been removed.");
                }

                var problem = Validate(input, db, bookId, out var clean);
                if (problem != null)
                {
                    return problem;
                }

                if (!db.UpdateBookDetails(bookId, clean.Title, clean.Author, clean.Isbn, clean.Publisher, clean.Year, clean.Genre, clean.Description))
                {
                    return Fail(FieldIsbn, "Unable to save the changes. The ISBN may already be in use.");
                }

                return new CatalogueResult
                {
                    Success = true,
                    Message = $"'{clean.Title}' updated.",
                    BookId = bookId
                };
            }
        }

        public CatalogueResult RemoveBook(int bookId) // Removes a book only if it is Available and has no loan or reservation history
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                var book = db.GetBookById(bookId);

                if (book == null) // Check if book exists
                {
                    return Fail(null, "Book not found. It may already have been removed.");
                }

                string title = book["Title"]?.ToString() ?? "This book";
                string status = book["Status"]?.ToString() ?? "";

                if (!string.Equals(status, "Available", StringComparison.OrdinalIgnoreCase)) // Never remove a book that is out or held (REQ-14)
                {
                    return Fail(null, $"'{title}' is currently {status} and cannot be removed.");
                }

                if (db.BookHasLoanOrReservationHistory(bookId)) // Loan and reservation records must keep pointing at a real book
                {
                    return Fail(null, $"'{title}' has loan or reservation history, which must be kept, so it cannot be removed.");
                }

                if (!db.DeleteBook(bookId))
                {
                    return Fail(null, $"Unable to remove '{title}'. Please try again.");
                }

                return new CatalogueResult { Success = true, Message = $"'{title}' removed from the catalogue.", BookId = bookId };
            }
        }

        // Wording follows UserMessage's style ("Please enter a ...", "... must be a number.") - Core cannot
        // reference the WPF project, so the same pattern is repeated here rather than called.
        private static CatalogueResult? Validate(BookInput input, DatabaseHelper db, int? excludeBookId, out CleanBook clean)
        {
            clean = null!;

            string title = (input.Title ?? "").Trim();
            if (title.Length == 0)
            {
                return Fail(FieldTitle, "Please enter a title.");
            }
            if (title.Length > MaxTitleLength)
            {
                return Fail(FieldTitle, $"Title must be {MaxTitleLength} characters or fewer.");
            }

            string author = (input.Author ?? "").Trim();
            if (author.Length == 0)
            {
                return Fail(FieldAuthor, "Please enter an author.");
            }
            if (author.Length > MaxAuthorLength)
            {
                return Fail(FieldAuthor, $"Author must be {MaxAuthorLength} characters or fewer.");
            }

            string? isbn = BlankToNull(input.Isbn);
            string? normalisedIsbn = null;
            if (isbn != null)
            {
                normalisedIsbn = NormaliseIsbn(isbn);
                if (!HasValidIsbnFormat(normalisedIsbn))
                {
                    return Fail(FieldIsbn, "ISBN must be 10 or 13 digits (hyphens and spaces are fine).");
                }
            }

            int? year = null;
            string yearText = (input.PublicationYear ?? "").Trim();
            if (yearText.Length > 0)
            {
                if (!int.TryParse(yearText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int parsedYear))
                {
                    return Fail(FieldPublicationYear, "Publication year must be a number.");
                }

                int maxYear = DateTime.Today.Year + 1; // allows a book announced for next year
                if (parsedYear < MinPublicationYear || parsedYear > maxYear)
                {
                    return Fail(FieldPublicationYear, $"Publication year must be between {MinPublicationYear} and {maxYear}.");
                }

                year = parsedYear;
            }

            string? publisher = BlankToNull(input.Publisher);
            if (publisher != null && publisher.Length > MaxPublisherLength)
            {
                return Fail(FieldPublisher, $"Publisher must be {MaxPublisherLength} characters or fewer.");
            }

            string? genre = BlankToNull(input.Genre);
            if (genre != null && genre.Length > MaxGenreLength)
            {
                return Fail(FieldGenre, $"Genre must be {MaxGenreLength} characters or fewer.");
            }

            string? description = BlankToNull(input.Description);
            if (description != null && description.Length > MaxDescriptionLength)
            {
                return Fail(FieldDescription, $"Description must be {MaxDescriptionLength} characters or fewer.");
            }

            // Checked last because it needs the database. Done here, with a clear message naming the other
            // book, so staff never see a raw UNIQUE constraint error.
            if (normalisedIsbn != null)
            {
                string? usedBy = db.GetTitleOfBookWithIsbn(normalisedIsbn, excludeBookId);
                if (usedBy != null)
                {
                    return Fail(FieldIsbn, $"That ISBN is already used by '{usedBy}'.");
                }
            }

            clean = new CleanBook(title, author, isbn, publisher, year, genre, description);
            return null;
        }

        private static string? BlankToNull(string? value) // Optional fields are stored as NULL, not empty text
        {
            string trimmed = (value ?? "").Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        private static string NormaliseIsbn(string isbn) // Strip hyphens and spaces for comparison
        {
            return Regex.Replace(isbn, @"[\s-]", "").ToUpperInvariant();
        }

        // Format only. The seeded catalogue contains ISBNs that fail the standard check-digit test, so a
        // checksum rule would lock staff out of editing those books.
        private static bool HasValidIsbnFormat(string normalisedIsbn)
        {
            return Regex.IsMatch(normalisedIsbn, "^[0-9]{13}$") || Regex.IsMatch(normalisedIsbn, "^[0-9]{9}[0-9X]$");
        }

        private static CatalogueResult Fail(string? field, string message) // A refusal naming the field at fault (REQ-9)
        {
            return new CatalogueResult { Success = false, Message = message, Field = field };
        }

        private sealed record CleanBook(string Title, string Author, string? Isbn, string? Publisher, int? Year, string? Genre, string? Description); // Validated, trimmed values ready to save
    }
}