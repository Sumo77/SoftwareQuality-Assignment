namespace LibraryQA.Models
{
    // Stores one current loan's details for display purposes
    public class MyLoanDisplayModel
    {
        public int LoanID { get; set; }
        public int BookID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string BorrowedOn { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}