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

            Assert.IsTrue(new DatabaseInitializer(_dbPath).InitializeDatabase(),
                "Database schema could not be created - check DatabaseSchema.sql is in the test output folder.");
            Assert.IsTrue(new DatabaseSeeder(_dbPath).SeedSampleData(),
                "Sample data could not be loaded - check SampleData.sql is in the test output folder.");

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
        [TestCategory("Smoke")]
        public void Authenticate_ValidMemberCredentials_ReturnsMemberRole()
        {
            UserRole? result = _auth.Authenticate("alice.member", "member123");

            Assert.AreEqual(UserRole.Member, result);
        }

        // TC-2: Valid Staff Login resolves as role = Staff (REQ-6, REQ-7, REQ-11)
        [TestMethod]
        [TestCategory("Smoke")]
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

        // TC-20: A suspended member cannot log in, and reactivating restores access (REQ-19, REQ-12)
        [TestMethod]
        public void SetAccountStatus_SuspendThenReactivate_BlocksThenRestoresLogin()
        {
            int? accountId = _auth.GetAccountId("alice.member");
            Assert.IsNotNull(accountId);

            using (var db = new DatabaseHelper($"Data Source={_dbPath}"))
            {
                Assert.IsTrue(db.SetAccountStatus(accountId.Value, "Suspended"));
            }

            Assert.IsNull(_auth.Authenticate("alice.member", "member123"),
                "A suspended account must be refused even with the correct password.");

            using (var db = new DatabaseHelper($"Data Source={_dbPath}"))
            {
                Assert.IsTrue(db.SetAccountStatus(accountId.Value, "Active"));
            }

            Assert.AreEqual(UserRole.Member, _auth.Authenticate("alice.member", "member123"),
                "Reactivating the account must restore access.");
        }

        // TC-39: A wrong password and an unknown username are rejected identically, so the
        // login screen cannot be used to confirm whether an account exists (DEF-20, REQ-11)
        [TestMethod]
        public void Authenticate_WrongPasswordAndUnknownUsername_AreRejectedIdentically()
        {
            UserRole? wrongPassword = _auth.Authenticate("alice.member", "wrongpassword");
            UserRole? unknownUsername = _auth.Authenticate("ghost.member", "wrongpassword");

            Assert.IsNull(wrongPassword);
            Assert.IsNull(unknownUsername);
            Assert.AreEqual(wrongPassword, unknownUsername,
                "Both failures must be indistinguishable to the caller.");
        }
    }
}