namespace gestaotcc.Domain.Errors;

public class FormatDetails
{
    public string Type { get; set; } // O URI que identifica o tipo de problema (opcional)
    public string Title { get; set; } // Um título resumido para o tipo de problema
    public int? Status { get; set; } // O código de status HTTP (ex.: 400, 404)
    public string Detail { get; set; } // Descrição detalhada do erro
    public string Instance { get; set; } // Um identificador para a instância do problema (opcional)
    public Dictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>(); // Permite adicionar informações personalizadas

    public FormatDetails() { }
    public FormatDetails(string title, int? status, string detail, string? type = null, string? instance = null)
    {
        Type = type ?? "about:blank"; // URI padrão "about:blank" se não for fornecido
        Title = title;
        Status = status;
        Detail = detail;
        Instance = instance ?? $"urn:problem:{Guid.NewGuid()}"; // Instância padrão gerada com GUID
    }

    // Método para adicionar uma chave/valor personalizada à extensão
    public void AddExtension(string key, object value)
    {
        if (!Extensions.ContainsKey(key))
        {
            Extensions.Add(key, value);
        }
    }
}