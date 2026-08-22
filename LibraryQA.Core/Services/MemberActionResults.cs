using System;

namespace LibraryQA.Core.Services
{
    public class BorrowResult // Stored result of a book borrowing operation
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public DateTime? DueDate { get; init; }
    }

    public class ReserveResult // Stored result of a book reservation operation
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}