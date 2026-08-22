namespace LibraryQA.Models
{
    // Stores one current reservation's details for display purposes
    public class MyReservationDisplayModel
    {
        public int ReservationID { get; set; }
        public int BookID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ReservedOn { get; set; } = string.Empty;
    }
}

