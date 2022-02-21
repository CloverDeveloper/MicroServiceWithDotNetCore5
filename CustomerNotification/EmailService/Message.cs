using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailService
{
    public class Message
    {
        public List<MailboxAddress> To { get; set; }

        public string Subject { get; set; }

        public string Content { get; set; }

        public List<byte[]> Attachments { get; set; }

        public Message(IEnumerable<string> to,string subject,string content,List<byte[]> attachments) 
        {
            this.To = new List<MailboxAddress>();
            this.To.AddRange(to.Select(x => new MailboxAddress(x)));

            this.Subject = subject;
            this.Content = content;
            this.Attachments = attachments;
        }
    }
}
