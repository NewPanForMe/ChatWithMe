using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChatWithMe.Client.Models;

namespace ChatWithMe.Client.Services;

/// <summary>
/// 后端 API 客户端。刷新令牌走 HttpOnly Cookie，所以必须共用同一个 CookieContainer。
/// 后端地址默认 http://localhost:5217，可用环境变量 CHATWITHME_API 覆盖。
/// </summary>
public sealed class ApiClient : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private string? _accessToken;

    public ApiClient() : this(CreateHttpClient())
    {
    }

    public ApiClient(HttpClient httpClient)
    {
        _http = httpClient;
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static HttpClient CreateHttpClient()
    {
        var baseUrl = Environment.GetEnvironmentVariable("CHATWITHME_API") ?? "http://localhost:5217";

        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true,
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30),
        };
    }

    public Uri BaseAddress => _http.BaseAddress!;

    public string? AccessToken => _accessToken;

    public event Action? SessionExpired;

    /// <summary>注册聊天用户（匿名接口），成功后需要再调用 LoginAsync。</summary>
    public async Task<UserDto> RegisterAsync(string email, string nickName, string password, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(
            "api/users",
            new { email, nickName, password },
            SerializerOptions,
            cancellationToken);

        return await ReadAsync<UserDto>(response, cancellationToken);
    }

    public async Task<AuthSessionDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password }, SerializerOptions, cancellationToken);
        var session = await ReadAsync<AuthSessionDto>(response, cancellationToken);
        _accessToken = session.AccessToken;

        return session;
    }

    public void ClearToken() => Interlocked.Exchange(ref _accessToken, null);

    private void ExpireSession(string rejectedToken)
    {
        if (Interlocked.CompareExchange(ref _accessToken, null, rejectedToken) == rejectedToken)
        {
            SessionExpired?.Invoke();
        }
    }

    public async Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "api/conversations", null, cancellationToken);

        return await ReadAsync<List<ConversationDto>>(response, cancellationToken);
    }

    public async Task<ConversationDto> CreateDirectConversationAsync(Guid peerUserId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            "api/conversations",
            new { type = "Direct", peerUserId },
            cancellationToken);

        return await ReadAsync<ConversationDto>(response, cancellationToken);
    }

    public async Task<UserSummaryDto?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            "api/friends/search?keyword=" + Uri.EscapeDataString(email),
            null,
            cancellationToken);

        var users = await ReadAsync<List<UserSummaryDto>>(response, cancellationToken);

        return users.FirstOrDefault(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            ?? users.FirstOrDefault();
    }

    public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "api/users/" + userId, null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadAsync<UserDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(Guid conversationId, int limit = 50, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"api/messages?conversationId={conversationId}&limit={limit}",
            null,
            cancellationToken);

        var page = await ReadAsync<MessagePageDto>(response, cancellationToken);

        return page.Items;
    }

    public async Task<MessageDto> SendMessageAsync(
        Guid conversationId,
        string content,
        string clientMessageId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            "api/messages",
            new { conversationId, content, clientMessageId },
            cancellationToken);

        return await ReadAsync<MessageDto>(response, cancellationToken);
    }


    public async Task<IReadOnlyList<UserSummaryDto>> SearchUsersForFriendAsync(string keyword, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            "api/friends/search?keyword=" + Uri.EscapeDataString(keyword),
            null,
            cancellationToken);

        return await ReadAsync<List<UserSummaryDto>>(response, cancellationToken);
    }

    public async Task<FriendRequestDto> SendFriendRequestAsync(Guid addresseeId, string? message, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            "api/friends/requests",
            new { addresseeId, message },
            cancellationToken);

        return await ReadAsync<FriendRequestDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<FriendRequestDto>> GetIncomingFriendRequestsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "api/friends/requests", null, cancellationToken);

        return await ReadAsync<List<FriendRequestDto>>(response, cancellationToken);
    }

    public async Task<FriendRequestDto> AcceptFriendRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/friends/requests/{requestId}/accept",
            null,
            cancellationToken);

        return await ReadAsync<FriendRequestDto>(response, cancellationToken);
    }

    public async Task<FriendRequestDto> RejectFriendRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Post,
            $"api/friends/requests/{requestId}/reject",
            null,
            cancellationToken);

        return await ReadAsync<FriendRequestDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<FriendDto>> GetFriendsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "api/friends", null, cancellationToken);

        return await ReadAsync<List<FriendDto>>(response, cancellationToken);
    }

    public async Task RemoveFriendAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, "api/friends/" + userId, null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }
    }

    public async Task BlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Post, "api/friends/blocked/" + userId, null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }
    }

    public async Task UnblockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, "api/friends/blocked/" + userId, null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<BlockedUserDto>> GetBlockedUsersAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "api/friends/blocked", null, cancellationToken);

        return await ReadAsync<List<BlockedUserDto>>(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var accessToken = _accessToken;
        var response = await SendOnceAsync(method, path, body, cancellationToken);

        // 访问令牌 15 分钟过期；刷新令牌在 HttpOnly Cookie 里，过期后自动换一次再重试。
        if (response.StatusCode != HttpStatusCode.Unauthorized || accessToken is null)
        {
            return response;
        }

        try
        {
            if (!await TryRefreshAsync(accessToken, cancellationToken))
            {
                return response;
            }
        }
        catch
        {
            response.Dispose();
            throw;
        }

        response.Dispose();
        accessToken = _accessToken;
        response = await SendOnceAsync(method, path, body, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && accessToken is not null)
        {
            ExpireSession(accessToken);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);

        if (_accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: SerializerOptions);
        }

        // 必须在这里 await：方法返回后 request 会被释放，未完成的发送会读到已释放的内容。
        return await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> TryRefreshAsync(string rejectedToken, CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_accessToken != rejectedToken)
            {
                return _accessToken is not null;
            }

            using var response = await _http.PostAsync("api/auth/refresh", content: null, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                ExpireSession(rejectedToken);
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw await CreateExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }

            var session = await response.Content
                .ReadFromJsonAsync<AuthSessionDto>(SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                throw new HttpRequestException("服务端返回的刷新会话无效，请稍后重试。");
            }

            return Interlocked.CompareExchange(ref _accessToken, session.AccessToken, rejectedToken) == rejectedToken;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }

        var value = await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);

        return value ?? throw new ApiException(response.StatusCode, "服务端返回了空响应。");
    }

    private static async Task<ApiException> CreateExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var fallback = $"请求失败（HTTP {(int)response.StatusCode}）";

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(SerializerOptions, cancellationToken);

            if (problem is not null)
            {
                var message = string.IsNullOrWhiteSpace(problem.Detail) ? problem.Title ?? fallback : problem.Detail;

                return new ApiException(response.StatusCode, message ?? fallback);
            }
        }
        catch (JsonException)
        {
            // 非 ProblemDetails 响应，用兜底文案。
        }

        return new ApiException(response.StatusCode, fallback);
    }

    public void Dispose() => _http.Dispose();

    private sealed record ProblemDetailsDto(string? Title, string? Detail, int? Status);
}

public sealed class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
