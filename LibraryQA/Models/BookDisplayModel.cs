namespace LibraryQA.Models
{
    // Lightweight DTO used only for binding search results to CatalogueListView.
    // Keeps the WPF view decoupled from DatabaseHelper's raw Dictionary results.
    public class BookDisplayModel
    {
        public int BookID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}