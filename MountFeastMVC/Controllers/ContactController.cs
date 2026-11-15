using MountFeastMVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace MountFeastMVC.Controllers
{
    public class ContactController : Controller
    {
        [Route("~/hotel-naran-contact-number", Name = "hotelMurreecontactnumber")]
        // GET: Contact
        public ActionResult Index()
        {
            var locations = new List<Location>
                {
                    new Location
                    {
                        Name = "Hotel in Naran",
                        GoogleMapUrl = "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d818.1958151953545!2d73.6390275695782!3d34.88678849839014!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x38e751f809a0fe3d%3A0xf3430ac4de399efc!2sMount%20Feast%20Hotel%20Naran!5e0!3m2!1sen!2s!4v1745131036907!5m2!1sen!2s",
                        ViewUrl = "https://www.google.com/maps/place/Mount+Feast+Hotel+Naran/@34.886788,73.639028,17z/data=!4m9!3m8!1s0x38e751f809a0fe3d:0xf3430ac4de399efc!5m2!4m1!1i2!8m2!3d34.8867874!4d73.6396713!16s%2Fg%2F11j2wn511q?entry=ttu&g_ep=EgoyMDI1MDQxNi4xIKXMDSoASAFQAw%3D%3D"
                    },
                    new Location
                    {
                        Name = "Hotel in Islamabad",
                        GoogleMapUrl = "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3323.629755408954!2d72.85867569999999!3d33.588961100000006!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x38df9934a1b37d75%3A0x822022b052ca37f8!2sMount%20Feast%20Hotel%20Islamabad!5e0!3m2!1sen!2s!4v1745130728811!5m2!1sen!2s",
                        ViewUrl = "https://www.google.com/maps/place/Mount+Feast+Hotel+Islamabad/@33.5889611,72.8586757,17z/data=!3m1!4b1!4m6!3m5!1s0x38df9934a1b37d75:0x822022b052ca37f8!8m2!3d33.5889611!4d72.8586757!16s%2Fg%2F11m5sf5mr2?entry=ttu&g_ep=EgoyMDI1MDQxNi4xIKXMDSoASAFQAw%3D%3D"
                    },
                    new Location
                    {
                        Name = "Hotel in Skardu",
                        GoogleMapUrl = "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3255.386142773004!2d75.5327051!3d35.3212315!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x38e46700147cf5af%3A0xe212dd3649ac7881!2sMount%20Feast%20Hotel%20Skardu!5e0!3m2!1sen!2s!4v1745130598806!5m2!1sen!2s",
                        ViewUrl = "https://www.google.com/maps/place/Mount+Feast+Hotel+Skardu/@35.321231,75.532705,17z/data=!4m9!3m8!1s0x38e46700147cf5af:0xe212dd3649ac7881!5m2!4m1!1i2!8m2!3d35.3212315!4d75.5327051!16s%2Fg%2F11x1qwcn8c?entry=ttu&g_ep=EgoyMDI1MDQxNi4xIKXMDSoASAFQAw%3D%3D"
                    }
                };

            return View(locations);
        }

        [Route("~/hotel-skardu-contact-number", Name = "hotelSkarducontactnumber")]
        public ActionResult Skardu()
        {
            return View();
        }
        [Route("~/hotel-islamabad-contact-number", Name = "hotelIslamabadcontactnumber")]
        public ActionResult Islamabad()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Contact(string firstname, string lastname, string email, string phone, string message)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(email))
                {
                    string fullName = $"{firstname} {lastname}";

                    // Build HTML email
                    StringBuilder htmlString = new StringBuilder();
                    htmlString.Append(@"<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <style>
    body {
        font-family: Arial, sans-serif;
        background-color: #f4f6f9;
        margin: 0;
        padding: 0;
    }
    .container {
        max-width: 600px;
        margin: 20px auto;
        background: #ffffff;
        border-radius: 8px;
        overflow: hidden;
        box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }
    .header {
        background: #2C3E50;
        color: #fff;
        padding: 20px;
        text-align: center;
    }
    .header h2 {
        margin: 0;
        font-size: 22px;
    }
    .content {
        padding: 20px;
        color: #333;
    }
    table {
        width: 100%;
        border-collapse: collapse;
        margin-bottom: 20px;
    }
    table td {
        padding: 10px;
        border: 1px solid #ddd;
        font-size: 15px;
    }
    table td.label {
        color: red;
        font-weight: bold;
        width: 25%;
        background: #fdf2f2;
    }
    table td.value {
        color: #333;
    }
    .message-box {
        background:#f9f9f9;
        padding:12px;
        border-left:3px solid #e74c3c;
        margin-top:10px;
    }
    .footer {
        background: #f1f1f1;
        text-align: center;
        padding: 15px;
        font-size: 13px;
        color: #555;
    }
    .btn {
        display: inline-block;
        margin-top: 15px;
        background: #e74c3c;
        color: #fff !important;
        text-decoration: none;
        padding: 10px 20px;
        border-radius: 4px;
        font-size: 14px;
    }
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h2>📩 New Inquiry from website</h2>
    </div>
    <div class='content'>
      <table>
        <tr>
          <td class='label'>Name</td>
          <td class='value'>" + fullName + @"</td>
        </tr>
        <tr>
          <td class='label'>Email</td>
          <td class='value'>" + email + @"</td>
        </tr>
        <tr>
          <td class='label'>Phone</td>
          <td class='value'>" + phone + @"</td>
        </tr>
      </table>

      <p><strong style='color:red;'>Message:</strong></p>
      <div class='message-box'>" + message + @"</div>

    </div>
    <div class='footer'>
      <p>© " + DateTime.Now.Year + @" Mount Feast Hotel. All rights reserved.</p>
    </div>
  </div>
</body>
</html>");


                    // SMTP Config
                    string smtpHost = "smtp.gmail.com";
                    int smtpPort = 587;
                    string smtpUser = "info@mountfeasthotel.com";  // sender
                    string smtpPass = "xhhusxiqfemxcpan";    // Gmail App Password (no spaces)
                    bool enableSsl = true;

                    string recipient = "info@mountfeasthotel.com"; // send to yourself

                    // Prepare mail
                    MailMessage mm = new MailMessage
                    {
                        From = new MailAddress(smtpUser),
                        Subject = "New Query from MountFeast Website",
                        Body = htmlString.ToString(),
                        IsBodyHtml = true
                    };
                    mm.To.Add(recipient);
                    mm.ReplyToList.Add(new MailAddress(email));

                    using (SmtpClient smc = new SmtpClient(smtpHost, smtpPort))
                    {
                        smc.EnableSsl = enableSsl;
                        smc.UseDefaultCredentials = false;
                        smc.Credentials = new NetworkCredential(smtpUser, smtpPass);
                        smc.Send(mm);
                    }

                    TempData["msg"] = "We received your message. We'll get back to you soon. Thank you!";
                    return RedirectToAction("Index", "Contact");
                }
                else
                {
                    TempData["msg"] = "Email is required.";
                    return RedirectToAction("Index", "Contact");
                }
            }
            catch (Exception ex)
            {
                TempData["msg"] = "Error: " + ex.Message;
                return RedirectToAction("Index", "Contact");
            }
        }


    }
}