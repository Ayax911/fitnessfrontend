using System;
using System.Collections.Generic;
using System.Text;

namespace FitnessFrontend.Servicios.Reportes;

// RF-033: Generador de CSV en función pura
// UTF-8 con BOM (\uFEFF) para Excel, separador ';', cabeceras en español y protección contra inyección de fórmulas
public static class GeneradorCsv
{
    private const string BomUtf8 = "\uFEFF";
    private const string Separador = ";";
    private static readonly char[] CaracteresPeligrososFormula = { '=', '+', '-', '@' };

    public static string Generar(IReadOnlyList<string> cabeceras, IEnumerable<IReadOnlyList<string>> filas)
    {
        var sb = new StringBuilder();
        sb.Append(BomUtf8);

        // Cabeceras
        for (int i = 0; i < cabeceras.Count; i++)
        {
            if (i > 0) sb.Append(Separador);
            sb.Append(FormatearCampo(cabeceras[i]));
        }
        sb.Append("\r\n");

        // Filas
        foreach (var fila in filas)
        {
            for (int i = 0; i < fila.Count; i++)
            {
                if (i > 0) sb.Append(Separador);
                sb.Append(FormatearCampo(fila[i]));
            }
            sb.Append("\r\n");
        }

        return sb.ToString();
    }

    private static string FormatearCampo(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;

        var texto = valor;

        // Protección contra inyección de fórmulas CSV (OWASP CSV Injection)
        if (CaracteresPeligrososFormula.Contains(texto[0]))
        {
            texto = "'" + texto;
        }

        // Si contiene separador, comillas dobles o saltos de línea, se encierra entre comillas
        if (texto.Contains(Separador) || texto.Contains("\"") || texto.Contains("\n") || texto.Contains("\r"))
        {
            return $"\"{texto.Replace("\"", "\"\"")}\"";
        }

        return texto;
    }
}
