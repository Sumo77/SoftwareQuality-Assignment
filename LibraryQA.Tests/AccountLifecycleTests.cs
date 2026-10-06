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
    // Account Creation (REQ-16) and Login Lockout (REQ-21)
    public class AccountLifecycleTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private AuthenticationService _auth = null!;
        private RegistrationService _registration = null!;

        [TestInitialize]
        public void Setup() // Setup the tests
        {
            // Fresh database per test due to randomised run/completion order
            _dbPath = Path.Combine(Path.GetTempPath(), $"accounttest_{Guid.NewGuid()}.db");

            Assert.IsTrue(new DatabaseInitializer(_dbPath).InitializeDatabase(),
                "Database schema could not be created - check DatabaseSchema.sql is in the test output folder.");
            Assert.IsTrue(new DatabaseSeeder(_dbPath).SeedSampleData(),
                "Sample data could not be loaded - check SampleData.sql is in the test output folder.");

            _connectionString = $"Data Source={_dbPath}";
            _auth = new AuthenticationService(_connectionString);
            _registration = new RegistrationService(_connectionString);
        }

        [TestCleanup]
        public void Cleanup() // Cleans/Deletes the tests
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-40: A new registration creates a Pending account that cannot be used until staff activate it (REQ-16)
        [TestMethod]
        public void Register_NewUsername_CreatesPendingAccountThatCannotLogIn()
        {
            var result = _registration.Register("new.member", "password123", "New", "Member");

            Assert.IsTrue(result.Success, result.Message);
            Assert.IsNotNull(result.AccountId);

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.AreEqual("Pending", db.GetAccountStatus(result.AccountId!.Value),
                    "A new account must start Pending, not Active.");

                // The password is stored hashed, so the raw text does not match the stored value (REQ-11).
                Assert.IsNull(db.ValidateLogin("new.member", "password123"));
                Assert.AreEqual(result.AccountId, db.ValidateLogin("new.member", AuthenticationService.HashPassword("password123")));
            }

            Assert.IsNull(_auth.Authenticate("new.member", "password123"),
                "A Pending account must not be able to log in before staff activate it.");
        }

        // TC-41: A username that is already taken is rejected, so usernames stay unique (REQ-16)
        [TestMethod]
        public void Register_DuplicateUsername_IsRejected()
        {
            var result = _registration.Register("alice.member", "password123", "Another", "Alice");

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.AccountId);
            Assert.AreEqual("That username is already taken. Please choose another.", result.Message);

            // The original account must be untouched - still Alice's, still usable.
            Assert.AreEqual(UserRole.Member, _auth.Authenticate("alice.member", "member123"));
        }

        // TC-42: Staff activation turns a Pending account into one that can log in (REQ-16)
        [TestMethod]
        public void Activate_PendingAccount_AllowsLogin()
        {
            var result = _registration.Register("new.member", "password123", "New", "Member");
            Assert.IsTrue(result.Success, result.Message);

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.IsTrue(db.SetAccountStatus(result.AccountId!.Value, "Active"));
            }

            Assert.AreEqual(UserRole.Member, _auth.Authenticate("new.member", "password123"),
                "Once staff activate the account it must behave like any other member account.");
        }

        // TC-43: Five failed attempts lock the account, and the correct password is then refused (REQ-21)
        [TestMethod]
        public void Authenticate_FiveFailedAttempts_LocksTheAccount()
        {
            int? accountId = _auth.GetAccountId("alice.member");
            Assert.IsNotNull(accountId);

            for (int attempt = 0; attempt < AuthenticationService.MaxFailedLoginAttempts; attempt++)
            {
                Assert.IsNull(_auth.Authenticate("alice.member", "wrongpassword"));
            }

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.AreEqual("Locked", db.GetAccountStatus(accountId!.Value));
                Assert.AreEqual(AuthenticationService.MaxFailedLoginAttempts, db.GetFailedLoginAttempts(accountId.Value));
            }

            Assert.IsNull(_auth.Authenticate("alice.member", "member123"),
                "A locked account must be refused even with the correct password.");
        }

        // TC-44: Staff unlocking an account clears the attempt count and restores access (REQ-21)
        [TestMethod]
        public void UnlockAccount_ResetsAttemptsAndRestoresLogin()
        {
            int? accountId = _auth.GetAccountId("alice.member");
            Assert.IsNotNull(accountId);

            for (int attempt = 0; attempt < AuthenticationService.MaxFailedLoginAttempts; attempt++)
            {
                _auth.Authenticate("alice.member", "wrongpassword");
            }

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.AreEqual("Locked", db.GetAccountStatus(accountId!.Value), "Precondition check: the account should be locked.");
                Assert.IsTrue(db.UnlockAccount(accountId.Value));

                Assert.AreEqual("Active", db.GetAccountStatus(accountId.Value));
                Assert.AreEqual(0, db.GetFailedLoginAttempts(accountId.Value),
                    "Unlocking must also clear the count, or one more mistake re-locks the account.");
            }

            Assert.AreEqual(UserRole.Member, _auth.Authenticate("alice.member", "member123"));
        }

        // TC-49: A staff account locks like any other and stays visible to staff, so a lockout is
        // recoverable from inside the application (REQ-21)
        [TestMethod]
        public void Authenticate_StaffAccount_LocksAndRemainsVisibleForUnlocking()
        {
            int? accountId = _auth.GetAccountId("jane.staff");
            Assert.IsNotNull(accountId);

            for (int attempt = 0; attempt < AuthenticationService.MaxFailedLoginAttempts; attempt++)
            {
                Assert.IsNull(_auth.Authenticate("jane.staff", "wrongpassword"));
            }

            Assert.IsNull(_auth.Authenticate("jane.staff", "staff456"),
                "A locked staff account must be refused like any other.");

            using (var db = new DatabaseHelper(_connectionString))
            {
                bool listed = false;

                foreach (var account in db.GetAllAccounts())
                {
                    if (Convert.ToInt32(account["AccountID"]) == accountId!.Value)
                    {
                        listed = true;
                        Assert.AreEqual("Locked", account["AccountStatus"]?.ToString());
                        Assert.AreEqual("Staff", account["Role"]?.ToString());
                    }
                }

                Assert.IsTrue(listed,
                    "A locked staff account must appear in the Accounts tab, or no one can unlock it.");

                Assert.IsTrue(db.UnlockAccount(accountId!.Value));
            }

            Assert.AreEqual(UserRole.Staff, _auth.Authenticate("jane.staff", "staff456"));
        }

        // TC-45: A successful login clears any earlier failed attempts, so they never accumulate (REQ-21)
        [TestMethod]
        public void Authenticate_SuccessfulLogin_ResetsFailedAttempts()
        {
            int? accountId = _auth.GetAccountId("alice.member");
            Assert.IsNotNull(accountId);

            _auth.Authenticate("alice.member", "wrongpassword");
            _auth.Authenticate("alice.member", "wrongpassword");

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.AreEqual(2, db.GetFailedLoginAttempts(accountId!.Value), "Precondition check: two failures recorded.");
            }

            Assert.AreEqual(UserRole.Member, _auth.Authenticate("alice.member", "member123"));

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.AreEqual(0, db.GetFailedLoginAttempts(accountId!.Value),
                    "Occasional typos spread over time must not eventually lock a legitimate member out.");
            }
        }

        // TC-50: Every registration rule rejects bad input, and nothing is written when it does (REQ-16, REQ-9)
        [TestMethod]
        public void Register_InvalidInput_IsRejectedWithoutCreatingAnAccount()
        {
            int accountsBefore;

            using (var db = new DatabaseHelper(_connectionString))
            {
                accountsBefore = db.GetAllAccounts().Count;
            }

            Assert.IsFalse(_registration.Register("ab", "password123", "New", "Member").Success, "username too short");
            Assert.IsFalse(_registration.Register("bad user", "password123", "New", "Member").Success, "username has a space");
            Assert.IsFalse(_registration.Register("good.user", "short", "New", "Member").Success, "password too short");
            Assert.IsFalse(_registration.Register("good.user", "password123", "", "Member").Success, "first name missing");
            Assert.IsFalse(_registration.Register("good.user", "password123", "New", "").Success, "last name missing");
            Assert.IsFalse(_registration.Register("good.user", "password123", "New", "Member", "not-an-email").Success, "malformed email");
            Assert.IsFalse(_registration.Register("good.user", "password123", "New", "Member", "a@b.com", "12ab34").Success, "letters in phone number");

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.AreEqual(accountsBefore, db.GetAllAccounts().Count,
                    "A rejected registration must not leave a partial account behind.");
            }
        }

        // TC-51: The registration screen can tell a taken username from a free one before submitting (REQ-16)
        [TestMethod]
        public void IsUsernameAvailable_DistinguishesTakenFromFree()
        {
            Assert.IsFalse(_registration.IsUsernameAvailable("alice.member"));
            Assert.IsTrue(_registration.IsUsernameAvailable("brand.new.member"));

            // Advisory only: the rule is still enforced by CreateAccount, so the two must agree.
            Assert.IsTrue(_registration.Register("brand.new.member", "password123", "Brand", "New").Success);
            Assert.IsFalse(_registration.IsUsernameAvailable("brand.new.member"));
        }

        // TC-52: Failed attempts against an unknown username change nothing, so the lockout cannot
        // be used to discover which usernames exist (REQ-11, REQ-21)
        [TestMethod]
        public void Authenticate_UnknownUsername_RecordsNothing()
        {
            int? aliceId = _auth.GetAccountId("alice.member");
            Assert.IsNotNull(aliceId);

            int accountsBefore;

            using (var db = new DatabaseHelper(_connectionString))
            {
                accountsBefore = db.GetAllAccounts().Count;
            }

            for (int attempt = 0; attempt < AuthenticationService.MaxFailedLoginAttempts + 2; attempt++)
            {
                Assert.IsNull(_auth.Authenticate("ghost.member", "wrongpassword"));
            }

            Assert.IsNull(_auth.GetAccountId("ghost.member"), "No account may be created by a failed login.");

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.AreEqual(accountsBefore, db.GetAllAccounts().Count);
                Assert.AreEqual(0, db.GetFailedLoginAttempts(aliceId!.Value),
                    "A failure against one username must never be counted against another.");
            }
        }

        // TC-53: A suspended account is not converted to Locked by failed attempts (REQ-19, REQ-21)
        [TestMethod]
        public void Authenticate_SuspendedAccount_IsNotCountedTowardsLockout()
        {
            int? accountId = _auth.GetAccountId("alice.member");
            Assert.IsNotNull(accountId);

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.IsTrue(db.SetAccountStatus(accountId!.Value, "Suspended"));
            }

            for (int attempt = 0; attempt < AuthenticationService.MaxFailedLoginAttempts; attempt++)
            {
                Assert.IsNull(_auth.Authenticate("alice.member", "wrongpassword"));
            }

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.AreEqual("Suspended", db.GetAccountStatus(accountId!.Value),
                    "Suspension is a staff decision - the lockout must not overwrite it.");
                Assert.AreEqual(0, db.GetFailedLoginAttempts(accountId.Value));
            }
        }
    }
}