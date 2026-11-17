using ChatWithMe.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithMe.Api.Handlers;

internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = MapStatusCode(exception);

        if (statusCode is null)
        {
            return false;
        }

        logger.LogWarning(exception, "请求失败，返回 {StatusCode}：{Message}", statusCode.Value, exception.Message);

        httpContext.Response.StatusCode = statusCode.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = TitleFor(statusCode.Value),
                Detail = exception.Message,
            },
        });
    }

    private static int? MapStatusCode(Exception exception) => exception switch
    {
        FileTooLargeException => StatusCodes.Status413PayloadTooLarge,
        BadHttpRequestException badRequest when badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge => StatusCodes.Status413PayloadTooLarge,
        InvalidDataException => StatusCodes.Status413PayloadTooLarge,
        FileValidationException => StatusCodes.Status400BadRequest,
        AuthenticationFailedException => StatusCodes.Status401Unauthorized,
        ForbiddenException => StatusCodes.Status403Forbidden,
        NotFoundException => StatusCodes.Status404NotFound,
        ConflictException => StatusCodes.Status409Conflict,
        UserAlreadyExistsException => StatusCodes.Status409Conflict,
        MongoUnavailableException => StatusCodes.Status503ServiceUnavailable,
        ArgumentException => StatusCodes.Status400BadRequest,
        _ => null,
    };

    private static string TitleFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "请求参数不合法",
        StatusCodes.Status401Unauthorized => "身份验证失败",
        StatusCodes.Status403Forbidden => "无权访问",
        StatusCodes.Status404NotFound => "资源不存在",
        StatusCodes.Status409Conflict => "资源冲突",
        StatusCodes.Status413PayloadTooLarge => "请求体超过大小限制",
        StatusCodes.Status503ServiceUnavailable => "依赖服务不可用",
        _ => "请求处理失败",
    };
}
