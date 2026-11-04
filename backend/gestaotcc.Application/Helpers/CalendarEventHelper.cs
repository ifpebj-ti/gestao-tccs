using System.Text;

namespace gestaotcc.Application.Helpers;

public static class CalendarEventHelper
{
    public const int DefaultDurationHours = 2;

    public static string BuildGoogleCalendarUrl(
        string title,
        string? summary,
        string location,
        DateTime scheduledDate,
        int durationHours = DefaultDurationHours)
    {
        var startUtc = scheduledDate.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
        var endUtc = scheduledDate.AddHours(durationHours).ToUniversalTime().ToString("yyyyMMddTHHmmssZ");

        var eventTitle = $"Defesa de TCC: {title}";
        var details = $"Apresentação e avaliação do Trabalho de Conclusão de Curso.\n\nTítulo: {title}\nLocal: {location}\nResumo: {summary ?? "Não informado."}";

        return $"https://calendar.google.com/calendar/render?action=TEMPLATE&text={Uri.EscapeDataString(eventTitle)}&dates={startUtc}/{endUtc}&details={Uri.EscapeDataString(details)}&location={Uri.EscapeDataString(location)}";
    }

    public static string BuildOutlookCalendarUrl(
        string title,
        string? summary,
        string location,
        DateTime scheduledDate,
        int durationHours = DefaultDurationHours)
    {
        var startUtc = scheduledDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        var endUtc = scheduledDate.AddHours(durationHours).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

        var eventTitle = $"Defesa de TCC: {title}";
        var details = $"Apresentação e avaliação do Trabalho de Conclusão de Curso.\n\nTítulo: {title}\nLocal: {location}\nResumo: {summary ?? "Não informado."}";

        return $"https://outlook.live.com/calendar/0/deeplink/compose?subject={Uri.EscapeDataString(eventTitle)}&startdt={Uri.EscapeDataString(startUtc)}&enddt={Uri.EscapeDataString(endUtc)}&body={Uri.EscapeDataString(details)}&location={Uri.EscapeDataString(location)}";
    }

    public static string BuildIcsContent(
        long tccId,
        string title,
        string? summary,
        string location,
        DateTime scheduledDate,
        int sequence = 0,
        int durationHours = DefaultDurationHours)
    {
        var startUtc = scheduledDate.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
        var endUtc = scheduledDate.AddHours(durationHours).ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
        var nowUtc = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        var uid = $"defesa-tcc-{tccId}@gradusflow.ifpe.edu.br";

        var sb = new StringBuilder();
        sb.Append("BEGIN:VCALENDAR\r\n");
        sb.Append("VERSION:2.0\r\n");
        sb.Append("PRODID:-//Gradus Flow//Defesas de TCC//PT-BR\r\n");
        sb.Append("CALSCALE:GREGORIAN\r\n");
        sb.Append("METHOD:REQUEST\r\n");
        sb.Append("BEGIN:VEVENT\r\n");
        sb.Append($"UID:{uid}\r\n");
        sb.Append($"SEQUENCE:{sequence}\r\n");
        sb.Append($"DTSTAMP:{nowUtc}\r\n");
        sb.Append($"DTSTART:{startUtc}\r\n");
        sb.Append($"DTEND:{endUtc}\r\n");
        sb.Append($"SUMMARY:{EscapeIcsText($"Defesa de TCC: {title}")}\r\n");
        sb.Append($"DESCRIPTION:{EscapeIcsText($"Defesa do Trabalho de Conclusão de Curso: {title}\nLocal: {location}\nResumo: {summary ?? "Não informado."}")}\r\n");
        sb.Append($"LOCATION:{EscapeIcsText(location)}\r\n");
        sb.Append("STATUS:CONFIRMED\r\n");
        sb.Append("TRANSP:OPAQUE\r\n");
        sb.Append("END:VEVENT\r\n");
        sb.Append("END:VCALENDAR\r\n");

        return sb.ToString();
    }

    public static string EscapeIcsText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n");
    }
}

