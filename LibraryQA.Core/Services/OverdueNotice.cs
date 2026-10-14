using System;
using System.Collections.Generic;

namespace LibraryQA.Core.Services
{
    // REQ-17: builds the notice from the caller's loan rows, so both screens word it identically
    // (REQ-13) and it stays unit testable. A returned loan leaves the list, so the notice stops.
    public static class OverdueNotice
    {
        // The notice shown to a member when they log in, or null when nothing of theirs is overdue.
        // Expects rows from DatabaseHelper.GetActiveLoans.
        public static string? ForMember(IEnumerable<Dictionary<string, object>>? activeLoans)
        {
            if (activeLoans == null)
            {
                return null;
            }

            var overdueItems = new List<string>();

            foreach (var loan in activeLoans)
            {
                if (!loan.TryGetValue("IsOverdue", out object? isOverdue) || !Convert.ToBoolean(isOverdue))
                {
                    continue;
                }

                string title = loan.TryGetValue("Title", out object? loanTitle)
                    ? loanTitle?.ToString() ?? string.Empty
                    : string.Empty;

                int daysOverdue = loan.TryGetValue("DaysOverdue", out object? days) ? Convert.ToInt32(days) : 0;

                overdueItems.Add($"'{title}' is overdue by {DayCount(daysOverdue)}.");
            }

            if (overdueItems.Count == 0)
            {
                return null;
            }

            string heading = overdueItems.Count == 1
                ? "You have 1 overdue item. Please return it to the library."
                : $"You have {overdueItems.Count} overdue items. Please return them to the library.";

            return heading + Environment.NewLine + string.Join(Environment.NewLine, overdueItems);
        }

        // The notice shown on the staff dashboard, or null when nothing is overdue.
        // Expects rows from DatabaseHelper.GetAllOverdueLoans.
        public static string? ForStaff(IEnumerable<Dictionary<string, object>>? overdueLoans)
        {
            if (overdueLoans == null)
            {
                return null;
            }

            int overdueCount = 0;

            foreach (var _ in overdueLoans)
            {
                overdueCount++;
            }

            if (overdueCount == 0)
            {
                return null;
            }

            return overdueCount == 1
                ? "1 item is overdue across all members."
                : $"{overdueCount} items are overdue across all members.";
        }

        private static string DayCount(int days) // "1 day" or "n days"
        {
            return days == 1 ? "1 day" : $"{days} days";
        }
    }
}
