using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using To_DoListApp.Models;

namespace To_DoListApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _config;

    public HomeController(ILogger<HomeController> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult SendEmail([FromBody] JsonElement data)
    {
        // Extract data from the dynamic JSON
        var recipientEmail = data.GetProperty("recipientEmail").GetString();
        var items = data.GetProperty("items").EnumerateArray();

        if (string.IsNullOrWhiteSpace(recipientEmail))
            return BadRequest("Recipient email is required.");

        //var body = "";
        //foreach (var item in items)
        //{
        //    var id = item.GetProperty("id").GetString();
        //    var description = item.GetProperty("description").GetString();
        //    var status = item.GetProperty("status").GetString();
        //    body += $"ID: {id}, Description: {description}, Status: {status}\n";
        //}

        var body = @"
<div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;'>
  <h2 style='color:#007bff;text-align:center;margin-bottom:24px;'>To-Do List</h2>
  <table style='border-collapse:collapse;width:100%;box-shadow:0 2px 8px #eee;'>
    <thead>
      <tr>
        <th style='background:#007bff;color:#fff;padding:12px 8px;border:1px solid #007bff;text-align:left;'>ID</th>
        <th style='background:#007bff;color:#fff;padding:12px 8px;border:1px solid #007bff;text-align:left;'>Description</th>
        <th style='background:#007bff;color:#fff;padding:12px 8px;border:1px solid #007bff;text-align:left;'>Status</th>
      </tr>
    </thead>
    <tbody>
";

        foreach (var item in items)
        {
            var id = item.GetProperty("id").GetString();
            var description = item.GetProperty("description").GetString();
            var status = item.GetProperty("status").GetString();

            // Color code the status
            var statusColor = status switch
            {
                "Complete" => "#28a745",
                "Pending" => "#ffc107",
                "InProgress" => "#17a2b8",
                _ => "#6c757d"
            };

            body += $@"
      <tr>
        <td style='padding:10px 8px;border:1px solid #ddd;background:#f9f9f9;'>{WebUtility.HtmlEncode(id)}</td>
        <td style='padding:10px 8px;border:1px solid #ddd;background:#fff;'>{WebUtility.HtmlEncode(description)}</td>
        <td style='padding:10px 8px;border:1px solid #ddd;background:#f9f9f9;'>
          <span style='
            display:inline-block;
            padding:4px 12px;
            border-radius:12px;
            background:{statusColor};
            color:#fff;
            font-size:0.95em;
          '>{WebUtility.HtmlEncode(status)}</span>
        </td>
      </tr>
    ";
        }

        body += @"
    </tbody>
  </table>
  <div style='margin-top:24px;font-size:0.95em;color:#888;text-align:center;'>
    Sent from your To-Do List App
  </div>
</div>
";

        // Configure your SMTP settings
        var smtpSection = _config.GetSection("Smtp");
        var smtpClient = new SmtpClient(smtpSection["Host"])
        {
            Port = int.Parse(smtpSection["Port"]),
            Credentials = new NetworkCredential(smtpSection["User"], smtpSection["Password"]),
            EnableSsl = bool.Parse(smtpSection["EnableSsl"]),
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpSection["User"]),
            Subject = "To-Do List Selected Items",
            Body = body,
            IsBodyHtml = true,
        };
        mailMessage.To.Add(recipientEmail);

        try
        {
            smtpClient.Send(mailMessage);
            return Ok("Email sent.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Failed to send email: " + ex.Message);
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
