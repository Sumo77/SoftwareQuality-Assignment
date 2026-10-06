using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

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

        public string? GetAccountStatus(int accountId) // Retrieves the current AccountStatus ("Active" or "Suspended") for REQ-19/REQ-21
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = "SELECT AccountStatus FROM Accounts WHERE AccountID = @accountId";
                command.Parameters.AddWithValue("@accountId", accountId);

                var result = command.ExecuteScalar();
                return result?.ToString();
            }
        }

        // The four states an account can be in. Kept here as the one list the schema's CHECK
        // constraint must agree with (REQ-16 Pending, REQ-19 Suspended, REQ-21 Locked).
        public static readonly string[] AccountStatuses = { "Active", "Pending", "Suspended", "Locked" };

        public bool SetAccountStatus(int accountId, string status) // Sets an account's status (REQ-16 activation, REQ-19 suspend/reactivate, REQ-21 lockout)
        {
            if (Array.IndexOf(AccountStatuses, status) < 0)
            {
                throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", AccountStatuses)}.");
            }

            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = "UPDATE Accounts SET AccountStatus = @status WHERE AccountID = @accountId";
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@accountId", accountId);

                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

        public int GetFailedLoginAttempts(int accountId) // Groundwork for REQ-21 (Login Lockout) - reads the current failed-attempt count
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = "SELECT FailedLoginAttempts FROM Accounts WHERE AccountID = @accountId";
                command.Parameters.AddWithValue("@accountId", accountId);

                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        public void IncrementFailedLoginAttempts(int accountId) // Groundwork for REQ-21 - call after a failed login attempt
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = "UPDATE Accounts SET FailedLoginAttempts = FailedLoginAttempts + 1 WHERE AccountID = @accountId";
                command.Parameters.AddWithValue("@accountId", accountId);
                command.ExecuteNonQuery();
            }
        }

        public void ResetFailedLoginAttempts(int accountId) // Groundwork for REQ-21 - call after a successful login
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = "UPDATE Accounts SET FailedLoginAttempts = 0 WHERE AccountID = @accountId";
                command.Parameters.AddWithValue("@accountId", accountId);
                command.ExecuteNonQuery();
            }
        }

        public Dictionary<string, object>? GetAccountInfo(int accountId) // Retrieves full account information for a given account ID
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT AccountID, Username, Role, FirstName, LastName, Email, PhoneNumber, CreatedDate, AccountStatus
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
                            ["CreatedDate"] = reader["CreatedDate"],
                            ["AccountStatus"] = reader["AccountStatus"]
                        };
                    }
                }
            }

            return null;
        }

        public List<Dictionary<string, object>> GetAllMembers() // Gets all member accounts (any status), for staff selection lists and member management (REQ-19, "pick from a list" improvement)
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT AccountID, Username, FirstName, LastName, Email, PhoneNumber, AccountStatus, FailedLoginAttempts
                    FROM Accounts
                    WHERE Role = 'Member' AND IsActive = 1
                    ORDER BY LastName, FirstName";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["AccountID"] = reader["AccountID"],
                            ["Username"] = reader["Username"],
                            ["FirstName"] = reader["FirstName"],
                            ["LastName"] = reader["LastName"],
                            ["Email"] = reader["Email"] ?? "Not Provided",
                            ["PhoneNumber"] = reader["PhoneNumber"] ?? "Not Provided",
                            ["AccountStatus"] = reader["AccountStatus"],
                            ["FailedLoginAttempts"] = reader["FailedLoginAttempts"]
                        });
                    }
                }
            }

            return results;
        }

        // REQ-19/REQ-21: every account, staff included, for the management tab. Staff accounts lock
        // like any other, so they have to be visible here or a locked staff account is unrecoverable.
        public List<Dictionary<string, object>> GetAllAccounts()
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT AccountID, Username, FirstName, LastName, Role, Email, PhoneNumber, AccountStatus, FailedLoginAttempts
                    FROM Accounts
                    WHERE IsActive = 1
                    ORDER BY AccountID";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["AccountID"] = reader["AccountID"],
                            ["Username"] = reader["Username"],
                            ["FirstName"] = reader["FirstName"],
                            ["LastName"] = reader["LastName"],
                            ["Role"] = reader["Role"],
                            ["Email"] = reader["Email"] ?? "",
                            ["PhoneNumber"] = reader["PhoneNumber"] ?? "",
                            ["AccountStatus"] = reader["AccountStatus"],
                            ["FailedLoginAttempts"] = reader["FailedLoginAttempts"]
                        });
                    }
                }
            }

            return results;
        }

        // REQ-16: lets a screen check a username before the whole form is filled in. This is
        // advisory only - CreateAccount re-checks inside its transaction, so a username taken
        // between the two calls is still rejected.
        public bool UsernameExists(string username)
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM Accounts WHERE Username = @username";
                command.Parameters.AddWithValue("@username", username);

                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        // REQ-16: registers a new account. Returns null if the username is already taken, so the
        // uniqueness rule is enforced by the database rather than by a check the caller might skip.
        public int? CreateAccount(string username, string passwordHash, string role, string firstName,
            string lastName, string? email, string? phoneNumber, string accountStatus)
        {
            if (role != "Member" && role != "Staff")
            {
                throw new ArgumentException("Invalid role. Must be 'Member' or 'Staff'.");
            }

            if (Array.IndexOf(AccountStatuses, accountStatus) < 0)
            {
                throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", AccountStatuses)}.");
            }

            OpenConnection();

            using (var transaction = _connection!.BeginTransaction())
            {
                using (var existsCommand = _connection.CreateCommand())
                {
                    existsCommand.Transaction = transaction;
                    existsCommand.CommandText = "SELECT COUNT(*) FROM Accounts WHERE Username = @username";
                    existsCommand.Parameters.AddWithValue("@username", username);

                    if (Convert.ToInt32(existsCommand.ExecuteScalar()) > 0)
                    {
                        transaction.Rollback();
                        return null;
                    }
                }

                using (var insertCommand = _connection.CreateCommand())
                {
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = @"
                        INSERT INTO Accounts (Username, PasswordHash, Role, FirstName, LastName, Email, PhoneNumber, AccountStatus)
                        VALUES (@username, @passwordHash, @role, @firstName, @lastName, @email, @phoneNumber, @accountStatus);
                        SELECT last_insert_rowid();";

                    insertCommand.Parameters.AddWithValue("@username", username);
                    insertCommand.Parameters.AddWithValue("@passwordHash", passwordHash);
                    insertCommand.Parameters.AddWithValue("@role", role);
                    insertCommand.Parameters.AddWithValue("@firstName", firstName);
                    insertCommand.Parameters.AddWithValue("@lastName", lastName);
                    insertCommand.Parameters.AddWithValue("@email", (object?)email ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@phoneNumber", (object?)phoneNumber ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@accountStatus", accountStatus);

                    var result = insertCommand.ExecuteScalar();

                    if (result == null)
                    {
                        transaction.Rollback();
                        return null;
                    }

                    transaction.Commit();
                    return Convert.ToInt32(result);
                }
            }
        }

        #endregion

        #region Book/Catalogue Operations

        public List<Dictionary<string, object>> SearchCatalogue(string searchTerm) // Searches the catalogue for books matching the search term in title, author, or ISBN, returning a list of matching books
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                // Escape LIKE wildcard characters in the user's search term so that a literal
                // "%" or "_" is matched literally, rather than being treated as a wildcard.
                string escapedSearchTerm = searchTerm
                    .Replace("\\", "\\\\")
                    .Replace("%", "\\%")
                    .Replace("_", "\\_");

                command.CommandText = @"
                    SELECT BookID, ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description
                    FROM Books 
                    WHERE Title LIKE @searchTerm ESCAPE '\'
                       OR Author LIKE @searchTerm ESCAPE '\'
                       OR ISBN LIKE @searchTerm ESCAPE '\'
                    ORDER BY Title";

                command.Parameters.AddWithValue("@searchTerm", $"%{escapedSearchTerm}%");

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

        // DEF-09: the caller supplies today's local date. SQLite's date('now') is UTC, which in
        // New Zealand is up to a day behind, so loans could stay unflagged on the day they fell due.
        // Overdue status and the day count both come from OverdueRules, so the member and staff
        // views can never disagree (REQ-4, REQ-13).
        public List<Dictionary<string, object>> GetActiveLoans(int memberId, DateTime? today = null) // Gets all active loans for a specific member, including overdue status and days overdue
        {
            OpenConnection();
            DateTime asAt = (today ?? DateTime.Today).Date;
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT L.LoanID, L.BookID, B.Title, B.Author, L.LoanDate, L.DueDate
                    FROM Loans L
                    JOIN Books B ON L.BookID = B.BookID
                    WHERE L.MemberID = @memberId AND L.ReturnDate IS NULL
                    ORDER BY L.DueDate";

                command.Parameters.AddWithValue("@memberId", memberId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime dueDate = ReadDate(reader["DueDate"]);

                        results.Add(new Dictionary<string, object>
                        {
                            ["LoanID"] = reader["LoanID"],
                            ["BookID"] = reader["BookID"],
                            ["Title"] = reader["Title"],
                            ["Author"] = reader["Author"],
                            ["LoanDate"] = reader["LoanDate"],
                            ["DueDate"] = reader["DueDate"],
                            ["IsOverdue"] = OverdueRules.IsOverdue(dueDate, asAt),
                            ["DaysOverdue"] = OverdueRules.DaysOverdue(dueDate, asAt)
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

        // DEF-09: same local-date rule as GetActiveLoans, and the same day count via OverdueRules.
        public List<Dictionary<string, object>> GetAllOverdueLoans(DateTime? today = null) // Retrieves all overdue loans, including member and book details, for staff reporting
        {
            OpenConnection();
            DateTime asAt = (today ?? DateTime.Today).Date;
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT L.LoanID, L.BookID, B.Title, B.Author, 
                           L.MemberID, A.FirstName, A.LastName, A.Email,
                           L.LoanDate, L.DueDate
                    FROM Loans L
                    JOIN Books B ON L.BookID = B.BookID
                    JOIN Accounts A ON L.MemberID = A.AccountID
                    WHERE L.ReturnDate IS NULL AND date(@today) > date(L.DueDate)
                    ORDER BY L.DueDate";

                command.Parameters.AddWithValue("@today", asAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime dueDate = ReadDate(reader["DueDate"]);

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
                            ["DaysOverdue"] = OverdueRules.DaysOverdue(dueDate, asAt)
                        });
                    }
                }
            }

            return results;
        }

        public List<Dictionary<string, object>> GetAllActiveLoans() // Gets all currently active (unreturned) loans system-wide, for staff selection when processing a return (the "pick from a list" improvement)
        {
            OpenConnection();
            var results = new List<Dictionary<string, object>>();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT L.LoanID, L.BookID, B.Title, L.MemberID, A.FirstName, A.LastName, L.DueDate
                    FROM Loans L
                    JOIN Books B ON L.BookID = B.BookID
                    JOIN Accounts A ON L.MemberID = A.AccountID
                    WHERE L.ReturnDate IS NULL
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
                            ["MemberID"] = reader["MemberID"],
                            ["MemberName"] = $"{reader["FirstName"]} {reader["LastName"]}",
                            ["DueDate"] = reader["DueDate"]
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

        public int? FulfillReservation(int reservationId, DateTime collectionDate, int loanPeriodDays) // Marks a reservation as fulfilled when the reserving member collects the held book, and issues the loan for it
        {
            OpenConnection();

            using (var transaction = _connection!.BeginTransaction())
            {
                int bookId;
                int memberId;
                using (var lookupCommand = _connection.CreateCommand())
                {
                    lookupCommand.Transaction = transaction;
                    lookupCommand.CommandText = @"
                        SELECT BookID, MemberID FROM Reservations
                        WHERE ReservationID = @reservationId AND FulfilledDate IS NULL";
                    lookupCommand.Parameters.AddWithValue("@reservationId", reservationId);

                    using (var reader = lookupCommand.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            transaction.Rollback();
                            return null;
                        }

                        bookId = Convert.ToInt32(reader["BookID"]);
                        memberId = Convert.ToInt32(reader["MemberID"]);
                    }
                }

                // Only collectable if the book has actually been returned and held for this reservation
                using (var statusCommand = _connection.CreateCommand())
                {
                    statusCommand.Transaction = transaction;
                    statusCommand.CommandText = "SELECT Status FROM Books WHERE BookID = @bookId";
                    statusCommand.Parameters.AddWithValue("@bookId", bookId);

                    var status = statusCommand.ExecuteScalar()?.ToString();
                    if (status != "Reserved")
                    {
                        transaction.Rollback();
                        return null;
                    }
                }

                using (var fulfillCommand = _connection.CreateCommand())
                {
                    fulfillCommand.Transaction = transaction;
                    fulfillCommand.CommandText = @"
                        UPDATE Reservations
                        SET FulfilledDate = @fulfilledDate
                        WHERE ReservationID = @reservationId AND FulfilledDate IS NULL";
                    fulfillCommand.Parameters.AddWithValue("@fulfilledDate", collectionDate.ToString("yyyy-MM-dd"));
                    fulfillCommand.Parameters.AddWithValue("@reservationId", reservationId);

                    if (fulfillCommand.ExecuteNonQuery() == 0)
                    {
                        transaction.Rollback();
                        return null;
                    }
                }

                int? loanId;
                using (var loanCommand = _connection.CreateCommand())
                {
                    loanCommand.Transaction = transaction;
                    loanCommand.CommandText = @"
                        INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate)
                        VALUES (@bookId, @memberId, @loanDate, @dueDate);
                        SELECT last_insert_rowid();";

                    loanCommand.Parameters.AddWithValue("@bookId", bookId);
                    loanCommand.Parameters.AddWithValue("@memberId", memberId);
                    loanCommand.Parameters.AddWithValue("@loanDate", collectionDate.ToString("yyyy-MM-dd"));
                    loanCommand.Parameters.AddWithValue("@dueDate", collectionDate.AddDays(loanPeriodDays).ToString("yyyy-MM-dd"));

                    var result = loanCommand.ExecuteScalar();
                    loanId = result != null ? Convert.ToInt32(result) : (int?)null;
                }

                if (loanId == null)
                {
                    transaction.Rollback();
                    return null;
                }

                using (var updateBookCommand = _connection.CreateCommand())
                {
                    updateBookCommand.Transaction = transaction;
                    updateBookCommand.CommandText = "UPDATE Books SET Status = 'On Loan' WHERE BookID = @bookId";
                    updateBookCommand.Parameters.AddWithValue("@bookId", bookId);
                    updateBookCommand.ExecuteNonQuery();
                }

                transaction.Commit();
                return loanId;
            }
        }

        #endregion

        #region Staff Reporting Operations


        /// Gets statistics for staff dashboard/reporting.
        public Dictionary<string, int> GetStaffStatistics(DateTime? today = null)
        {
            OpenConnection();
            DateTime asAt = (today ?? DateTime.Today).Date;
            var stats = new Dictionary<string, int>();

            using (var command = _connection!.CreateCommand())
            {
                // DEF-09: the overdue count uses the same local date as the loan lists.
                command.Parameters.AddWithValue("@today", asAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

                // Total items on loan
                command.CommandText = "SELECT COUNT(*) FROM Loans WHERE ReturnDate IS NULL";
                stats["TotalOnLoan"] = Convert.ToInt32(command.ExecuteScalar());

                // Overdue items
                command.CommandText = "SELECT COUNT(*) FROM Loans WHERE ReturnDate IS NULL AND date(@today) > date(DueDate)";
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

        // REQ-21: staff clear a lockout. The status and the attempt counter are reset in the same
        // statement, so an unlocked account can never be left one failure away from locking again.
        public bool UnlockAccount(int accountId)
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE Accounts
                    SET AccountStatus = 'Active', FailedLoginAttempts = 0
                    WHERE AccountID = @accountId";

                command.Parameters.AddWithValue("@accountId", accountId);

                return command.ExecuteNonQuery() > 0;
            }
        }

        #endregion

        // Dates are stored as yyyy-MM-dd text, so they are parsed with the invariant culture
        // rather than whatever the machine is set to.
        private static DateTime ReadDate(object value)
        {
            return DateTime.Parse(value.ToString()!, CultureInfo.InvariantCulture);
        }

        #region IDisposable Implementation

        public void Dispose() // Ensure the database connection is properly closed and disposed of
        {
            CloseConnection();
            _connection?.Dispose();
        }

        #endregion
    }
}