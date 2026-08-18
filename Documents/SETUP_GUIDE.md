# Setup Guide

How to setup the application for use, run tests, and inspect the database.

---

## Prerequisites

- **.NET 8 SDK** - `dotnet --version` should report 8.x or higher
- **Windows** - the app uses Windows Presentation Foundation (WPF) and will not run on macOS or Linux
- **Visual Studio 2022** (or any other compatible IDE)

---

## Setup (Terminal Instructions)

**1. Clone and restore**

```bash
git clone https://github.com/Sumo77/SoftwareQuality-Assignment1.git
cd SoftwareQuality-Assignment1
dotnet restore
```

**2. Build**

```bash
dotnet build
```

This also copies `DatabaseSchema.sql` and `SampleData.sql` into the output
folders, which the app needs at runtime.

**3. Run**

```bash
dotnet run --project LibraryQA
```

Or open `SoftwareQuality-Assignment1.slnx` in Visual Studio, set `LibraryQA` as
the startup project, and press F5.

On first launch the app creates `library.db` in its output folder and loads the
sample data. Later launches reuse it.

**4. Log in**

Sign in as a member account for the member view (i.e `alice.member` / `member123`), 
or staff account for the staff view (i.e `jane.staff` / `staff456`).
Full list in the root README.

---

## Setup (Visual Studio 2022)

Requires Visual Studio 2022 version 17.8 or later, with the **.NET desktop
development** workload installed.

**1. Clone the repository**

In Visual Studio: **Git → Clone Repository**, paste
`https://github.com/Sumo77/SoftwareQuality-Assignment1.git`, pick a local folder
and click **Clone**.

Already have it locally? **File → Open → Project/Solution** and select
`SoftwareQuality-Assignment1.slnx`.

**2. Restore packages**

Visual Studio restores automatically on open. If it doesn't, right-click the
solution in Solution Explorer and choose **Restore NuGet Packages**.

**3. Build**

**Build → Build Solution**, or Ctrl+Shift+B. Check the Output window for errors.

This also copies `DatabaseSchema.sql` and `SampleData.sql` into the output
folders, which the app needs at runtime.

**4. Set the startup project**

Right-click `LibraryQA` in Solution Explorer → **Set as Startup Project**. It
should turn bold. Getting this wrong is the most common reason F5 does nothing
useful - `LibraryQA.Core` and `LibraryQA.Tests` are libraries and can't run.

**5. Run**

Press **F5** (with debugging) or Ctrl+F5 (without).

On first launch the app creates `library.db` in its output folder and loads the
sample data. Later launches reuse it.

**6. Log in**

Sign in as a member account for the member view (i.e `alice.member` / `member123`), 
or staff account for the staff view (i.e `jane.staff` / `staff456`).
Full list in the root README.

---

## Running the tests

In Terminal:

```bash
dotnet test
```

In Visual Studio: **Test → Test Explorer → Run All**.

Each test builds its own temporary database and deletes it afterwards, so the
tests do not touch `library.db` and can be run in any order.

---

## Inspecting the database

Install [DB Browser for SQLite](https://sqlitebrowser.org/), then open:

```
LibraryQA/bin/Debug/net8.0-windows/library.db
```

Useful for checking that a borrow or return actually updated `Books.Status`.

Close DB Browser before running the app again - it holds a lock on the file.

---

## Resetting the database

Delete `library.db` from the output folder and run the app again. It will be
recreated and reseeded from the SQL scripts.

---

## Troubleshooting

**"The current .NET SDK does not support targeting .NET x"**
You are on an older SDK. Install .NET 8 or later from
https://dotnet.microsoft.com/download.

**`FileNotFoundException: DatabaseSchema.sql`**
The SQL scripts did not get copied to the output folder. Run `dotnet build`
again. If it persists, check that the project's `.csproj` still has the
`Content Include="..\Database\*.sql"` block.

**"Database is locked"**
Something else has the file open - usually DB Browser, or a previous run of the
app that did not close properly. Close it and try again.

**"UNIQUE constraint failed: Reservations.BookID"**
Working as intended. The database is rejecting a second active reservation on a
book that already has one (REQ-3).

**"Foreign key constraint failed"**
A loan or reservation referenced a `BookID` or `MemberID` that does not exist.
Check the IDs against the database.

**Login fails with correct-looking credentials**
Use the plain-text passwords from the README, not a hash. The app hashes the
password before comparing it.