# Database Setup Guide

## Quick Start (5 Minutes)

Follow these steps to get the database running in your project:

---

## Step 1: Install DB Browser for SQLite (Recommended)

This tool lets you view and manage your database visually.

1. **Download**: Go to https://sqlitebrowser.org/
2. **Install**: Run the installer for your operating system
3. **Launch**: Open DB Browser for SQLite

You'll use this later to view your tables and data.

---

## Step 2: Restore NuGet Packages

The SQLite package is already referenced in the `.csproj` file. Just restore it:

### Option A: In Visual Studio
1. Right-click the solution in Solution Explorer
2. Click **Restore NuGet Packages**
3. Wait for the restore to complete

### Option B: Command Line
```bash
cd D:\SoftwareQuality-Assignment1\SoftwareQuality-Assignment1
dotnet restore
```

This will download `Microsoft.Data.Sqlite` version 9.0.0.

---

## Step 3: Build the Project

Make sure the SQL scripts are copied to the output directory:

### In Visual Studio
1. Press **Ctrl+Shift+B** or go to **Build → Build Solution**
2. Check for errors in the Output window

### Command Line
```bash
dotnet build
```

---

## Step 4: Create and Initialize the Database

Add this code to your `Program.cs` or create a new test file:

```csharp
using SoftwareQuality_Assignment1.Database;

// Define database path (will be created in Debug/Release folder)
string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library.db");

Console.WriteLine($"Creating database at: {dbPath}");

// Step 1: Initialize the database schema
var initializer = new DatabaseInitializer(dbPath);

if (initializer.InitializeDatabase())
{
	Console.WriteLine("✅ Database schema created successfully!");

	// Step 2: Load sample data
	var dataLoader = new SampleDataLoader(initializer.ConnectionString);

	if (dataLoader.LoadSampleData())
	{
		Console.WriteLine("✅ Sample data loaded successfully!");

		// Step 3: Display summary
		dataLoader.DisplayDataSummary();

		// Step 4: Verify data integrity
		if (dataLoader.VerifySampleData())
		{
			Console.WriteLine("✅ All data validation checks passed!");
		}
	}
}
else
{
	Console.WriteLine("❌ Database initialization failed. Check the error messages above.");
}
```

---

## Step 5: Run the Application

### In Visual Studio
1. Press **F5** or click **Start Debugging**
2. Check the console output for success messages

### Command Line
```bash
cd D:\SoftwareQuality-Assignment1\SoftwareQuality-Assignment1
dotnet run
```

**Expected Output:**
```
Creating database at: D:\...\bin\Debug\net10.0\library.db
Database initialized successfully at: D:\...\library.db
Sample data loaded successfully.

=== DATABASE SUMMARY ===

Accounts:
  Member: 5
  Staff: 2

Books by Status:
  Available: 22
  On Loan: 7
  Reserved: 1

Loans:
  Total: 13
  Active: 7
  Returned: 6
  Overdue: 2

Reservations:
  Total: 3
  Active: 2
  Fulfilled: 1

========================

✅ All data validation checks passed!
```

---

## Step 6: View the Database in DB Browser

1. Open **DB Browser for SQLite**
2. Click **File → Open Database**
3. Navigate to your build folder:
   ```
   D:\SoftwareQuality-Assignment1\SoftwareQuality-Assignment1\bin\Debug\net10.0\library.db
   ```
4. Click **Open**

### Explore Your Database:

**Browse Data Tab:**
- Click on any table name (Accounts, Books, Loans, Reservations)
- View all records in the table

**Execute SQL Tab:**
- Run queries from the documentation
- Example: `SELECT * FROM Books WHERE Status = 'Available';`

**Database Structure Tab:**
- View table schemas
- See indexes and constraints

---

## Step 7: Test Database Operations

Create a simple test file to verify database operations:

```csharp
using SoftwareQuality_Assignment1.Database;

string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library.db");
string connectionString = $"Data Source={dbPath}";

using (var dbHelper = new DatabaseHelper(connectionString))
{
	Console.WriteLine("\n=== TESTING DATABASE OPERATIONS ===\n");

	// Test 1: Search catalogue
	Console.WriteLine("1. Searching for 'Harry Potter'...");
	var books = dbHelper.SearchCatalogue("Harry Potter");
	foreach (var book in books)
	{
		Console.WriteLine($"   Found: {book["Title"]} by {book["Author"]} - Status: {book["Status"]}");
	}

	// Test 2: Member login
	Console.WriteLine("\n2. Testing member login...");
	int? memberId = dbHelper.ValidateLogin("alice.member", 
		"5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8");

	if (memberId.HasValue)
	{
		Console.WriteLine($"   ✅ Login successful! Member ID: {memberId}");

		// Test 3: Get member's active loans
		Console.WriteLine("\n3. Getting active loans for alice.member...");
		var loans = dbHelper.GetActiveLoans(memberId.Value);
		foreach (var loan in loans)
		{
			Console.WriteLine($"   - {loan["Title"]} due on {loan["DueDate"]}");
		}
	}

	// Test 4: Staff statistics
	Console.WriteLine("\n4. Getting staff statistics...");
	var stats = dbHelper.GetStaffStatistics();
	Console.WriteLine($"   Total books on loan: {stats["TotalOnLoan"]}");
	Console.WriteLine($"   Total overdue: {stats["TotalOverdue"]}");
	Console.WriteLine($"   Active reservations: {stats["TotalReservations"]}");

	// Test 5: Check for duplicate reservation (should fail)
	Console.WriteLine("\n5. Testing duplicate reservation prevention...");
	var reservation1 = dbHelper.CreateReservation(7, 1, DateTime.Now); // First reservation
	var reservation2 = dbHelper.CreateReservation(7, 2, DateTime.Now); // Duplicate (should fail)

	if (reservation1.HasValue && !reservation2.HasValue)
	{
		Console.WriteLine("   ✅ Duplicate reservation correctly prevented!");
	}
}

Console.WriteLine("\n=== ALL TESTS COMPLETE ===\n");
```

---

## Test Account Credentials

Use these credentials to test login functionality:

### Member Accounts
**Username:** `alice.member`  
**Password (plain):** `member123`  
**Password (hash):** `5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8`

**Other member usernames:** `bob.member`, `carol.member`, `david.member`, `emma.member` (same password)

### Staff Accounts
**Username:** `jane.staff`  
**Password (plain):** `staff456`  
**Password (hash):** `0b14d501a594442a01c6859541bcb3e8164d183d32937b851835442f69d5c94e`

**Other staff username:** `mike.staff` (same password)

---

## Troubleshooting

### Issue: "File not found: DatabaseSchema.sql"

**Solution:** The SQL files need to be copied to the output directory. This is already configured in the `.csproj`:

```xml
<ItemGroup>
  <None Update="Database\DatabaseSchema.sql">
	<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Update="Database\SampleData.sql">
	<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

Rebuild the project to ensure files are copied.

### Issue: "Package Microsoft.Data.Sqlite not found"

**Solution:**
```bash
dotnet add package Microsoft.Data.Sqlite
```

Or manually edit `.csproj` and add:
```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.0" />
```

### Issue: "Foreign key constraint failed"

**Solution:** This means you're trying to create a loan/reservation with an invalid BookID or MemberID. The database prevents orphaned records. Check that IDs exist before using them.

### Issue: "UNIQUE constraint failed on Reservations"

**Solution:** This is expected! It means you tried to create a second active reservation for a book that already has one. This is the database enforcing REQ-3 (single reservation per book).

### Issue: Database file is locked

**Solution:** Close any open connections in your code:
```csharp
using (var dbHelper = new DatabaseHelper(connectionString))
{
	// Operations here
} // Connection automatically closed
```

Or close DB Browser if it has the database open.

---

## Resetting the Database

If you need to start fresh (WARNING: Deletes all data):

```csharp
var initializer = new DatabaseInitializer("library.db");
initializer.ResetDatabase();
```

Or manually delete the `library.db` file and run initialization again.

---

## File Locations

After building, your database files will be at:

```
D:\SoftwareQuality-Assignment1\
├── Database\                                    (Source SQL scripts)
│   ├── DatabaseSchema.sql
│   ├── SampleData.sql
│   └── README_Database.md
│
└── SoftwareQuality-Assignment1\
	├── Database\                                (C# classes)
	│   ├── DatabaseInitializer.cs
	│   ├── DatabaseHelper.cs
	│   └── SampleDataLoader.cs
	│
	└── bin\Debug\net10.0\                      (Build output)
		├── library.db                           ← Your database file
		├── Database\
		│   ├── DatabaseSchema.sql               (Copied)
		│   └── SampleData.sql                   (Copied)
		└── SoftwareQuality-Assignment1.exe
```

---

## Next Steps

1. ✅ **Complete**: Database schema created and validated
2. ✅ **Complete**: Sample data loaded
3. 🔜 **Next**: Create GUI (Login, Member View, Staff View)
4. 🔜 **Next**: Implement business logic layer
5. 🔜 **Next**: Write MSTest unit tests
6. 🔜 **Next**: Integrate with interfaces

---

## Documentation Reference

- **Full Database Documentation**: `Database/README_Database.md`
- **Requirements Validation**: `Database/REQUIREMENTS_VALIDATION.md`
- **Schema SQL**: `Database/DatabaseSchema.sql`
- **Sample Data SQL**: `Database/SampleData.sql`

---

## Support

If you encounter issues:

1. Check the console output for error messages
2. Verify SQL files are in the `bin/Debug/net10.0/Database/` folder
3. Use DB Browser to inspect the database directly
4. Review the documentation in `README_Database.md`
5. Check the GitHub repository for updates

---

**Setup Status:** ✅ Ready for development  
**Database Version:** 1.0  
**Last Updated:** 2024
