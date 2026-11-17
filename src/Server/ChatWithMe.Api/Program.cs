using System.Text.Json.Serialization;
using ChatWithMe.Api.Auth;
using ChatWithMe.Api.Filters;
using ChatWithMe.Api.Handlers;
using ChatWithMe.Api.Hosting;
using ChatWithMe.Api.Middleware;
using ChatWithMe.Api.OpenApi;
using ChatWithMe.Api.Realtime;
using ChatWithMe.Api.Serialization;
using ChatWithMe.Application.Admin;
using ChatWithMe.Application.Auth;
using ChatWithMe.Application.Conversations;
using ChatWithMe.Application.Files;
using ChatWithMe.Application.Friendships;
using ChatWithMe.Application.Messages;
using ChatWithMe.Application.Realtime;
using ChatWithMe.Application.Users;
using ChatWithMe.Infrastructure;
using ChatWithMe.Infrastructure.ConnectionStrings;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Rewrite;
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
builder.Services.Configure<AuthCookieOptions>(builder.Configuration.GetSection(AuthCookieOptions.SectionName));

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
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new LocalDateTimeConverter(timeZone));
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
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
builder.Services.AddScoped<IConversationAppService, ConversationAppService>();
builder.Services.AddScoped<IFriendAppService, FriendAppService>();
builder.Services.AddScoped<IAdminUserAppService, AdminUserAppService>();
builder.Services.AddScoped<IAdminAuthAppService, AdminAuthAppService>();
builder.Services.AddScoped<IAdminAccountAppService, AdminAccountAppService>();

builder.Services.AddSignalR();
builder.Services.AddSingleton<IOnlineUserRegistry, InMemoryOnlineUserRegistry>();
builder.Services.AddSingleton<IConnectionTracker, InMemoryConnectionTracker>();
builder.Services.AddSingleton<IOnlineSessionTerminator, SignalROnlineSessionTerminator>();
builder.Services.AddSingleton<IChatNotifier, SignalRChatNotifier>();
builder.Services.AddHostedService<AdminBootstrapService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

// 关键密钥属于"配置错误"而非"运行期依赖不可用"：与 Jwt:SigningKey 一致，启动阶段 fail-fast，
// 不再让数据库相关端点静默降级。远端连接串服务不可用才走"缓存兜底 + 健康检查暴露"。
DeliveryKeyProvider.ValidateAtStartup(builder.Configuration[DeliveryKeyProvider.ConfigurationKey]);

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

    var status = connectionStringProvider.GetStatus();
    app.Logger.LogInformation(
        "连接串来源：{Source}（缓存时间 {CachedAtUtc}，端点 {Endpoint}）：{Detail}",
        status.Source,
        status.CachedAtUtc,
        status.Endpoint,
        status.Detail);
}

app.UseExceptionHandler();
app.UseRouting();
app.UseCors(CorsPolicyName);
app.UseMiddleware<UploadSizeLimitMiddleware>();
app.UseAuthentication();
app.UseMiddleware<SessionVersionMiddleware>();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("ChatWithMe API"));
app.MapGet("/", () => Results.Redirect("~/scalar"));
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

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
