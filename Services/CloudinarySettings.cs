namespace satinalmaportali2.Services
{
    /// <summary>
    /// appsettings.json içindeki "Cloudinary" bölümüne karşılık gelen ayarlar.
    /// Değerler ortam değişkeni ile de geçilebilir (örn. Cloudinary__ApiSecret).
    /// </summary>
    public class CloudinarySettings
    {
        public const string SectionName = "Cloudinary";

        /// <summary>
        /// Cloudinary panelindeki hazır bağlantı dizesi:
        /// cloudinary://&lt;api_key&gt;:&lt;api_secret&gt;@&lt;cloud_name&gt;
        /// Doluysa CloudName/ApiKey/ApiSecret yerine bu kullanılır.
        /// CLOUDINARY_URL ortam değişkeni de otomatik okunur.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        public string CloudName { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public string ApiSecret { get; set; } = string.Empty;

        /// <summary>Yüklenen dosyaların toplanacağı varsayılan klasör.</summary>
        public string DefaultFolder { get; set; } = "satinalmaportali";

        /// <summary>İzin verilen maksimum dosya boyutu (MB).</summary>
        public int MaxFileSizeMb { get; set; } = 5;

        /// <summary>Url alanı boşsa CLOUDINARY_URL ortam değişkenine düşer.</summary>
        public string EffectiveUrl =>
            !string.IsNullOrWhiteSpace(Url)
                ? Url.Trim()
                : Environment.GetEnvironmentVariable("CLOUDINARY_URL")?.Trim() ?? string.Empty;

        public bool IsConfigured =>
            EffectiveUrl.StartsWith("cloudinary://", StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(CloudName) &&
             !string.IsNullOrWhiteSpace(ApiKey) &&
             !string.IsNullOrWhiteSpace(ApiSecret));
    }
}
