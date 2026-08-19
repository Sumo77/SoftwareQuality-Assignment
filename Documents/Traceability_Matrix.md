# Requirements Traceability Matrix

Maps each requirement to the test cases that verify it. Every requirement in
scope should have at least one passing test case before testing is declared
complete.

---

## Test cases

| ID | Test | Type | Owner | Status |
|---|---|---|---|---|
| TC-1 | Valid member credentials return the Member role | Unit | Summer | Pass |
| TC-2 | Valid staff credentials return the Staff role | Unit | Summer | Pass |
| TC-3 | Correct username with wrong password is rejected | Unit | Summer | Pass |
| TC-4 | Staff view shows the correct active reservation count | Unit | John | Pass |
| TC-5 | Issue loan with an invalid book ID shows a warning | Unit | John | Pass |
| TC-6 | Return button closes the loan and updates status | Integration | John | Pass |
| TC-7 | Member account is denied access to staff features | Unit | John | Pass |
| TC-8 | Borrowing an available book sets a 14-day due date and status "On Loan" | Integration | Daria | Pass |
| TC-9 | Reserving an already-reserved book is rejected | Unit | Daria | Pass |
| TC-10 | Member at the 2-loan limit is prevented from borrowing a 3rd | Unit | Daria | Pass |
| TC-11 | A member sees only their own active loans | Unit | Daria | Pass |

---

## Matrix

| Req ID | Requirement | Type | Test Case(s) | Covered |
|--------|-------------|------|--------------|---------|
| REQ-1 | Catalogue Management | Functional | | No |
| REQ-2 | Borrow and Return | Functional | TC-6, TC-8 | Yes |
| REQ-3 | Reservations | Functional | TC-9 | Yes |
| REQ-4 | Overdue | Functional | | No |
| REQ-5 | Member Portal | Functional | TC-1, TC-11 | Yes |
| REQ-6 | Staff Portal | Functional | TC-2, TC-4 | Yes |
| REQ-7 | Access Control | Functional | TC-1, TC-2, TC-7 | Yes |
| REQ-8 | Loan Limits | Functional | TC-10 | Yes |
| REQ-9 | Input Validation | Functional | | No |
| REQ-10 | Usability | Non-Functional | Usability walkthrough | Informal |
| REQ-11 | Security | Non-Functional | TC-2, TC-3, TC-7 | Yes |
| REQ-12 | Reliability | Non-Functional | TC-3 | Yes |
| REQ-13 | Maintainability | Non-Functional | Code review at merge | Informal |
| REQ-14 | Integrity | Non-Functional | TC-8, TC-9 | Yes |
| REQ-15 | Performance | Non-Functional | | No |

---

## Notes:

While test cases aim to cover all requirements, some requirements are not entirely covered or fulfilled by the current set of test cases. 
Additional test cases may be needed to ensure complete coverage, particularly for requirements related to 
catalogue management, overdue handling, input validation, and performance.