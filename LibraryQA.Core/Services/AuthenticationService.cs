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

        private readonly string _connectionString;

        public AuthenticationService(string connectionString)
        {
            _connectionString = connectionString
                ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public UserRole? Authenticate(string username, string password) // Returns the account's role, or null if credentials are rejected.
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return null;

            using (var db = new DatabaseHelper(_connectionString))
            {
                int? accountId = db.ValidateLogin(username.Trim(), HashPassword(password));

                if (accountId == null)
                    return null;

                string? role = db.GetAccountRole(accountId.Value);

                // Fails closed: only an exact 'Staff' match grants staff access.
                return role == StaffRole ? UserRole.Staff : UserRole.Member;
            }
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