using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// Observación de uso (2026-11-02): la interfaz no debe mostrar identificadores de requisitos (RF-xxx, RN-xxx, AMB-xxx).
// Siguen permitidos en comentarios de código (trazabilidad): @* ... *@, // y /* ... */.
public class SinCodigosDeRequisitosEnPantallaTests
{
    private static string RaizFuentes()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src", "FitnessFrontend"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", "FitnessFrontend");
    }

    private static string SinComentarios(string texto)
    {
        texto = Regex.Replace(texto, @"@\*.*?\*@", "", RegexOptions.Singleline);
        texto = Regex.Replace(texto, @"<!--.*?-->", "", RegexOptions.Singleline);
        texto = Regex.Replace(texto, @"/\*.*?\*/", "", RegexOptions.Singleline);
        texto = Regex.Replace(texto, @"(?m)^\s*//.*$", "");
        texto = Regex.Replace(texto, @"(?m)\s//[^""\n]*$", "");
        return texto;
    }

    [Fact]
    public void LasPantallasYLosMensajesNoMuestranCodigosDeRequisitos()
    {
        var raiz = RaizFuentes();
        var archivos = new[] { "Paginas", "Componentes", "Layout", "Servicios" }
            .SelectMany(c => Directory.EnumerateFiles(Path.Combine(raiz, c), "*.*", SearchOption.AllDirectories))
            .Where(f => f.EndsWith(".razor") || f.EndsWith(".cs"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToList();
        Assert.NotEmpty(archivos);

        var codigo = new Regex(@"\b(RF|RN|AMB|RNF|OPS)-[A-Z]?\d+", RegexOptions.IgnoreCase);
        var infracciones = archivos
            .SelectMany(f => SinComentarios(File.ReadAllText(f)).Split('\n')
                .Where(l => codigo.IsMatch(l))
                .Select(l => $"{Path.GetRelativePath(raiz, f)}: {l.Trim()}"))
            .ToList();

        Assert.True(infracciones.Count == 0, "Códigos de requisitos visibles:\n" + string.Join("\n", infracciones));
    }
}
