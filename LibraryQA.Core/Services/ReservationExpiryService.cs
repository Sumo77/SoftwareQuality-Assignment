using System;
using LibraryQA.Core.Database;

namespace LibraryQA.Core.Services
{
    // REQ-20: Reservation Expiry - an uncollected reservation is cancelled after 5 days of the
    // item becoming available, returning the item's status to "Available". This is run lazily
    // (on login / view load) rather than via a background job, so the behaviour stays testable
    // and deterministic.
    public class ReservationExpiryService
    {
        public const int ReservationExpiryDays = 5;

        private readonly string _connectionString;

        public ReservationExpiryService(string connectionString)
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
