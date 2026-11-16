namespace ChatWithMe.Application.Auth;

public interface IAuthAppService
{
    Task<AuthTokenDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthTokenDto> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
}
