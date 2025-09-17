using gestaotcc.Application.Gateways;

namespace gestaotcc.Application.UseCases.Signature;

public class RemoveFilledPdfsUseCase(IMinioGateway minioGateway)
{
    public async Task Execute()
    {
        var today = DateTime.Today;

        await foreach (var file in await minioGateway.ListBuckets("filled/"))
        {
            var parts = file.Key.Split('_');
            if (parts.Length > 1)
            {
                var datePart = parts[1]; // ex: 16-09-2025
                if (DateTime.TryParseExact(datePart, "dd-MM-yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var fileDate))
                {
                    if (fileDate < today)
                    {
                        await minioGateway.Remove(file.Key);
                    }
                }
            }
        }
        
    }
}