using System;

namespace LibraryQA.Core.Services
{
    // The overdue rule in one place, so the member and staff screens cannot disagree (REQ-4, REQ-13, DEF-09).
    // "today" comes from the caller: SQLite's date('now') is UTC, up to a day behind local time.
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
