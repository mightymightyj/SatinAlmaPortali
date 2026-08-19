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
        /// Satın alma talebine gelen yeni fiyat teklifi için zengin HTML e-posta içeriği üretir.
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
            string requestsUrl)
        {
            var sendTime = DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm");
            var unitPrice = quantity > 0 ? (offerPrice / quantity) : offerPrice;
            var formattedTotalPrice = offerPrice.ToString("N2", new System.Globalization.CultureInfo("tr-TR"));
            var formattedUnitPrice = unitPrice.ToString("N2", new System.Globalization.CultureInfo("tr-TR"));
            var deliveryText = deliveryDays.HasValue && deliveryDays.Value > 0 ? $"{deliveryDays.Value} İş Günü" : "Hemen Teslim";
            var validityText = validityDays.HasValue && validityDays.Value > 0 ? $"{validityDays.Value} Gün" : "30 Gün";
            var displayUnit = string.IsNullOrWhiteSpace(unit) ? "Adet" : unit;
            var displayBuyer = string.IsNullOrWhiteSpace(buyerName) ? "Değerli Müşterimiz" : buyerName;
            var displaySupplier = string.IsNullOrWhiteSpace(supplierName) ? "Onaylı Tedarikçi Firma" : supplierName;
            var displayRfq = string.IsNullOrWhiteSpace(rfqCode) ? "#TALEP-2026" : (rfqCode.StartsWith("#") ? rfqCode : $"#{rfqCode}");

            // Ürün görseli HTML yapısı
            string productImageHtml;
            if (!string.IsNullOrWhiteSpace(productImageUrl))
            {
                productImageHtml = $@"
                    <div style='text-align:center;margin-bottom:20px;'>
                        <img src='{productImageUrl}' alt='{productName}' style='max-width:100%;max-height:240px;object-fit:cover;border-radius:14px;border:1px solid #e2e8f0;box-shadow:0 6px 18px rgba(0,0,0,0.06);display:inline-block;' />
                    </div>";
            }
            else
            {
                productImageHtml = $@"
                    <div style='background:linear-gradient(135deg,#eff6ff 0%,#dbeafe 100%);border:1.5px dashed #93c5fd;border-radius:14px;padding:32px 20px;text-align:center;margin-bottom:20px;'>
                        <div style='width:64px;height:64px;background:#ffffff;border-radius:16px;line-height:64px;font-size:32px;margin:0 auto 12px;box-shadow:0 4px 14px rgba(37,99,235,0.15);'>
                            📦
                        </div>
                        <span style='color:#1e40af;font-size:13px;font-weight:800;letter-spacing:0.5px;text-transform:uppercase;'>
                            Satın Alma Ürün Talebi
                        </span>
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
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:620px;background-color:#ffffff;border-radius:24px;box-shadow:0 10px 40px rgba(0,0,0,0.07);border:1px solid #e2e8f0;overflow:hidden;'>
                    
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
                                Satın alma talebiniz için bir tedarikçi fiyat teklifi sundu
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
                                Oluşturmuş olduğunuz satın alma talebi için <strong>{displaySupplier}</strong> tarafından yeni bir fiyat teklifi hazırlanarak tarafınıza iletilmiştir. Teklif özetini aşağıda görebilirsiniz:
                            </p>

                            <!-- ÜRÜN KARTI (Görsel + Ürün Adı + Miktar) -->
                            <div style='background:#ffffff;border:1.5px solid #e2e8f0;border-radius:18px;padding:24px;margin-bottom:24px;box-shadow:0 4px 16px rgba(0,0,0,0.03);'>
                                
                                <!-- Ürün Görseli -->
                                {productImageHtml}

                                <!-- Ürün Başlığı -->
                                <h2 style='margin:0 0 8px;color:#0f172a;font-size:19px;font-weight:800;line-height:1.35;text-align:center;'>
                                    {productName}
                                </h2>

                                <!-- Miktar ve Talep Bilgisi Rozetleri -->
                                <div style='text-align:center;margin-top:12px;'>
                                    <span style='display:inline-block;padding:6px 14px;background:#eff6ff;color:#2563eb;font-size:13px;font-weight:700;border-radius:8px;border:1px solid #bfdbfe;margin:0 4px 6px;'>
                                        📦 Talep Miktarı: <strong>{quantity} {displayUnit}</strong>
                                    </span>
                                    <span style='display:inline-block;padding:6px 14px;background:#f8fafc;color:#475569;font-size:13px;font-weight:700;border-radius:8px;border:1px solid #e2e8f0;margin:0 4px 6px;'>
                                        🏢 Satıcı: <strong>{displaySupplier}</strong>
                                    </span>
                                </div>
                            </div>

                            <!-- FİYAT VURGULAMA KART (Yeşil / Premium Highlight) -->
                            <div style='background:linear-gradient(135deg,#f0fdf4 0%,#dcfce7 100%);border:1.5px solid #86efac;border-radius:16px;padding:22px 24px;margin-bottom:24px;text-align:center;box-shadow:0 4px 16px rgba(22,163,74,0.1);'>
                                <div style='color:#166534;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:0.6px;margin-bottom:6px;'>
                                    💰 Toplam Teklif Fiyatı
                                </div>
                                <div style='color:#15803d;font-size:34px;font-weight:900;letter-spacing:-0.5px;line-height:1.1;'>
                                    {formattedTotalPrice} ₺
                                </div>
                                <div style='color:#166534;font-size:13px;font-weight:600;margin-top:6px;'>
                                    (Birim Fiyat: ~{formattedUnitPrice} ₺ / {displayUnit})
                                </div>
                            </div>

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
                                        <td style='border-radius:14px;background:linear-gradient(135deg,#1e40af 0%,#2563eb 100%);box-shadow:0 6px 20px rgba(37,99,235,0.38);'>
                                            <a href='{requestsUrl}' target='_blank' style='display:inline-block;padding:16px 40px;color:#ffffff;font-size:15.5px;font-weight:800;text-decoration:none;letter-spacing:0.3px;'>
                                                📋 Taleplerime Git ve İncele &rarr;
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
                                                    <strong>Sonraki Adım:</strong> Taleplerim sayfasına giderek teklifi onaylayabilir, reddedebilir veya satıcı ile doğrudan görüşme başlatabilirsiniz.
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
                                    <a href='{requestsUrl}' style='color:#3b82f6;font-size:11.5px;text-decoration:underline;'>{requestsUrl}</a>
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
        /// Satın alma talebine yeni teklif geldiğinde alıcıya HTML e-posta bildirimi gönderir.
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
            string requestsUrl)
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
                    requestsUrl);

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
    }
}
