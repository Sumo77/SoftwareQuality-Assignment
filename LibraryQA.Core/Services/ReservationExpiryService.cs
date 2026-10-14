using System;
using LibraryQA.Core.Database;

namespace LibraryQA.Core.Services
{
    // REQ-20: cancels a reservation left uncollected for 5 days and frees the item.
    // Run on view load rather than as a background job, which keeps it deterministic and testable.
    public class ReservationExpiryService
    {
        public const int ReservationExpiryDays = 5;

        private readonly string _connectionString;

        public ReservationExpiryService(string connectionString) // Initialise database access
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // Expires any reservations that have sat uncollected for more than ReservationExpiryDays,
        // and returns the number of reservations that were expired.
        public int ExpireOverdueReservations(DateTime? today = null)
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                return db.ExpireReservations(today, ReservationExpiryDays);
            }
        }
    }
}
