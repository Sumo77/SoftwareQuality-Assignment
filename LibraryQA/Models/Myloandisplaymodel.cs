namespace LibraryQA.Models
{
    public class MyLoanDisplayModel
    {
        public int LoanID { get; set; }
        public int BookID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string BorrowedOn { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;

        // Derived from DatabaseHelper's IsOverdue flag — "On Loan" or "Overdue".
        public string Status { get; set; } = string.Empty;
    }
}