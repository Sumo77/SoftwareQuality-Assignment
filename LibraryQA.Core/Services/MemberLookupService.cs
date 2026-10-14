using LibraryQA.Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LibraryQA.Core.Services
{
    public class MemberLookupResult // Stored result of a staff member lookup - deliberately holds basic details only
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int MemberId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string AccountStatus { get; init; } = string.Empty;
        public string? Email { get; init; } // Only filled while the member has overdue items
        public string? PhoneNumber { get; init; } // Only filled while the member has overdue items
        public IReadOnlyList<Dictionary<string, object>> ActiveLoans { get; init; } = Array.Empty<Dictionary<string, object>>();
        public IReadOnlyList<Dictionary<string, object>> OverdueLoans { get; init; } = Array.Empty<Dictionary<string, object>>();
        public IReadOnlyList<Dictionary<string, object>> Reservations { get; init; } = Array.Empty<Dictionary<string, object>>();
    }

    // REQ-6: one member's loans, overdue items and reservations - basic details only, never the full
    // account record, so contact details are returned only while something is overdue.
    public class MemberLookupService
    {
        private readonly string _connectionString;

        public MemberLookupService(string connectionString) // Initialise database access
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public MemberLookupResult LookUpMember(int memberId, DateTime? today = null) // Gathers one member's basic details, active loans, overdue items and active reservations
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                var member = db.GetMemberBasicInfo(memberId);

                if (member == null) // Unknown ID, or an account that is not a member
                {
                    return new MemberLookupResult { Success = false, Message = $"No member found with ID {memberId}." };
                }

                DateTime asAt = (today ?? DateTime.Today).Date;

                // Overdue status and day counts come from OverdueRules through GetActiveLoans (DEF-09),
                // so this screen can never disagree with the Overdue tab or the Member view.
                var loans = db.GetActiveLoans(memberId, asAt);
                var overdue = loans.Where(loan => Convert.ToBoolean(loan["IsOverdue"])).ToList();
                var reservations = db.GetActiveReservations(memberId);

                bool followUpNeeded = overdue.Count > 0;

                return new MemberLookupResult
                {
                    Success = true,
                    MemberId = Convert.ToInt32(member["AccountID"]),
                    FullName = $"{member["FirstName"]} {member["LastName"]}",
                    AccountStatus = member["AccountStatus"]?.ToString() ?? "",
                    Email = followUpNeeded ? member["Email"]?.ToString() : null,
                    PhoneNumber = followUpNeeded ? member["PhoneNumber"]?.ToString() : null,
                    ActiveLoans = loans,
                    OverdueLoans = overdue,
                    Reservations = reservations
                };
            }
        }
    }
}