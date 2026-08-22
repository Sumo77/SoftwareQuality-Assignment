# Defect Report

Defects found during development and testing.

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
reservation exists, and Available otherwise.

---

## DEF-03

| | |
|---|---|
| **Title** | Reservation is not marked fulfilled when the reserved item is returned |
| **Severity** | Medium |
| **Status** | Open - To Do Next |
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
| **Status** | Open - To Do Next |
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
convention for user feedback. Each developer chose a reasonable pattern
independently. The result is inconsistent, but not incorrect.

**Fix:**

Delayed, to do next. In context it's correct, dialogs suit one-off
member actions requiring acknowledgement, inline status suits staff processing
transactions in sequence. The next phase will agree a single convention and
document it, to keep things consistent across the application.

---

## Summary

| ID | Title | Severity | Status |
|---|---|---|---|
| DEF-01 | Database stored password hashes do not match the actual documented passwords | High | Fixed |
| DEF-02 | Book status not restored when a loan is returned | High | Resolved |
| DEF-03 | Reservation not marked fulfilled when reserved item is returned | Medium | Open |
| DEF-04 | Error presentation is inconsistent across the three interfaces | Low | Open |