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
    public class AuthenticationServiceTests
    {
        private string _dbPath = string.Empty;
        private AuthenticationService _auth = null!;

        [TestInitialize]
        public void Setup()
        {
            // Fresh database per test - MSTest doesn't guarantee execution order.
            _dbPath = Path.Combine(Path.GetTempPath(), $"logintest_{Guid.NewGuid()}.db");

            new DatabaseInitializer(_dbPath).InitializeDatabase();
            new DatabaseSeeder(_dbPath).SeedSampleData();

            _auth = new AuthenticationService($"Data Source={_dbPath}");
        }

        [TestCleanup]
        public void Cleanup()
        {
            SqliteConnection.ClearAllPools();   // releases the file handle
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-1: REQ-5, REQ-7
        [TestMethod]
        public void Authenticate_ValidMemberCredentials_ReturnsMemberRole()
        {
            UserRole? result = _auth.Authenticate("alice.member", "member123");

            Assert.AreEqual(UserRole.Member, result);
        }

        // TC-2: REQ-6, REQ-7, REQ-11
        [TestMethod]
        public void Authenticate_ValidStaffCredentials_ReturnsStaffRole()
        {
            UserRole? result = _auth.Authenticate("jane.staff", "staff456");

            Assert.AreEqual(UserRole.Staff, result);
        }

        // TC-3: REQ-11, REQ-12
        [TestMethod]
        public void Authenticate_IncorrectPassword_ReturnsNull()
        {
            UserRole? result = _auth.Authenticate("alice.member", "wrongpassword");

            Assert.IsNull(result);
        }
    }
}