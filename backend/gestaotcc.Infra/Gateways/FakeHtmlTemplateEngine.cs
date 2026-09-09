using System.Threading.Tasks;
using gestaotcc.Application.UseCases;

namespace gestaotcc.Infra.Gateways;

public class FakeHtmlTemplateEngine : IHtmlTemplateEngine
{
    public Task<string> GenerateDocumentAsync(string templateName, object data)
    {
        return Task.FromResult("<html><body><h1>Document</h1></body></html>");
    }
}
