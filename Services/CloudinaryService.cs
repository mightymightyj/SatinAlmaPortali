using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace satinalmaportali2.Services
{
    /// <inheritdoc cref="ICloudinaryService"/>
    public class CloudinaryService : ICloudinaryService
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };

        private readonly CloudinarySettings _settings;
        private readonly Cloudinary? _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IOptions<CloudinarySettings> options, ILogger<CloudinaryService> logger)
        {
            _settings = options.Value;
            _logger = logger;

            if (_settings.IsConfigured)
            {
                var url = _settings.EffectiveUrl;

                _cloudinary = url.StartsWith("cloudinary://", StringComparison.OrdinalIgnoreCase)
                    ? new Cloudinary(url)
                    : new Cloudinary(new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret));

                _cloudinary.Api.Secure = true;
            }
            else
            {
                _logger.LogWarning(
                    "Cloudinary kimlik bilgileri eksik. appsettings.json içindeki \"{Section}\" bölümünü doldurun; " +
                    "aksi halde görsel yükleme devre dışı kalır.", CloudinarySettings.SectionName);
            }
        }

        public bool IsConfigured => _cloudinary != null;

        public string? CloudName => _cloudinary?.Api.Account.Cloud;

        public async Task<(bool Ok, string Message)> PingAsync(CancellationToken cancellationToken = default)
        {
            if (_cloudinary == null)
            {
                return (false, "Cloudinary yapılandırılmamış.");
            }

            try
            {
                var result = await _cloudinary.PingAsync(cancellationToken);

                if (result.Error != null)
                {
                    return (false, Explain(result.Error.Message));
                }

                return (true, $"Bağlantı başarılı (cloud: {CloudName}).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloudinary ping hatası.");
                return (false, Explain(ex.Message));
            }
        }

        /// <summary>
        /// Cloudinary'nin ham hata mesajlarını uygulanabilir bir açıklamaya çevirir.
        /// </summary>
        private string Explain(string apiMessage)
        {
            var lower = apiMessage.ToLowerInvariant();

            if (lower.Contains("cloud_name"))
            {
                return $"Bağlantı başarısız: \"{apiMessage}\". Girilen cloud adı (\"{CloudName}\") bu API anahtarına ait değil. " +
                       "Cloudinary Console > Programmable Media > Dashboard > Product Environment Credentials bölümündeki " +
                       "CLOUDINARY_URL satırının tamamını kopyalayıp Cloudinary:Url ayarına yapıştırın.";
            }

            if (lower.Contains("signature") || lower.Contains("api_key") || lower.Contains("unauthorized"))
            {
                return $"Bağlantı başarısız: \"{apiMessage}\". API anahtarı veya secret hatalı görünüyor.";
            }

            return $"Bağlantı başarısız: {apiMessage}";
        }

        public async Task<CloudinaryUploadResult> UploadImageAsync(
            IFormFile file,
            string? folder = null,
            int? width = null,
            CancellationToken cancellationToken = default)
        {
            if (_cloudinary == null)
            {
                return CloudinaryUploadResult.Fail("Cloudinary yapılandırılmamış. Lütfen sistem yöneticisi ile iletişime geçin.");
            }

            if (file == null || file.Length == 0)
            {
                return CloudinaryUploadResult.Fail("Lütfen bir dosya seçin.");
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                return CloudinaryUploadResult.Fail(
                    $"Geçersiz dosya türü. İzin verilen türler: {string.Join(", ", AllowedExtensions)}");
            }

            var maxBytes = (long)_settings.MaxFileSizeMb * 1024 * 1024;
            if (file.Length > maxBytes)
            {
                return CloudinaryUploadResult.Fail($"Dosya boyutu en fazla {_settings.MaxFileSizeMb} MB olabilir.");
            }

            try
            {
                await using var stream = file.OpenReadStream();

                // Dosya adını ve PublicId'yi anonim GUID ile oluştur (Kullanıcı cihaz/dosya adı bilgisi dışarı sızmaz)
                var anonymousId = $"img_{Guid.NewGuid():N}";
                var anonymousFileName = $"{anonymousId}{extension}";

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(anonymousFileName, stream),
                    PublicId = anonymousId,
                    Folder = string.IsNullOrWhiteSpace(folder) ? _settings.DefaultFolder : folder,
                    UseFilename = false,
                    UniqueFilename = false,
                    Overwrite = false
                };

                if (width.HasValue)
                {
                    uploadParams.Transformation = new Transformation()
                        .Width(width.Value)
                        .Crop("limit")
                        .Quality("auto")
                        .FetchFormat("auto");
                }

                var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary yükleme hatası: {Message}", result.Error.Message);
                    return CloudinaryUploadResult.Fail("Görsel yüklenemedi: " + result.Error.Message);
                }

                return CloudinaryUploadResult.Ok(result.SecureUrl.ToString(), result.PublicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloudinary yükleme sırasında beklenmeyen hata.");
                return CloudinaryUploadResult.Fail("Görsel yüklenirken bir hata oluştu.");
            }
        }

        public async Task<IReadOnlyList<CloudinaryImage>> ListImagesAsync(
            string? folder = null,
            int maxResults = 30,
            CancellationToken cancellationToken = default)
        {
            if (_cloudinary == null)
            {
                return Array.Empty<CloudinaryImage>();
            }

            var prefix = string.IsNullOrWhiteSpace(folder) ? _settings.DefaultFolder : folder;

            try
            {
                var result = await _cloudinary.ListResourcesAsync(
                    new ListResourcesByPrefixParams
                    {
                        Prefix = prefix,
                        Type = "upload",
                        ResourceType = ResourceType.Image,
                        MaxResults = maxResults
                    },
                    cancellationToken);

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary listeleme hatası: {Message}", result.Error.Message);
                    return Array.Empty<CloudinaryImage>();
                }

                return result.Resources?
                    .Select(r => new CloudinaryImage(
                        r.PublicId,
                        r.SecureUrl?.ToString() ?? r.Url?.ToString() ?? string.Empty,
                        r.Bytes,
                        DateTime.TryParse(r.CreatedAt, out var createdAt) ? createdAt : null))
                    .ToList() ?? new List<CloudinaryImage>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloudinary listeleme sırasında hata. Prefix: {Prefix}", prefix);
                return Array.Empty<CloudinaryImage>();
            }
        }

        public async Task<bool> DeleteImageAsync(string? publicIdOrUrl, CancellationToken cancellationToken = default)
        {
            if (_cloudinary == null || string.IsNullOrWhiteSpace(publicIdOrUrl))
            {
                return false;
            }

            var publicId = ExtractPublicId(publicIdOrUrl);
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return false;
            }

            try
            {
                var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image,
                    Invalidate = true
                });

                var ok = string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase);
                if (!ok)
                {
                    _logger.LogWarning("Cloudinary silme başarısız. PublicId: {PublicId}, Sonuç: {Result}",
                        publicId, result.Result);
                }

                return ok;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloudinary silme sırasında hata. PublicId: {PublicId}", publicId);
                return false;
            }
        }

        /// <summary>
        /// Secure URL verilmişse içinden PublicId'yi çıkarır; zaten PublicId ise olduğu gibi döner.
        /// Örn. https://res.cloudinary.com/demo/image/upload/v1234/klasor/logo.png -> klasor/logo
        /// </summary>
        private static string? ExtractPublicId(string publicIdOrUrl)
        {
            if (!publicIdOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return publicIdOrUrl;
            }

            if (!Uri.TryCreate(publicIdOrUrl, UriKind.Absolute, out var uri))
            {
                return null;
            }

            var segments = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var uploadIndex = segments.FindIndex(s => s.Equals("upload", StringComparison.OrdinalIgnoreCase));
            if (uploadIndex < 0 || uploadIndex == segments.Count - 1)
            {
                return null;
            }

            var pathParts = segments.Skip(uploadIndex + 1).ToList();

            // Sürüm ("v1234567890") ve dönüşüm ("w_200,c_limit") segmentlerini at.
            while (pathParts.Count > 1 &&
                   (IsVersionSegment(pathParts[0]) || IsTransformationSegment(pathParts[0])))
            {
                pathParts.RemoveAt(0);
            }

            if (pathParts.Count == 0)
            {
                return null;
            }

            var last = pathParts[^1];
            var dotIndex = last.LastIndexOf('.');
            if (dotIndex > 0)
            {
                pathParts[^1] = last[..dotIndex];
            }

            return Uri.UnescapeDataString(string.Join('/', pathParts));
        }

        private static bool IsVersionSegment(string segment) =>
            segment.Length > 1 && segment[0] == 'v' && segment[1..].All(char.IsDigit);

        private static bool IsTransformationSegment(string segment) =>
            segment.Contains(',') && segment.Contains('_');
    }
}
