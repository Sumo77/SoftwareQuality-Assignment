# Requirements

## Functional Requirements

### REQ-1 – Catalogue Management
The system shall return catalogue search results matching title, author or ISBN, and shall display each result's status as one of: "available", "on loan", or "reserved". Staff shall additionally be able to add, edit and remove catalogue records.

### REQ-2a – Borrow
When staff issue a loan for an "available" item, the system shall set the loan date, the due/return date, and change the item's status from "available" to "on loan", such that the item cannot be borrowed or edited as Available by any other staff action until returned.

### REQ-2b – Return
When staff process a return for an item that is "on loan", the system shall close the associated loan record, set the item's status to "available" (or "reserved", if a pending reservation exists for it), and record the return date and item condition.

### REQ-3 – Reservations
A member may place a reservation on an item currently "on loan" only if there is no already-existing reservation placed by another member. If a reservation exists, the system shall reject the new reservation attempt and display a message indicating the item is already reserved. A reservation shall be recorded against the item without changing its status, which remains "on loan" until the item is returned. On return, the item status shall change to "reserved" rather than "available".

### REQ-4 – Overdue
The system shall flag a loan as "overdue" when the current date exceeds the due date and no return has been recorded. It shall also calculate the number of days overdue (as current date - due date) to display for staff records.

### REQ-5 – Member Portal
Following successful login, a member may view their own current loans with due dates, their loan history, and their active reservations. A member shall not be able to view another member's records, or any staff-only information (i.e. reporting).

### REQ-6 – Staff Portal
Following successful login, staff shall be able to view the total number of items on loan, the number of overdue items, the number of active reservations, and the most frequently borrowed titles. Staff shall be able to view basic member information (for example, member's names and contact details on overdue loans to follow up) but shall not have access to a member's full account record.

### REQ-7 – Access Control
The system shall determine a user's role based on their account credentials. It shall deny loan issuing, returns processing, catalogue editing, and not display reporting information to any account that is not Staff, by either not providing the option or preventing the action while displaying a "not authorised" error message rather than failing silently.

### REQ-8 – Loan Limits
The system shall prevent a member from borrowing more than 2 items "on loan" simultaneously, rejecting a 3rd loan attempt with an error message stating "the limit has been reached". A pending loan or reservation request counts towards this limit until it is approved or rejected.

### REQ-9 – Input Validation
The system shall reject invalid and empty required fields before they are submitted, displaying an error message that specifies the field at fault (i.e. "Member ID must be a number") rather than failing silently, producing an unexplainable error or system crashing.

## Non-Functional Requirements

### REQ-10 – Usability
At least 75% of test participants unfamiliar with the system should be able to navigate the core features of the interface with no issues (for example, logging in, searching the catalogue and placing a reservation without instruction).

### REQ-11 – Security
The application has a login interface with role-based accounts. A member account shall never see or have access to staff-only information or features. In addition, passwords shall be stored as SHA256 hashes rather than plain-text.

### REQ-12 – Reliability
The system should handle errors such as invalid input (i.e. blank fields, invalid actions) gracefully, with clear error messages rather than failing silently, producing an unexplainable error or system crashing.

### REQ-13 – Maintainability
Each core business rule (for example, loan period, loan limit and status transitions) shall be defined in exactly one location and shall be testable independently of the user interface.

### REQ-14 – Integrity
An item's status shall always match its loan and reservation records, and no item shall hold more than one unfulfilled reservation. No operation shall leave the status inconsistent with the underlying data.

### REQ-15 – Performance
Every feature of the system should be responsive and smooth. For example, catalogue searches should be displayed within near real time and should be able to display a pre-defined minimum number of books at one time.


## New Requirements - Version 2

### REQ-16 – Account Creation
The system shall allow a new user to create a member account by providing a unique username and password. The system shall reject the attempt if the username already exists or if required fields are blank, and shall store the password as a SHA256 hash, consistent with REQ-11. A newly created account is activated by a staff member before use.

**Acceptance Criteria:**
- Given a username that does not already exist, when a new user submits valid registration details, then a new member account is created with status 'Pending', and the user cannot yet log in.
- Given a username that already exists, when a new user attempts to register with it, then the system rejects the attempt and displays a message indicating the username is already taken.
- Given a registration attempt with a blank username or password, when the user submits the form, then the system rejects the attempt and displays a message specifying the missing field.
- Given a Pending account, when staff activate it, then the member can log in and use the system as normal.

### REQ-17 – Overdue Notices

The system shall display an overdue notice to a member at login for each of their loans that is overdue, visible to both the member and staff. The overdue notice shall stop appearing once the item is returned.

**Acceptance Criteria:**
- Given a member has a loan that has just become overdue, when they next log in, then a notice informing them of the overdue item is displayed.
- Given an overdue loan, when a member or staff member views it, then the correct days amount is displayed.
- Given an overdue loan is returned, when the member next logs in, then the notice is not shown for that loan.

### REQ-18 – Loan and Reservation Approval
The system shall require staff to manually approve a member’s loan or reservation request before it is finalised, holding the request in a "pending" status until a staff member approves or rejects it.
**Acceptance Criteria:**
- Given a member submits a loan or reservation request, when the request is created, then it is held in a "pending" status and is not yet active.
- Given a pending request, when a staff member approves it, then the request becomes active and the item's status updates accordingly.
- Given a pending request, when a staff member rejects it, then the request is closed without changing the item's status, and the member is informed of the rejection.
- Given a member with 2 active or pending items, when they submit a third request, then the system rejects it with the message stating the limit has been reached.
- Given a pending request is rejected by staff, when the rejection is recorded, then that item no longer counts towards the member's limit.

### REQ-19 – Member Suspension
The system shall allow staff to suspend or reactivate a member account using the account status field (Active / Pending / Suspended / Locked). A suspended member shall be denied the ability to log in and shall see a message stating their account is suspended rather than a generic invalid-credentials message.

**Acceptance Criteria:**
- Given a member account in good standing, when staff suspend the account, then its status updates accordingly and takes effect immediately.
- Given a suspended member account, when that member attempts to log in, then the system denies access and displays a message stating the account is suspended, not "invalid username or password".
- Given a suspended member account, when staff reactivate it, then the member can log in and borrow/reserve items as normal.

### REQ-20 – Reservation Expiry
The system shall automatically ‘cancel’ a reservation if the reserved item is not collected within 5 days of becoming available. An uncollected and automatically cancelled item's status will return to "available" for loan or reservation by other members.

**Acceptance Criteria:**
- Given a reserved item becomes available and is not collected within 5 days, when the expiry period elapses, then the reservation is cancelled and the item’s status returns to "available".
- Given a reserved item is collected within the 5-day window, when the collection is recorded, then the reservation is fulfilled and does not expire.
- Given a reservation expires, when the member next logs in, then the reservation no longer appears in their active reservations.

### REQ-21 – Login Lockout
The system shall ‘lock’ (suspend) a login account after 5 consecutive failed login attempts, denying further attempts until the ‘lock’ is cleared. An account lock can be cleared by a staff member, who is able to reactivate the account from their portal using the account status field (Active / Pending / Suspended / Locked).

**Acceptance Criteria:**
- Given a login account has 4 consecutive failed attempts, when a 5th attempt also fails, then the account is locked and further login attempts are denied.
- Given a locked account, when a correct username and password are entered, then access is still denied until the lock is cleared.
- Given a successful login occurs before reaching the failed-attempt threshold, when the login succeeds, then the failed-attempt count resets to zero.

## Matrix 
| Req ID | Requirement | Type | Test Case(s) | Implementation Status |
|--------|-------------|------|---------------|------------------------|
| REQ-1 | Catalogue Management | Functional | — | Partial |
| REQ-2a | Borrow | Functional | TC-8 | Implemented | 
| REQ-2b | Return | Functional | TC-6 | Implemented | 
| REQ-3 | Reservations | Functional | TC-9 | Implemented | 
| REQ-4 | Overdue | Functional | — | Missing | 
| REQ-5 | Member Portal | Functional | TC-1, TC-11 | Implemented | 
| REQ-6 | Staff Portal | Functional | TC-2, TC-4 | Partial | 
| REQ-7 | Access Control | Functional | TC-1, TC-2, TC-7 | Implemented |
| REQ-8 | Loan Limits | Functional | TC-10 | Implemented | 
| REQ-9 | Input Validation | Functional | TC-5 | Partial |
| REQ-10 | Usability | Non-Functional | Usability walkthrough | Partial |
| REQ-11 | Security | Non-Functional | TC-2, TC-3, TC-7 | Implemented | 
| REQ-12 | Reliability | Non-Functional | TC-3 | Partial | 
| REQ-13 | Maintainability | Non-Functional | Code review at merge | Partial |
| REQ-14 | Integrity | Non-Functional | TC-8, TC-9 | Implemented | 
| REQ-15 | Performance | Non-Functional | — | Missing | 
| REQ-16 | Account Creation | Functional | — | Missing |
| REQ-17 | Overdue Notices | Functional | — | Missing | 
| REQ-18 | Loan and Reservation Approval | Functional | — | Missing |
| REQ-19 | Member Suspension | Functional | — | Missing | 
| REQ-20 | Reservation Expiry | Functional | — | Missing |
| REQ-21 | Login Lockout | Non-Functional | — | Missing |
