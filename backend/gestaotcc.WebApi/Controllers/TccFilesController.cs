using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gestaotcc.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/Tcc/{tccId:long}/files")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TccFilesController(TccFilesUseCase files) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(long tccId)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        var result = await files.List(tccId, userId);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.ErrorDetails?.Status ?? 500, result.ErrorDetails);
    }

    [HttpGet("{documentId:long}/download")]
    public async Task<IActionResult> Download(long tccId, long documentId)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        return ToDownload(await files.Download(tccId, userId, documentId));
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadAll(long tccId)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        return ToDownload(await files.DownloadAll(tccId, userId));
    }

    private bool TryUserId(out long userId) => long.TryParse(User.FindFirst("userId")?.Value, out userId) && userId > 0;
    private IActionResult ToDownload(ResultPattern<TccFileDownload> result)
    {
        if (result.IsFailure) return StatusCode(result.ErrorDetails?.Status ?? 500, result.ErrorDetails);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Access-Control-Expose-Headers"] = "Content-Disposition";
        return File(result.Data.File, "application/octet-stream", result.Data.FileName);
    }
}
