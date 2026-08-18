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
| TC-4 | Staff view shows the correct active reservation count | | John | |
| TC-5 | Issue loan with an invalid book ID shows a warning | | John | |
| TC-6 | Return button closes the loan and updates status | | John | |
| TC-7 | Member account is denied access to staff features | | John | |
| TC-8 | _[test]_ | | Daria | |
| TC-9 | _[test]_ | | Daria | |
| TC-10 | _[test]_ | | Daria | |
| TC-11 | _[test]_ | | Daria | |

---

## Matrix

| Requirement | Description | Test cases | Covered |
|---|---|---|---|
| REQ-1 | Catalogue management | | |
| REQ-2 | Borrow and return | | |
| REQ-3 | Reservations | | |
| REQ-4 | Overdue flagging | | |
| REQ-5 | Member portal | TC-1 | Yes |
| REQ-6 | Staff portal | TC-2 | Yes |
| REQ-7 | Access control | TC-1, TC-2 | Yes |
| REQ-8 | Loan limits | | |
| REQ-9 | Input validation | | |
| REQ-10 | Usability | | |
| REQ-11 | Security | TC-2, TC-3 | Yes |
| REQ-12 | Reliability | TC-3 | Yes |
| REQ-13 | Maintainability | | |
| REQ-14 | Data integrity | | |

---

## Notes

---