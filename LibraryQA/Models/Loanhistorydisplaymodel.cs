namespace LibraryQA.Models
{
    // Stores the loan history information for display purposes
    public class LoanHistoryDisplayModel
    {
        public int LoanID { get; set; }
        public int BookID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string BorrowedOn { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public string ReturnedOn { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
    }
}