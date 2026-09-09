using System.Threading.Tasks;

namespace gestaotcc.Application.UseCases;

public interface IHtmlTemplateEngine
{
    Task<string> GenerateDocumentAsync(string templateName, object payload);
}
