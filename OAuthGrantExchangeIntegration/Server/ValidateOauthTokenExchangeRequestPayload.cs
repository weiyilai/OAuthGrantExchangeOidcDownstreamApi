﻿using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mail;
using System.Security.Claims;

namespace OAuthGrantExchangeIntegration.Server;

public static class ValidateOauthTokenExchangeRequestPayload
{
    public static (bool Valid, string Reason, string Error) IsValid(OauthTokenExchangePayload oauthTokenExchangePayload, OauthTokenExchangeConfiguration oauthTokenExchangeConfiguration)
    {
        if (!oauthTokenExchangePayload.grant_type.Equals(OAuthGrantExchangeConsts.GRANT_TYPE))
        {
            return (false, $"grant_type parameter has an incorrect value, expected {OAuthGrantExchangeConsts.GRANT_TYPE}",
                OAuthGrantExchangeConsts.ERROR_UNSUPPORTED_GRANT_TYPE);
        };

        if (!oauthTokenExchangePayload.subject_token_type.ToLower().Equals(OAuthGrantExchangeConsts.TOKEN_TYPE_ACCESS_TOKEN))
        {
            return (false, $"subject_token_type parameter has an incorrect value, expected {OAuthGrantExchangeConsts.TOKEN_TYPE_ACCESS_TOKEN}",
                OAuthGrantExchangeConsts.ERROR_INVALID_REQUEST);
        };

        if (!oauthTokenExchangePayload.audience!.Equals(oauthTokenExchangeConfiguration.Audience))
        {
            return (false, "OAuth token exchange client_id parameter has an incorrect value",
                OAuthGrantExchangeConsts.ERROR_INVALID_CLIENT);
        };

        if (!oauthTokenExchangePayload.scope!.ToLower().Equals(oauthTokenExchangeConfiguration.ScopeForNewAccessToken.ToLower()))
        {
            return (false, "scope parameter has an incorrect value",
                OAuthGrantExchangeConsts.ERROR_INVALID_SCOPE);
        };

        return (true, string.Empty, string.Empty);
    }

    public async static Task<(bool Valid, string Reason, ClaimsIdentity? ClaimsIdentity)> ValidateTokenAndSignature(
        string jwtToken,
        OauthTokenExchangeConfiguration oauthTokenExchangeConfiguration,
        ICollection<SecurityKey> signingKeys)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,
                ValidateIssuer = true,
                ValidIssuer = oauthTokenExchangeConfiguration.AccessTokenAuthority,
                ValidateAudience = true,
                ValidAudience = oauthTokenExchangeConfiguration.AccessTokenAudience
            };

            var tokenValidator = new JsonWebTokenHandler
            {
                MapInboundClaims = false
            };

            var tokenValidationResult = await tokenValidator.ValidateTokenAsync(jwtToken, validationParameters);

            return (true, string.Empty, tokenValidationResult.ClaimsIdentity);
        }
        catch (Exception ex)
        {
            return (false, $"Access Token Authorization failed {ex.Message}", null);
        }
    }

    public static bool IsDelegatedAadAccessToken(ClaimsIdentity claimsIdentity)
    {
        // oid if magic MS namespaces not user
        var oid = claimsIdentity.Claims.FirstOrDefault(t => t.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier");
        // scp if magic MS namespaces not added
        var scp = claimsIdentity.Claims.FirstOrDefault(t => t.Type == "http://schemas.microsoft.com/identity/claims/scope");

        if (oid != null && scp != null)
        {
            return true;
        }

        oid = claimsIdentity.Claims.FirstOrDefault(t => t.Type == "oid");
        scp = claimsIdentity.Claims.FirstOrDefault(t => t.Type == "scp");
        if (oid != null && scp != null)
        {
            return true;
        }

        return false;
    }

    public static string GetPreferredUserName(ClaimsIdentity claimsIdentity)
    {
        var preferred_username = claimsIdentity.Claims.FirstOrDefault(t => t.Type == "preferred_username");
        return preferred_username?.Value ?? string.Empty;
    }

    public static string GetAzpacr(ClaimsIdentity claimsIdentity)
    {
        var azpacrClaim = claimsIdentity.Claims.FirstOrDefault(t => t.Type == "azpacr");
        return azpacrClaim?.Value ?? string.Empty;
    }

    public static string GetAzp(ClaimsIdentity claimsIdentity)
    {
        var azpClaim = claimsIdentity.Claims.FirstOrDefault(t => t.Type == "azp");
        return azpClaim?.Value ?? string.Empty;
    }

    public static bool IsEmailValid(string email)
    {
        if (!MailAddress.TryCreate(email, out var mailAddress))
            return false;

        // And if you want to be more strict:
        var hostParts = mailAddress.Host.Split('.');
        if (hostParts.Length == 1)
            return false; // No dot.
        if (hostParts.Any(p => p == string.Empty))
            return false; // Double dot.
        if (hostParts[^1].Length < 2)
            return false; // TLD only one letter.

        if (mailAddress.User.Contains(' '))
            return false;
        if (mailAddress.User.Split('.').Any(p => p == string.Empty))
            return false; // Double dot or dot at end of user part.

        return true;
    }
}
