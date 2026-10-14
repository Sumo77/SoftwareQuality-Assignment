using System;
using System.IO;
using System.Linq;
using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryQA.Tests
{
    [TestClass]
    // Staff Member Lookup Tests (REQ-6: staff see a member's loans, overdue items and reservations, basic details only)
    public class MemberLookupServiceTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private MemberLookupService _service = null!;

        [TestInitialize]
        public void Setup() // Setup the tests
        {
            // Fresh database per test due to randomised run/completion order
            _dbPath = Path.Combine(Path.GetTempPath(), $"lookuptest_{Guid.NewGuid()}.db");

            Assert.IsTrue(new DatabaseInitializer(_dbPath).InitializeDatabase(),
                "Database schema could not be created - check DatabaseSchema.sql is in the test output folder.");
            Assert.IsTrue(new DatabaseSeeder(_dbPath).SeedSampleData(),
                "Sample data could not be loaded - check SampleData.sql is in the test output folder.");

            _connectionString = $"Data Source={_dbPath}";
            _service = new MemberLookupService(_connectionString);
        }

        [TestCleanup]
        public void Cleanup() // Cleans/Deletes the tests
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // Each test builds the loans it needs, so none depends on specific seeded loans or IDs.
        private int AccountId(string username)
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                int? id = db.GetAccountIdByUsername(username);
                Assert.IsNotNull(id, $"Seed account '{username}' was not found.");
                return id.Value;
            }
        }

        private int AddBook(string title)
        {
            var result = new CatalogueService(_connectionString).AddBook(new BookInput { Title = title, Author = "Test Author" });
            Assert.IsTrue(result.Success, result.Message);
            return result.BookId!.Value;
        }

        private void ReturnAllLoans(int memberId) // Gives the member a clean slate whatever the seed data contains
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                foreach (var loan in db.GetActiveLoans(memberId))
                {
                    Assert.IsTrue(db.ProcessReturn(Convert.ToInt32(loan["LoanID"]), DateTime.Today));
                }
            }
        }

        private void Lend(int bookId, int memberId, int dueInDays) // Negative means the loan fell due that many days ago
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.IsNotNull(db.CreateLoan(bookId, memberId, DateTime.Today.AddDays(dueInDays - 14), DateTime.Today.AddDays(dueInDays)));
            }
        }

        // TC-83: Looking up an ID that does not exist fails with a clear message instead of an error (REQ-12)
        [TestMethod]
        public void LookUpMember_UnknownId_Fails()
        {
            var result = _service.LookUpMember(999999);

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Message, "No member found");
        }

        // TC-84: A staff account cannot be looked up - the lookup only covers members (REQ-6, REQ-11)
        [TestMethod]
        public void LookUpMember_StaffAccount_Fails()
        {
            var result = _service.LookUpMember(AccountId("jane.staff"));

            Assert.IsFalse(result.Success);
        }

        // TC-85: A member's loan list holds only that member's loans, never another member's (REQ-6, REQ-5)
        [TestMethod]
        public void LookUpMember_ReturnsOnlyThatMembersLoans()
        {
            int alice = AccountId("alice.member");
            int bob = AccountId("bob.member");
            ReturnAllLoans(alice);
            ReturnAllLoans(bob);

            Lend(AddBook("Alice's Book"), alice, dueInDays: 7);
            Lend(AddBook("Bob's First Book"), bob, dueInDays: 7);
            Lend(AddBook("Bob's Second Book"), bob, dueInDays: 7);

            var aliceResult = _service.LookUpMember(alice);
            var bobResult = _service.LookUpMember(bob);

            Assert.HasCount(1, aliceResult.ActiveLoans);
            Assert.AreEqual("Alice's Book", aliceResult.ActiveLoans[0]["Title"].ToString());
            Assert.HasCount(2, bobResult.ActiveLoans);
        }

        // TC-86: Overdue items are split from current loans, with the day count from the shared overdue rule (REQ-4, DEF-09)
        [TestMethod]
        public void LookUpMember_SplitsOverdueFromCurrentLoans()
        {
            int alice = AccountId("alice.member");
            ReturnAllLoans(alice);

            Lend(AddBook("On Time Book"), alice, dueInDays: 5);
            Lend(AddBook("Late Book"), alice, dueInDays: -1);

            var result = _service.LookUpMember(alice);

            Assert.HasCount(2, result.ActiveLoans);
            Assert.HasCount(1, result.OverdueLoans);
            Assert.AreEqual("Late Book", result.OverdueLoans[0]["Title"].ToString());
            Assert.AreEqual(1, Convert.ToInt32(result.OverdueLoans[0]["DaysOverdue"])); // due yesterday = exactly one day overdue
        }

        // TC-87: A loan due today is not overdue (boundary, REQ-4)
        [TestMethod]
        public void LookUpMember_LoanDueToday_IsNotOverdue()
        {
            int alice = AccountId("alice.member");
            ReturnAllLoans(alice);

            Lend(AddBook("Due Today Book"), alice, dueInDays: 0);

            var result = _service.LookUpMember(alice);

            Assert.HasCount(1, result.ActiveLoans);
            Assert.IsEmpty(result.OverdueLoans);
        }

        // TC-88: Contact details are returned only while the member has overdue items (REQ-6, REQ-11)
        [TestMethod]
        public void LookUpMember_ContactDetails_OnlyWhileOverdue()
        {
            int alice = AccountId("alice.member");
            ReturnAllLoans(alice);

            var withoutOverdue = _service.LookUpMember(alice);
            Assert.IsNull(withoutOverdue.Email);
            Assert.IsNull(withoutOverdue.PhoneNumber);

            Lend(AddBook("Late Book"), alice, dueInDays: -3);

            var withOverdue = _service.LookUpMember(alice);
            Assert.IsFalse(string.IsNullOrEmpty(withOverdue.Email));
            Assert.IsFalse(string.IsNullOrEmpty(withOverdue.PhoneNumber));
        }

        // TC-89: A member's active reservations appear in the lookup (REQ-6, REQ-3)
        [TestMethod]
        public void LookUpMember_ReturnsActiveReservations()
        {
            int alice = AccountId("alice.member");
            int bob = AccountId("bob.member");
            int before = _service.LookUpMember(alice).Reservations.Count;

            int bookId = AddBook("Reserved Book");
            Lend(bookId, bob, dueInDays: 7);

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.IsNotNull(db.CreateReservation(bookId, alice, DateTime.Today));
            }

            var after = _service.LookUpMember(alice).Reservations;

            Assert.HasCount(before + 1, after);
            Assert.IsTrue(after.Any(reservation => reservation["Title"].ToString() == "Reserved Book"));
        }

        // TC-90: The data staff can read about a member holds no password hash or login data (REQ-6, REQ-11)
        [TestMethod]
        public void GetMemberBasicInfo_DoesNotExposeSensitiveFields()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                var info = db.GetMemberBasicInfo(AccountId("alice.member"));

                Assert.IsNotNull(info);
                Assert.IsFalse(info.ContainsKey("PasswordHash"));
                Assert.IsFalse(info.ContainsKey("FailedLoginAttempts"));
                Assert.IsFalse(info.ContainsKey("Role"));
                Assert.IsFalse(info.ContainsKey("CreatedDate"));
            }
        }
    }
}