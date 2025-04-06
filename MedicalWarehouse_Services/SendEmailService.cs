using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;


namespace MedicalWarehouse_Services
{
    public class SendEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationSection _smtpSetting;

        public SendEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpSetting = _configuration.GetSection("SMTP");
        }

        public async Task SendEmailAsync(string toEmail)
        {
            // Lấy thông tin SMTP từ cấu hình
            string smtpServer = _smtpSetting.GetSection("Server").Value;
            int port = int.Parse(_smtpSetting.GetSection("Port").Value);
            string userName = _smtpSetting.GetSection("Username").Value;
            string password = _smtpSetting.GetSection("Password").Value;

            // Nội dung email
            string subject = "[MedicalWarehouse]Verify your email";
            string verifyUrl = $"http://localhost:5273/auth/verify_email?email={Uri.EscapeDataString(toEmail)}";

            string body = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Account Activation</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f4f4f4;
            padding: 20px;
            text-align: center;
        }}
        .container {{
            max-width: 500px;
            background: #ffffff;
            padding: 30px;
            border-radius: 12px;
            box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
            margin: auto;
        }}
        .title {{
            font-size: 22px;
            font-weight: bold;
            color: #333;
            margin-bottom: 20px;
        }}
        .message {{
            font-size: 16px;
            color: #555;
            margin-bottom: 25px;
            line-height: 1.5;
        }}
        .button {{
            display: inline-block;
            background: linear-gradient(135deg, #007BFF, #0056b3);
            color: white !important;
            padding: 14px 24px;
            text-decoration: none;
            font-size: 16px;
            border-radius: 6px;
            font-weight: bold;
            transition: 0.3s;
            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
            text-align: center;
        }}
        .button:hover {{
            background: linear-gradient(135deg, #0056b3, #003f7f);
            transform: scale(1.05);
            color: white !important;
        }}
        .footer {{
            font-size: 13px;
            color: #888;
            margin-top: 30px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <p class='title'>Verify Your Email Address</p>
        <p class='message'>Thank you for registering! Please confirm your email by clicking the button below.</p>
        <a href='{verifyUrl}' class='button'>Activate Account</a>
        <p class='footer'>If you didn’t request this, you can safely ignore this email.</p>
    </div>
</body>
</html>";





            using (var smtpClient = new SmtpClient(smtpServer))
            {
                smtpClient.Port = port;
                smtpClient.Credentials = new NetworkCredential(userName, password);
                smtpClient.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(userName, "MedicalWarehouse"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                try
                {
                    await smtpClient.SendMailAsync(mailMessage);
                    Console.WriteLine("Email sent successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email: {ex.Message}");
                }
            }
        }
    }
}
