using System.Text.Encodings.Web;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace gestaotcc.WebApi.Logs;

public class SimpleJsonLogFormatter : ITextFormatter
{
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var logObject = new
        {
            Timestamp = logEvent.Timestamp.LocalDateTime.ToString("o"),
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Application = logEvent.Properties.ContainsKey("Application") ? logEvent.Properties["Application"].ToString().Trim('"') : null,
            Environment = logEvent.Properties.ContainsKey("env") ? logEvent.Properties["env"].ToString().Trim('"') : null
        };

        var json = JsonSerializer.Serialize(logObject, _options);
        output.WriteLine(json);
    }
}