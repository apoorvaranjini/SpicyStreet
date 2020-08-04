using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using SendGrid;
using SendGrid.Helpers.Mail;
using Spice.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Services
{
    public class EmailSender:IEmailSender
    {
        public EmailOptions Options { get; set;}
        public EmailSender(IOptions<EmailOptions> emailOptions)
        {
            Options = emailOptions.Value;
        }
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return Execute(Options.SendGridKey, subject, htmlMessage, email);
        }

        public Task Execute(string SendGridKey,string subject, string htmlMessage,string email)
        {
            var client = new SendGridClient(SendGridKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("admin@spice.com", "Spice Resturant"),
                Subject = subject,
                PlainTextContent = htmlMessage,
                HtmlContent = htmlMessage
            };
            msg.AddTo(new EmailAddress(email));
            try
            {
                return client.SendEmailAsync(msg);
            }
            catch(Exception ex)
            {

            }
            return null;
        }
    }
}
