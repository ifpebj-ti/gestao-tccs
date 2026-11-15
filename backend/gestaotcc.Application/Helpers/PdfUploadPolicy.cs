using System.Text;

namespace gestaotcc.Application.Helpers;

public static class PdfUploadPolicy
{
    public const int MaxBytes = 20 * 1024 * 1024;
    public const int MaxRequestBytes = MaxBytes + 64 * 1024;

    public static bool IsValid(byte[]? bytes, string? fileName, string? contentType) =>
        bytes is { Length: >= 8 and <= MaxBytes } &&
        string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase) &&
        bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8) &&
        Encoding.ASCII.GetString(bytes, Math.Max(0, bytes.Length - 1024), Math.Min(1024, bytes.Length))
            .Contains("%%EOF", StringComparison.Ordinal);
}
