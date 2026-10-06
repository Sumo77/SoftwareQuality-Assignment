# Requirements Traceability Matrix

Maps each requirement to the test cases that verify it, its implementation state, and any open
defects against it. Every requirement in scope should have at least one passing test case before
testing is declared complete.

The requirements themselves are defined in `Requirements.md`.

---

## Test cases

| ID | Test | Type | Owner | Status |
|---|---|---|---|---|
| TC-1 | Valid member credentials return the Member role | Unit | Summer | Pass |
| TC-2 | Valid staff credentials return the Staff role | Unit | Summer | Pass |
| TC-3 | Correct username with wrong password is rejected | Unit | Summer | Pass |
| TC-4 | `GetTotalActiveReservationsCount` returns the correct count after a new reservation is added | Unit | John | Pass |
| TC-5 | Issue loan with an invalid book ID is rejected by `CreateLoan` without creating a loan record | Unit | John | Pass |
| TC-6 | Return button closes the loan and updates the book's catalogue status | Integration | John | Pass |
| TC-7 | A member account resolves only to the Member role | Unit | John | Pass |
| TC-7b | Role routing never sends a Member account to StaffView | Unit | John | Pass |
| TC-7c | Role routing sends a Staff account to StaffView | Unit | John | Pass |
| TC-8 | Borrowing an available book sets a 14-day due date and status "On Loan" | Integration | Daria | Pass |
| TC-9 | Reserving an already-reserved book is rejected | Unit | Daria | Pass |
| TC-10 | Member at the 2-loan limit is prevented from borrowing a 3rd | Unit | Daria | Pass |
| TC-11 | A member sees only their own active loans | Unit | Daria | Pass |
| TC-12 | Searching for a literal `%` or `_` does not match every book | Unit | John | Pass |
| TC-13 | Fulfilling a reservation closes it and issues the loan | Integration | John | Pass |
| TC-14 | Reserving a book that is on loan leaves it On Loan | Integration | John | Pass |
| TC-15 | A member cannot reserve a book they already have on loan | Unit | John | Pass |
| TC-16 | The condition chosen at return is recorded on the loan | Integration | John | Pass |
| TC-20 | A suspended member cannot log in; reactivating restores access | Integration | Daria | Pass |
| TC-30 | A loan due today is not overdue (boundary) | Unit | Summer | Pass |
| TC-31 | A loan due yesterday is overdue by exactly 1 day (boundary) | Unit | Summer | Pass |
| TC-32 | A loan not yet due reports 0 days overdue, never negative | Unit | Summer | Pass |
| TC-33 | Member and staff views report the same days overdue for the same loan | Integration | Summer | Pass |
| TC-34 | Overdue uses the supplied local date, not the database UTC date | Integration | Summer | Pass |
| TC-35 | Every sample book status matches its loan and reservation records | Integration | Summer | Pass |
| TC-36 | A schema-only database is re-seeded rather than left empty | Integration | Summer | Pass |
| TC-37 | Catalogue search on 5,000 books completes within budget | Performance | Summer | Pass |
| TC-38 | Generated books can be removed without touching seeded books | Integration | Summer | Pass |
| TC-39 | A wrong password and an unknown username are rejected identically | Unit | Summer | Pass |
| TC-40 | A new registration creates a Pending account that cannot log in | Integration | Summer | Pass |
| TC-41 | A username that is already taken is rejected | Integration | Summer | Pass |
| TC-42 | Staff activation lets a Pending account log in | Integration | Summer | Pass |
| TC-43 | Five failed attempts lock the account and refuse the correct password | Integration | Summer | Pass |
| TC-44 | Unlocking clears the attempt count and restores access | Integration | Summer | Pass |
| TC-45 | A successful login resets the failed-attempt count | Integration | Summer | Pass |
| TC-46 | The member's overdue notice names the item and how late it is | Unit | Summer | Pass |
| TC-47 | A member with nothing overdue sees no notice | Unit | Summer | Pass |
| TC-48 | The notice stops once the item is returned | Integration | Summer | Pass |
| TC-49 | A locked staff account stays visible so it can be unlocked | Integration | Summer | Pass |
| TC-50 | Invalid registration input is rejected and writes nothing | Integration | Summer | Pass |
| TC-51 | A taken username is distinguished from a free one before submitting | Integration | Summer | Pass |
| TC-52 | Failed attempts against an unknown username record nothing | Integration | Summer | Pass |
| TC-53 | A suspended account is not converted to Locked by failed attempts | Integration | Summer | Pass |
| TC-54 | The staff notice counts every overdue item, and is absent when there are none | Unit | Summer | Pass |

Test numbers are allocated in blocks to avoid collisions during parallel development:
John TC-12 to TC-19, Daria TC-20 to TC-29, Summer TC-30 to TC-54.

---

## Matrix

| Req ID | Requirement | Type | Test Case(s) | Defects raised | Implementation | Tested |
|---|---|---|---|---|---|---|
| REQ-1 | Catalogue Management | Functional | TC-12 | DEF-14 | Partial | Partial |
| REQ-2a | Borrow | Functional | TC-8 | DEF-08 | Implemented | Yes |
| REQ-2b | Return | Functional | TC-6, TC-16 | DEF-08, DEF-10 | Implemented | Yes |
| REQ-3 | Reservations | Functional | TC-9, TC-13, TC-14, TC-15 | DEF-03, DEF-06, DEF-13 | Implemented | Yes |
| REQ-4 | Overdue | Functional | TC-30, TC-31, TC-32, TC-33, TC-34 | — | Implemented | Yes |
| REQ-5 | Member Portal | Functional | TC-1, TC-11 | — | Implemented | Yes |
| REQ-6 | Staff Portal | Functional | TC-2, TC-4, TC-7c, TC-42, TC-49 | DEF-03 | Implemented | Partial |
| REQ-7 | Access Control | Functional | TC-1, TC-2, TC-7, TC-7b, TC-7c | — | Implemented | Yes |
| REQ-8 | Loan Limits | Functional | TC-10 | DEF-07 | Implemented | Partial |
| REQ-9 | Input Validation | Functional | TC-5, TC-12, TC-41, TC-50 | DEF-12, DEF-14 | Implemented | Partial |
| REQ-10 | Usability | Non-Functional | Usability walkthrough (Week 11) | DEF-04, DEF-12 | Partial | Planned |
| REQ-11 | Security | Non-Functional | TC-2, TC-3, TC-7, TC-39, TC-40, TC-52 | — | Implemented | Yes |
| REQ-12 | Reliability | Non-Functional | TC-3, TC-36 | DEF-11 | Implemented | Partial |
| REQ-13 | Maintainability | Non-Functional | Code review at merge, CI quality gates | DEF-08 | Implemented | Informal |
| REQ-14 | Integrity | Non-Functional | TC-8, TC-9, TC-35 | DEF-03, DEF-06, DEF-08 | Implemented | Partial |
| REQ-15 | Performance | Non-Functional | TC-37 | — | Partial | Partial |
| REQ-16 | Account Creation | Functional | TC-40, TC-41, TC-42, TC-50, TC-51 | — | Implemented | Yes |
| REQ-17 | Overdue Notices | Functional | TC-46, TC-47, TC-48, TC-54 | — | Implemented | Yes |
| REQ-18 | Loan and Reservation Approval | Functional | — | — | Missing | No |
| REQ-19 | Member Suspension | Functional | TC-20, TC-53 | — | Implemented | Yes |
| REQ-20 | Reservation Expiry | Functional | — | — | Partial | No |
| REQ-21 | Login Lockout | Functional | TC-43, TC-44, TC-45, TC-49, TC-52, TC-53 | — | Implemented | Yes |

**Column meanings**

- **Implementation** — Implemented, Partial, or Missing. Whether the behaviour the requirement
  describes is built.
- **Tested** — Yes, Partial, No, Planned, or Informal. Whether a passing test case verifies it.
  The two are separate: a requirement can be built but untested, or tested but only partly built.

All defects listed under Defects raised are Resolved as of the Week 10 code freeze. See
`Defect_Report.md` for status.

---

## Tested + Verified Manually (Not Automated)

The following behaviour is implemented in WPF view code, which this project cannot unit test —
the same limitation that is the reason code coverage is collected as a metric rather than enforced
as a merge gate. Each item was verified by a manual walkthrough during Week 10, with screenshots
held as evidence.

| Behaviour | Requirement |
|---|---|
| The Create an Account screen, including the password confirmation check | REQ-16 |
| The live "username already taken" check as the field loses focus | REQ-16 |
| Activate refusing an account that is already Active, or that is Locked | REQ-16, REQ-21 |
| Suspend refusing a Staff account | REQ-19 |
| The Days Overdue column and the overdue notice appearing on the member screen | REQ-17 |
| The overdue notice on the staff dashboard clearing after a return | REQ-17 |
| The Pending, Suspended and Locked messages shown on the login screen | REQ-16, REQ-19, REQ-21 |

---

## Known Limitations (For Future Iterations)

- **REQ-15** — Performance is measured for catalogue search only, not for every feature as the
  requirement is worded.
- **REQ-17** — The overdue notice is shown at login and on refresh, not at the moment a loan tips
  overdue during an open session. This matches the requirement as written.
- **REQ-21** — If every staff account is locked at the same time, there is no in-application
  recovery path. A single locked staff account is recoverable by any other staff account (TC-49).