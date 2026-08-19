# Quick Reference Card - Library Database

## 🔐 Test Login Credentials

### Members (Password: "member123")
```
Username: alice.member
Username: bob.member
Username: carol.member
Username: david.member
Username: emma.member
```

### Staff (Password: "staff456")
```
Username: jane.staff
Username: mike.staff
```

### Password Hashes (for code)
```csharp
// Member password hash
"5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8"

// Staff password hash
"0b14d501a594442a01c6859541bcb3e8164d183d32937b851835442f69d5c94e"
```

---

## 📊 Database Tables

| Table | Records | Description |
|-------|---------|-------------|
| **Accounts** | 7 | Member and Staff logins |
| **Books** | 30 | Catalogue with status |
| **Loans** | 13 | Borrowing history |
| **Reservations** | 3 | Book reservations |

---

## 🚀 Quick Setup (3 Commands)

```bash
# 1. Restore packages
dotnet restore

# 2. Build project
dotnet build

# 3. Run application (initializes database)
dotnet run
```

Database file created at: `bin/Debug/net10.0/library.db`

---

## 💻 Basic Usage Code

### Initialize Database
```csharp
using SoftwareQuality_Assignment1.Database;

var dbPath = "library.db";
var initializer = new DatabaseInitializer(dbPath);
initializer.InitializeDatabase();

var loader = new SampleDataLoader(initializer.ConnectionString);
loader.LoadSampleData();
```

### Common Operations
```csharp
using var db = new DatabaseHelper("Data Source=library.db");

// Login
int? userId = db.ValidateLogin("alice.member", passwordHash);

// Search books
var books = db.SearchCatalogue("Harry Potter");

// Get member's loans
var loans = db.GetActiveLoans(memberId);

// Check loan limit
int loanCount = db.GetActiveLoanCount(memberId);

// Create loan (staff only)
var loanId = db.CreateLoan(bookId, memberId, DateTime.Now, dueDate);

// Process return (staff only)
bool success = db.ProcessReturn(loanId, DateTime.Now);
```

---

## 🔍 Useful SQL Queries

### View All Books
```sql
SELECT Title, Author, Status FROM Books ORDER BY Title;
```

### View Active Loans
```sql
SELECT B.Title, A.FirstName, A.LastName, L.DueDate
FROM Loans L
JOIN Books B ON L.BookID = B.BookID
JOIN Accounts A ON L.MemberID = A.AccountID
WHERE L.ReturnDate IS NULL;
```

### Find Overdue Items
```sql
SELECT B.Title, L.DueDate, 
	   julianday('now') - julianday(L.DueDate) as DaysOverdue
FROM Loans L
JOIN Books B ON L.BookID = B.BookID
WHERE L.ReturnDate IS NULL 
  AND date('now') > date(L.DueDate);
```

### Check Member's Loan Count
```sql
SELECT COUNT(*) 
FROM Loans 
WHERE MemberID = 1 AND ReturnDate IS NULL;
```

---

## 📁 File Locations

```
Database/
├── DatabaseSchema.sql          ← Table definitions
├── SampleData.sql              ← Test data
├── README_Database.md          ← Full documentation
├── SETUP_GUIDE.md              ← Setup instructions
├── REQUIREMENTS_VALIDATION.md  ← Validation results
└── QUICK_REFERENCE.md          ← This file

SoftwareQuality-Assignment1/Database/
├── DatabaseInitializer.cs      ← Schema creation
├── DatabaseHelper.cs           ← Data access layer
└── SampleDataLoader.cs         ← Sample data loader
```

---

## 🛠️ View Database Tools

### Option 1: DB Browser for SQLite ⭐ Recommended
- Download: https://sqlitebrowser.org/
- Open `library.db` from bin folder
- Browse tables, run queries, view data

### Option 2: VS Code Extension
- Install "SQLite Viewer" extension
- Right-click `library.db` → Open Database

### Option 3: Command Line
```bash
sqlite3 library.db
.tables
.schema Books
SELECT * FROM Books;
```

---

## ✅ Data Validation Checks

Run these to ensure database integrity:

```sql
-- No orphaned loans
SELECT * FROM Loans L
LEFT JOIN Books B ON L.BookID = B.BookID
WHERE B.BookID IS NULL;

-- No duplicate active reservations
SELECT BookID, COUNT(*) 
FROM Reservations 
WHERE FulfilledDate IS NULL
GROUP BY BookID 
HAVING COUNT(*) > 1;

-- Books status matches loans
SELECT B.BookID, B.Title, B.Status,
	   COUNT(L.LoanID) as ActiveLoans
FROM Books B
LEFT JOIN Loans L ON B.BookID = L.BookID 
  AND L.ReturnDate IS NULL
GROUP BY B.BookID;
```

---

## 🎯 Key Business Rules

| Rule | How Enforced |
|------|--------------|
| Single reservation per book | UNIQUE index on Reservations(BookID) |
| Book status validation | CHECK constraint (Available/On Loan/Reserved) |
| 14-day loan period | Application logic: dueDate = loanDate + 14 |
| 2-item loan limit | Query-based: GetActiveLoanCount() |
| Overdue detection | Generated column: date('now') > DueDate |
| No duplicate accounts | UNIQUE constraint on Username |

---

## 🧪 Sample Data Scenarios

✅ **Alice** - Has 1 active loan, returning soon  
✅ **Bob** - At loan limit (2 active), 1 overdue  
✅ **Carol** - 1 active loan, has 1 reservation  
✅ **David** - 1 overdue loan  
✅ **Emma** - New member, 1 active loan  

✅ **Overdue Books**: 2 loans past due date  
✅ **Reserved Books**: 2 active reservations  
✅ **Available Books**: 22 ready to borrow  

---

## 📞 Common Issues & Fixes

**Issue**: Database file not found  
**Fix**: Check `bin/Debug/net10.0/` folder or run initialization code

**Issue**: UNIQUE constraint violation on Reservations  
**Fix**: Expected! This prevents double-booking (REQ-3)

**Issue**: Foreign key constraint failed  
**Fix**: Use valid BookID and MemberID from database

**Issue**: Package not found  
**Fix**: Run `dotnet restore`

---

## 📚 Documentation Links

- **Full Docs**: `Database/README_Database.md`
- **Setup Guide**: `Database/SETUP_GUIDE.md`
- **Validation**: `Database/REQUIREMENTS_VALIDATION.md`
- **Summary**: `Database/IMPLEMENTATION_SUMMARY.md`

---

## 🎓 Next Development Steps

1. ✅ Database - COMPLETE
2. 🔜 Create business logic services
3. 🔜 Build GUI (Login/Member/Staff views)
4. 🔜 Write MSTest unit tests
5. 🔜 Integration testing

---

**Quick Start Time**: ~5 minutes  
**Status**: ✅ Ready to use  
**Requirements**: 14/14 validated  
**Build**: ✅ Passing  

---

*Keep this card handy for quick reference during development!*
