using System;
using System.IO;
using LibraryQA.Core.Database;
using LibraryQA.Core.Models;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryQA.Tests
{
    [TestClass]
    // Login Interface Tests
    public class LoginViewTests
    {
        private string _dbPath = string.Empty;
        private AuthenticationService _auth = null!;

        [TestInitialize]
        public void Setup() // Setup the tests
        {
            // Fresh database per test due to randomised run/completion order
            _dbPath = Path.Combine(Path.GetTempPath(), $"logintest_{Guid.NewGuid()}.db");

            new DatabaseInitializer(_dbPath).InitializeDatabase();
            new DatabaseSeeder(_dbPath).SeedSampleData();

            _auth = new AuthenticationService($"Data Source={_dbPath}");
        }

        [TestCleanup]
        public void Cleanup() // Cleans/Deletes the tests
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-1: Valid Member Login resolves as role = Member (REQ-5, REQ-7)
        [TestMethod]
        public void Authenticate_ValidMemberCredentials_ReturnsMemberRole()
        {
            UserRole? result = _auth.Authenticate("alice.member", "member123");

            Assert.AreEqual(UserRole.Member, result);
        }

        // TC-2: Valid Staff Login resolves as role = Staff (REQ-6, REQ-7, REQ-11)
        [TestMethod]
        public void Authenticate_ValidStaffCredentials_ReturnsStaffRole()
        {
            UserRole? result = _auth.Authenticate("jane.staff", "staff456");

            Assert.AreEqual(UserRole.Staff, result);
        }

        // TC-3: Wrong password returns no role, and username cannot be used alone (REQ-11, REQ-12)
        [TestMethod]
        public void Authenticate_IncorrectPassword_ReturnsNull()
        {
            UserRole? result = _auth.Authenticate("alice.member", "wrongpassword");

            Assert.IsNull(result);
        }
    }
}