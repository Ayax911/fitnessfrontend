using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;

namespace FitnessFrontend.Servicios.Identidad;

public static class JwtTokenParser
{
    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return claims;
        }

        var payload = parts[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs == null)
        {
            return claims;
        }

        foreach (var kvp in keyValuePairs)
        {
            if (kvp.Value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        var claimType = MapearTipoClaim(kvp.Key);
                        claims.Add(new Claim(claimType, item.ToString()));
                    }
                }
                else
                {
                    var claimType = MapearTipoClaim(kvp.Key);
                    claims.Add(new Claim(claimType, element.ToString()));
                }
            }
            else
            {
                var claimType = MapearTipoClaim(kvp.Key);
                claims.Add(new Claim(claimType, kvp.Value.ToString() ?? string.Empty));
            }
        }

        return claims;
    }

    private static string MapearTipoClaim(string tipoOriginal) => tipoOriginal switch
    {
        "role" or "roles" => ClaimTypes.Role,
        "name" or "unique_name" => ClaimTypes.Name,
        "email" => ClaimTypes.Email,
        "sub" or "nameid" => ClaimTypes.NameIdentifier,
        _ => tipoOriginal
    };

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
