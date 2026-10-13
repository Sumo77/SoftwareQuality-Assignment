using System;
using System.IO;
using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryQA.Tests
{
    [TestClass]
    // Catalogue Management Tests (REQ-1: staff add, edit and remove catalogue records, with validation)
    public class CatalogueServiceTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private CatalogueService _service = null!;

        [TestInitialize]
        public void Setup() // Setup the tests
        {
            // Fresh database per test due to randomised run/completion order
            _dbPath = Path.Combine(Path.GetTempPath(), $"cataloguetest_{Guid.NewGuid()}.db");

            Assert.IsTrue(new DatabaseInitializer(_dbPath).InitializeDatabase(),
                "Database schema could not be created - check DatabaseSchema.sql is in the test output folder.");
            Assert.IsTrue(new DatabaseSeeder(_dbPath).SeedSampleData(),
                "Sample data could not be loaded - check SampleData.sql is in the test output folder.");

            _connectionString = $"Data Source={_dbPath}";
            _service = new CatalogueService(_connectionString);
        }

        [TestCleanup]
        public void Cleanup() // Cleans/Deletes the tests
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // Each test builds the books and loans it needs, so none depends on specific seeded IDs.
        private static BookInput ValidBook(string title = "A New Book", string isbn = "") =>
            new BookInput { Title = title, Author = "Some Author", Isbn = isbn };

        private int CountBooks()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                return db.SearchCatalogue("").Count;
            }
        }

        private int AddValidBook(string title = "A New Book", string isbn = "")
        {
            var result = _service.AddBook(ValidBook(title, isbn));
            Assert.IsTrue(result.Success, result.Message);
            return result.BookId!.Value;
        }

        // TC-66: Adding a book with valid details saves it as Available, with the details trimmed (REQ-1, REQ-14)
        [TestMethod]
        public void AddBook_ValidDetails_AddsAvailableBook()
        {
            var result = _service.AddBook(new BookInput
            {
                Title = "  Dune Messiah  ",
                Author = "Frank Herbert",
                Isbn = "978-0-306-40615-7",
                PublicationYear = "1969",
                Genre = "Science Fiction"
            });

            Assert.IsTrue(result.Success, result.Message);

            using (var db = new DatabaseHelper(_connectionString))
            {
                var book = db.GetBookById(result.BookId!.Value);
                Assert.IsNotNull(book);
                Assert.AreEqual("Dune Messiah", book["Title"].ToString());
                Assert.AreEqual("Available", book["Status"].ToString());
            }
        }

        // TC-67: A blank title is rejected with a message naming the Title field, and nothing is added (REQ-9, REQ-12)
        [TestMethod]
        public void AddBook_BlankTitle_RejectedNamingTheField()
        {
            int before = CountBooks();

            var result = _service.AddBook(new BookInput { Title = "   ", Author = "Some Author" });

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CatalogueService.FieldTitle, result.Field);
            StringAssert.Contains(result.Message, "title");
            Assert.AreEqual(before, CountBooks());
        }

        // TC-68: A blank author is rejected with a message naming the Author field (REQ-9)
        [TestMethod]
        public void AddBook_BlankAuthor_RejectedNamingTheField()
        {
            var result = _service.AddBook(new BookInput { Title = "A New Book", Author = "" });

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CatalogueService.FieldAuthor, result.Field);
            StringAssert.Contains(result.Message, "author");
        }

        // TC-69: A title over the length limit is rejected, while one exactly at the limit is accepted (boundary, REQ-9)
        [TestMethod]
        public void AddBook_TitleLengthBoundary_RejectsOneOverTheLimit()
        {
            var atLimit = _service.AddBook(ValidBook(new string('a', CatalogueService.MaxTitleLength)));
            var overLimit = _service.AddBook(ValidBook(new string('a', CatalogueService.MaxTitleLength + 1)));

            Assert.IsTrue(atLimit.Success, atLimit.Message);
            Assert.IsFalse(overLimit.Success);
            Assert.AreEqual(CatalogueService.FieldTitle, overLimit.Field);
        }

        // TC-70: A non-numeric publication year is rejected with a message naming that field (REQ-9)
        [TestMethod]
        public void AddBook_NonNumericYear_RejectedNamingTheField()
        {
            var result = _service.AddBook(new BookInput { Title = "A New Book", Author = "Some Author", PublicationYear = "nineteen eighty" });

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CatalogueService.FieldPublicationYear, result.Field);
            StringAssert.Contains(result.Message, "must be a number");
        }

        // TC-71: A negative year is accepted, because the catalogue already holds The Odyssey as -800 (REQ-1)
        [TestMethod]
        public void AddBook_NegativeYear_IsAccepted()
        {
            var result = _service.AddBook(new BookInput { Title = "The Iliad", Author = "Homer", PublicationYear = "-750" });

            Assert.IsTrue(result.Success, result.Message);
        }

        // TC-72: A year beyond next year is rejected (REQ-9)
        [TestMethod]
        public void AddBook_YearTooFarInTheFuture_Rejected()
        {
            string tooFar = (DateTime.Today.Year + 2).ToString();

            var result = _service.AddBook(new BookInput { Title = "A New Book", Author = "Some Author", PublicationYear = tooFar });

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CatalogueService.FieldPublicationYear, result.Field);
        }

        // TC-73: ISBNs of the wrong length or containing letters are rejected; a 10-digit ISBN ending in X is accepted (REQ-9)
        [TestMethod]
        public void AddBook_IsbnFormat_RejectsBadValuesAndAcceptsValidOnes()
        {
            Assert.IsFalse(_service.AddBook(ValidBook(isbn: "12345")).Success);
            Assert.IsFalse(_service.AddBook(ValidBook(isbn: "97803064061")).Success);
            Assert.IsFalse(_service.AddBook(ValidBook(isbn: "ABCDEFGHIJ")).Success);

            var tenDigitWithX = _service.AddBook(ValidBook(isbn: "080442957X"));
            Assert.IsTrue(tenDigitWithX.Success, tenDigitWithX.Message);

            var rejected = _service.AddBook(ValidBook(isbn: "12345"));
            Assert.AreEqual(CatalogueService.FieldIsbn, rejected.Field);
        }

        // TC-74: An ISBN already in use is rejected with a clear message, even if typed with different hyphens (REQ-14)
        [TestMethod]
        public void AddBook_DuplicateIsbn_RejectedNamingTheExistingBook()
        {
            AddValidBook("First Book", "978-0-306-40615-7");
            int before = CountBooks();

            var result = _service.AddBook(ValidBook("Second Book", "9780306406157"));

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CatalogueService.FieldIsbn, result.Field);
            StringAssert.Contains(result.Message, "First Book");
            Assert.AreEqual(before, CountBooks());
        }

        // TC-75: Several books may have no ISBN - blank is stored as NULL, not as a repeated empty string (REQ-1)
        [TestMethod]
        public void AddBook_BlankIsbn_AllowsMoreThanOneBookWithoutAnIsbn()
        {
            var first = _service.AddBook(ValidBook("No ISBN One"));
            var second = _service.AddBook(ValidBook("No ISBN Two"));

            Assert.IsTrue(first.Success, first.Message);
            Assert.IsTrue(second.Success, second.Message);
        }

        // TC-76: Editing a book changes its details and keeps its status unchanged, even while it is on loan (REQ-1, REQ-14)
        [TestMethod]
        public void UpdateBook_WhileOnLoan_ChangesDetailsButNotStatus()
        {
            int bookId = AddValidBook("Original Title");

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.IsNotNull(db.CreateLoan(bookId, 1, DateTime.Today, DateTime.Today.AddDays(14)));
            }

            var result = _service.UpdateBook(bookId, ValidBook("Corrected Title"));

            Assert.IsTrue(result.Success, result.Message);

            using (var db = new DatabaseHelper(_connectionString))
            {
                var book = db.GetBookById(bookId);
                Assert.AreEqual("Corrected Title", book!["Title"].ToString());
                Assert.AreEqual("On Loan", book["Status"].ToString());
            }
        }

        // TC-77: A book may keep its own ISBN when edited, but cannot take another book's ISBN (REQ-14)
        [TestMethod]
        public void UpdateBook_Isbn_AllowsOwnButRejectsAnotherBooks()
        {
            AddValidBook("Book One", "978-0-306-40615-7");
            int secondId = AddValidBook("Book Two", "080442957X");

            var keepingOwn = _service.UpdateBook(secondId, ValidBook("Book Two Renamed", "080442957X"));
            var takingOthers = _service.UpdateBook(secondId, ValidBook("Book Two", "9780306406157"));

            Assert.IsTrue(keepingOwn.Success, keepingOwn.Message);
            Assert.IsFalse(takingOthers.Success);
            Assert.AreEqual(CatalogueService.FieldIsbn, takingOthers.Field);
        }

        // TC-78: Editing a book that does not exist fails with a clear message instead of an error (REQ-12)
        [TestMethod]
        public void UpdateBook_UnknownBook_Rejected()
        {
            var result = _service.UpdateBook(999999, ValidBook());

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Message, "not found");
        }

        // TC-79: A book that has never been loaned or reserved can be removed (REQ-1)
        [TestMethod]
        public void RemoveBook_NeverLoaned_IsRemoved()
        {
            int bookId = AddValidBook("Short Lived Book");
            int before = CountBooks();

            var result = _service.RemoveBook(bookId);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(before - 1, CountBooks());

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.IsNull(db.GetBookById(bookId));
            }
        }

        // TC-80: A book that is on loan cannot be removed, and its status is untouched (REQ-1, REQ-14)
        [TestMethod]
        public void RemoveBook_OnLoan_RejectedAndBookRemains()
        {
            int bookId = AddValidBook("Out On Loan");

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.IsNotNull(db.CreateLoan(bookId, 1, DateTime.Today, DateTime.Today.AddDays(14)));
            }

            var result = _service.RemoveBook(bookId);

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Message, "On Loan");

            using (var db = new DatabaseHelper(_connectionString))
            {
                var book = db.GetBookById(bookId);
                Assert.IsNotNull(book);
                Assert.AreEqual("On Loan", book["Status"].ToString());
            }
        }

        // TC-81: A book returned long ago still has loan history, so it cannot be removed even though it is Available (REQ-14)
        [TestMethod]
        public void RemoveBook_WithLoanHistory_RejectedAndBookRemains()
        {
            int bookId = AddValidBook("Returned Book");

            using (var db = new DatabaseHelper(_connectionString))
            {
                int? loanId = db.CreateLoan(bookId, 1, DateTime.Today.AddDays(-20), DateTime.Today.AddDays(-6));
                Assert.IsNotNull(loanId);
                Assert.IsTrue(db.ProcessReturn(loanId.Value, DateTime.Today.AddDays(-7)));
            }

            var result = _service.RemoveBook(bookId);

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Message, "history");

            using (var db = new DatabaseHelper(_connectionString))
            {
                var book = db.GetBookById(bookId);
                Assert.IsNotNull(book);
                Assert.AreEqual("Available", book["Status"].ToString());
            }
        }

        // TC-82: Removing a book that does not exist fails with a clear message instead of an error (REQ-12)
        [TestMethod]
        public void RemoveBook_UnknownBook_Rejected()
        {
            var result = _service.RemoveBook(999999);

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Message, "not found");
        }
    }
}