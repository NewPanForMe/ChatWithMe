using ChatWithMe.Application.Auth;
using ChatWithMe.Domain.Users;
using ChatWithMe.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ChatWithMe.Api.Auth;

internal static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        jwtOptions.Validate();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtOptions.GetSigningKeyBytes()),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.ChatUser, policy => policy.RequireClaim(AuthClaimNames.PrincipalType, AuthClaimNames.PrincipalUser));
            options.AddPolicy(
            AuthPolicies.Admin,
            policy => policy
                .RequireClaim(AuthClaimNames.Role, UserRole.Admin.ToString())
                .RequireClaim(AuthClaimNames.PrincipalType, AuthClaimNames.PrincipalAdmin));
        });

        return services;
    }
}
