using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Infra.Gateways;
using Microsoft.Extensions.Configuration;
using Scriban;

namespace gestaotcc.Test.Integration;

public class EmailGatewayTemplateTests
{
    [Theory]
    [InlineData("PROPOSAL-DECLINED", "proposal-declined")]
    [InlineData("PROPOSAL-PARTICIPANT", "proposal-participant")]
    [InlineData("CREATE-USER", "create-user")]
    [InlineData("AUTO-REGISTER-USER", "auto-register-user")]
    [InlineData("RESEND-INVITE-TCC", "resend-invite-tcc")]
    [InlineData("ADD-USER-TCC", "add-user-tcc")]
    [InlineData("INVITE-USER", "invite-tcc")]
    [InlineData("LINK-BANKING-USER", "link-banking-user")]
    [InlineData("SEND-PENDING-SIGNATURE", "send-pending-signature")]
    [InlineData("SCHEDULE-TCC", "schedule-tcc")]
    [InlineData("SCHEDULE-TCC-STUDENT", "schedule-tcc-student")]
    [InlineData("SCHEDULE-TCC-ADVISOR", "schedule-tcc-advisor")]
    [InlineData("SCHEDULE-TCC-PARTICIPANT", "schedule-tcc-participant")]
    [InlineData("BANKING-INVITE", "banking-invite")]
    [InlineData("BANKING-REMINDER", "banking-reminder")]
    [InlineData("BANKING-INTERNAL-REMINDER", "banking-internal-reminder")]
    [InlineData("BANKING-EVALUATION", "banking-evaluation")]
    [InlineData("BANKING-INTERNAL-SCHEDULE", "banking-internal-schedule")]
    [InlineData("BANKING-INTERNAL-EVALUATION", "banking-internal-evaluation")]
    [InlineData("UPDATE-PASSWORD", "update-password")]
    [InlineData("SCHEDULE-ALLOWED", "schedule-allowed")]
    [InlineData("FINAL-SUBMISSION-NOTICE", "final-submission-notice")]
    [InlineData("FORWARD-LIBRARY", "forward-library")]
    [InlineData("TCC-COMPLETED", "tcc-completed")]
    [InlineData("ADVISOR-CHANGE-COORDINATION", "advisor-change-coordination")]
    [InlineData("ADVISOR-CHANGE-APPROVED", "advisor-change-approved")]
    [InlineData("ADVISOR-CHANGE-REJECTED", "advisor-change-rejected")]
    public async Task Send_RendersTheExpectedProductionTemplate(string templateType, string fileName)
    {
        const string frontendUrl = "https://gradus.example.org";
        var gateway = CreateGateway(frontendUrl);
        var message = new SendEmailDTO("", "Test notice", "student@example.org", templateType, new()
        {
            ["username"] = "Test Student",
            ["recipientName"] = "Test Student",
            ["token"] = "test-token",
            ["has_tcc_attachment"] = true
        });
        var expectedVariables = new Dictionary<string, object>(message.Variables) { ["urlfront"] = frontendUrl };
        var path = Path.Combine(AppContext.BaseDirectory, "Templates", fileName + "-template.html");
        var expected = Template.Parse(await File.ReadAllTextAsync(path)).Render(expectedVariables);

        var result = await gateway.Send(message);

        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.False(string.IsNullOrWhiteSpace(expected));
        Assert.Equal(expected, message.EmailBody);
    }

    [Fact]
    public async Task Send_ReturnsAnErrorForAnUnknownTemplate()
    {
        var message = new SendEmailDTO("", "Test notice", "student@example.org", "UNKNOWN", new());

        var result = await CreateGateway().Send(message);

        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Contains("Template type UNKNOWN not found", result.Message);
        Assert.Empty(message.EmailBody);
    }

    [Fact]
    public async Task Send_PreservesRawHtml()
    {
        const string html = "<p>Test notice</p>";
        var message = new SendEmailDTO(html, "Test notice", "student@example.org", "RAW", new());

        var result = await CreateGateway().Send(message);

        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Equal(html, message.EmailBody);
        Assert.Empty(message.Variables);
    }

    private static EmailGateway CreateGateway(string? frontendUrl = null)
    {
        // The empty sender ends the call during message construction, before an SMTP connection.
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MailSettings:Host"] = "localhost",
            ["MailSettings:Port"] = "1025",
            ["MailSettings:EmailId"] = "",
            ["CORS_SETTINGS:URL_FRONT"] = frontendUrl
        }).Build();

        return new EmailGateway(configuration);
    }
}
