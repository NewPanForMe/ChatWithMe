using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Common;
using ChatWithMe.Application.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChatWithMe.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(
    IFileAppService fileAppService,
    IFileLinkSigner fileLinkSigner,
    IOptions<FileUploadOptions> options) : ControllerBase
{
    private const long MaxUploadBytes = 32 * 1024 * 1024;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<PagedResult<FileAssetDto>>> GetListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        [FromQuery] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var result = await fileAppService.ListAsync(page, pageSize, keyword, cancellationToken);

        return Ok(result);
    }

    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<FileAssetDto>> UploadAsync([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "未收到文件", Detail = "请用 multipart/form-data 的 file 字段上传文件。" });
        }

        await using var content = file.OpenReadStream();
        var fileAsset = await fileAppService.UploadAsync(content, file.FileName, file.ContentType, file.Length, cancellationToken);

        return Created(fileAsset.Url, fileAsset);
    }

    [HttpPost("upload-batch")]
    [Authorize]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<IReadOnlyList<FileAssetDto>>> UploadBatchAsync([FromForm] List<IFormFile>? files, CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "未收到文件", Detail = "请用 multipart/form-data 的 files 字段上传文件。" });
        }

        var limit = options.Value.MaxFilesPerRequest;

        if (files.Count > limit)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "文件数量超过上限", Detail = "一次最多上传 " + limit + " 个文件，本次 " + files.Count + " 个。" });
        }

        var uploaded = new List<FileAssetDto>(files.Count);

        foreach (var file in files)
        {
            await using var content = file.OpenReadStream();
            uploaded.Add(await fileAppService.UploadAsync(content, file.FileName, file.ContentType, file.Length, cancellationToken));
        }

        return Ok(uploaded);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FileAssetDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var fileAsset = await fileAppService.GetAsync(id, cancellationToken);

        return fileAsset is null ? NotFound() : Ok(fileAsset);
    }

    /// <summary>
    /// 文件回读。两种放行方式：带 Bearer 的登录用户；或带 <c>exp</c> + <c>sig</c> 的短时签名链接
    /// （由 <c>GET /api/files/{id}/link</c> 生成）。不再有匿名静态目录。
    /// </summary>
    [HttpGet("{id:guid}/content")]
    [AllowAnonymous]
    public async Task<IActionResult> GetContentAsync(
        Guid id,
        [FromQuery] long? exp,
        [FromQuery] string? sig,
        CancellationToken cancellationToken)
    {
        if (!HasSignatureAccess(id, exp, sig) && User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "身份验证失败",
                Detail = "文件回读需要 Bearer 令牌，或使用 GET /api/files/{id}/link 生成的短时签名链接。",
            });
        }

        var content = await fileAppService.OpenContentAsync(id, cancellationToken);

        return content is null
            ? NotFound()
            : File(content.Content, content.ContentType, enableRangeProcessing: true);
    }

    /// <summary>为登录用户生成短时签名回读链接（文件未公开暴露，链接过期即失效）。</summary>
    [HttpGet("{id:guid}/link")]
    [Authorize]
    public async Task<ActionResult<FileLinkDto>> CreateLinkAsync(Guid id, CancellationToken cancellationToken)
    {
        var link = await fileAppService.CreateLinkAsync(id, cancellationToken);

        return link is null ? NotFound() : Ok(link);
    }

    private bool HasSignatureAccess(Guid id, long? exp, string? sig)
    {
        if (exp is null || string.IsNullOrWhiteSpace(sig))
        {
            return false;
        }

        DateTime expiresAtUtc;

        try
        {
            expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(exp.Value).UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        return fileLinkSigner.Verify(id, expiresAtUtc, sig);
    }
}
