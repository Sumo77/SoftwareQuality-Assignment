# Defect Report

Defects found during development and testing.

**Severity:** 
- Critical (interupts core functionality)
- High (wrong behaviour, workaround exists)
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
| **Title** | Documented and inputed password hashes do not match the actual password |
| **Severity** | High |
| **Status** | Resolved |
| **Found by** | Summer |
| **Found in** | Original `README.md` + `Database: Accounts Table` |
| **Related** | REQ-5 + REQ-6 + REQ-7: Member + Staff Views + Access Control, REQ-10 + REQ-11: Usability + Security |

**Description:**

The documentation listed `5e884898da28…` as the hash for `member123`. That value
is actually SHA256 of the literal string `"password"` and appears nowhere in the
database.

**Root cause:**

Documentation and input was not verified, allowing a mismatch between 
the documented hash and the actual hash in the database.

**Fix:**

Hashes recomputed from the seed file and corrected. Overlapping documents and inputs
consolidated so there is one place to keep accurate rather than three.

---

## DEF-02

...


---

## Summary

| ID | Title | Severity | Status |
|---|---|---|---|
| DEF-01 | Documented password hashes wrong | Critical | Fixed |
| DEF-02 | | | |