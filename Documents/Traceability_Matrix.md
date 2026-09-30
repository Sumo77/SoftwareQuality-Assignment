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
| TC-7 | A member account resolves only to the Member role, and role routing never sends a member to StaffView | Unit | John | Pass |
| TC-7b | The role to view routing decision denies staff features to Member accounts | Unit | John | Pass |
| TC-8 | Borrowing an available book sets a 14-day due date and status "On Loan" | Integration | Daria | Pass |
| TC-9 | Reserving an already-reserved book is rejected | Unit | Daria | Pass |
| TC-10 | Member at the 2-loan limit is prevented from borrowing a 3rd | Unit | Daria | Pass |
| TC-11 | A member sees only their own active loans | Unit | Daria | Pass |
| TC-30 | A loan due today is not overdue (boundary) | Unit | Summer | Pass |
| TC-31 | A loan due yesterday is overdue by exactly 1 day (boundary) | Unit | Summer | Pass |
| TC-32 | A loan not yet due reports 0 days overdue, never negative | Unit | Summer | Pass |
| TC-33 | Member and staff views report the same days overdue for the same loan | Integration | Summer | Pass |
| TC-34 | Overdue uses the supplied local date, not the database UTC date | Integration | Summer | Pass |
| TC-35 | Every sample book status matches its loan and reservation records | Integration | Summer | Pass |
| TC-36 | A schema-only database is re-seeded rather than left empty | Integration | Summer | Pass |
| TC-37 | Catalogue search on 5,000 books completes within budget | Performance | Summer | Pass |
| TC-38 | Generated books can be removed without touching seeded books | Integration | Summer | Pass |

**NOTE FOR MERGE: Number blocks:** John TC-12 to TC-19, Daria TC-20 to TC-29, Summer TC-30 to TC-39. 

---

## Matrix

| Req ID | Requirement | Type | Test Case(s) | Open defects | Implementation | Tested |
|---|---|---|---|---|---|---|
| REQ-1 | Catalogue Management | Functional | — | DEF-14 | Partial | No |
| REQ-2a | Borrow | Functional | TC-8 | DEF-08 | Implemented | Yes |
| REQ-2b | Return | Functional | TC-6 | DEF-08, DEF-10 | Partial | Yes |
| REQ-3 | Reservations | Functional | TC-9 | DEF-03, DEF-06, DEF-13 | Partial | Partial |
| REQ-4 | Overdue | Functional | TC-30, TC-31, TC-32, TC-33, TC-34 | — | Implemented | Yes |
| REQ-5 | Member Portal | Functional | TC-1, TC-11 | — | Implemented | Yes |
| REQ-6 | Staff Portal | Functional | TC-2, TC-4 | DEF-03 | Partial | Partial |
| REQ-7 | Access Control | Functional | TC-1, TC-2, TC-7, TC-7b | — | Implemented | Yes |
| REQ-8 | Loan Limits | Functional | TC-10 | DEF-07 | Partial | Partial |
| REQ-9 | Input Validation | Functional | TC-5 | DEF-12, DEF-14 | Partial | Partial |
| REQ-10 | Usability | Non-Functional | Usability walkthrough (Week 11) | DEF-04, DEF-12 | Partial | Planned |
| REQ-11 | Security | Non-Functional | TC-2, TC-3, TC-7 | — | Implemented | Yes |
| REQ-12 | Reliability | Non-Functional | TC-3, TC-36 | DEF-11 | Partial | Partial |
| REQ-13 | Maintainability | Non-Functional | Code review at merge, CI quality gates | DEF-08 | Partial | Informal |
| REQ-14 | Integrity | Non-Functional | TC-8, TC-9, TC-35 | DEF-03, DEF-06, DEF-08 | Partial | Partial |
| REQ-15 | Performance | Non-Functional | TC-37 | — | Partial | Partial |
| REQ-16 | Account Creation | Functional | — | — | Missing | No |
| REQ-17 | Overdue Notices | Functional | — | — | Missing | No |
| REQ-18 | Loan and Reservation Approval | Functional | — | — | Missing | No |
| REQ-19 | Member Suspension | Functional | — | — | Missing | No |
| REQ-20 | Reservation Expiry | Functional | — | — | Missing | No |
| REQ-21 | Login Lockout | Non-Functional | — | — | Missing | No |

**Column meanings**

- **Implementation** — Implemented, Partial, or Missing. Whether the behaviour the requirement
  describes is built.
- **Tested** — Yes, Partial, No, Planned, or Informal. Whether a passing test case verifies it.
  The two are separate: a requirement can be built but untested, or tested but only partly built.

---
