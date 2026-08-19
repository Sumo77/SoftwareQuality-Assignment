using System;

namespace LibraryQA.Core.Services
{
    public class BorrowResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public DateTime? DueDate { get; init; }
    }

    public class ReserveResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}