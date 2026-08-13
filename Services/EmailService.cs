using Resend;

namespace satinalmaportali2.Services {
    public class EmailService {
        private readonly string _apiKey = "re_gyM2f6dc_7fLbehKf6gpDvQosJnFzJr8Y";
        private readonly string _fromEmail = "onboarding@resend.dev";

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink) {
            try {
                IResend resend = ResendClient.Create(_apiKey);

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

                var message = new EmailMessage {
                    From = _fromEmail,
                    To = toEmail,
                    Subject = "🔐 Tedarik Portal — Şifre Sıfırlama Bağlantınız",
                    HtmlBody = htmlBody
                };

                await resend.EmailSendAsync(message);
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"E-posta gönderim hatası: {ex.Message}");
                return false;
            }
        }
    }
}
