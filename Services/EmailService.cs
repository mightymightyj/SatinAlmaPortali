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
    }
}
