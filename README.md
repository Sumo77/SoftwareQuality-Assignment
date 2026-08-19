# Library Book Loaning, Returns and Catalogue Management System

A prototype library management system built for the Software Quality Assurance Course (ENSE707) Assignment 1.
Members can search the catalogue, borrow items and place reservations.
Staff can issue loans, process returns and view reporting data.

**Group:** Daria Bondareva (23221970), John O'Connor (23223840), Summer Harris (23211693)

---

## Project Structure

| Project | Purpose |
|---|---|
| `LibraryQA` | Desktop app - Login, Member and Staff views |
| `LibraryQA.Core` | Business logic and data access. No UI dependency, so it can be unit tested |
| `LibraryQA.Tests` | MSTest unit tests, references Core only |
| `Database/` | `DatabaseSchema.sql` and `SampleData.sql` |
| `Documents/` | Setup guide, defect report, traceability matrix |

This structure keeps user interface logic (frontend) and business rules and database logic (backend) separate.

---

## Running the app

Clone the repository, open in a terminal or compatible interface (such as Visual Studio 2022 17.8 or later) and run the `LibraryQA` project.

Else (optionally), run with the following commands:

```bash
git clone https://github.com/Sumo77/SoftwareQuality-Assignment1.git
cd SoftwareQuality-Assignment1
dotnet run --project LibraryQA
```

Requires .NET 8 SDK and Windows.

The database is created and seeded automatically on first launch. It is written
to the app's output folder as `library.db` and reused on later runs. Delete that
file to start fresh.

See `Documents/SETUP_GUIDE.md` if you hit problems or require further details or explanation.

---

## Test Accounts

The placeholder accounts below are setup and ready to use on login to the app.

The accounts are not real, and were created for the purposes of prototype demonstration.
The intention is that they act as a stand-in for real accounts/a real account portal system, managed by the library's staff.

| Username | Password | Role |
|---|---|---|
| `alice.member` | `member123` | Member |
| `bob.member` | `member123` | Member |
| `carol.member` | `member123` | Member |
| `david.member` | `member123` | Member |
| `emma.member` | `member123` | Member |
| `jane.staff` | `staff456` | Staff |
| `mike.staff` | `staff456` | Staff |

Passwords are stored as SHA256 hashes, not plain text. Unsalted SHA256 is not suitable for production.
It was chosen to keep the prototype simple, and formal, intensive password security is listed as out of scope in the test plan.

---

## Running the tests

On the interface, right click the `LibraryQA.Tests` project and select "Run Tests", or run the following command in the terminal:

`dotnet test`

Tests live in `LibraryQA.Tests`. Each one builds a throwaway database in the temp
folder during setup and deletes it afterwards, so tests never depend on each
other's data or on the app's `library.db`.

---

## Database

Four Tables: `Accounts`, `Books`, `Loans`, `Reservations`

| Table | Description |
|---|---|
| `Accounts` | Members and staff in one table, with the `Role` column deciding which view a user gets and what they can do |
| `Books` | The catalogue, with a `Status` column that is the single source of truth for whether an item is available, on loan or reserved |
| `Loans` | Every borrowing transaction, past and present; a null `ReturnDate` means the loan is still active |
| `Reservations` | Holds placed on items that are currently on loan; a null `FulfilledDate` means the reservation is still waiting |

Two design decisions worth noting:

**`Books.Status` is the single source of truth for availability** - one of
`Available`, `On Loan` or `Reserved`, enforced by a CHECK constraint. Every
borrow, return and reservation must keep this in step with the underlying
records (REQ-14).

**One active reservation per book** is enforced by the database, not by code:

```sql
CREATE UNIQUE INDEX idx_reservations_active_book
    ON Reservations(BookID)
    WHERE FulfilledDate IS NULL;
```

A second reservation attempt fails with a UNIQUE constraint violation rather
than relying on the application remembering to check (REQ-3).

Active loans are rows where `ReturnDate IS NULL`. Overdue status is calculated
at query time rather than stored, so it is never stale.

---

## Documents

- `Documents/SETUP_GUIDE.md` - setup and troubleshooting
- `Documents/Traceability_Matrix.md` - requirements mapped to test cases
- `Documents/Defect_Report.md` - defects found and their status