# Defect Report

Defects found during development and testing.

**Priority Levels:**
- P1 = Fix Now
- P2 = Assign at the next meeting
- P3 = Fix before the code freeze

**Defect lifecycle:**
- Open -> In Progress -> Resolved -> Retested -> Closed

**Severity:** 
- Critical (interupts core functionality)
- High (wrong behaviour needing correction but a workaround exists)
- Medium (minor incorrect behaviour)
- Low (cosmetic or documentation)

**Status:** 
- Open
- In Progress
- Resolved
- Won't Fix

---

## DEF-01

| | |
|---|---|
| **Title** | Database stored password hashes do not match the actual documented passwords |
| **Severity** | High |
| **Status** | Resolved |
| **Found by** | Summer |
| **Found in** | Original `README.md` + `Database: Accounts Table` |
| **Related** | REQ-5 + REQ-6 + REQ-7: Member + Staff Views + Access Control, REQ-10 + REQ-11: Usability + Security |

**Description:**

The documentation listed `5e884898da28…` as the hash for `member123`. That value
is actually SHA256 of the literal string `"password"` and appears nowhere in the
database. Probably an AI generation error.

**Root cause:**

Documentation and input was not verified, allowing a mismatch between 
the documented hash and the actual hash in the database.

**Fix:**

Hashes recomputed from the seed file and corrected. Overlapping documents and inputs
consolidated so there is one place to keep accurate rather than three.

---

## DEF-02

| | |
|---|---|
| **Title** | Book status not restored when a loan is returned |
| **Severity** | High |
| **Status** | Resolved |
| **Found by** | Daria |
| **Found in** | `StaffView.ReturnButton_Click` |
| **Related** | REQ-2b (Return), REQ-14 (Integrity) |

**Description:**

Processing a return closed the loan record but left the book's status unchanged.
An item returned to the library still showed as On Loan in the catalogue, so it
could not be borrowed by anyone else. The return reported success and the
application raised no error, so the fault was only visible by checking the
catalogue against the loan record.

**Root cause:**

`ProcessReturn` updates the `Loans` table only. The corresponding status
transition on `Books` was never called.

**Fix:**

After a successful return, the book status is set to Reserved if an active
reservation exists, and Available otherwise. Also as a quick note, the book 
status at the moment is also being updated twice on borrow + return, meaning 
the same rule lives in two areas.

---

## DEF-03

| | |
|---|---|
| **Title** | Reservation is not marked fulfilled when the reserved item is returned |
| **Severity** | Critical |
| **Status** | Open |
| **Found by** | John |
| **Found in** | `StaffView.ReturnButton_Click` / `Reservations` table |
| **Related** | REQ-3 (Reservations), REQ-14 (Integrity) |

**Description:**

When staff process a return for an item with an active reservation, the book's
status correctly changes to Reserved, but the reservation row retains a NULL
`FulfilledDate`. The reservation therefore stays active indefinitely: it
continues to appear in the staff Reservations tab, is counted in
`GetStaffStatistics()`, and the unique index on active reservations prevents any
future reservation of that title.

**Root cause:**

Return processing was implemented against the loan and book status requirements.
Closing the reservation is a separate step that was not part of the original
return flow.

**Fix:**

Delayed, to do next. Marking a reservation fulfilled requires a collection workflow — the
reserving member must claim the item, and there must be a rule for expiry if they
do not. Both are outside the current prototype scope. The status transition
itself is correct and tested; only reservation closure is outstanding.


---

## DEF-04

| | |
|---|---|
| **Title** | Error presentation is inconsistent across the three interfaces |
| **Severity** | Low |
| **Status** | Open |
| **Found by** | Summer |
| **Found in** | `LoginView`, `MemberView`, `StaffView` |
| **Related** | REQ-10 (Usability), REQ-12 (Reliability) |

**Description:**

The three interfaces report errors in different ways. Login uses inline text
beneath the form, Member view uses blocking `MessageBox` dialogs, and Staff view
uses an inline status line. A user moving between views encounters three
different conventions for the same kind of feedback, which can be confusing.

**Root cause:**

The interfaces were built in parallel on separate branches without an agreed
convention for user feedback. Each developer chose a resonable choice
independently. The result is inconsistent, but not incorrect.

**Fix:**

Delayed, to do next. The next phase will agree a single convention and
document it, to keep things consistent across the application.

---

## DEF-05

| | |
|---|---|
| **Title** | Partially entered sample data |
| **Severity** | Medium |
| **Status** | Open |
| **Found by** | Summer |
| **Found in** | `Database` |
| **Related** | REQ-14 (Integrity) |

**Description:**

During the generation of the database, there must have been
partial generation of some inputs. Someone labeled a book as "On Loan" but did not 
create a corresponding loan record, so the book is effectively lost to the catalogue. 
Similarly, a reservation was created and is partially stored the same way.

**Root cause:**

Likely an AI generation error on initial development that was overlooked.

**Fix:**

Delayed, to do next. This is a simple fix, but the whole database should go under review
to ensure that no other sample data has been effected in the same.

---

## DEF-06

| | |
|---|---|
| **Title** | Reserving a book changes it to "Reserved" while still on loan – it should stay "On Loan". |
| **Severity** |High |
| **Status** | Open |
| **Found by** | Daria |
| **Found in** | `MemberActionsService.ReserveBook()` |
| **Related** | REQ-2 (Borrow) + REQ-3 (Reservations), REQ-14 (Integrity) |

**Description:**

MemberActionsService.ReserveBook() changes the book status to reserved even though the book is still on loan.
It does not check if it is on loan or anything. Reserved should mean that it is on loan and is also being held
for some other member after the loan expires for the current user.

**Root cause:**

MemberActionsService.ReserveBook() only checks two conditions before executing the reserve. It never never reasons why the book is unavailable.
It treats both On Loan and reserved the same.

**Fix:**

Don't blindly set status to reserved. Only change the status if the current status is not alreadyt on loan or reserved.

---

## DEF-07

| | |
|---|---|
| **Title** | Staff Issue Loan skips the 2-book limit and doesn't check the member ID. |
| **Severity** | High |	
| **Status** | Open |
| **Found by** | John |
| **Found in** | `StaffView.IssueButton_Click` / `DatabaseHelper.CreateLoan` |
| **Related** | REQ-1 (Issue Loan), REQ-14 (Integrity) |

**Description:**

There is no limit to how many loans a staff can give to members. There is also no check to see if
the member ID that the staff has inputted actually exists. They can indefinitely issue loans to any member ID without
any checks.

**Root cause:**

StaffView.IssueLoanButton_Click() avoids/bypasses MemberActionsService.BorrowBook() completely. This is the method that implements the MaxActiveLoans.

**Fix:**

Have The IssueLoanButton method call the MemberActionsService.BorrowBook() method so its already using the pre-existing logic for the limit checks.

---

## DEF-08

| | |
|---|---|
| **Title** | Book status is updated twice on borrow + return (the same rule lives in two places). |
| **Severity** | Low |
| **Status** | Open |
| **Found by** | Summer |
| **Found in** | `MemberActionsService.BorrowBook` + `StaffView.ReturnButton_Click` / `DatabaseHelper.CreateLoan` + `DatabaseHelper.ProcessReturn` |
| **Related** | REQ-14 (Integrity) |

**Description:**

The DatabaseHelper.ProcessReturn function already updates the reservation status and updates the book status.
This just adding unnecessary steps which could lead to bugs later on.

**Root cause:**

This was coded by AI that was not aware of the other function that was already doing the same thing.

**Fix:**

Remove the redundant status-update calls since CreateLoan and processReturn already handle the status updates.

---

## DEF-09

| | |
|---|---|
| **Title** | Overdue checks use UTC, so loans can be flagged a day late; days overdue differs between member + staff views. |
| **Severity** | High |
| **Status** | Open |
| **Found by** | Summer |
| **Found in** | `DatabaseHelper.GetActiveLoans` / `DatabaseHelper.GetAllOverdueLoans` (SQL `julianday('now')`) + `MemberView` / `StaffView` (C# local-time calculations) |
| **Related** | REQ-2b (Return), REQ-12 (Reliability) |

**Description:**

SQL and the C# code use different ways to calculate the current date. SQL uses UTC but the C# code uses local time.
This could show conflicting information to the user, even though it has one more day left, it may already say it is
overdue which can be frutrating to the user.

**Root cause:**

As these two parts were developed by two different people, they were not aware of the other part and did not check for consistency.

**Fix:**

Decide on one time reference across the whole app so there is no confusion between the two.

---

## DEF-10

| | |
|---|---|
| **Title** | Return condition is never recorded – every return is saved as "Good". |
| **Severity** | Medium |
| **Status** | Open |
| **Found by** | John |
| **Found in** | `StaffView.ReturnButton_Click` / `DatabaseHelper.ProcessReturn` |
| **Related** | REQ-2b (Return), REQ-14 (Integrity) |

**Description:**

Return just goes off its default value of Good and does not check the condition of the book.
This can lead to issues where a book is returned in bad condition without the staff being able to record it.

**Root cause:**

This was overlooked at the time of development and was not included in the requirements. 

**Fix:**

Add a UI control on the staff return screen for selecting the condition of the book.

---

## DEF-11

| | |
|---|---|
| **Title** | Member + Staff views don't catch database errors, so the app could crash. |
| **Severity** | Medium	|
| **Status** | Open |
| **Found by** | Summer |
| **Found in** | `MemberView` (`BorrowButton_Click`, `ReserveButton_Click`, `Load*` methods) + `StaffView` (`IssueButton_Click`, `ReturnButton_Click`, `LoadOverdueItems`) |
| **Related** | REQ-12 (Reliability) |

**Description:**

Neither the member or the staff view catch database errors. If the database is not available,
or doesn't load, the app could crash.

**Root cause:**

An error handle was not implemented at the time of the development.

**Fix:**

Wrap the database call sites in both views with try/catch blocks for the SQliteException and display a message to the user that the database is not available/not loading.

---

## DEF-12

| | |
|---|---|
| **Title** |  Validation messages don't say which field is wrong. |
| **Severity** | Low |
| **Status** | Open |
| **Found by** | Summer |
| **Found in** | `MemberActionsService.ReserveBook` (Member view Reserve form) + `StaffView.IssueButton_Click` (Staff Issue Loan form) |
| **Related** | REQ-10 (Usability) |

**Description:**

The ReserveBook form and the IssueLoan form both do not say which of the fields were inputted incorrectly.
It just says "Invalid input" and does not explain which field was wrong, leaving the user having to guess which field is the right or wrong one.

**Root cause:**

As this is not a functional issue, the developer at the time did not realise that this could be an issue as they would be aware of what they were inputting.

**Fix:**

Replace the Invalid input message with a more descriptive message that explains which field was wrongly inputted.

---

## DEF-13

| | |
|---|---|
| **Title** | A member can reserve a book they already have on loan. |
| **Severity** | Low |
| **Status** | Open |
| **Found by** | Daria |
| **Found in** | `MemberActionsService.ReserveBook()` |
| **Related** | REQ-2 (Borrow) + REQ-3 (Reservations), REQ-14 (Integrity) |

**Description:**

A member can reserve the same book that they already have on loan. This can lead to the member exploiting the system
by constantly loaning and reserving the same book without allowing other members to borrow it.

**Root cause:**

This was another issue where the functionality itself was not broken, however with this current logic it could cause some exploitation.
This was not part of the quality assurance testing at the tiem of development.

**Fix:**

Add a check for MemberActionsService.ReserveBook() that if the member already has the book on loan, they can't reserve it.

---

## DEF-14

| | |
|---|---|
| **Title** | Searching for "%" or "_" matches every book. |
| **Severity** | Low |
| **Status** |  |
| **Found by** | Summer |
| **Found in** | `DatabaseHelper.SearchCatalogue()` |
| **Related** | REQ-4 (Search/Catalogue) |

**Description:**

Searching for "%" or "_" matches every book. This could cause issues where a book with a title that could contain those
characters could be searched for and return every book.

**Root cause:**

searchTerm is concatenated into the SQL LIKE pattern without escaping the LIKE wildcard characters. This is a LIKE-pattern escaping issue and needs these values to escape
using the ESCAPE clause.

**Fix:**

Escape % and _ in searchTerm before wrapping it. and add an ESCAPE clause to each LIKE condition.

---

## DEF-15

| | |
|---|---|
| **Title** | If sample data fails to load once, the app never retries – the catalogue stays empty. |
| **Severity** | Low |
| **Status** | Open |
| **Found by** | Summer |
| **Found in** | `App.xaml.cs` / `DatabaseInitializer.DatabaseExists()` |
| **Related** | REQ-12 (Reliability), REQ-14 (Integrity) |

**Description:**

If the data fails to load once, the app does not try again. This is because if the DatabaseExists() function only checks if the tables exist.
This is an issue because if the InitializeDatabase() function succeeds without the SeedSampleData() function succeeding, the app will not try to load
the data again.

**Root cause:**

DatabaseExists() verifies that the four expected tables exist but doesn't know whether the tables actually contain any rows.
This is a silent failure that is not reported.

**Fix:**

Change DatabaseExists() to check the books table actually has rows and not just that the table exists.

---

## DEF-16

| | |
|---|---|
| **Title** | TC-5 doesn't test the app (it would pass even if the app broke); TC-4's name is wrong. |
| **Severity** | Medium |
| **Status** | Open |
| **Found by** | Summer |
| **Found in** | `LibraryQA.Tests/StaffViewTests.cs` (TC-4, TC-5) |
| **Related** | REQ-6 (Reservation count), REQ-9 (Guard clause) |

**Description:**

TC-5 has a guard in the test but it does not use the app's own guard clause. This leads to the test passing even if CreateLoan had no invalid-ID protection.
TC-4's name is wrong because it is saying that the Staff view shows correct active reservation, when the funciton itself does not touch the Staff UI.

**Root cause:**

It does not call db.CreateLoan(). Since this never gets invoked the test would still pass even if the guard clause was removed. 
It is testing the test's logic instead of the app.

**Fix:**

Rewrite TC-5 to call db.CreateLoan() directly so the assertion does do the production guard clause. For TC-4, rename the test that it is a testing the 
GetTotalActiveREeservationsCount() and add a comment/test area to make clear tit does not test the Staff UI.

---

## DEF-17

| | |
|---|---|
| **Title** | TC-6 and TC-7 check less than the RTM says they do. |
| **Severity** | Medium |
| **Status** | Open |
| **Found by** | Summer |
| **Found in** | `LibraryQA.Tests/StaffViewTests.cs` (TC-6, TC-7) |
| **Related** | REQ-2 (Return) + REQ-14 (Integrity), REQ-7 + REQ-11 (Access Control) |

**Description:**

TC-6 only asserts ReturnDate is set, and never assert Books.Status as changed.
TC-7 does not check if it actually denied the Staff privileged features from the user.

**Root cause:**

TC-7's root cause starts from the REQ-7 and REQ-11 having tests that run independently. They authenticate the right role retursn but does not check
if the StaffView ui is actually denied do non staff users. For TC-6, the assertion copmutes expected status using the same production method
that ProcessReturn relies on. So if the shared logic is broken the test value would be wrong alongside with the actual value.

**Fix:**

TC-6, Replace the db.HasActiveReservation() expectation with a hard coded expect status that has a known seed value. THis will stop it reusing the same logic.
TC-7 should add a test that actually instantiates StaffView for a membver resolved context and check if the StaffView is actually denied.

---

## DEF-18

| | |
|---|---|
| **Title** | Tests fail with a confusing error if the database can't be created (e.g. on CI).  |
| **Severity** | Medium |
| **Status** | Open |
| **Found by** | Summer |
| **Found in** | `LibraryQA.Tests/StaffViewTests.cs`, `LoginViewTests.cs`, `MemberViewTests.cs` (`Setup()`) |
| **Related** | REQ-12 (Reliability) |

**Description:**

Test classes call DatabaseInitializer without checking the boolean return value and just calls the SeedSampleData() function.
If the DatabaseInitializer fails, this will cause the SQLite error message instead of a clear message that the database could not be loaded or created.

**Root cause:**

Error handling was skipped for this case. There were no Setup methods that were checking the return value of DatabaseInitializer or the DatabaseSeeder().

**Fix:**

Wrapping the Assert messages in Setup() methods to check the return value of DatabaseInitializer() and DatabaseSeeder() and throw a clear message if it fails.

## Summary

| ID | Title | Severity | Status |
|---|---|---|---|
| DEF-01 | Database stored password hashes do not match the actual documented passwords | High | Fixed |
| DEF-02 | Book status not restored when a loan is returned | High | Resolved |
| DEF-03 | Reservation not marked fulfilled when reserved item is returned | Medium | Open |
| DEF-04 | Error presentation is inconsistent across the three interfaces | Low | Open |
| DEF-05 | Partially entered sample data | Medium | Open |