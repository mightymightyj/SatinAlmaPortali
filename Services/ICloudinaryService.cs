namespace satinalmaportali2.Services
{
    /// <summary>Tek bir görsel yükleme işleminin sonucu.</summary>
    public record CloudinaryUploadResult(bool Success, string? SecureUrl, string? PublicId, string? Error)
    {
        public static CloudinaryUploadResult Fail(string error) => new(false, null, null, error);

        public static CloudinaryUploadResult Ok(string secureUrl, string publicId) => new(true, secureUrl, publicId, null);
    }

    /// <summary>Cloudinary'de duran tek bir görselin özeti.</summary>
    public record CloudinaryImage(string PublicId, string Url, long Bytes, DateTime? CreatedAt);

    /// <summary>
    /// Cloudinary üzerinde görsel yükleme / listeleme / silme işlemleri.
    /// </summary>
    public interface ICloudinaryService
    {
        /// <summary>Cloudinary kimlik bilgileri yapılandırılmış mı?</summary>
        bool IsConfigured { get; }

        /// <summary>Kullanılan cloud adı (teşhis amaçlı).</summary>
        string? CloudName { get; }

        /// <summary>
        /// Kimlik bilgilerini Cloudinary'ye doğrulatır. Dosya yüklemeden bağlantıyı test etmek için.
        /// </summary>
        Task<(bool Ok, string Message)> PingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Bir görseli Cloudinary'ye yükler ve güvenli (https) URL'i döner.
        /// </summary>
        /// <param name="file">Formdan gelen dosya.</param>
        /// <param name="folder">Hedef klasör. Boş bırakılırsa varsayılan klasör kullanılır.</param>
        /// <param name="width">İsteğe bağlı en boy sınırı (piksel). Verilirse görsel bu sınıra küçültülür.</param>
        Task<CloudinaryUploadResult> UploadImageAsync(
            IFormFile file,
            string? folder = null,
            int? width = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Belirtilen klasördeki görselleri listeler. Kaynak doğrudan Cloudinary olduğu için
        /// listenin içeriği her zaman Cloudinary'deki gerçek durumu yansıtır.
        /// </summary>
        Task<IReadOnlyList<CloudinaryImage>> ListImagesAsync(
            string? folder = null,
            int maxResults = 30,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cloudinary'deki bir görseli siler.
        /// </summary>
        /// <param name="publicIdOrUrl">PublicId ya da daha önce dönen secure URL.</param>
        Task<bool> DeleteImageAsync(string? publicIdOrUrl, CancellationToken cancellationToken = default);
    }
}
