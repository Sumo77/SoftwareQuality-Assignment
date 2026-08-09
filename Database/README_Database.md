# Library Management System - Database Documentation

## Overview

This document describes the SQLite database schema for the Library Book Loaning, Returns, and Catalogue Management System. The database is designed to support quality assurance requirements outlined in the project specification, with emphasis on data integrity, reliability, and maintainability.

---

## Table of Contents

1. [Database Architecture](#database-architecture)
2. [Table Schemas](#table-schemas)
3. [Relationships & Constraints](#relationships--constraints)
4. [Business Rules Enforcement](#business-rules-enforcement)
5. [Common Queries](#common-queries)
6. [Setup Instructions](#setup-instructions)
7. [Viewing the Database](#viewing-the-database)

---

## Database Architecture

### Technology
- **Database Engine**: SQLite 3
- **Connection Library**: Microsoft.Data.Sqlite (.NET 10)
- **File Storage**: Single `.db` file (embedded database)

### Design Principles
- **Single Source of Truth**: Book status is the authoritative field for availability
- **Normalized Schema**: Reduces redundancy while maintaining query performance
- **Constraint-Based Validation**: Enforces business rules at database level
- **Referential Integrity**: Foreign keys with appropriate CASCADE/RESTRICT rules

---

## Table Schemas

### 1. Accounts Table

Stores both **Member** and **Staff** accounts in a unified structure.

```sql
CREATE TABLE Accounts (
	AccountID       INTEGER PRIMARY KEY AUTOINCREMENT,
	Username        TEXT NOT NULL UNIQUE,
	PasswordHash    TEXT NOT NULL,
	Role            TEXT NOT NULL CHECK(Role IN ('Member', 'Staff')),
	FirstName       TEXT NOT NULL,
	LastName        TEXT NOT NULL,
	Email           TEXT,
	PhoneNumber     TEXT,
	CreatedDate     TEXT NOT NULL DEFAULT (datetime('now')),
	IsActive        INTEGER NOT NULL DEFAULT 1 CHECK(IsActive IN (0, 1))
);
```

**Key Fields:**
- `AccountID`: Auto-incrementing primary key
- `Role`: Either 'Member' or 'Staff' (enforced via CHECK constraint)
- `PasswordHash`: Stores hashed passwords (SHA256 for prototype; production would use bcrypt/PBKDF2)
- `IsActive`: Soft delete flag (0 = inactive, 1 = active)

**Indexes:**
- `idx_accounts_username`: Fast login lookups
- `idx_accounts_role`: Role-based filtering

**Supports Requirements:** REQ-5 (Member Account), REQ-6 (Staff Account), REQ-7 (Access Control), REQ-11 (Security)

---

### 2. Books Table

Catalogue of all library items with availability status.

```sql
CREATE TABLE Books (
	BookID          INTEGER PRIMARY KEY AUTOINCREMENT,
	ISBN            TEXT UNIQUE,
	Title           TEXT NOT NULL,
	Author          TEXT NOT NULL,
	Publisher       TEXT,
	PublicationYear INTEGER,
	Genre           TEXT,
	Status          TEXT NOT NULL DEFAULT 'Available' 
					CHECK(Status IN ('Available', 'On Loan', 'Reserved')),
	Description     TEXT,
	AddedDate       TEXT NOT NULL DEFAULT (datetime('now'))
);
```

**Key Fields:**
- `BookID`: Auto-incrementing primary key
- `Status`: **Single source of truth** for availability ('Available', 'On Loan', or 'Reserved')
- `ISBN`: Unique identifier (optional field)

**Indexes:**
- `idx_books_title`: Search by title
- `idx_books_author`: Search by author
- `idx_books_status`: Filter by availability status
- `idx_books_isbn`: Lookup by ISBN

**Supports Requirements:** REQ-1 (Catalogue Management), REQ-14 (Data Integrity)

---

### 3. Loans Table

Tracks all borrowing transactions (active and historical).

```sql
CREATE TABLE Loans (
	LoanID          INTEGER PRIMARY KEY AUTOINCREMENT,
	BookID          INTEGER NOT NULL,
	MemberID        INTEGER NOT NULL,
	LoanDate        TEXT NOT NULL DEFAULT (date('now')),
	DueDate         TEXT NOT NULL,
	ReturnDate      TEXT,
	ReturnCondition TEXT,
	IsOverdue       INTEGER AS (
		CASE 
			WHEN ReturnDate IS NULL AND date('now') > date(DueDate) THEN 1
			ELSE 0
		END
	) STORED,
	FOREIGN KEY (BookID) REFERENCES Books(BookID) ON DELETE RESTRICT,
	FOREIGN KEY (MemberID) REFERENCES Accounts(AccountID) ON DELETE RESTRICT
);
```

**Key Fields:**
- `LoanID`: Auto-incrementing primary key
- `ReturnDate`: NULL indicates **active loan**; non-NULL indicates returned
- `DueDate`: Calculated as `LoanDate + 14 days` (enforced in application logic)
- `IsOverdue`: **Generated column** that auto-calculates overdue status

**Indexes:**
- `idx_loans_member`: Find loans by member
- `idx_loans_book`: Find loans by book
- `idx_loans_active`: Filter active loans (ReturnDate IS NULL)
- `idx_loans_overdue`: Overdue loan queries

**Supports Requirements:** REQ-2 (Borrow and Return), REQ-4 (Overdue), REQ-5 (Member Portal), REQ-6 (Staff Portal), REQ-8 (Loan Limits)

---

### 4. Reservations Table

Tracks active and fulfilled reservations.

```sql
CREATE TABLE Reservations (
	ReservationID   INTEGER PRIMARY KEY AUTOINCREMENT,
	BookID          INTEGER NOT NULL,
	MemberID        INTEGER NOT NULL,
	ReservationDate TEXT NOT NULL DEFAULT (date('now')),
	FulfilledDate   TEXT,
	NotifiedDate    TEXT,
	IsActive        INTEGER AS (
		CASE 
			WHEN FulfilledDate IS NULL THEN 1
			ELSE 0
		END
	) STORED,
	FOREIGN KEY (BookID) REFERENCES Books(BookID) ON DELETE RESTRICT,
	FOREIGN KEY (MemberID) REFERENCES Accounts(AccountID) ON DELETE RESTRICT
);
```

**Key Fields:**
- `ReservationID`: Auto-incrementing primary key
- `FulfilledDate`: NULL indicates **active reservation**
- `NotifiedDate`: Tracks when member was notified that book is available

**Critical Constraint:**
```sql
CREATE UNIQUE INDEX idx_reservations_active_book 
	ON Reservations(BookID) 
	WHERE FulfilledDate IS NULL;
```
This **prevents double-booking** by ensuring only one active reservation per book.

**Indexes:**
- `idx_reservations_active_book`: Enforce single active reservation (UNIQUE)
- `idx_reservations_member`: Find reservations by member
- `idx_reservations_active`: Filter active reservations

**Supports Requirements:** REQ-3 (Reservations), REQ-4 (Reservation Management), REQ-14 (Data Integrity)

---

## Relationships & Constraints

### Entity Relationship Diagram (Textual)

```
Accounts (1) ──< (M) Loans
Accounts (1) ──< (M) Reservations
Books (1) ──< (M) Loans
Books (1) ──< (M) Reservations
```

### Foreign Key Rules

| Child Table    | Parent Table | Column   | On Delete |
|---------------|-------------|----------|-----------|
| Loans         | Books       | BookID   | RESTRICT  |
| Loans         | Accounts    | MemberID | RESTRICT  |
| Reservations  | Books       | BookID   | RESTRICT  |
| Reservations  | Accounts    | MemberID | RESTRICT  |

**Rationale:** `ON DELETE RESTRICT` prevents accidental deletion of books or accounts that have associated loans or reservations (preserves audit trail).

---

## Business Rules Enforcement

### 1. Single Reservation Per Book (REQ-3)

**Enforced by:**
```sql
CREATE UNIQUE INDEX idx_reservations_active_book 
	ON Reservations(BookID) 
	WHERE FulfilledDate IS NULL;
```

**Result:** Database rejects second reservation attempt with `UNIQUE constraint violation`.

---

### 2. Book Status Validation (REQ-1, REQ-14)

**Enforced by:**
```sql
CHECK(Status IN ('Available', 'On Loan', 'Reserved'))
```

**Result:** Database rejects any invalid status value.

---

### 3. 14-Day Loan Period (REQ-2)

**Enforced in application logic:**
```csharp
DateTime dueDate = loanDate.AddDays(14);
```

---

### 4. Loan Limit (2 Items Per Member) (REQ-8)

**Enforced in application logic:**
```csharp
int activeLoans = dbHelper.GetActiveLoanCount(memberId);
if (activeLoans >= 2) {
	// Reject loan
}
```

**Rationale:** Variable limits are better handled in code than database constraints for flexibility.

---

### 5. Overdue Calculation (REQ-4)

**Calculated dynamically:**
```sql
SELECT * FROM Loans 
WHERE ReturnDate IS NULL AND date('now') > date(DueDate);
```

**Rationale:** Dynamic calculation ensures accuracy without requiring scheduled updates.

---

## Common Queries

### 1. Member Login Authentication

```sql
SELECT AccountID, Role 
FROM Accounts 
WHERE Username = ? AND PasswordHash = ? AND IsActive = 1;
```

### 2. Search Catalogue by Title/Author

```sql
SELECT BookID, Title, Author, Status 
FROM Books 
WHERE Title LIKE '%search%' OR Author LIKE '%search%'
ORDER BY Title;
```

### 3. Get Member's Active Loans

```sql
SELECT L.LoanID, B.Title, L.DueDate,
	   CASE WHEN date('now') > date(L.DueDate) THEN 1 ELSE 0 END as IsOverdue
FROM Loans L
JOIN Books B ON L.BookID = B.BookID
WHERE L.MemberID = ? AND L.ReturnDate IS NULL;
```

### 4. Check if Book Has Active Reservation

```sql
SELECT COUNT(*) 
FROM Reservations 
WHERE BookID = ? AND FulfilledDate IS NULL;
```

### 5. Get All Overdue Loans (Staff View)

```sql
SELECT L.LoanID, B.Title, A.FirstName, A.LastName, L.DueDate,
	   julianday('now') - julianday(L.DueDate) as DaysOverdue
FROM Loans L
JOIN Books B ON L.BookID = B.BookID
JOIN Accounts A ON L.MemberID = A.AccountID
WHERE L.ReturnDate IS NULL AND date('now') > date(L.DueDate)
ORDER BY L.DueDate;
```

### 6. Most Frequently Borrowed Books

```sql
SELECT B.BookID, B.Title, B.Author, COUNT(L.LoanID) as BorrowCount
FROM Books B
JOIN Loans L ON B.BookID = L.BookID
GROUP BY B.BookID
ORDER BY BorrowCount DESC
LIMIT 10;
```

---

## Setup Instructions

### 1. Install NuGet Package

Add the SQLite package to your .NET project:

```bash
dotnet add package Microsoft.Data.Sqlite
```

Or via Package Manager Console in Visual Studio:

```powershell
Install-Package Microsoft.Data.Sqlite
```

### 2. Initialize Database

```csharp
using SoftwareQuality_Assignment1.Database;

// Create database initializer
var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library.db");
var initializer = new DatabaseInitializer(dbPath);

// Initialize schema
if (initializer.InitializeDatabase())
{
	Console.WriteLine("Database created successfully!");

	// Load sample data
	var dataLoader = new SampleDataLoader(initializer.ConnectionString);
	if (dataLoader.LoadSampleData())
	{
		dataLoader.DisplayDataSummary();
	}
}
```

### 3. Verify Installation

Run the included verification queries in `DatabaseSchema.sql`:

```sql
-- View all tables
SELECT name FROM sqlite_master WHERE type='table';

-- Check foreign keys
PRAGMA foreign_key_list(Loans);
```

---

## Viewing the Database

### Option 1: DB Browser for SQLite (Recommended)

1. **Download**: https://sqlitebrowser.org/
2. **Install**: Run the installer (Windows/Mac/Linux)
3. **Open Database**: File → Open Database → Select `library.db`
4. **Browse Data**: Click "Browse Data" tab to view tables
5. **Execute SQL**: Use "Execute SQL" tab to run queries

### Option 2: Visual Studio Extension

1. Open Visual Studio
2. Go to **Extensions → Manage Extensions**
3. Search for "SQLite/SQL Server Compact Toolbox"
4. Install and restart Visual Studio
5. Access via **View → Other Windows → SQLite Toolbox**

### Option 3: Command Line

1. Download SQLite tools: https://www.sqlite.org/download.html
2. Open terminal in your project directory
3. Run: `sqlite3 library.db`
4. Execute commands:
   - `.tables` - View all tables
   - `.schema Books` - View table structure
   - `SELECT * FROM Books;` - Query data

### Option 4: Visual Studio Code Extension

1. Install "SQLite Viewer" extension
2. Right-click `library.db` in Explorer
3. Select "Open Database"

---

## Sample Data Overview

The `SampleData.sql` script provides:

- **5 Member Accounts**: alice.member, bob.member, carol.member, david.member, emma.member
- **2 Staff Accounts**: jane.staff, mike.staff
- **30 Books**: Covering 10+ genres (Fiction, Science Fiction, Mystery, Fantasy, Biography, etc.)
- **Active Loans**: 5 active loans (2 are overdue)
- **Loan History**: Multiple returned loans showing member history
- **Active Reservations**: 2 active reservations demonstrating queue management

### Test Account Credentials

**Member Accounts** (Password: "member123"):
- Username: `alice.member`
- Username: `bob.member`
- Username: `carol.member`
- Username: `david.member`
- Username: `emma.member`

**Staff Accounts** (Password: "staff456"):
- Username: `jane.staff`
- Username: `mike.staff`

*Note: Passwords are hashed in the database. The plain-text passwords above are for testing only.*

---

## Maintenance & Updates

### Reset Database (WARNING: Deletes all data)

```csharp
var initializer = new DatabaseInitializer("library.db");
initializer.ResetDatabase();
```

### Backup Database

Simply copy the `library.db` file to a backup location:

```bash
cp library.db library_backup_2024-01-15.db
```

### Modify Schema

To add new fields or tables:

1. Update `DatabaseSchema.sql`
2. Run `DatabaseInitializer.ResetDatabase()` (for development only)
3. For production, write migration scripts to preserve data

---

## Testing & Quality Assurance

### Data Integrity Checks

Run these queries to verify database consistency:

```sql
-- Check for books on loan with no active loan record
SELECT B.BookID, B.Title, B.Status
FROM Books B
LEFT JOIN Loans L ON B.BookID = L.BookID AND L.ReturnDate IS NULL
WHERE B.Status = 'On Loan' AND L.LoanID IS NULL;

-- Check for duplicate active reservations (should return 0 rows)
SELECT BookID, COUNT(*) as ReservationCount
FROM Reservations
WHERE FulfilledDate IS NULL
GROUP BY BookID
HAVING COUNT(*) > 1;

-- Verify loan limits (members with >2 active loans)
SELECT A.AccountID, A.FirstName, A.LastName, COUNT(L.LoanID) as ActiveLoans
FROM Accounts A
JOIN Loans L ON A.AccountID = L.MemberID
WHERE L.ReturnDate IS NULL
GROUP BY A.AccountID
HAVING COUNT(L.LoanID) > 2;
```

### Performance Testing

For larger datasets, add these additional indexes:

```sql
CREATE INDEX idx_loans_dates ON Loans(LoanDate, DueDate);
CREATE INDEX idx_books_genre ON Books(Genre);
```

---

## Requirements Traceability

| Requirement | Database Support |
|------------|------------------|
| REQ-1: Catalogue Management | `Books` table with indexed search fields |
| REQ-2: Borrow and Return | `Loans` table with status tracking |
| REQ-3: Reservations | `Reservations` table with UNIQUE constraint |
| REQ-4: Overdue | Calculated field in `Loans`, indexed for performance |
| REQ-5: Member Portal | `Accounts` table with role='Member' |
| REQ-6: Staff Portal | `Accounts` table with role='Staff' |
| REQ-7: Access Control | `Accounts.Role` field with CHECK constraint |
| REQ-8: Loan Limits | Query-based validation via `GetActiveLoanCount()` |
| REQ-9: Input Validation | Parameterized queries in `DatabaseHelper` |
| REQ-11: Security | Password hashing, role-based access |
| REQ-14: Data Integrity | Foreign keys, CHECK constraints, UNIQUE indexes |

---

## Contact & Support

For questions about the database schema or implementation:

- **Project Repository**: https://github.com/Sumo77/SoftwareQuality-Assignment1
- **Branch**: Database-SQLite
- **Document Version**: 1.0
- **Last Updated**: 2024

---

## Appendix: SQL Scripts

All SQL scripts are located in the `Database/` directory:

1. **DatabaseSchema.sql** - Table definitions, constraints, and indexes
2. **SampleData.sql** - Test data for development and QA
3. **DatabaseInitializer.cs** - C# class for schema initialization
4. **SampleDataLoader.cs** - C# class for data population
5. **DatabaseHelper.cs** - C# data access layer with common queries

Execute scripts in order: Schema → Sample Data → Application Code.

---

*End of Database Documentation*
