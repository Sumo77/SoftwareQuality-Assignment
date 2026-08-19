using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;

namespace LibraryQA.Core.Database
{
    /// Provides data access methods for the Library Management System.
    /// Handles all database queries and commands with proper parameterization
    /// to prevent SQL injection and ensure data integrity.
    public class DatabaseHelper : IDisposable
    {
        private readonly string _connectionString;
        private SqliteConnection? _connection;

        
        /// Initializes a new instance of DatabaseHelper with the specified connection string.
        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// Opens a connection to the database. Connection is reused for multiple operations.
        private void OpenConnection()
        {
            if (_connection == null)
            {
                _connection = new SqliteConnection(_connectionString);
            }

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();

                // Enable foreign keys for this connection
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA foreign_keys = ON;";
                    command.ExecuteNonQuery();
                }
            }
        }

        /// Closes the database connection if open.
        public void CloseConnection()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _connection.Close();
            }
        }

        #region Account Operations

        
        /// Authenticates a user by username and password hash.
        public int? ValidateLogin(string username, string passwordHash)
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT AccountID 
                    FROM Accounts 
                    WHERE Username = @username 
                    AND PasswordHash = @passwordHash 
                    AND IsActive = 1";

                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@passwordHash", passwordHash);

                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : null;
            }
        }

        
        /// Gets the role (Member or Staff) for the specified account.
        public string? GetAccountRole(int accountId)
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = "SELECT Role FROM Accounts WHERE AccountID = @accountId";
                command.Parameters.AddWithValue("@accountId", accountId);

                var result = command.ExecuteScalar();
                return result?.ToString();
            }
        }

        
        /// Gets full account information for the specified account ID.
        public Dictionary<string, object>? GetAccountInfo(int accountId)
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT AccountID, Username, Role, FirstName, LastName, Email, PhoneNumber, CreatedDate
                    FROM Accounts 
                    WHERE AccountID = @accountId AND IsActive = 1";

                command.Parameters.AddWithValue("@accountId", accountId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Dictionary<string, object>
                        {
                            ["AccountID"] = reader["AccountID"],
                            ["Username"] = reader["Username"],
                            ["Role"] = reader["Role"],
                            ["FirstName"] = reader["FirstName"],
                            ["LastName"] = reader["LastName"],
                            ["Email"] = reader["Email"] ?? "",
                            ["PhoneNumber"] = reader["PhoneNumber"] ?? "",
                            ["CreatedDate"] = reader["CreatedDate"]
                        };
                    }
                }
            }

            return null;
        }

        #endregion

        #region Book/Catalogue Operations

        
        /// Searches the catalogue by title, author, or ISBN.
        /// Returns all matches as a list of book dictionaries.
        public List<Dictionary<string, object>> SearchCatalogue(string searchTerm)
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT BookID, ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description
                    FROM Books 
                    WHERE Title LIKE @searchTerm 
                       OR Author LIKE @searchTerm 
                       OR ISBN LIKE @searchTerm
                    ORDER BY Title";

                command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["BookID"] = reader["BookID"],
                            ["ISBN"] = reader["ISBN"] ?? "",
                            ["Title"] = reader["Title"],
                            ["Author"] = reader["Author"],
                            ["Publisher"] = reader["Publisher"] ?? "",
                            ["PublicationYear"] = reader["PublicationYear"] ?? "",
                            ["Genre"] = reader["Genre"] ?? "",
                            ["Status"] = reader["Status"],
                            ["Description"] = reader["Description"] ?? ""
                        });
                    }
                }
            }

            return results;
        }

        
        /// Gets full details for a specific book by ID.
        public Dictionary<string, object>? GetBookById(int bookId)
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT BookID, ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description
                    FROM Books 
                    WHERE BookID = @bookId";

                command.Parameters.AddWithValue("@bookId", bookId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Dictionary<string, object>
                        {
                            ["BookID"] = reader["BookID"],
                            ["ISBN"] = reader["ISBN"] ?? "",
                            ["Title"] = reader["Title"],
                            ["Author"] = reader["Author"],
                            ["Publisher"] = reader["Publisher"] ?? "",
                            ["PublicationYear"] = reader["PublicationYear"] ?? "",
                            ["Genre"] = reader["Genre"] ?? "",
                            ["Status"] = reader["Status"],
                            ["Description"] = reader["Description"] ?? ""
                        };
                    }
                }
            }

            return null;
        }

        
        /// Updates the status of a book.
        public bool UpdateBookStatus(int bookId, string newStatus)
        {
            if (newStatus != "Available" && newStatus != "On Loan" && newStatus != "Reserved")
            {
                throw new ArgumentException("Invalid status. Must be 'Available', 'On Loan', or 'Reserved'.");
            }

            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = "UPDATE Books SET Status = @status WHERE BookID = @bookId";
                command.Parameters.AddWithValue("@status", newStatus);
                command.Parameters.AddWithValue("@bookId", bookId);

                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

        
        /// Gets all books with a specific status.
        public List<Dictionary<string, object>> GetBooksByStatus(string status)
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT BookID, ISBN, Title, Author, Genre, Status
                    FROM Books 
                    WHERE Status = @status
                    ORDER BY Title";

                command.Parameters.AddWithValue("@status", status);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["BookID"] = reader["BookID"],
                            ["ISBN"] = reader["ISBN"] ?? "",
                            ["Title"] = reader["Title"],
                            ["Author"] = reader["Author"],
                            ["Genre"] = reader["Genre"] ?? "",
                            ["Status"] = reader["Status"]
                        });
                    }
                }
            }

            return results;
        }

        #endregion

        #region Loan Operations

        
        /// Gets the count of active (unreturned) loans for a specific member.
        /// Used to enforce loan limits (REQ-8).
        public int GetActiveLoanCount(int memberId)
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COUNT(*) 
                    FROM Loans 
                    WHERE MemberID = @memberId AND ReturnDate IS NULL";

                command.Parameters.AddWithValue("@memberId", memberId);

                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        
        /// Gets all active loans for a specific member.
        public List<Dictionary<string, object>> GetActiveLoans(int memberId)
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT L.LoanID, L.BookID, B.Title, B.Author, L.LoanDate, L.DueDate,
                           CASE WHEN date('now') > date(L.DueDate) THEN 1 ELSE 0 END as IsOverdue,
                           julianday('now') - julianday(L.DueDate) as DaysOverdue
                    FROM Loans L
                    JOIN Books B ON L.BookID = B.BookID
                    WHERE L.MemberID = @memberId AND L.ReturnDate IS NULL
                    ORDER BY L.DueDate";

                command.Parameters.AddWithValue("@memberId", memberId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["LoanID"] = reader["LoanID"],
                            ["BookID"] = reader["BookID"],
                            ["Title"] = reader["Title"],
                            ["Author"] = reader["Author"],
                            ["LoanDate"] = reader["LoanDate"],
                            ["DueDate"] = reader["DueDate"],
                            ["IsOverdue"] = Convert.ToInt32(reader["IsOverdue"]) == 1,
                            ["DaysOverdue"] = Math.Max(0, Convert.ToInt32(reader["DaysOverdue"]))
                        });
                    }
                }
            }

            return results;
        }

        
        /// Gets loan history (returned loans) for a specific member.
        public List<Dictionary<string, object>> GetLoanHistory(int memberId)
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT L.LoanID, L.BookID, B.Title, B.Author, L.LoanDate, L.DueDate, L.ReturnDate, L.ReturnCondition
                    FROM Loans L
                    JOIN Books B ON L.BookID = B.BookID
                    WHERE L.MemberID = @memberId AND L.ReturnDate IS NOT NULL
                    ORDER BY L.ReturnDate DESC";

                command.Parameters.AddWithValue("@memberId", memberId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["LoanID"] = reader["LoanID"],
                            ["BookID"] = reader["BookID"],
                            ["Title"] = reader["Title"],
                            ["Author"] = reader["Author"],
                            ["LoanDate"] = reader["LoanDate"],
                            ["DueDate"] = reader["DueDate"],
                            ["ReturnDate"] = reader["ReturnDate"],
                            ["ReturnCondition"] = reader["ReturnCondition"] ?? ""
                        });
                    }
                }
            }

            return results;
        }

        
        /// Creates a new loan record (staff action).
        public int? CreateLoan(int bookId, int memberId, DateTime loanDate, DateTime dueDate)
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate)
                    VALUES (@bookId, @memberId, @loanDate, @dueDate);
                    SELECT last_insert_rowid();";

                command.Parameters.AddWithValue("@bookId", bookId);
                command.Parameters.AddWithValue("@memberId", memberId);
                command.Parameters.AddWithValue("@loanDate", loanDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dueDate", dueDate.ToString("yyyy-MM-dd"));

                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : null;
            }
        }

        
        /// Processes a return for an active loan (staff action).
        public bool ProcessReturn(int loanId, DateTime returnDate, string condition = "Good")
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE Loans 
                    SET ReturnDate = @returnDate, ReturnCondition = @condition
                    WHERE LoanID = @loanId AND ReturnDate IS NULL";

                command.Parameters.AddWithValue("@loanId", loanId);
                command.Parameters.AddWithValue("@returnDate", returnDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@condition", condition);

                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

        
        /// Gets all overdue loans (staff view).
        public List<Dictionary<string, object>> GetAllOverdueLoans()
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT L.LoanID, L.BookID, B.Title, B.Author, 
                           L.MemberID, A.FirstName, A.LastName, A.Email,
                           L.LoanDate, L.DueDate,
                           CAST(julianday('now') - julianday(L.DueDate) AS INTEGER) as DaysOverdue
                    FROM Loans L
                    JOIN Books B ON L.BookID = B.BookID
                    JOIN Accounts A ON L.MemberID = A.AccountID
                    WHERE L.ReturnDate IS NULL AND date('now') > date(L.DueDate)
                    ORDER BY L.DueDate";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["LoanID"] = reader["LoanID"],
                            ["BookID"] = reader["BookID"],
                            ["Title"] = reader["Title"],
                            ["Author"] = reader["Author"],
                            ["MemberID"] = reader["MemberID"],
                            ["FirstName"] = reader["FirstName"],
                            ["LastName"] = reader["LastName"],
                            ["Email"] = reader["Email"] ?? "",
                            ["LoanDate"] = reader["LoanDate"],
                            ["DueDate"] = reader["DueDate"],
                            ["DaysOverdue"] = reader["DaysOverdue"]
                        });
                    }
                }
            }

            return results;
        }

        #endregion

        #region Reservation Operations

        
        /// Checks if a book already has an active reservation.
        public bool HasActiveReservation(int bookId)
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COUNT(*) 
                    FROM Reservations 
                    WHERE BookID = @bookId AND FulfilledDate IS NULL";

                command.Parameters.AddWithValue("@bookId", bookId);

                var result = command.ExecuteScalar();
                return result != null && Convert.ToInt32(result) > 0;
            }
        }

        
        /// Creates a new reservation for a book (member action).
        public int? CreateReservation(int bookId, int memberId, DateTime reservationDate)
        {
            // Check if reservation already exists (extra safety beyond UNIQUE constraint)
            if (HasActiveReservation(bookId))
            {
                return null;
            }

            OpenConnection();

            try
            {
                using (var command = _connection!.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO Reservations (BookID, MemberID, ReservationDate)
                        VALUES (@bookId, @memberId, @reservationDate);
                        SELECT last_insert_rowid();";

                    command.Parameters.AddWithValue("@bookId", bookId);
                    command.Parameters.AddWithValue("@memberId", memberId);
                    command.Parameters.AddWithValue("@reservationDate", reservationDate.ToString("yyyy-MM-dd"));

                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : null;
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UNIQUE constraint violation
            {
                return null;
            }
        }

        
        /// Gets all active reservations for a specific member.
        public List<Dictionary<string, object>> GetActiveReservations(int memberId)
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT R.ReservationID, R.BookID, B.Title, B.Author, R.ReservationDate
                    FROM Reservations R
                    JOIN Books B ON R.BookID = B.BookID
                    WHERE R.MemberID = @memberId AND R.FulfilledDate IS NULL
                    ORDER BY R.ReservationDate";

                command.Parameters.AddWithValue("@memberId", memberId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["ReservationID"] = reader["ReservationID"],
                            ["BookID"] = reader["BookID"],
                            ["Title"] = reader["Title"],
                            ["Author"] = reader["Author"],
                            ["ReservationDate"] = reader["ReservationDate"]
                        });
                    }
                }
            }

            return results;
        }

        
        /// Gets the count of all active reservations system-wide (staff reporting).
        public int GetTotalActiveReservationsCount()
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM Reservations WHERE FulfilledDate IS NULL";

                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        #endregion

        #region Staff Reporting Operations

        
        /// Gets statistics for staff dashboard/reporting.
        public Dictionary<string, int> GetStaffStatistics()
        {
            OpenConnection();
            var stats = new Dictionary<string, int>();

            using (var command = _connection!.CreateCommand())
            {
                // Total items on loan
                command.CommandText = "SELECT COUNT(*) FROM Loans WHERE ReturnDate IS NULL";
                stats["TotalOnLoan"] = Convert.ToInt32(command.ExecuteScalar());

                // Overdue items
                command.CommandText = "SELECT COUNT(*) FROM Loans WHERE ReturnDate IS NULL AND date('now') > date(DueDate)";
                stats["TotalOverdue"] = Convert.ToInt32(command.ExecuteScalar());

                // Active reservations
                command.CommandText = "SELECT COUNT(*) FROM Reservations WHERE FulfilledDate IS NULL";
                stats["TotalReservations"] = Convert.ToInt32(command.ExecuteScalar());

                // Total members
                command.CommandText = "SELECT COUNT(*) FROM Accounts WHERE Role = 'Member' AND IsActive = 1";
                stats["TotalMembers"] = Convert.ToInt32(command.ExecuteScalar());

                // Total books in catalogue
                command.CommandText = "SELECT COUNT(*) FROM Books";
                stats["TotalBooks"] = Convert.ToInt32(command.ExecuteScalar());

                // Available books
                command.CommandText = "SELECT COUNT(*) FROM Books WHERE Status = 'Available'";
                stats["AvailableBooks"] = Convert.ToInt32(command.ExecuteScalar());
            }

            return stats;
        }

        
        /// Gets the most frequently borrowed books (staff reporting).
        public List<Dictionary<string, object>> GetMostBorrowedBooks(int topN = 10)
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT B.BookID, B.Title, B.Author, B.Genre, COUNT(L.LoanID) as BorrowCount
                    FROM Books B
                    JOIN Loans L ON B.BookID = L.BookID
                    GROUP BY B.BookID, B.Title, B.Author, B.Genre
                    ORDER BY BorrowCount DESC
                    LIMIT @topN";

                command.Parameters.AddWithValue("@topN", topN);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["BookID"] = reader["BookID"],
                            ["Title"] = reader["Title"],
                            ["Author"] = reader["Author"],
                            ["Genre"] = reader["Genre"] ?? "",
                            ["BorrowCount"] = reader["BorrowCount"]
                        });
                    }
                }
            }

            return results;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            CloseConnection();
            _connection?.Dispose();
        }

        #endregion
    }
}
