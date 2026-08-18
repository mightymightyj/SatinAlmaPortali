namespace satinalmaportali2.Services
{
    public class EmailSettings
    {
        public const string SectionName = "EmailSettings";

        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderName { get; set; } = "Tedarik Portalı";
        public string SenderEmail { get; set; } = "hebsatinalmaport001@gmail.com";
        public string SenderPassword { get; set; } = "nqmy ltld obzq snon";
        public bool EnableSsl { get; set; } = true;
    }
}
