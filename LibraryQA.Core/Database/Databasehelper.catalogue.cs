using Microsoft.Data.Sqlite;
using System;

namespace LibraryQA.Core.Database
{
    // Catalogue maintenance (REQ-1: staff add, edit and remove catalogue records).
    // Kept in its own file so it does not collide with other work in DatabaseHelper.cs.
    // Book status is never written here: new books start as 'Available' through the schema
    // default, and after that status only changes through loans and reservations (REQ-14).
    public partial class DatabaseHelper
    {
        public int? AddBook(string title, string author, string? isbn, string? publisher,
            int? publicationYear, string? genre, string? description) // Adds a new book to the catalogue and returns its ID, or null if the database rejects it (e.g. duplicate ISBN)
        {
            OpenConnection();

            try
            {
                using (var command = _connection!.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Description)
                        VALUES (@isbn, @title, @author, @publisher, @publicationYear, @genre, @description);
                        SELECT last_insert_rowid();";

                    AddBookParameters(command, title, author, isbn, publisher, publicationYear, genre, description);

                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : null;
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UNIQUE constraint on ISBN
            {
                return null;
            }
        }

        public bool UpdateBookDetails(int bookId, string title, string author, string? isbn, string? publisher,
            int? publicationYear, string? genre, string? description) // Updates a book's descriptive details only - never its status
        {
            OpenConnection();

            try
            {
                using (var command = _connection!.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE Books
                        SET ISBN = @isbn, Title = @title, Author = @author, Publisher = @publisher,
                            PublicationYear = @publicationYear, Genre = @genre, Description = @description
                        WHERE BookID = @bookId";

                    AddBookParameters(command, title, author, isbn, publisher, publicationYear, genre, description);
                    command.Parameters.AddWithValue("@bookId", bookId);

                    return command.ExecuteNonQuery() > 0;
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UNIQUE constraint on ISBN
            {
                return false;
            }
        }

        public bool DeleteBook(int bookId) // Deletes a book that is Available and has no loan or reservation records; the database refuses anything else
        {
            OpenConnection();

            try
            {
                using (var command = _connection!.CreateCommand())
                {
                    // The Status condition and the RESTRICT foreign keys are a second line of defence
                    // behind the checks CatalogueService makes first (REQ-14).
                    command.CommandText = "DELETE FROM Books WHERE BookID = @bookId AND Status = 'Available'";
                    command.Parameters.AddWithValue("@bookId", bookId);

                    return command.ExecuteNonQuery() > 0;
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // FOREIGN KEY ... ON DELETE RESTRICT
            {
                return false;
            }
        }

        public bool BookHasLoanOrReservationHistory(int bookId) // True if any loan or reservation, past or present, refers to this book
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT (SELECT COUNT(*) FROM Loans WHERE BookID = @bookId)
                         + (SELECT COUNT(*) FROM Reservations WHERE BookID = @bookId)";
                command.Parameters.AddWithValue("@bookId", bookId);

                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        // Finds another book already using this ISBN. Stored ISBNs may contain hyphens or spaces, so both
        // sides are compared with those removed. Returns that book's title, or null if the ISBN is free.
        public string? GetTitleOfBookWithIsbn(string normalisedIsbn, int? excludeBookId = null)
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT Title
                    FROM Books
                    WHERE ISBN IS NOT NULL
                      AND UPPER(REPLACE(REPLACE(ISBN, '-', ''), ' ', '')) = @isbn
                      AND (@excludeBookId IS NULL OR BookID <> @excludeBookId)
                    LIMIT 1";

                command.Parameters.AddWithValue("@isbn", normalisedIsbn);
                command.Parameters.AddWithValue("@excludeBookId", (object?)excludeBookId ?? DBNull.Value);

                return command.ExecuteScalar()?.ToString();
            }
        }

        private static void AddBookParameters(SqliteCommand command, string title, string author, string? isbn,
            string? publisher, int? publicationYear, string? genre, string? description)
        {
            // Optional values are stored as NULL rather than empty text. This matters for ISBN: it is
            // UNIQUE, and a second empty-string ISBN would be rejected as a duplicate of the first.
            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@author", author);
            command.Parameters.AddWithValue("@isbn", (object?)isbn ?? DBNull.Value);
            command.Parameters.AddWithValue("@publisher", (object?)publisher ?? DBNull.Value);
            command.Parameters.AddWithValue("@publicationYear", (object?)publicationYear ?? DBNull.Value);
            command.Parameters.AddWithValue("@genre", (object?)genre ?? DBNull.Value);
            command.Parameters.AddWithValue("@description", (object?)description ?? DBNull.Value);
        }
    }
}