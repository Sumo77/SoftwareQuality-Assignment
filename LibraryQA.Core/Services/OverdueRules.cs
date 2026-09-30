using System;

namespace LibraryQA.Core.Services
{
    // The overdue rule, defined in exactly one place (REQ-4, REQ-13).
    //
    // Before this, the member view rounded a fractional day count and the staff view truncated it,
    // so the two screens could show different numbers for the same loan. Both now call these
    // methods, so they cannot disagree (DEF-09).
    //
    // "today" is supplied by the caller rather than read inside SQL. SQLite's date('now') returns
    // the UTC date, which in New Zealand is up to a day behind local time, so a loan could stay
    // unflagged for most of the day it fell due.
    public static class OverdueRules
    {
        // A loan is overdue once the current date has passed the due date and no return is recorded.
        // A loan due today is not overdue.
        public static bool IsOverdue(DateTime dueDate, DateTime today)
        {
            return today.Date > dueDate.Date;
        }

        // Whole days between the due date and today. Never negative: a loan not yet due returns 0.
        public static int DaysOverdue(DateTime dueDate, DateTime today)
        {
            int days = (today.Date - dueDate.Date).Days;
            return days > 0 ? days : 0;
        }
    }
}
