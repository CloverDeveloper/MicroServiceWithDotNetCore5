using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailService
{
    public class EmailSender : IEmailSender
    {
        private EmailConfig emailConfig;
        public EmailSender(EmailConfig emailConfig) 
        {
            this.emailConfig = emailConfig;
        }

        public async Task SendEmailAsync(Message message)
        {
            var emailMessage = CreateEmailMessage(message);
            await this.SendAsync(emailMessage);
        }

        private MimeMessage CreateEmailMessage(Message message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(this.emailConfig.From));
            emailMessage.To.AddRange(message.To);
            emailMessage.Subject = message.Subject;
            var bodyBuilder = new BodyBuilder()
            {
                HtmlBody = $"<h2 style='color:red;'>{message.Content}</h2>"
            };

            if (message.Attachments != null && message.Attachments.Any()) 
            {
                int i = 1;
                foreach (var attachment in message.Attachments) 
                {
                    bodyBuilder.Attachments.Add("attachment" + i, attachment);
                    i += 1;
                }
            }

            emailMessage.Body = bodyBuilder.ToMessageBody();

            return emailMessage;
        }

        private async Task SendAsync(MimeMessage emailMessage) 
        {
            using (var client = new SmtpClient()) 
            {
                try
                {
                    // 啟用 SSL 設為 true
                    await client.ConnectAsync(this.emailConfig.SmtpServer, this.emailConfig.Port, true);
                    // 移除身分驗證機制的 XOAUTH2 屬性
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    // 驗證使用者 email
                    await client.AuthenticateAsync(this.emailConfig.UserName, this.emailConfig.Password);
                    await client.SendAsync(emailMessage);
                }
                catch (Exception ex) 
                {
                    // this need to be logged acttually
                    Console.Out.WriteLine(ex.Message);
                    throw;
                }
                finally 
                {
                    await client.DisconnectAsync(true);
                    client.Dispose();
                }
            }
        }
    }
}
