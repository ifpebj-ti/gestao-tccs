namespace gestaotcc.WebApi.InputModel;

public record DownloadDocumentHtmlInputModel(long TccId, long DocumentId, string? StudentId, string HtmlContent);
