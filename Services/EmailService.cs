using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace satinalmaportali2.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService>? _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService>? logger = null)
        {
            _settings = settings?.Value ?? new EmailSettings();
            _logger = logger;
        }

        /// <summary>
        /// Kullanıcıya şifre sıfırlama bağlantısı içeren modern HTML e-posta gönderir.
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(resetLink))
                {
                    _logger?.LogWarning("Şifre sıfırlama e-postası için e-posta adresi veya link boş olamaz.");
                    return false;
                }

                var htmlBody = $@"
<!DOCTYPE html>
<html lang='tr' xmlns='http://www.w3.org/1999/xhtml'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta name='color-scheme' content='light'>
    <meta name='supported-color-schemes' content='light'>
    <title>Şifre Sıfırlama</title>
    <!--[if mso]>
    <style type='text/css'>
        table, td {{ font-family: Arial, Helvetica, sans-serif !important; }}
    </style>
    <![endif]-->
</head>
<body style='margin:0;padding:0;background-color:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,""Helvetica Neue"",Arial,sans-serif;-webkit-font-smoothing:antialiased;'>
    <!-- Preheader (gizli önizleme metni) -->
    <div style='display:none;font-size:1px;color:#f1f5f9;line-height:1px;max-height:0px;max-width:0px;opacity:0;overflow:hidden;'>
        Hesabınız için şifre sıfırlama talebinde bulunuldu. Bağlantıya tıklayarak yeni şifrenizi belirleyebilirsiniz.
    </div>

    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f1f5f9;'>
        <tr>
            <td align='center' style='padding:48px 16px;'>
                <!-- Ana Kart -->
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:580px;background-color:#ffffff;border-radius:20px;box-shadow:0 8px 40px rgba(0,0,0,0.06);overflow:hidden;'>
                    
                    <!-- Header Gradient -->
                    <tr>
                        <td style='background:linear-gradient(135deg,#1e40af 0%,#3b82f6 50%,#60a5fa 100%);padding:40px 48px 36px;text-align:center;'>
                            <!-- Logo Alanı -->
                            <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='margin:0 auto 14px;'>
                                <tr>
                                    <td style='background:rgba(255,255,255,0.15);border-radius:14px;padding:12px 16px;'>
                                        <table role='presentation' cellpadding='0' cellspacing='0' border='0'>
                                            <tr>
                                                <td style='vertical-align:middle;padding-right:8px;'>
                                                    <div style='width:28px;height:28px;background:#fff;border-radius:8px;text-align:center;line-height:28px;font-size:15px;'>🔒</div>
                                                </td>
                                                <td style='vertical-align:middle;'>
                                                    <span style='color:#ffffff;font-size:18px;font-weight:800;letter-spacing:-0.3px;'>tedarik.portal</span>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            <h1 style='margin:0;color:#ffffff;font-size:24px;font-weight:800;letter-spacing:-0.5px;line-height:1.3;'>
                                Şifre Sıfırlama Talebi
                            </h1>
                            <p style='margin:10px 0 0;color:rgba(255,255,255,0.8);font-size:14px;font-weight:400;'>
                                Hesabınızın güvenliği bizim için önemlidir
                            </p>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style='padding:40px 48px 32px;'>
                            <!-- Selamlama -->
                            <p style='margin:0 0 20px;color:#1e293b;font-size:16px;font-weight:600;line-height:1.5;'>
                                Merhaba,
                            </p>
                            <p style='margin:0 0 28px;color:#475569;font-size:15px;line-height:1.7;'>
                                Hesabınız için bir şifre sıfırlama talebinde bulunuldu. Yeni şifrenizi belirlemek için aşağıdaki butona tıklayın:
                            </p>

                            <!-- CTA Butonu -->
                            <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='margin:0 auto 28px;'>
                                <tr>
                                    <td style='border-radius:12px;background:linear-gradient(135deg,#1e40af,#3b82f6);box-shadow:0 4px 16px rgba(59,130,246,0.35);'>
                                        <a href='{resetLink}' target='_blank' style='display:inline-block;padding:16px 48px;color:#ffffff;font-size:16px;font-weight:700;text-decoration:none;letter-spacing:0.3px;'>
                                            🔐 Şifremi Sıfırla
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Süre Bilgisi -->
                            <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='width:100%;margin-bottom:24px;'>
                                <tr>
                                    <td style='background:#fef3c7;border:1px solid #fde68a;border-radius:10px;padding:14px 18px;'>
                                        <table role='presentation' cellpadding='0' cellspacing='0' border='0'>
                                            <tr>
                                                <td style='vertical-align:top;padding-right:10px;font-size:16px;'>⏱️</td>
                                                <td style='color:#92400e;font-size:13px;line-height:1.5;font-weight:500;'>
                                                    Bu bağlantı <strong>30 dakika</strong> süreyle geçerlidir. Süre dolduktan sonra yeni bir sıfırlama talebi oluşturmanız gerekecektir.
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Link Kopyalama -->
                            <p style='margin:0 0 8px;color:#94a3b8;font-size:12px;'>Buton çalışmıyorsa aşağıdaki bağlantıyı tarayıcınıza yapıştırın:</p>
                            <p style='margin:0 0 24px;word-break:break-all;'>
                                <a href='{resetLink}' style='color:#3b82f6;font-size:12px;text-decoration:underline;'>{resetLink}</a>
                            </p>

                            <hr style='border:none;border-top:1px solid #e2e8f0;margin:0 0 20px;' />

                            <!-- Güvenlik Notu -->
                            <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='width:100%;'>
                                <tr>
                                    <td style='background:#f0fdf4;border:1px solid #bbf7d0;border-radius:10px;padding:14px 18px;'>
                                        <table role='presentation' cellpadding='0' cellspacing='0' border='0'>
                                            <tr>
                                                <td style='vertical-align:top;padding-right:10px;font-size:16px;'>🛡️</td>
                                                <td style='color:#166534;font-size:13px;line-height:1.5;font-weight:500;'>
                                                    Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz. Hesabınız güvende kalacaktır.
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background-color:#f8fafc;padding:24px 48px;text-align:center;border-top:1px solid #e2e8f0;'>
                            <p style='margin:0 0 6px;color:#64748b;font-size:12px;font-weight:600;'>
                                Tedarik Portal — Satın Alma Portalı
                            </p>
                            <p style='margin:0;color:#94a3b8;font-size:11px;'>
                                &copy; 2026 Tüm hakları saklıdır.
                            </p>
                        </td>
                    </tr>
                </table>

                <!-- Alt Bilgi -->
                <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='max-width:580px;margin-top:16px;'>
                    <tr>
                        <td style='text-align:center;'>
                            <p style='margin:0;color:#94a3b8;font-size:11px;line-height:1.5;'>
                                Bu e-posta <strong>{toEmail}</strong> adresine gönderilmiştir.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(new MailboxAddress("", toEmail.Trim()));
                message.Subject = "🔐 Tedarik Portal — Şifre Sıfırlama Bağlantınız";
                message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

                using var client = new SmtpClient();
                
                // Gmail SSL/TLS Sertifika Doğrulaması
                await client.ConnectAsync(
                    _settings.SmtpServer,
                    _settings.SmtpPort,
                    _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto
                );

                // Boşlukları temizlenmiş şifre ile kimlik doğrulama
                string cleanPassword = _settings.SenderPassword.Replace(" ", "");
                await client.AuthenticateAsync(_settings.SenderEmail, cleanPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger?.LogInformation("Şifre sıfırlama e-postası başarıyla gönderildi: {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Gmail SMTP e-posta gönderim hatası ({ToEmail}): {Message}", toEmail, ex.Message);
                Console.WriteLine($"Gmail SMTP E-posta Gönderim Hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// İletişim formu mesajlarını şirket/yönetici adresine iletir.
        /// </summary>
        public async Task<bool> SendContactMessageEmailAsync(string targetEmail, string fullName, string senderEmail, string? phone, string? topic, string messageContent)
        {
            try
            {
                var finalTarget = string.IsNullOrWhiteSpace(targetEmail) ? _settings.SenderEmail : targetEmail.Trim();

                var topicDisplay = topic switch
                {
                    "genel" => "Genel Bilgi",
                    "destek" => "Teknik Destek",
                    "satis" => "Satış & Fiyatlandırma",
                    "isbirligi" => "İş Birliği Teklifi",
                    "sikayet" => "Şikayet & Öneri",
                    _ => string.IsNullOrWhiteSpace(topic) ? "Genel" : topic
                };

                var sendTime = DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm");

                var htmlBody = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Yeni İletişim Mesajı</title>
</head>
<body style='margin:0;padding:0;background-color:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,""Helvetica Neue"",Arial,sans-serif;'>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f1f5f9;'>
        <tr>
            <td align='center' style='padding:40px 16px;'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:600px;background-color:#ffffff;border-radius:20px;box-shadow:0 8px 30px rgba(0,0,0,0.06);overflow:hidden;'>
                    
                    <!-- Header -->
                    <tr>
                        <td style='background:linear-gradient(135deg,#1e40af 0%,#2563eb 100%);padding:32px 40px;text-align:left;'>
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                                <tr>
                                    <td>
                                        <span style='background:rgba(255,255,255,0.2);color:#ffffff;font-size:12px;font-weight:700;padding:4px 12px;border-radius:20px;text-transform:uppercase;letter-spacing:0.5px;'>
                                            📬 Web İletişim Formu
                                        </span>
                                        <h1 style='margin:12px 0 4px;color:#ffffff;font-size:22px;font-weight:800;'>
                                            Yeni Bir Mesajınız Var!
                                        </h1>
                                        <p style='margin:0;color:rgba(255,255,255,0.85);font-size:13px;'>
                                            Tedarik Portalı iletişim formundan yeni bir talep iletildi.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style='padding:36px 40px;'>
                            
                            <!-- Bilgi Tablosu -->
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:24px;border-collapse:separate;border-spacing:0 8px;'>
                                <tr>
                                    <td style='width:130px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>👤 Gönderen:</td>
                                    <td style='color:#0f172a;font-size:14px;font-weight:700;padding:8px 12px;background:#f8fafc;border-radius:8px;'>{fullName}</td>
                                </tr>
                                <tr>
                                    <td style='width:130px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>✉️ E-posta:</td>
                                    <td style='color:#2563eb;font-size:14px;font-weight:600;padding:8px 12px;background:#f8fafc;border-radius:8px;'><a href='mailto:{senderEmail}' style='color:#2563eb;text-decoration:none;'>{senderEmail}</a></td>
                                </tr>
                                <tr>
                                    <td style='width:130px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>📞 Telefon:</td>
                                    <td style='color:#0f172a;font-size:14px;font-weight:600;padding:8px 12px;background:#f8fafc;border-radius:8px;'>{(string.IsNullOrWhiteSpace(phone) ? "Belirtilmedi" : phone)}</td>
                                </tr>
                                <tr>
                                    <td style='width:130px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>🏷️ Konu:</td>
                                    <td style='color:#0f172a;font-size:14px;font-weight:700;padding:8px 12px;background:#f8fafc;border-radius:8px;'>{topicDisplay}</td>
                                </tr>
                                <tr>
                                    <td style='width:130px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>🕒 Tarih:</td>
                                    <td style='color:#64748b;font-size:13px;padding:8px 12px;background:#f8fafc;border-radius:8px;'>{sendTime}</td>
                                </tr>
                            </table>

                            <!-- Mesaj Kutusu -->
                            <div style='margin-top:20px;padding:20px;background:#f8fafc;border:1px solid #e2e8f0;border-left:4px solid #2563eb;border-radius:12px;'>
                                <div style='color:#475569;font-size:12px;font-weight:800;text-transform:uppercase;margin-bottom:8px;letter-spacing:0.5px;'>Mesaj İçeriği:</div>
                                <p style='margin:0;color:#1e293b;font-size:14px;line-height:1.7;white-space:pre-wrap;'>{messageContent}</p>
                            </div>

                            <!-- Hızlı Yanıtla Butonu -->
                            <div style='margin-top:28px;text-align:center;'>
                                <a href='mailto:{senderEmail}?subject=Re: {topicDisplay} - Tedarik Portalı' style='display:inline-block;padding:12px 32px;background:#2563eb;color:#ffffff;font-size:14px;font-weight:700;text-decoration:none;border-radius:10px;box-shadow:0 4px 12px rgba(37,99,235,0.3);'>
                                    ✉️ Gönderene Yanıt Ver
                                </a>
                            </div>

                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background:#f8fafc;padding:20px 40px;text-align:center;border-top:1px solid #e2e8f0;'>
                            <p style='margin:0;color:#94a3b8;font-size:12px;'>
                                Bu bildirim <strong>{finalTarget}</strong> adresine gönderilmiştir.
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(new MailboxAddress("", finalTarget));
                
                // Cevapla dendiğinde direkt formu dolduran kişiye gitmesi için Reply-To ekle
                if (!string.IsNullOrWhiteSpace(senderEmail))
                {
                    message.ReplyTo.Add(new MailboxAddress(fullName, senderEmail.Trim()));
                }

                message.Subject = $"📬 İletişim Mesajı: {topicDisplay} — {fullName}";
                message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

                using var client = new SmtpClient();
                
                await client.ConnectAsync(
                    _settings.SmtpServer,
                    _settings.SmtpPort,
                    _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto
                );

                string cleanPassword = _settings.SenderPassword.Replace(" ", "");
                await client.AuthenticateAsync(_settings.SenderEmail, cleanPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger?.LogInformation("İletişim formu e-postası başarıyla iletildi: {Target}", finalTarget);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "İletişim e-posta gönderim hatası ({Target}): {Message}", targetEmail, ex.Message);
                Console.WriteLine($"İletişim E-posta Gönderim Hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// MailKit üzerinden HTML e-posta gönderir.
        /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger?.LogWarning("E-posta gönderimi başarısız: Alıcı e-posta adresi boş.");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(new MailboxAddress("", toEmail.Trim()));
                message.Subject = subject;
                message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

                using var client = new SmtpClient();

                await client.ConnectAsync(
                    _settings.SmtpServer,
                    _settings.SmtpPort,
                    _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto
                );

                string cleanPassword = _settings.SenderPassword.Replace(" ", "");
                await client.AuthenticateAsync(_settings.SenderEmail, cleanPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger?.LogInformation("E-posta başarıyla gönderildi: {Email} (Konu: {Subject})", toEmail, subject);
                Console.WriteLine($"✅ MailKit SMTP e-posta başarıyla gönderildi -> {toEmail} (Konu: {subject})");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Gmail SMTP e-posta gönderim hatası ({ToEmail}): {Message}", toEmail, ex.Message);
                Console.WriteLine($"❌ MailKit SMTP E-posta gönderim hatası ({toEmail}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Satın alma talebine gelen yeni fiyat teklifi için zengin HTML e-posta içeriği üretir (Tekil veya Çok Kalemli Destekli).
        /// </summary>
        public string GetOfferReceivedEmailHtml(
            string toEmail,
            string buyerName,
            string supplierName,
            string rfqCode,
            string productName,
            string? productImageUrl,
            int quantity,
            string unit,
            decimal offerPrice,
            int? deliveryDays,
            int? validityDays,
            string? offerNotes,
            string requestsUrl,
            List<OfferItemEmailDto>? items = null)
        {
            var sendTime = DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm");
            var formattedTotalPrice = offerPrice.ToString("N2", new System.Globalization.CultureInfo("tr-TR"));
            var deliveryText = deliveryDays.HasValue && deliveryDays.Value > 0 ? $"{deliveryDays.Value} İş Günü" : "Hemen Teslim";
            var validityText = validityDays.HasValue && validityDays.Value > 0 ? $"{validityDays.Value} Gün" : "30 Gün";
            var displayUnit = string.IsNullOrWhiteSpace(unit) ? "Adet" : unit;
            var displayBuyer = string.IsNullOrWhiteSpace(buyerName) ? "Değerli Müşterimiz" : buyerName;
            var displaySupplier = string.IsNullOrWhiteSpace(supplierName) ? "Onaylı Tedarikçi Firma" : supplierName;
            var displayRfq = string.IsNullOrWhiteSpace(rfqCode) ? "#TALEP-2026" : (rfqCode.StartsWith("#") ? rfqCode : $"#{rfqCode}");

            bool hasMultiItems = items != null && items.Any();

            // 1. Kalemler Tablosu (Varsa)
            string itemsTableHtml = "";
            if (hasMultiItems && items != null)
            {
                var rowsSb = new System.Text.StringBuilder();
                int idx = 1;
                foreach (var it in items)
                {
                    var itName = string.IsNullOrWhiteSpace(it.ItemName) ? $"Kalem #{idx}" : it.ItemName;
                    var itQty = it.Quantity.ToString("0.##", new System.Globalization.CultureInfo("tr-TR"));
                    var itUnit = string.IsNullOrWhiteSpace(it.Unit) ? "Adet" : it.Unit;
                    var itUnitPrice = it.UnitPrice.ToString("N2", new System.Globalization.CultureInfo("tr-TR"));
                    var itLineTotal = (it.LineTotal > 0 ? it.LineTotal : (it.UnitPrice * it.Quantity)).ToString("N2", new System.Globalization.CultureInfo("tr-TR"));

                    string itDescPart = !string.IsNullOrWhiteSpace(it.ItemDescription) 
                        ? $"<div style='color:#64748b;font-size:12px;margin-top:3px;'>📝 {it.ItemDescription}</div>" 
                        : "";
                    string itNotePart = !string.IsNullOrWhiteSpace(it.Notes) 
                        ? $"<div style='color:#2563eb;font-size:12px;margin-top:3px;font-style:italic;'>💬 Satıcı Notu: {it.Notes}</div>" 
                        : "";

                    rowsSb.Append($@"
                        <tr style='border-bottom:1px solid #e2e8f0;'>
                            <td style='padding:12px 14px;font-size:13px;font-weight:700;color:#64748b;vertical-align:top;text-align:center;'>{idx}</td>
                            <td style='padding:12px 14px;font-size:13.5px;color:#0f172a;vertical-align:top;'>
                                <strong style='color:#0f172a;'>{itName}</strong>
                                {itDescPart}
                                {itNotePart}
                            </td>
                            <td style='padding:12px 14px;font-size:13px;color:#1e293b;text-align:center;vertical-align:top;white-space:nowrap;'>
                                <span style='display:inline-block;background:#eff6ff;color:#1d4ed8;padding:4px 10px;border-radius:6px;font-weight:700;border:1px solid #bfdbfe;'>
                                    {itQty} {itUnit}
                                </span>
                            </td>
                            <td style='padding:12px 14px;font-size:13.5px;color:#0f172a;text-align:right;font-weight:600;vertical-align:top;white-space:nowrap;'>
                                {itUnitPrice} ₺
                            </td>
                            <td style='padding:12px 14px;font-size:13.5px;color:#15803d;text-align:right;font-weight:800;vertical-align:top;white-space:nowrap;'>
                                {itLineTotal} ₺
                            </td>
                        </tr>");
                    idx++;
                }

                itemsTableHtml = $@"
                    <div style='margin-bottom:24px;'>
                        <div style='color:#0f172a;font-size:15px;font-weight:800;margin-bottom:10px;letter-spacing:-0.2px;'>
                            📋 Kalem Bazlı Teklif Fiyat Dökümü ({items.Count} Kalem / Malzeme)
                        </div>
                        <div style='border:1.5px solid #e2e8f0;border-radius:14px;overflow:hidden;background:#ffffff;box-shadow:0 2px 8px rgba(0,0,0,0.02);'>
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='border-collapse:collapse;width:100%;'>
                                <thead>
                                    <tr style='background:#f8fafc;border-bottom:2px solid #e2e8f0;'>
                                        <th style='padding:10px 14px;text-align:center;color:#475569;font-size:11.5px;font-weight:800;text-transform:uppercase;letter-spacing:0.5px;width:35px;'>#</th>
                                        <th style='padding:10px 14px;text-align:left;color:#475569;font-size:11.5px;font-weight:800;text-transform:uppercase;letter-spacing:0.5px;'>Malzeme / Kalem</th>
                                        <th style='padding:10px 14px;text-align:center;color:#475569;font-size:11.5px;font-weight:800;text-transform:uppercase;letter-spacing:0.5px;width:95px;'>Miktar</th>
                                        <th style='padding:10px 14px;text-align:right;color:#475569;font-size:11.5px;font-weight:800;text-transform:uppercase;letter-spacing:0.5px;width:115px;'>Birim Fiyat</th>
                                        <th style='padding:10px 14px;text-align:right;color:#475569;font-size:11.5px;font-weight:800;text-transform:uppercase;letter-spacing:0.5px;width:125px;'>Kalem Tutarı</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {rowsSb}
                                </tbody>
                                <tfoot>
                                    <tr style='background:#f0fdf4;border-top:2px solid #86efac;'>
                                        <td colspan='4' style='padding:12px 14px;text-align:right;font-size:13px;font-weight:800;color:#166534;'>
                                            GENEL TEKLİF TOPLAMI:
                                        </td>
                                        <td style='padding:12px 14px;text-align:right;font-size:16px;font-weight:900;color:#15803d;white-space:nowrap;'>
                                            {formattedTotalPrice} ₺
                                        </td>
                                    </tr>
                                </tfoot>
                            </table>
                        </div>
                    </div>";
            }

            // 2. Üst Özet Kartı (Tek Kalem vs Çok Kalem)
            string productHeaderCardHtml;
            if (hasMultiItems && items != null && items.Count > 1)
            {
                productHeaderCardHtml = $@"
                    <div style='background:#ffffff;border:1.5px solid #e2e8f0;border-radius:18px;padding:22px 24px;margin-bottom:24px;box-shadow:0 4px 16px rgba(0,0,0,0.03);text-align:center;'>
                        <span style='display:inline-block;padding:5px 14px;background:#eff6ff;color:#2563eb;font-size:12px;font-weight:800;border-radius:20px;border:1px solid #bfdbfe;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:8px;'>
                            📋 Çok Kalemli Satın Alma Talebi
                        </span>
                        <h2 style='margin:0 0 8px;color:#0f172a;font-size:20px;font-weight:800;line-height:1.35;'>
                            {productName}
                        </h2>
                        <div style='color:#64748b;font-size:13.5px;'>
                            Bu talep toplam <strong>{items.Count} farklı malzeme kalemini</strong> içermektedir.
                        </div>
                    </div>";
            }
            else
            {
                string singleImgPart = !string.IsNullOrWhiteSpace(productImageUrl)
                    ? $"<div style='text-align:center;margin-bottom:16px;'><img src='{productImageUrl}' alt='{productName}' style='max-width:100%;max-height:220px;object-fit:cover;border-radius:12px;border:1px solid #e2e8f0;' /></div>"
                    : "<div style='background:linear-gradient(135deg,#eff6ff 0%,#dbeafe 100%);border:1.5px dashed #93c5fd;border-radius:14px;padding:24px 20px;text-align:center;margin-bottom:16px;'><div style='font-size:32px;margin-bottom:6px;'>📦</div><span style='color:#1e40af;font-size:13px;font-weight:800;text-transform:uppercase;'>Satın Alma Ürün Talebi</span></div>";

                productHeaderCardHtml = $@"
                    <div style='background:#ffffff;border:1.5px solid #e2e8f0;border-radius:18px;padding:24px;margin-bottom:24px;box-shadow:0 4px 16px rgba(0,0,0,0.03);'>
                        {singleImgPart}
                        <h2 style='margin:0 0 8px;color:#0f172a;font-size:19px;font-weight:800;line-height:1.35;text-align:center;'>
                            {productName}
                        </h2>
                        <div style='text-align:center;margin-top:12px;'>
                            <span style='display:inline-block;padding:6px 14px;background:#eff6ff;color:#2563eb;font-size:13px;font-weight:700;border-radius:8px;border:1px solid #bfdbfe;margin:0 4px 6px;'>
                                📦 Talep Miktarı: <strong>{quantity} {displayUnit}</strong>
                            </span>
                            <span style='display:inline-block;padding:6px 14px;background:#f8fafc;color:#475569;font-size:13px;font-weight:700;border-radius:8px;border:1px solid #e2e8f0;margin:0 4px 6px;'>
                                🏢 Tedarikçi: <strong>{displaySupplier}</strong>
                            </span>
                        </div>
                    </div>";
            }

            // Satıcı notu HTML yapısı
            string notesHtml = "";
            if (!string.IsNullOrWhiteSpace(offerNotes))
            {
                notesHtml = $@"
                    <div style='margin-top:20px;padding:16px 20px;background:#f8fafc;border:1px solid #e2e8f0;border-left:4px solid #2563eb;border-radius:10px;'>
                        <div style='color:#475569;font-size:12px;font-weight:800;text-transform:uppercase;margin-bottom:6px;letter-spacing:0.5px;'>
                            📝 Satıcı Açıklaması & Notu:
                        </div>
                        <p style='margin:0;color:#1e293b;font-size:14px;line-height:1.6;white-space:pre-wrap;'>{offerNotes}</p>
                    </div>";
            }

            return $@"
<!DOCTYPE html>
<html lang='tr' xmlns='http://www.w3.org/1999/xhtml'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta name='color-scheme' content='light'>
    <meta name='supported-color-schemes' content='light'>
    <title>Talebinize Yeni Teklif Geldi</title>
    <!--[if mso]>
    <style type='text/css'>
        table, td {{ font-family: Arial, Helvetica, sans-serif !important; }}
    </style>
    <![endif]-->
</head>
<body style='margin:0;padding:0;background-color:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,""Helvetica Neue"",Arial,sans-serif;-webkit-font-smoothing:antialiased;'>
    
    <!-- Preheader Gizli Önizleme Metni -->
    <div style='display:none;font-size:1px;color:#f1f5f9;line-height:1px;max-height:0px;max-width:0px;opacity:0;overflow:hidden;'>
        {displaySupplier} firmasından '{productName}' talebiniz için {formattedTotalPrice} ₺ tutarında yeni bir fiyat teklifi iletildi.
    </div>

    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f1f5f9;'>
        <tr>
            <td align='center' style='padding:40px 16px;'>
                
                <!-- Ana Konteyner Kart -->
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:640px;background-color:#ffffff;border-radius:24px;box-shadow:0 10px 40px rgba(0,0,0,0.07);border:1px solid #e2e8f0;overflow:hidden;'>
                    
                    <!-- Header Banner (Gradient) -->
                    <tr>
                        <td style='background:linear-gradient(135deg,#1e40af 0%,#2563eb 50%,#3b82f6 100%);padding:36px 44px;text-align:center;'>
                            
                            <!-- Logo Rozeti -->
                            <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='margin:0 auto 14px;'>
                                <tr>
                                    <td style='background:rgba(255,255,255,0.18);border-radius:14px;padding:10px 18px;'>
                                        <table role='presentation' cellpadding='0' cellspacing='0' border='0'>
                                             <tr>
                                                <td style='vertical-align:middle;padding-right:8px;'>
                                                    <div style='width:28px;height:28px;background:#ffffff;border-radius:8px;text-align:center;line-height:28px;font-size:16px;'>
                                                        🏷️
                                                    </div>
                                                </td>
                                                <td style='vertical-align:middle;'>
                                                    <span style='color:#ffffff;font-size:17px;font-weight:800;letter-spacing:-0.3px;'>
                                                        tedarik.portal
                                                    </span>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Başlık -->
                            <h1 style='margin:0;color:#ffffff;font-size:24px;font-weight:800;letter-spacing:-0.5px;line-height:1.3;'>
                                🎉 Yeni Fiyat Teklifi Geldi!
                            </h1>
                            <p style='margin:10px 0 0;color:rgba(255,255,255,0.9);font-size:14px;font-weight:400;'>
                                Satın alma talebiniz için {displaySupplier} detaylı fiyat teklifi sundu
                            </p>

                            <!-- Talep Kodu Rozeti -->
                            <div style='margin-top:14px;'>
                                <span style='display:inline-block;background:rgba(255,255,255,0.22);color:#ffffff;font-size:12px;font-weight:700;padding:5px 14px;border-radius:20px;letter-spacing:0.5px;'>
                                    TALEP: {displayRfq}
                                </span>
                            </div>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style='padding:36px 44px 32px;'>
                            
                            <!-- Selamlama -->
                            <p style='margin:0 0 14px;color:#1e293b;font-size:16px;font-weight:700;'>
                                Merhaba {displayBuyer},
                            </p>
                            <p style='margin:0 0 24px;color:#475569;font-size:14.5px;line-height:1.7;'>
                                Oluşturmuş olduğunuz <strong>{displayRfq}</strong> numaralı satın alma talebi için <strong>{displaySupplier}</strong> tarafından yeni bir fiyat teklifi hazırlanarak iletilmiştir:
                            </p>

                            <!-- Üst Özet Kartı -->
                            {productHeaderCardHtml}

                            <!-- FİYAT VURGULAMA KART (Yeşil / Premium Highlight) -->
                            <div style='background:linear-gradient(135deg,#f0fdf4 0%,#dcfce7 100%);border:1.5px solid #86efac;border-radius:16px;padding:22px 24px;margin-bottom:24px;text-align:center;box-shadow:0 4px 16px rgba(22,163,74,0.1);'>
                                <div style='color:#166534;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:0.6px;margin-bottom:6px;'>
                                    💰 Toplam Teklif Fiyatı
                                </div>
                                <div style='color:#15803d;font-size:34px;font-weight:900;letter-spacing:-0.5px;line-height:1.1;'>
                                    {formattedTotalPrice} ₺
                                </div>
                                {(!hasMultiItems && quantity > 0 ? $"<div style='color:#166534;font-size:13px;font-weight:600;margin-top:6px;'>(Birim Fiyat: ~{(offerPrice / quantity).ToString("N2", new System.Globalization.CultureInfo("tr-TR"))} ₺ / {displayUnit})</div>" : "")}
                            </div>

                            <!-- KALEM BAZLI TABLO (Varsa) -->
                            {itemsTableHtml}

                            <!-- TEKLİF DETAYLARI TABLOSU -->
                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:24px;border-collapse:separate;border-spacing:0 8px;'>
                                <tr>
                                    <td style='width:160px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>🏢 Teklif Veren:</td>
                                    <td style='color:#0f172a;font-size:14px;font-weight:700;padding:8px 14px;background:#f8fafc;border-radius:8px;'>{displaySupplier}</td>
                                </tr>
                                <tr>
                                    <td style='width:160px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>🚚 Teslimat Süresi:</td>
                                    <td style='color:#0f172a;font-size:14px;font-weight:700;padding:8px 14px;background:#f8fafc;border-radius:8px;'>{deliveryText}</td>
                                </tr>
                                <tr>
                                    <td style='width:160px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>⏱️ Geçerlilik Süresi:</td>
                                    <td style='color:#0f172a;font-size:14px;font-weight:700;padding:8px 14px;background:#f8fafc;border-radius:8px;'>{validityText}</td>
                                </tr>
                                <tr>
                                    <td style='width:160px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>🕒 Teklif Tarihi:</td>
                                    <td style='color:#64748b;font-size:13px;font-weight:600;padding:8px 14px;background:#f8fafc;border-radius:8px;'>{sendTime}</td>
                                </tr>
                            </table>

                            <!-- Satıcı Notu (Varsa) -->
                            {notesHtml}

                            <!-- CTA BUTONU (Taleplerim Sayfasına Yönlendirme) -->
                            <div style='margin:32px 0 24px;text-align:center;'>
                                <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='margin:0 auto;'>
                                    <tr>
                                        <td align='center' bgcolor='#2563eb' style='border-radius:14px;background-color:#2563eb;box-shadow:0 6px 20px rgba(37,99,235,0.38);'>
                                            <a href='{requestsUrl}' target='_blank' style='display:inline-block;padding:16px 40px;background-color:#2563eb;color:#ffffff;font-size:15.5px;font-weight:800;text-decoration:none;border-radius:14px;letter-spacing:0.3px;'>
                                                📋 Teklifi İncele ve Değerlendir &rarr;
                                            </a>
                                        </td>
                                    </tr>
                                </table>
                            </div>

                            <!-- Bilgilendirme / İpucu Kutusu -->
                            <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='width:100%;margin-top:18px;'>
                                <tr>
                                    <td style='background:#f0fdf4;border:1px solid #bbf7d0;border-radius:12px;padding:14px 18px;'>
                                        <table role='presentation' cellpadding='0' cellspacing='0' border='0'>
                                            <tr>
                                                <td style='vertical-align:top;padding-right:10px;font-size:16px;'>💡</td>
                                                <td style='color:#166534;font-size:13px;line-height:1.5;font-weight:500;'>
                                                    <strong>Sonraki Adım:</strong> Taleplerim sayfasına giderek bu teklifi onaylayabilir, reddedebilir veya diğer gelen tekliflerle karşılaştırabilirsiniz.
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Link Kopyalama -->
                            <div style='margin-top:20px;text-align:center;'>
                                <p style='margin:0 0 6px;color:#94a3b8;font-size:11.5px;'>
                                    Buton çalışmıyorsa aşağıdaki bağlantıyı tarayıcınıza yapıştırın:
                                </p>
                                <p style='margin:0;word-break:break-all;'>
                                    <a href='{requestsUrl}' target='_blank' style='color:#3b82f6;font-size:11.5px;text-decoration:underline;'>{requestsUrl}</a>
                                </p>
                            </div>

                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background-color:#f8fafc;padding:24px 44px;text-align:center;border-top:1px solid #e2e8f0;'>
                            <p style='margin:0 0 6px;color:#64748b;font-size:12px;font-weight:700;'>
                                Tedarik Portal — Satın Alma ve Tedarik Zinciri Platformu
                            </p>
                            <p style='margin:0 0 10px;color:#94a3b8;font-size:11px;'>
                                &copy; 2026 Tedarik Portalı. Tüm hakları saklıdır.
                            </p>
                            <p style='margin:0;color:#94a3b8;font-size:11px;'>
                                Bu bildirim <strong>{toEmail}</strong> alıcı adresine gönderilmiştir.
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        /// <summary>
        /// Satın alma talebine yeni teklif geldiğinde alıcıya HTML e-posta bildirimi gönderir (Kalem listesi destekli).
        /// </summary>
        public async Task<bool> SendOfferReceivedEmailAsync(
            string toEmail,
            string buyerName,
            string supplierName,
            string rfqCode,
            string productName,
            string? productImageUrl,
            int quantity,
            string unit,
            decimal offerPrice,
            int? deliveryDays,
            int? validityDays,
            string? offerNotes,
            string requestsUrl,
            List<OfferItemEmailDto>? items = null)
        {
            try
            {
                var htmlBody = GetOfferReceivedEmailHtml(
                    toEmail,
                    buyerName,
                    supplierName,
                    rfqCode,
                    productName,
                    productImageUrl,
                    quantity,
                    unit,
                    offerPrice,
                    deliveryDays,
                    validityDays,
                    offerNotes,
                    requestsUrl,
                    items);

                var formattedPrice = offerPrice.ToString("N0", new System.Globalization.CultureInfo("tr-TR"));
                var displayRfq = string.IsNullOrWhiteSpace(rfqCode) ? "Talep" : rfqCode;
                var subject = $"🎉 Yeni Teklif: {productName} ({displayRfq}) — {formattedPrice} ₺";

                return await SendEmailAsync(toEmail, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Teklif e-posta gönderim hatası ({ToEmail}): {Message}", toEmail, ex.Message);
                Console.WriteLine($"Teklif e-posta gönderim hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Teklifi kabul edilen kazanan tedarikçiye tebrik ve süreç tamamlama e-postası gönderir (Kalem listesi destekli).
        /// </summary>
        public async Task<bool> SendOfferAcceptedEmailAsync(
            string toEmail,
            string supplierName,
            string buyerName,
            string rfqCode,
            string productName,
            int quantity,
            string unit,
            decimal offeredPrice,
            int? deliveryDays,
            string requestsUrl,
            List<OfferItemEmailDto>? items = null)
        {
            try
            {
                var sendTime = DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm");
                var formattedPrice = offeredPrice.ToString("N2", new System.Globalization.CultureInfo("tr-TR"));
                var displaySupplier = string.IsNullOrWhiteSpace(supplierName) ? "Değerli Tedarikçimiz" : supplierName;
                var displayBuyer = string.IsNullOrWhiteSpace(buyerName) ? "Kurumsal Alıcı" : buyerName;
                var displayUnit = string.IsNullOrWhiteSpace(unit) ? "Adet" : unit;
                var displayRfq = string.IsNullOrWhiteSpace(rfqCode) ? "#TALEP" : (rfqCode.StartsWith("#") ? rfqCode : $"#{rfqCode}");
                var deliveryText = deliveryDays.HasValue && deliveryDays.Value > 0 ? $"{deliveryDays.Value} İş Günü" : "Belirtilen Süre";

                string itemsTableHtml = "";
                if (items != null && items.Any())
                {
                    var rowsSb = new System.Text.StringBuilder();
                    int idx = 1;
                    foreach (var it in items)
                    {
                        var itName = string.IsNullOrWhiteSpace(it.ItemName) ? $"Kalem #{idx}" : it.ItemName;
                        var itQty = it.Quantity.ToString("0.##", new System.Globalization.CultureInfo("tr-TR"));
                        var itUnit = string.IsNullOrWhiteSpace(it.Unit) ? "Adet" : it.Unit;
                        var itUnitPrice = it.UnitPrice.ToString("N2", new System.Globalization.CultureInfo("tr-TR"));
                        var itLineTotal = (it.LineTotal > 0 ? it.LineTotal : (it.UnitPrice * it.Quantity)).ToString("N2", new System.Globalization.CultureInfo("tr-TR"));

                        rowsSb.Append($@"
                            <tr style='border-bottom:1px solid #e2e8f0;'>
                                <td style='padding:10px 12px;font-size:13px;font-weight:700;color:#64748b;text-align:center;'>{idx}</td>
                                <td style='padding:10px 12px;font-size:13.5px;color:#0f172a;'>
                                    <strong>{itName}</strong>
                                    {(string.IsNullOrWhiteSpace(it.ItemDescription) ? "" : $"<div style='color:#64748b;font-size:12px;'>{it.ItemDescription}</div>")}
                                </td>
                                <td style='padding:10px 12px;font-size:13px;color:#1e293b;text-align:center;white-space:nowrap;'>
                                    <span style='background:#ecfdf5;color:#065f46;padding:3px 8px;border-radius:6px;font-weight:700;'>{itQty} {itUnit}</span>
                                </td>
                                <td style='padding:10px 12px;font-size:13.5px;color:#0f172a;text-align:right;font-weight:600;white-space:nowrap;'>{itUnitPrice} ₺</td>
                                <td style='padding:10px 12px;font-size:13.5px;color:#059669;text-align:right;font-weight:800;white-space:nowrap;'>{itLineTotal} ₺</td>
                            </tr>");
                        idx++;
                    }

                    itemsTableHtml = $@"
                        <div style='margin-bottom:24px;'>
                            <div style='color:#0f172a;font-size:14.5px;font-weight:800;margin-bottom:8px;'>
                                📋 Onaylanan Kalemler ({items.Count} Kalem)
                            </div>
                            <div style='border:1.5px solid #e2e8f0;border-radius:12px;overflow:hidden;background:#ffffff;'>
                                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='border-collapse:collapse;width:100%;'>
                                    <thead>
                                        <tr style='background:#f8fafc;border-bottom:2px solid #e2e8f0;'>
                                            <th style='padding:8px 12px;text-align:center;color:#475569;font-size:11px;font-weight:800;text-transform:uppercase;'>#</th>
                                            <th style='padding:8px 12px;text-align:left;color:#475569;font-size:11px;font-weight:800;text-transform:uppercase;'>Malzeme</th>
                                            <th style='padding:8px 12px;text-align:center;color:#475569;font-size:11px;font-weight:800;text-transform:uppercase;'>Miktar</th>
                                            <th style='padding:8px 12px;text-align:right;color:#475569;font-size:11px;font-weight:800;text-transform:uppercase;'>Birim Fiyat</th>
                                            <th style='padding:8px 12px;text-align:right;color:#475569;font-size:11px;font-weight:800;text-transform:uppercase;'>Tutar</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {rowsSb}
                                    </tbody>
                                    <tfoot>
                                        <tr style='background:#ecfdf5;border-top:2px solid #6ee7b7;'>
                                            <td colspan='4' style='padding:10px 12px;text-align:right;font-size:12.5px;font-weight:800;color:#065f46;'>TOPLAM KABUL EDİLEN TUTAR:</td>
                                            <td style='padding:10px 12px;text-align:right;font-size:15px;font-weight:900;color:#059669;white-space:nowrap;'>{formattedPrice} ₺</td>
                                        </tr>
                                    </tfoot>
                                </table>
                            </div>
                        </div>";
                }

                var htmlBody = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Tebrikler! Teklifiniz Kabul Edildi</title>
</head>
<body style='margin:0;padding:0;background-color:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,Arial,sans-serif;'>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f1f5f9;'>
        <tr>
            <td align='center' style='padding:40px 16px;'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:640px;background-color:#ffffff;border-radius:24px;box-shadow:0 10px 40px rgba(0,0,0,0.07);border:1px solid #e2e8f0;overflow:hidden;'>
                    <tr>
                        <td style='background:linear-gradient(135deg,#065f46 0%,#059669 50%,#10b981 100%);padding:36px 44px;text-align:center;'>
                            <div style='background:rgba(255,255,255,0.2);display:inline-block;padding:8px 16px;border-radius:20px;margin-bottom:12px;'>
                                <span style='color:#ffffff;font-size:14px;font-weight:800;'>🏆 KAZANAN TEKLİF</span>
                            </div>
                            <h1 style='margin:0;color:#ffffff;font-size:24px;font-weight:800;'>
                                🎉 Tebrikler! Teklifiniz Kabul Edildi
                            </h1>
                            <p style='margin:10px 0 0;color:rgba(255,255,255,0.9);font-size:14px;'>
                                {displayBuyer} firmanızın sunduğu teklifi onayladı!
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:36px 44px 32px;'>
                            <p style='margin:0 0 14px;color:#1e293b;font-size:16px;font-weight:700;'>
                                Sayın {displaySupplier},
                            </p>
                            <p style='margin:0 0 24px;color:#475569;font-size:14.5px;line-height:1.7;'>
                                <strong>{displayRfq}</strong> numaralı <strong>{productName}</strong> alım talebi için iletmiş olduğunuz teklif alıcı firma tarafından incelenmiş ve <strong>KABUL EDİLMİŞTİR</strong>.
                            </p>

                            <!-- Detay Kutusu -->
                            <div style='background:#f0fdf4;border:1.5px solid #86efac;border-radius:16px;padding:22px 24px;margin-bottom:24px;text-align:center;'>
                                <div style='color:#166534;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:0.5px;'>
                                    💰 Kabul Edilen Teklif Tutarı
                                </div>
                                <div style='color:#15803d;font-size:32px;font-weight:900;margin-top:6px;'>
                                    {formattedPrice} ₺
                                </div>
                                <div style='color:#166534;font-size:13px;font-weight:600;margin-top:4px;'>
                                    Teslimat Süresi: {deliveryText} {(items != null && items.Any() ? $"· {items.Count} Kalem" : $"· Miktar: {quantity} {displayUnit}")}
                                </div>
                            </div>

                            {itemsTableHtml}

                            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:24px;border-collapse:separate;border-spacing:0 8px;'>
                                <tr>
                                    <td style='width:160px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>🏢 Alıcı Firma:</td>
                                    <td style='color:#0f172a;font-size:14px;font-weight:700;padding:8px 14px;background:#f8fafc;border-radius:8px;'>{displayBuyer}</td>
                                </tr>
                                <tr>
                                    <td style='width:160px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>📦 Talep Başlığı:</td>
                                    <td style='color:#0f172a;font-size:14px;font-weight:700;padding:8px 14px;background:#f8fafc;border-radius:8px;'>{productName}</td>
                                </tr>
                                <tr>
                                    <td style='width:160px;color:#64748b;font-size:13px;font-weight:700;padding:8px 0;'>🕒 Onay Tarihi:</td>
                                    <td style='color:#64748b;font-size:13px;font-weight:600;padding:8px 14px;background:#f8fafc;border-radius:8px;'>{sendTime}</td>
                                </tr>
                            </table>

                            <div style='margin:30px 0;text-align:center;'>
                                <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='margin:0 auto;'>
                                    <tr>
                                        <td align='center' bgcolor='#059669' style='border-radius:12px;background-color:#059669;box-shadow:0 4px 16px rgba(16,185,129,0.35);'>
                                            <a href='{requestsUrl}' target='_blank' style='display:inline-block;padding:15px 38px;background-color:#059669;color:#ffffff;font-size:15px;font-weight:800;text-decoration:none;border-radius:12px;letter-spacing:0.3px;'>
                                                📋 Taleplerim Sayfasında Görüntüle &rarr;
                                            </a>
                                        </td>
                                    </tr>
                                </table>
                            </div>

                            <div style='margin-top:14px;text-align:center;'>
                                <p style='margin:0 0 4px;color:#94a3b8;font-size:11.5px;'>Buton çalışmıyorsa aşağıdaki bağlantıyı tarayıcınıza yapıştırın:</p>
                                <p style='margin:0;word-break:break-all;'><a href='{requestsUrl}' target='_blank' style='color:#059669;font-size:11.5px;text-decoration:underline;'>{requestsUrl}</a></p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style='background-color:#f8fafc;padding:24px 44px;text-align:center;border-top:1px solid #e2e8f0;'>
                            <p style='margin:0;color:#94a3b8;font-size:11px;'>
                                Tedarik Portal — Satın Alma Platformu · Bu bildirim <strong>{toEmail}</strong> adresine gönderilmiştir.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                var subject = $"🏆 Tebrikler! #{displayRfq} Teklifiniz Kabul Edildi — {productName}";
                return await SendEmailAsync(toEmail, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Kabul e-posta gönderim hatası ({ToEmail}): {Message}", toEmail, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Teklifi elenen tedarikçilere şeffaf red gerekçesini belirten bilgilendirme e-postası gönderir.
        /// </summary>
        public async Task<bool> SendOfferRejectedEmailAsync(
            string toEmail,
            string supplierName,
            string buyerName,
            string rfqCode,
            string productName,
            int quantity,
            string unit,
            decimal offeredPrice,
            string rejectionReason,
            string requestsUrl,
            List<OfferItemEmailDto>? items = null)
        {
            try
            {
                var sendTime = DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm");
                var formattedPrice = offeredPrice.ToString("N2", new System.Globalization.CultureInfo("tr-TR"));
                var displaySupplier = string.IsNullOrWhiteSpace(supplierName) ? "Değerli Tedarikçimiz" : supplierName;
                var displayBuyer = string.IsNullOrWhiteSpace(buyerName) ? "Kurumsal Alıcı" : buyerName;
                var displayUnit = string.IsNullOrWhiteSpace(unit) ? "Adet" : unit;
                var displayRfq = string.IsNullOrWhiteSpace(rfqCode) ? "#TALEP" : (rfqCode.StartsWith("#") ? rfqCode : $"#{rfqCode}");
                var reasonText = string.IsNullOrWhiteSpace(rejectionReason) ? "Talep için başka bir tedarikçinin teklifi kabul edildi." : rejectionReason.Trim();

                var htmlBody = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Teklif Değerlendirme Sonucu</title>
</head>
<body style='margin:0;padding:0;background-color:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,Arial,sans-serif;'>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f1f5f9;'>
        <tr>
            <td align='center' style='padding:40px 16px;'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:640px;background-color:#ffffff;border-radius:24px;box-shadow:0 10px 40px rgba(0,0,0,0.07);border:1px solid #e2e8f0;overflow:hidden;'>
                    <tr>
                        <td style='background:linear-gradient(135deg,#334155 0%,#475569 100%);padding:32px 44px;text-align:center;'>
                            <h1 style='margin:0;color:#ffffff;font-size:22px;font-weight:800;'>
                                Teklif Değerlendirme Bilgilendirmesi
                            </h1>
                            <p style='margin:8px 0 0;color:rgba(255,255,255,0.85);font-size:13.5px;'>
                                {displayRfq} numaralı talep sonuçlandı
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:36px 44px 32px;'>
                            <p style='margin:0 0 14px;color:#1e293b;font-size:16px;font-weight:700;'>
                                Sayın {displaySupplier},
                            </p>
                            <p style='margin:0 0 20px;color:#475569;font-size:14.5px;line-height:1.7;'>
                                <strong>{displayBuyer}</strong> tarafından açılan <strong>{productName}</strong> {(items != null && items.Any() ? $"({items.Count} Kalem)" : $"({quantity} {displayUnit})")} talebi için sunmuş olduğunuz <strong>{formattedPrice} ₺</strong> tutarındaki fiyat teklifiniz değerlendirilmiştir.
                            </p>

                            <!-- Reddetme Sebebi Kutusu -->
                            <div style='background:#fef2f2;border:1.5px solid #fca5a5;border-left:5px solid #ef4444;border-radius:12px;padding:18px 20px;margin-bottom:24px;'>
                                <div style='color:#991b1b;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:4px;'>
                                    🚫 Teklif Durumu & Red Gerekçesi:
                                </div>
                                <p style='margin:0;color:#7f1d1d;font-size:14.5px;font-weight:600;line-height:1.5;'>
                                    {reasonText}
                                </p>
                            </div>

                            <p style='margin:0 0 24px;color:#64748b;font-size:13.5px;line-height:1.6;'>
                                İhale sürecine katılımınız ve verdiğiniz teklif için teşekkür ederiz. Portaldaki diğer açık talepleri inceleyerek yeni teklifler sunabilirsiniz.
                            </p>

                            <div style='margin:28px 0;text-align:center;'>
                                <a href='{requestsUrl}' target='_blank' style='display:inline-block;padding:14px 34px;background:#2563eb;color:#ffffff;font-size:14.5px;font-weight:700;text-decoration:none;border-radius:10px;box-shadow:0 4px 14px rgba(37,99,235,0.3);'>
                                    🌐 Açık İhaleleri İncele &rarr;
                                </a>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style='background-color:#f8fafc;padding:24px 44px;text-align:center;border-top:1px solid #e2e8f0;'>
                            <p style='margin:0;color:#94a3b8;font-size:11px;'>
                                Tedarik Portal — Satın Alma Platformu · Bu bildirim <strong>{toEmail}</strong> adresine gönderilmiştir.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                var subject = $"ℹ️ Teklif Bilgilendirmesi: {productName} ({displayRfq})";
                return await SendEmailAsync(toEmail, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Red e-posta gönderim hatası ({ToEmail}): {Message}", toEmail, ex.Message);
                return false;
            }
        }
    }

    /// <summary>
    /// E-postalarda kalem listesi dökümü için kullanılan veri modeli.
    /// </summary>
    public class OfferItemEmailDto
    {
        public int ItemOrder { get; set; } = 1;
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public string Unit { get; set; } = "Adet";
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? ItemDescription { get; set; }
        public string? Notes { get; set; }
    }
}
