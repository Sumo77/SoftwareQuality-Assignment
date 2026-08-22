using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;

namespace LibraryQA.Core.Database
{
    // Database Helper, provides data access methods for the Library Management System
    // Handles all database queries and commands securely
    public class DatabaseHelper : IDisposable
    {
        private readonly string _connectionString;
        private SqliteConnection? _connection;

        public DatabaseHelper(string connectionString) // Initializes a new instance of DatabaseHelper with the specified connection string.
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private void OpenConnection() // Opens the database connection if not already open, and enables foreign keys
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

        public void CloseConnection() // Closes the database connection if it is open
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _connection.Close();
            }
        }

        public int? GetAccountIdByUsername(string username) // Retrieves the account ID for a given username, returning null if not found or inactive
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
            SELECT AccountID 
            FROM Accounts 
            WHERE Username = @username AND IsActive = 1";

                command.Parameters.AddWithValue("@username", username);

                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : null;
            }
        }
        #region Account Operations

        public int? ValidateLogin(string username, string passwordHash) // Validates login credentials and returns the account ID if valid
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

        public string? GetAccountRole(int accountId) // Retrieves the role of an account by its ID
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
        
        public Dictionary<string, object>? GetAccountInfo(int accountId) // Retrieves full account information for a given account ID
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

        public List<Dictionary<string, object>> SearchCatalogue(string searchTerm) // Searches the catalogue for books matching the search term in title, author, or ISBN, returning a list of matching books
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

        public Dictionary<string, object>? GetBookById(int bookId) // Retrieves detailed information for a specific book by its ID
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

        public bool UpdateBookStatus(int bookId, string newStatus) // Updates the validated status of a book in the catalogue
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

        public List<Dictionary<string, object>> GetBooksByStatus(string status) // Gets all books with a specific status
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

        public int GetActiveLoanCount(int memberId) // Gets the count of active loans for a specific member
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

        public List<Dictionary<string, object>> GetActiveLoans(int memberId) // Gets all active loans for a specific member, including overdue status and days overdue
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

        public List<Dictionary<string, object>> GetLoanHistory(int memberId) // Gets loan history (returned loans) for a specific member
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

        public int? CreateLoan(int bookId, int memberId, DateTime loanDate, DateTime dueDate) // Creates a new loan for a book, ensuring the book is available and updating its status to "On Loan"
        {
            OpenConnection();

            using (var transaction = _connection!.BeginTransaction())
            {
                // Reject if the item is not currently available (for example already on loan or reserved elsewhere)
                using (var checkCommand = _connection.CreateCommand())
                {
                    checkCommand.Transaction = transaction;
                    checkCommand.CommandText = "SELECT Status FROM Books WHERE BookID = @bookId";
                    checkCommand.Parameters.AddWithValue("@bookId", bookId);

                    var status = checkCommand.ExecuteScalar()?.ToString();
                    if (status != "Available")
                    {
                        transaction.Rollback();
                        return null;
                    }
                }

                int? loanId;
                using (var command = _connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate)
                        VALUES (@bookId, @memberId, @loanDate, @dueDate);
                        SELECT last_insert_rowid();";

                    command.Parameters.AddWithValue("@bookId", bookId);
                    command.Parameters.AddWithValue("@memberId", memberId);
                    command.Parameters.AddWithValue("@loanDate", loanDate.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@dueDate", dueDate.ToString("yyyy-MM-dd"));

                    var result = command.ExecuteScalar();
                    loanId = result != null ? Convert.ToInt32(result) : (int?)null;
                }

                if (loanId == null)
                {
                    transaction.Rollback();
                    return null;
                }

                using (var updateCommand = _connection.CreateCommand())
                {
                    updateCommand.Transaction = transaction;
                    updateCommand.CommandText = "UPDATE Books SET Status = 'On Loan' WHERE BookID = @bookId";
                    updateCommand.Parameters.AddWithValue("@bookId", bookId);
                    updateCommand.ExecuteNonQuery();
                }

                transaction.Commit();
                return loanId;
            }
        }

        public Dictionary<string, object>? GetLoanById(int loanId) // Retrieves detailed information for a specific loan by its ID, including book and member details
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT L.LoanID, L.BookID, B.Title, L.MemberID, A.FirstName, A.LastName,
                           L.LoanDate, L.DueDate, L.ReturnDate
                    FROM Loans L
                    JOIN Books B ON L.BookID = B.BookID
                    JOIN Accounts A ON L.MemberID = A.AccountID
                    WHERE L.LoanID = @loanId";

                command.Parameters.AddWithValue("@loanId", loanId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Dictionary<string, object>
                        {
                            ["LoanID"] = reader["LoanID"],
                            ["BookID"] = reader["BookID"],
                            ["Title"] = reader["Title"],
                            ["MemberID"] = reader["MemberID"],
                            ["MemberName"] = $"{reader["FirstName"]} {reader["LastName"]}",
                            ["LoanDate"] = reader["LoanDate"],
                            ["DueDate"] = reader["DueDate"],
                            ["ReturnDate"] = reader["ReturnDate"] ?? DBNull.Value
                        };
                    }
                }
            }

            return null;
        }

        public bool ProcessReturn(int loanId, DateTime returnDate, string condition = "Good") // Processes the return of a loaned book, updating the loan record and restoring the book's status based on pending reservations
        {
            OpenConnection();

            using (var transaction = _connection!.BeginTransaction())
            {
                int bookId;
                using (var lookupCommand = _connection.CreateCommand())
                {
                    lookupCommand.Transaction = transaction;
                    lookupCommand.CommandText = "SELECT BookID FROM Loans WHERE LoanID = @loanId AND ReturnDate IS NULL";
                    lookupCommand.Parameters.AddWithValue("@loanId", loanId);

                    var bookIdResult = lookupCommand.ExecuteScalar();
                    if (bookIdResult == null)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    bookId = Convert.ToInt32(bookIdResult);
                }

                using (var command = _connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
                        UPDATE Loans 
                        SET ReturnDate = @returnDate, ReturnCondition = @condition
                        WHERE LoanID = @loanId AND ReturnDate IS NULL";

                    command.Parameters.AddWithValue("@loanId", loanId);
                    command.Parameters.AddWithValue("@returnDate", returnDate.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@condition", condition);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }

                // Restore catalogue status: "reserved" if a pending reservation exists, otherwise "available"
                using (var reservationCheckCommand = _connection.CreateCommand())
                {
                    reservationCheckCommand.Transaction = transaction;
                    reservationCheckCommand.CommandText = "SELECT COUNT(*) FROM Reservations WHERE BookID = @bookId AND FulfilledDate IS NULL";
                    reservationCheckCommand.Parameters.AddWithValue("@bookId", bookId);

                    bool hasReservation = Convert.ToInt32(reservationCheckCommand.ExecuteScalar()) > 0;

                    using (var updateCommand = _connection.CreateCommand())
                    {
                        updateCommand.Transaction = transaction;
                        updateCommand.CommandText = "UPDATE Books SET Status = @status WHERE BookID = @bookId";
                        updateCommand.Parameters.AddWithValue("@status", hasReservation ? "Reserved" : "Available");
                        updateCommand.Parameters.AddWithValue("@bookId", bookId);
                        updateCommand.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return true;
            }
        }

        public List<Dictionary<string, object>> GetAllOverdueLoans() // Retrieves all overdue loans, including member and book details, for staff reporting
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
                            ["MemberName"] = $"{reader["FirstName"]} {reader["LastName"]}",
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

        public bool HasActiveReservation(int bookId) // Checks if a book already has an active reservation
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

        public int? CreateReservation(int bookId, int memberId, DateTime reservationDate) // Creates a new reservation for a book, ensuring no active reservation exists for the same book
        {

            if (HasActiveReservation(bookId)) // Check if reservation already exists
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
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                return null;
            }
        }

        public List<Dictionary<string, object>> GetActiveReservations(int memberId) // Gets all active reservations for a specific member, including book details and reservation date
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

        public List<Dictionary<string, object>> GetAllActiveReservations() // Gets all active reservations system-wide, including book and member details, for staff reporting
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT R.ReservationID, R.BookID, B.Title, B.Author,
                           R.MemberID, A.FirstName, A.LastName, R.ReservationDate
                    FROM Reservations R
                    JOIN Books B ON R.BookID = B.BookID
                    JOIN Accounts A ON R.MemberID = A.AccountID
                    WHERE R.FulfilledDate IS NULL
                    ORDER BY R.ReservationDate";

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
                            ["MemberID"] = reader["MemberID"],
                            ["MemberName"] = $"{reader["FirstName"]} {reader["LastName"]}",
                            ["ReservationDate"] = reader["ReservationDate"]
                        });
                    }
                }
            }

            return results;
        }

        public int GetTotalActiveReservationsCount() // Gets the total count of active reservations system-wide, for staff reporting
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

        public void Dispose() // Ensure the database connection is properly closed and disposed of
        {
            CloseConnection();
            _connection?.Dispose();
        }

        #endregion
    }
}
