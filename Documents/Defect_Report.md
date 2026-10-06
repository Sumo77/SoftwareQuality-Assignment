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
- Retested
- Closed

---

## DEF-01

| | |
|---|---|
| **Title** | Database stored password hashes do not match the actual documented passwords |
| **Severity** | High |
| **Priority** | P1 |
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
| **Priority** | P1 |
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
reservation exists, and Available otherwise. 

At the time this was fixed, the book status was also being updated twice on 
borrow and return, meaning the same rule lived in two places. 
That duplication was tracked separately as DEF-08 and has since been resolved.

---

## DEF-03

| | |
|---|---|
| **Title** | Reservation is not marked fulfilled when the reserved item is returned |
| **Severity** | Critical |
| **Priority** | P1 |
| **Status** | Resolved |
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

DatabaseHelper.FulfillReservation() was added. It sets the reservation's FulfilledDate and issues the loan to the collecting member in a single transaction, so the reservation closes and the book moves to On Loan together.

Note: Still outstanding: an automatic expiry if the member never collects (REQ-20). Without it a hold can sit indefinitely. Recorded as a scope limitation rather than reopening this defect. 

Covered by TC-13.

---

## DEF-04

| | |
|---|---|
| **Title** | Error presentation is inconsistent across the three interfaces |
| **Severity** | Low |
| **Priority** | P1 |
| **Status** | Resolved |
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

LibraryQA/Views/UserMessage.cs is the single place where user-facing messages are worded and formatted (REQ-13). Severity is carried by a symbol as well as a colour, so meaning does not depend on colour alone. Applied to all three views, now cohesive ! :)

Defect is view-layer (WPF) and can't be unit tested.

---

## DEF-05

| | |
|---|---|
| **Title** | Partially entered sample data |
| **Severity** | Medium |
| **Priority** | P2 |
| **Status** | Resolved |
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

Two mismatches were found and corrected in SampleData.sql. 'Gone Girl' (BookID 11) was marked 'Reserved' with no reservation row, and 'Becoming' (BookID 24) was marked 'On Loan' with no active loan row. Both are now 'Available', which matches their records.

Reviewing the whole file also showed the loan dates were hardcoded to 2024, so every active loan read as years overdue and the file's own comments ("one overdue", "one active") no longer described the data. All loan and reservation dates are now relative to the current local date, so the seed always means what it says.

TC-35 asserts that no book status disagrees with its loan and reservation records, so this cannot silently return.

---

## DEF-06

| | |
|---|---|
| **Title** | Reserving a book changes it to "Reserved" while still on loan – it should stay "On Loan". |
| **Severity** | High |
| **Priority** | P1 |
| **Status** | Resolved |
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

MemberActionsService.ReserveBook() now only promotes the catalogue status to Reserved when the book is not currently On Loan. A book on loan keeps that status until it is returned, and ProcessReturn makes the transition to Reserved once the loan closes. The reservation itself is still recorded against the item immediately, which is what REQ-3 asks for.
Covered by TC-14.

---

## DEF-07

| | |
|---|---|
| **Title** | Staff Issue Loan skips the 2-book limit and doesn't check the member ID. |
| **Severity** | High |
| **Priority** | P1 |
| **Status** | Resolved |
| **Found by** | John |
| **Found in** | `StaffView.IssueButton_Click` / `DatabaseHelper.CreateLoan` |
| **Related** | REQ-2a (Borrow), REQ-8 (Loan Limits), REQ-9 (Input Validation) |

**Description:**

There is no limit to how many loans a staff can give to members. There is also no check to see if
the member ID that the staff has inputted actually exists. They can indefinitely issue loans to any member ID without
any checks.

**Root cause:**

StaffView.IssueLoanButton_Click() avoids/bypasses MemberActionsService.BorrowBook() completely. This is the method that implements the MaxActiveLoans.

**Fix:**

StaffView.IssueButton_Click now calls MemberActionsService.BorrowBook(), so staff-issued loans run through the same limit and availability checks. The member and book are also selected from populated ComboBoxes rather than typed as raw IDs, so a non-existent ID cannot be entered. Covered by TC-10, which exercises the loan limit through the shared service path staff now use.

---

## DEF-08

| | |
|---|---|
| **Title** | Book status is updated twice on borrow + return (the same rule lives in two places). |
| **Severity** | Low |
| **Priority** | P2 |
| **Status** | Resolved |
| **Found by** | Summer |
| **Found in** | `MemberActionsService.BorrowBook` + `StaffView.ReturnButton_Click` / `DatabaseHelper.CreateLoan` + `DatabaseHelper.ProcessReturn` |
| **Related** | REQ-14 (Integrity) |

**Description:**

The DatabaseHelper.ProcessReturn function already updates the reservation status and updates the book status.
This just adding unnecessary steps which could lead to bugs later on.

**Root cause:**

This was coded by AI that was not aware of the other function that was already doing the same thing.

**Fix:**

The redundant status-update calls were removed from MemberActionsService.BorrowBook and StaffView.ReturnButton_Click. CreateLoan and ProcessReturn already perform the transition inside their own transactions, so the rule now lives in exactly one place.

Covered by TC-8.

---

## DEF-09

| | |
|---|---|
| **Title** | Overdue checks use UTC, so loans can be flagged a day late; days overdue differs between member + staff views. |
| **Severity** | High |
| **Priority** | P1 |
| **Status** | Resolved |
| **Found by** | Summer |
| **Found in** | `DatabaseHelper.GetActiveLoans` / `DatabaseHelper.GetAllOverdueLoans` (SQL `julianday('now')`) + `MemberView` / `StaffView` (C# local-time calculations) |
| **Related** | REQ-2b (Return), REQ-12 (Reliability), REQ-4 (Overdue) |

**Description:**

SQL and the C# code use different ways to calculate the current date. SQL uses UTC but the C# code uses local time.
This could show conflicting information to the user, even though it has one more day left, it may already say it is
overdue which can be frutrating to the user.

**Root cause:**

As these two parts were developed by two different people, they were not aware of the other part and did not check for consistency.

**Fix:**

Two separate faults were behind this. SQLite's date('now') returns the UTC date, which in New Zealand is up to a day behind local time, so a loan could stay unflagged for most of the day it fell due. Separately, the member view rounded a fractional day count while the staff view truncated it, so the two screens could differ by a day on the same loan.

The rule now lives in one place, LibraryQA.Core/Services/OverdueRules.cs, and both screens call it (REQ-13). GetActiveLoans, GetAllOverdueLoans and GetStaffStatistics take the current date from the caller rather than reading it inside SQL, defaulting to the local date.

Covered by TC-30 to TC-34, including the two boundary cases: a loan due today is not overdue, and a loan due yesterday is overdue by exactly one day.

---

## DEF-10

| | |
|---|---|
| **Title** | Return condition is never recorded – every return is saved as "Good". |
| **Severity** | Medium |
| **Priority** | P2 |
| **Status** | Resolved |
| **Found by** | John |
| **Found in** | `StaffView.ReturnButton_Click` / `DatabaseHelper.ProcessReturn` |
| **Related** | REQ-2b (Return), REQ-14 (Integrity) |

**Description:**

Return just goes off its default value of Good and does not check the condition of the book.
This can lead to issues where a book is returned in bad condition without the staff being able to record it.

**Root cause:**

This was overlooked at the time of development and was not included in the requirements. 

**Fix:**

A condition picker (Good, Fair, Damaged, Lost) was added to the staff return form, and the selected value is passed through to ProcessReturn, which already supported the parameter. A return cannot be processed without choosing a condition. The recorded condition appears in the member's Loan History. Covered by TC-16.

---

## DEF-11

| | |
|---|---|
| **Title** | Member + Staff views don't catch database errors, so the app could crash. |
| **Severity** | Medium	|
| **Priority** | P1 |
| **Status** | Resolved |
| **Found by** | Summer |
| **Found in** | `MemberView` (`BorrowButton_Click`, `ReserveButton_Click`, `Load*` methods) + `StaffView` (`IssueButton_Click`, `ReturnButton_Click`, `LoadOverdueItems`) |
| **Related** | REQ-12 (Reliability) |

**Description:**

Neither the member or the staff view catch database errors. If the database is not available,
or doesn't load, the app could crash.

**Root cause:**

An error handle was not implemented at the time of the development.

**Fix:**

Every database call site in the Member view is now wrapped, and a failure shows UserMessage.DatabaseUnavailable rather than closing the application. The Login view already handled this and now uses the shared message. In the Staff view one guard around RefreshAllData() covers all seven loaders, with separate guards on the Issue, Return, Suspend, Reactivate and Fulfil actions.

Defect is view-layer (WPF) and can't be unit tested.

---

## DEF-12

| | |
|---|---|
| **Title** |  Validation messages don't say which field is wrong. |
| **Severity** | Low |
| **Priority** | P2 |
| **Status** | Resolved |
| **Found by** | Summer |
| **Found in** | `MemberActionsService.ReserveBook` (Member view Reserve form) + `StaffView.IssueButton_Click` (Staff Issue Loan form) |
| **Related** | REQ-10 (Usability) |

**Description:**

The ReserveBook form and the IssueLoan form both do not say which of the fields were inputted incorrectly.
It just says "Invalid input" and does not explain which field was wrong, leaving the user having to guess which field is the right or wrong one.

**Root cause:**

As this is not a functional issue, the developer at the time did not realise that this could be an issue as they would be aware of what they were inputting.

**Fix:**

Standard wordings now live in UserMessage: FieldIsRequired, FieldMustBeANumber and NothingSelected. The Login view + Staff view names the field at fault ("Please enter a username") instead of describing the whole form.

One message is deliberately excluded. UserMessage.InvalidCredentials stays vague about whether the username or the password was wrong, so the login screen cannot be used to confirm that an account exists (REQ-11). Validation errors name the field; authentication failures do not.

Defect is view-layer (WPF) and can't be unit tested.

---

## DEF-13

| | |
|---|---|
| **Title** | A member can reserve a book they already have on loan. |
| **Severity** | Low |
| **Priority** | P2 |
| **Status** | Resolved |
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

ReserveBook() now checks the member's own active loans before creating a reservation, and rejects the attempt with 'You already have this book on loan.' 

Covered by TC-15.

---

## DEF-14

| | |
|---|---|
| **Title** | Searching for "%" or "_" matches every book. |
| **Severity** | Low |
| **Priority** | P2 |
| **Status** | Resolved |
| **Found by** | Summer |
| **Found in** | `DatabaseHelper.SearchCatalogue()` |
| **Related** | REQ-1 (Catalogue Management), REQ-9 (Input Validation) |

**Description:**

Searching for "%" or "_" matches every book. This could cause issues where a book with a title that could contain those
characters could be searched for and return every book.

**Root cause:**

searchTerm is concatenated into the SQL LIKE pattern without escaping the LIKE wildcard characters. This is a LIKE-pattern escaping issue and needs these values to escape
using the ESCAPE clause.

**Fix:**

SearchCatalogue() now escapes backslash, % and _ before building the LIKE pattern, and each condition carries an ESCAPE '\' clause. Covered by TC-12.

---

## DEF-15

| | |
|---|---|
| **Title** | If sample data fails to load once, the app never retries – the catalogue stays empty. |
| **Severity** | Low |
| **Priority** | P2 |
| **Status** | Resolved |
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

DatabaseSeeder.HasSampleData() was added, and App.xaml.cs now checks seeding separately from schema creation. A database with tables but no rows is seeded again on the next launch instead of being treated as ready.

This differs from the fix originally proposed here. Changing DatabaseExists() to check the Books table would conflate two different questions: whether the schema exists, and whether it holds data. Once staff catalogue management (REQ-1) ships, a librarian could legitimately empty the Books table, and the app would then re-seed over their work. HasSampleData() checks Accounts instead, which the application never deletes, so an empty Accounts table can only mean the seed did not complete.

Covered by TC-36.

---

## DEF-16

| | |
|---|---|
| **Title** | TC-5 doesn't test the app (it would pass even if the app broke); TC-4's name is wrong. |
| **Severity** | Medium |
| **Priority** | P1 |
| **Status** | Resolved |
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

TC-5 now calls db.CreateLoan() unconditionally, so the assertion exercises the production guard clause rather than a check performed by the test. TC-4 was renamed to GetTotalActiveReservationsCount_AfterNewReservation_ReturnsUpdatedCount and carries a note that it is a data-layer test which does not exercise StaffView's UI.

Verified by TC-5 itself, as the rewritten test now fails if CreateLoan's guard clause is removed.

---

## DEF-17

| | |
|---|---|
| **Title** | TC-6 and TC-7 check less than the RTM says they do. |
| **Severity** | Medium |
| **Priority** | P1 |
| **Status** | Resolved |
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

TC-6 now asserts a hardcoded expected status from known seed data instead of recomputing it with HasActiveReservation. TC-7b was added, testing the role-to-view routing decision via MainWindow.ResolveViewType, and TC-7's claim was narrowed to role resolution only. Verified by TC-6 and TC-7b.

---

## DEF-18

| | |
|---|---|
| **Title** | Tests fail with a confusing error if the database can't be created (e.g. on CI).  |
| **Severity** | Medium |
| **Priority** | P1 |
| **Status** | Resolved |
| **Found by** | Summer |
| **Found in** | `LibraryQA.Tests/StaffViewTests.cs`, `LoginViewTests.cs`, `MemberViewTests.cs` (`Setup()`) |
| **Related** | REQ-12 (Reliability) |

**Description:**

Test classes call DatabaseInitializer without checking the boolean return value and just calls the SeedSampleData() function.
If the DatabaseInitializer fails, this will cause the SQLite error message instead of a clear message that the database could not be loaded or created.

**Root cause:**

Error handling was skipped for this case. There were no Setup methods that were checking the return value of DatabaseInitializer or the DatabaseSeeder().

**Fix:**

The Setup() method in every test class now asserts the return values of InitializeDatabase() and SeedSampleData(), with messages naming the SQL file that failed to copy. On a fresh machine such as a CI runner, a missing content file produces one clear failure instead of every test failing for an unrelated-looking reason.

Verified by the Setup assertions in all three test classes.

---

## DEF-19

| | |
|---|---|
| **Title** | Suspend and Reactivate report success even when the database update fails  |
| **Severity** | Medium |
| **Priority** | P1 |
| **Status** | Resolved |
| **Found by** | Summer (code review) |
| **Found in** | `StaffView.SuspendButton_Click`, `StaffView.ReactivateButton_Click` |
| **Related** | REQ-12 (Reliability), REQ-19 (Member Suspension) |

**Description:**

DatabaseHelper.SetAccountStatus() returns a boolean indicating whether any row was updated, but both button handlers discarded it. If the update affected no rows — for example an account removed between the list being loaded and the button being pressed — the interface still displayed "has been suspended", so staff would believe an account was suspended when it was not.

**Root cause:**

The handlers were written against the happy path. The method's return value was available but never inspected, which is the same class of fault as DEF-02: an operation reporting success without confirming it occurred.

**Fix:**

Both handlers now check the return value and show a failure message when no row was updated. The surrounding try/catch added for DEF-11 covers the case where the call throws instead. Covered by TC-20.

---

## DEF-20

| | |
|---|---|
| **Title** | Login screen reveals that a suspended account exists before any password check  |
| **Severity** | Medium |
| **Priority** | P1 |
| **Status** | Resolved |
| **Found by** | Summer (code review) |
| **Found in** | `LoginView.LoginButton_Click`, `AuthenticationService.IsAccountSuspended` |
| **Related** | REQ-11 (Security), REQ-19 (Member Suspension) |

**Description:**

The suspended-account check ran before the password was verified, so entering any password against a real username revealed that the account existed and was suspended. This defeated the deliberate vagueness of the invalid-credentials message.

**Root cause:**

REQ-19 and REQ-11 pull in opposite directions — one asks for a specific message, the other for an uninformative one. Implementing REQ-19 in isolation satisfied its acceptance criteria without checking the security requirement it interacts with.

**Fix:**

The suspension message is now shown only after the password is verified, via IsSuspendedWithValidCredentials. Covered by TC-39.

---


## Summary

| ID | Title | Severity | Priority | Status |
|---|---|---|---|---|
| DEF-01 | Database stored password hashes do not match the documented passwords | High | P1 | Resolved |
| DEF-02 | Book status not restored when a loan is returned | High | P1 | Resolved |
| DEF-03 | Reservation not marked fulfilled when the reserved item is returned | Critical | P1 | Resolved |
| DEF-04 | Error presentation is inconsistent across the three interfaces | Low | P1 | Resolved |
| DEF-05 | Partially entered sample data | Medium | P2 | Resolved |
| DEF-06 | Reserving a book sets it to "Reserved" while still on loan | High | P1 | Resolved |
| DEF-07 | Staff Issue Loan skips the 2-book limit and the member ID check | High | P1 | Resolved |
| DEF-08 | Book status is updated twice on borrow and return | Low | P2 | Resolved |
| DEF-09 | Overdue checks use UTC; day counts differ between views | High | P1 | Resolved |
| DEF-10 | Return condition is never recorded | Medium | P2 | Resolved |
| DEF-11 | Member and Staff views do not catch database errors | Medium | P1 | Resolved |
| DEF-12 | Validation messages do not say which field is wrong | Low | P2 | Resolved |
| DEF-13 | A member can reserve a book they already have on loan | Low | P2 | Resolved |
| DEF-14 | Searching for "%" or "_" matches every book | Low | P2 | Resolved |
| DEF-15 | Sample data never retries after a failed load | Low | P2 | Resolved |
| DEF-16 | TC-5 does not test the app; TC-4's name is wrong | Medium | P1 | Resolved |
| DEF-17 | TC-6 and TC-7 check less than the RTM says they do | Medium | P1 | Resolved |
| DEF-18 | Tests fail with a confusing error if the database cannot be created | Medium | P1 | Resolved |
| DEF-19 | Suspend and Reactivate report success even when the database update fails | Medium | P1 | Resolved |
| DEF-20 | Login screen reveals that a suspended account exists before any password check | Medium | P1 | Resolved |