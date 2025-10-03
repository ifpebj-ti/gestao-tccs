using gestaotcc.Application.Gateways;

namespace gestaotcc.Application.UseCases.Signature;

public class RemoveFilledPdfsUseCase(IMinioGateway minioGateway, IAppLoggerGateway<RemoveFilledPdfsUseCase> logger)
{
    public async Task Execute()
    {
        logger.LogInformation("Iniciando a tarefa de remoção de PDFs preenchidos antigos.");
        var today = DateTime.Today;
        logger.LogInformation("Data de referência para exclusão (arquivos mais antigos que): {TodayDate}", today.ToString("dd-MM-yyyy"));

        logger.LogInformation("Listando arquivos do bucket 'filled/'...");
        await foreach (var file in await minioGateway.ListBuckets("filled/"))
        {
            logger.LogDebug("Processando arquivo: {FileName}", file.Key);

            var parts = file.Key.Split('_');
            if (parts.Length > 1)
            {
                var datePart = parts[1]; 
                if (DateTime.TryParseExact(datePart, "dd-MM-yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var fileDate))
                {
                    if (fileDate < today)
                    {
                        logger.LogInformation("Removendo arquivo antigo: {FileName} (Data: {FileDate})", file.Key, fileDate.ToString("dd-MM-yyyy"));
                        await minioGateway.Remove(file.Key);
                    }
                    else
                    {
                        logger.LogDebug("Arquivo {FileName} mantido (Data: {FileDate} não é anterior a hoje).", file.Key, fileDate.ToString("dd-MM-yyyy"));
                    }
                }
                else
                {
                    logger.LogWarning("Não foi possível parsear a data do arquivo: {FileName}. Parte da data encontrada: '{DatePart}'", file.Key, datePart);
                }
            }
            else
            {
                logger.LogWarning("Arquivo {FileName} ignorado: formato de nome inválido (não contém '_').", file.Key);
            }
        }
        
        logger.LogInformation("Tarefa de remoção de PDFs concluída.");
    }
}