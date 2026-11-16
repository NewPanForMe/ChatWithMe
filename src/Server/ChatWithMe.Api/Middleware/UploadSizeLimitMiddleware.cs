using ChatWithMe.Application.Files;
using Microsoft.Extensions.Options;

namespace ChatWithMe.Api.Middleware;

internal sealed class UploadSizeLimitMiddleware
{
    private const int MultipartSlackBytes = 1024 * 1024;

    private readonly RequestDelegate _next;
    private readonly long _limit;

    public UploadSizeLimitMiddleware(RequestDelegate next, IOptions<FileUploadOptions> options)
    {
        _next = next;
        _limit = options.Value.MaxFileSizeBytes + MultipartSlackBytes;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsUploadRequest(context.Request))
        {
            var declaredLength = context.Request.ContentLength;

            if (declaredLength.HasValue && declaredLength.Value > _limit)
            {
                await TypedResults.Problem(
                        detail: "请求体约 " + declaredLength.Value + " 字节，超过上限 " + _limit + " 字节。",
                        statusCode: StatusCodes.Status413PayloadTooLarge,
                        title: "请求体超过大小限制")
                    .ExecuteAsync(context);
                return;
            }
        }

        await _next(context);
    }

    private static bool IsUploadRequest(HttpRequest request) =>
        HttpMethods.IsPost(request.Method) && request.Path.StartsWithSegments("/api/files/upload");
}
