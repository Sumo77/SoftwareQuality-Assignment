namespace LibraryQA.Models
{
    // Stores the book details for display purposes (i.e search results)
    public class BookDisplayModel
    {
        public int BookID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}