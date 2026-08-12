namespace satinalmaportali2.Models
{
    public class Requests
    {
        public int? RequestID { get; set; }
        public DateTime? RequestDate { get; set; }
        public int? SenderID { get; set;}
        public int? RecieverID { get; set;}
        public string? RequestType { get; set;}
    }
}