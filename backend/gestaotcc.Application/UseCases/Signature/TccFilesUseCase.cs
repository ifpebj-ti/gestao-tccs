using System.IO.Compression;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public record TccFileDTO(long Id, string Name, string? Student, int SignatureCount, DateTime LastSignature);
public record TccFileDownload(byte[] File, string FileName);

public class TccFilesUseCase(ITccGateway tccGateway, IUserGateway userGateway, IStorageGateway storage)
{
    public async Task<TccEntity?> FindAccessible(long tccId, long userId)
    {
        if (tccId <= 0 || userId <= 0) return null;
        var user = await userGateway.FindById(userId);
        if (user is null || user.Status != "ACTIVE") return null;
        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null) return null;
        var roles = user.Profile.Select(p => p.Role).ToArray();
        var linked = tcc.UserTccs.Any(link => link.UserId == userId && roles.Contains(link.Profile.Role));
        var scopedManagement = roles.Any(role => role is "COORDINATOR" or "SUPERVISOR" or "LIBRARY")
            && user.CampiCourseId is > 0
            && tcc.UserTccs.Any(link => link.Profile.Role == "STUDENT" && link.User.CampiCourseId == user.CampiCourseId);
        return linked || roles.Contains("ADMIN") || scopedManagement ? tcc : null;
    }

    public async Task<ResultPattern<List<TccFileDTO>>> List(long tccId, long userId)
    {
        var tcc = await FindAccessible(tccId, userId);
        if (tcc is null) return ResultPattern<List<TccFileDTO>>.FailureResult("TCC não encontrado ou acesso indisponível.", 404);
        return ResultPattern<List<TccFileDTO>>.SuccessResult(SignedDocuments(tcc).Select(doc => new TccFileDTO(
            doc.Id, doc.DocumentType.Name ?? "Documento", tcc.UserTccs.FirstOrDefault(link => link.UserId == doc.UserId)?.User.Name,
            doc.Signatures.Count, doc.Signatures.Max(signature => signature.SignatureDate))).ToList());
    }

    public async Task<ResultPattern<TccFileDownload>> Download(long tccId, long userId, long documentId)
    {
        var tcc = await FindAccessible(tccId, userId);
        var doc = tcc is null ? null : SignedDocuments(tcc).FirstOrDefault(document => document.Id == documentId);
        if (doc is null) return ResultPattern<TccFileDownload>.FailureResult("Documento não encontrado ou acesso indisponível.", 404);
        return await Read(doc);
    }

    public async Task<ResultPattern<TccFileDownload>> DownloadAll(long tccId, long userId)
    {
        var tcc = await FindAccessible(tccId, userId);
        if (tcc is null) return ResultPattern<TccFileDownload>.FailureResult("TCC não encontrado ou acesso indisponível.", 404);
        var documents = SignedDocuments(tcc).ToList();
        if (documents.Count == 0) return ResultPattern<TccFileDownload>.FailureResult("Nenhum documento assinado disponível.", 404);
        using var output = new MemoryStream();
        long total = 0;
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach (var document in documents)
            {
                var result = await Read(document);
                if (result.IsFailure) return result;
                total += result.Data.File.Length;
                if (total > 50 * 1024 * 1024) return ResultPattern<TccFileDownload>.FailureResult("Baixe os documentos individualmente: o conjunto excede 50 MB.", 413);
                using var entry = archive.CreateEntry(result.Data.FileName).Open();
                await entry.WriteAsync(result.Data.File);
            }
        }
        return ResultPattern<TccFileDownload>.SuccessResult(new(output.ToArray(), $"TCC_{tccId}_assinados.zip"));
    }

    private static IEnumerable<DocumentEntity> SignedDocuments(TccEntity tcc) =>
        tcc.Documents.Where(doc => doc.Signatures.Count > 0 && !string.IsNullOrWhiteSpace(doc.FileName)).OrderBy(doc => doc.Id);

    private async Task<ResultPattern<TccFileDownload>> Read(DocumentEntity doc)
    {
        try
        {
            foreach (var extension in new[] { ".pdf", ".html" })
            {
                var bytes = await storage.Download(doc.FileName + extension, true);
                if (bytes.Length > 0) return ResultPattern<TccFileDownload>.SuccessResult(new(bytes, $"Documento_{doc.Id}{extension}"));
            }
            return ResultPattern<TccFileDownload>.FailureResult("Arquivo assinado não encontrado no armazenamento.", 404);
        }
        catch
        {
            return ResultPattern<TccFileDownload>.FailureResult("Não foi possível acessar o arquivo. Tente novamente.", 502);
        }
    }
}
