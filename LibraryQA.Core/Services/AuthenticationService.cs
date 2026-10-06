using System;
using System.Security.Cryptography;
using System.Text;
using LibraryQA.Core.Database;
using LibraryQA.Core.Models;

namespace LibraryQA.Core.Services
{
    // Authentication Service, verifies credentials against the Accounts table and resolves the role
    public class AuthenticationService
    {
        private const string StaffRole = "Staff";
        private const string SuspendedStatus = "Suspended";
        private const string ActiveStatus = "Active";

        private readonly string _connectionString;

        public AuthenticationService(string connectionString)
        {
            _connectionString = connectionString
                ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public UserRole? Authenticate(string username, string password) // Returns the account's role, or null if credentials are rejected or the account is suspended.
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return null;

            using (var db = new DatabaseHelper(_connectionString))
            {
                int? accountId = db.ValidateLogin(username.Trim(), HashPassword(password));

                if (accountId == null)
                    return null;

                // Only an Active account may log in. Pending (REQ-16), Suspended (REQ-19) and
                // Locked (REQ-21) are all refused here, so no screen can bypass the check.
                if (!string.Equals(db.GetAccountStatus(accountId.Value), ActiveStatus, StringComparison.OrdinalIgnoreCase))
                    return null;

                string? role = db.GetAccountRole(accountId.Value);

                // Fails closed: only an exact 'Staff' match grants staff access.
                return role == StaffRole ? UserRole.Staff : UserRole.Member;
            }
        }

        // Returns the account's status, but only once the password has been verified, so the login
        // screen can explain a refusal without revealing that an account exists (REQ-11, DEF-20).
        // Null means the credentials themselves were wrong.
        public string? GetStatusWithValidCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return null;

            using (var db = new DatabaseHelper(_connectionString))
            {
                int? accountId = db.ValidateLogin(username.Trim(), HashPassword(password));

                if (accountId == null)
                    return null;

                return db.GetAccountStatus(accountId.Value);
            }
        }

        public bool IsSuspendedWithValidCredentials(string username, string password) // REQ-19, DEF-20
        {
            return string.Equals(GetStatusWithValidCredentials(username, password),
                SuspendedStatus, StringComparison.OrdinalIgnoreCase);
        }

        public static string HashPassword(string password) // Hash the password using SHA256 for secure comparison with stored hashes
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public int? GetAccountId(string username) // Returns the account ID for a given username, or null if the username does not exist
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            using (var db = new DatabaseHelper(_connectionString))
            {
                return db.GetAccountIdByUsername(username.Trim());
            }
        }
    }
}