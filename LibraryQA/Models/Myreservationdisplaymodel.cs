namespace LibraryQA.Models
{
    public class MyReservationDisplayModel
    {
        public int ReservationID { get; set; }
        public int BookID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ReservedOn { get; set; } = string.Empty;
    }
}

