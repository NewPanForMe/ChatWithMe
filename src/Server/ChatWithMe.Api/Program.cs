using ChatWithMe.Api.Auth;
using ChatWithMe.Api.Filters;
using ChatWithMe.Api.Handlers;
using ChatWithMe.Api.Middleware;
using ChatWithMe.Api.OpenApi;
using ChatWithMe.Api.Serialization;
using ChatWithMe.Application.Auth;
using ChatWithMe.Application.Files;
using ChatWithMe.Application.Messages;
using ChatWithMe.Application.Users;
using ChatWithMe.Infrastructure;
using ChatWithMe.Infrastructure.ConnectionStrings;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;

const string CorsPolicyName = "ClientApp";
const long FormOverheadBytes = 64 * 1024;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

var timeZone = TimeZoneResolution.Resolve(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var fileUploadOptions = builder.Configuration.GetSection("FileStorage").Get<FileUploadOptions>() ?? new FileUploadOptions();
var allowLocalhostOrigins = builder.Environment.IsDevelopment();

var uploadRoot = Path.GetFullPath(
    Path.IsPathRooted(fileUploadOptions.StorageRoot)
        ? fileUploadOptions.StorageRoot
        : Path.Combine(builder.Environment.ContentRootPath, fileUploadOptions.StorageRoot));

Directory.CreateDirectory(uploadRoot);

builder.Services.Configure<FileUploadOptions>(builder.Configuration.GetSection("FileStorage"));

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = fileUploadOptions.MaxFileSizeBytes + FormOverheadBytes;
    options.ValueLengthLimit = (int)Math.Min(fileUploadOptions.MaxFileSizeBytes, int.MaxValue);
    options.MultipartHeadersLengthLimit = 16 * 1024;
    options.MemoryBufferThreshold = 64 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = fileUploadOptions.MaxFileSizeBytes + FormOverheadBytes);

builder.Services.AddSingleton(timeZone);

builder.Services.AddControllers(options => options.Filters.Add<RequestSizeLimitFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new LocalDateTimeConverter(timeZone)));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

builder.Services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
    .SetIsOriginAllowed(origin => IsOriginAllowed(origin, allowedOrigins, allowLocalhostOrigins))
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddScoped<IUserAppService, UserAppService>();
builder.Services.AddScoped<IFileAppService, FileAppService>();
builder.Services.AddScoped<IAuthAppService, AuthAppService>();
builder.Services.AddScoped<IMessageAppService, MessageAppService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.Logger.LogInformation("时间输出时区：{TimeZone}", TimeZoneResolution.Describe(timeZone));

using (var startupScope = app.Services.CreateScope())
{
    var connectionStringProvider = startupScope.ServiceProvider.GetRequiredService<IConnectionStringProvider>();

    try
    {
        var connectionString = connectionStringProvider.GetConnectionString();
        app.Logger.LogInformation("远端连接串解析成功（已脱敏）：{ConnectionString}", ConnectionStringMasker.Mask(connectionString));
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "远端连接串解析失败，数据库相关端点暂不可用（其他端点仍可访问）：{Reason}", exception.Message);
    }
}

app.UseExceptionHandler();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadRoot),
    RequestPath = string.IsNullOrWhiteSpace(fileUploadOptions.PublicUrlPrefix) ? "/uploads" : fileUploadOptions.PublicUrlPrefix,
});
app.UseRouting();
app.UseCors(CorsPolicyName);
app.UseMiddleware<UploadSizeLimitMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("ChatWithMe API"));
app.MapGet("/", () => Results.Redirect("~/scalar"));
app.MapControllers();

app.Run();

static bool IsOriginAllowed(string origin, string[] allowedOrigins, bool allowLocalhostOrigins)
{
    if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!allowLocalhostOrigins || !Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    return uri.Scheme == Uri.UriSchemeHttp && (uri.Host == "localhost" || uri.Host == "127.0.0.1");
}
