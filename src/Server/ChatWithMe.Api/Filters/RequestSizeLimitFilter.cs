using ChatWithMe.Application.Files;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ChatWithMe.Api.Filters;

public sealed class RequestSizeLimitFilter : IResourceFilter, IActionFilter
{
    private const int MultipartSlackBytes = 1024 * 1024;

    private readonly FileUploadOptions _options;

    public RequestSizeLimitFilter(IOptions<FileUploadOptions> options) => _options = options.Value;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var tooLarge = BuildTooLargeResult(context.HttpContext);

        if (tooLarge is not null)
        {
            context.Result = tooLarge;
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var tooLarge = BuildTooLargeResult(context.HttpContext);

        if (tooLarge is not null)
        {
            context.Result = tooLarge;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private ObjectResult? BuildTooLargeResult(HttpContext httpContext)
    {
        var declaredLength = httpContext.Request.ContentLength;
        var limit = _options.MaxFileSizeBytes + MultipartSlackBytes;

        if (declaredLength is null || declaredLength.Value <= limit)
        {
            return null;
        }

        return new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status413PayloadTooLarge,
            Title = "请求体超过大小限制",
            Detail = "请求体约 " + declaredLength.Value + " 字节，超过上限 " + _options.MaxFileSizeBytes + " 字节。",
        })
        {
            StatusCode = StatusCodes.Status413PayloadTooLarge,
        };
    }
}
