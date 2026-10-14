using System;
using LibraryQA.Core.Database;

namespace LibraryQA.Core.Services
{
    // REQ-18: staff approve or reject pending requests. Approval activates the request and updates
    // the item's status; rejection closes it and leaves the status alone.
    public class ApprovalService
    {
        private readonly string _connectionString;

        public ApprovalService(string connectionString) // Initialise database access
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public ApprovalResult ApproveLoan(int loanId) // Activate a pending loan request (REQ-18)
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                bool success = db.ApproveLoan(loanId);
                return success
                    ? new ApprovalResult { Success = true, Message = "Loan approved." }
                    : new ApprovalResult { Success = false, Message = "Unable to approve the loan request. It may no longer be pending, or the item may no longer be available." };
            }
        }

        public ApprovalResult RejectLoan(int loanId) // Close a pending loan request (REQ-18)
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                bool success = db.RejectLoan(loanId);
                return success
                    ? new ApprovalResult { Success = true, Message = "Loan request rejected." }
                    : new ApprovalResult { Success = false, Message = "Unable to reject the loan request. It may no longer be pending." };
            }
        }

        public ApprovalResult ApproveReservation(int reservationId) // Activate a pending reservation request (REQ-18)
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                bool success = db.ApproveReservation(reservationId);
                return success
                    ? new ApprovalResult { Success = true, Message = "Reservation approved." }
                    : new ApprovalResult { Success = false, Message = "Unable to approve the reservation request. It may no longer be pending." };
            }
        }

        public ApprovalResult RejectReservation(int reservationId) // Close a pending reservation request (REQ-18)
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                bool success = db.RejectReservation(reservationId);
                return success
                    ? new ApprovalResult { Success = true, Message = "Reservation request rejected." }
                    : new ApprovalResult { Success = false, Message = "Unable to reject the reservation request. It may no longer be pending." };
            }
        }
    }

    public class ApprovalResult // Stored result of a staff approval/rejection operation
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
