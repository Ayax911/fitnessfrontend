using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FitnessFrontend.Servicios.Identidad;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

public class JwtTokenParserTests
{
    [Fact]
    public void ParseClaimsFromJwt_DebeExtraerSubEmailYRoles()
    {
        var guid = Guid.NewGuid();
        var payloadObj = new
        {
            sub = guid.ToString(),
            email = "estudiante@correo.itm.edu.co",
            role = new[] { "Usuario", "Entrenador" },
            exp = 1800000000
        };

        var json = JsonSerializer.Serialize(payloadObj);
        var base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var fakeJwt = $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.{base64Payload}.c2lnbmF0dXJl";

        var claims = JwtTokenParser.ParseClaimsFromJwt(fakeJwt).ToList();

        Assert.Equal(guid.ToString(), claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("estudiante@correo.itm.edu.co", claims.First(c => c.Type == ClaimTypes.Email).Value);

        var roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains("Usuario", roles);
        Assert.Contains("Entrenador", roles);
    }
}
