using System;
using FitnessFrontend.Servicios.Infraestructura;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

public class ZonaHorariaServiceTests
{
    private readonly IZonaHorariaService _servicio = new ZonaHorariaService();

    [Fact]
    public void AHoraLocal_DebeConvertirUtcABogota_MenosCincoHoras()
    {
        // 2026-10-15 15:30:00 UTC -> 2026-10-15 10:30:00 -05:00
        var utc = new DateTimeOffset(2026, 10, 15, 15, 30, 0, TimeSpan.Zero);

        var local = _servicio.AHoraLocal(utc);

        Assert.Equal(TimeSpan.FromHours(-5), local.Offset);
        Assert.Equal(2026, local.Year);
        Assert.Equal(10, local.Month);
        Assert.Equal(15, local.Day);
        Assert.Equal(10, local.Hour);
        Assert.Equal(30, local.Minute);
    }

    [Fact]
    public void FormatearHora_DebeRetornarFormatoDoceHorasConAmPm()
    {
        // 2026-09-28 19:00:00 UTC = 14:00 (02:00 p. m.) en Bogotá
        var utc = new DateTimeOffset(2026, 9, 28, 19, 0, 0, TimeSpan.Zero);

        var horaStr = _servicio.FormatearHora(utc);

        Assert.Contains("02:00", horaStr);
        Assert.Contains("p.", horaStr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatearFecha_DebeRetornarFormatoDiaMesAnio()
    {
        var utc = new DateTimeOffset(2026, 9, 28, 3, 0, 0, TimeSpan.Zero);
        // En Bogotá UTC-5 son las 22:00 del 27 de septiembre
        var fechaStr = _servicio.FormatearFecha(utc);

        Assert.Equal("27/09/2026", fechaStr);
    }

    [Fact]
    public void FormatearRango_MismoDia_DebeCompactarFormato()
    {
        var inicioUtc = new DateTimeOffset(2026, 10, 1, 13, 0, 0, TimeSpan.Zero); // 08:00 Bogotá
        var finUtc = new DateTimeOffset(2026, 10, 1, 15, 0, 0, TimeSpan.Zero);    // 10:00 Bogotá

        var rango = _servicio.FormatearRango(inicioUtc, finUtc);

        Assert.Contains("01/10/2026", rango);
        Assert.Contains("08:00", rango);
        Assert.Contains("10:00", rango);
    }

    [Fact]
    public void AUtc_DebeConvertirHoraLocalBogotaAUtc_MasCincoHoras()
    {
        // 2026-10-15 08:30:00 hora local Bogotá -> 2026-10-15 13:30:00 UTC
        var local = new DateTime(2026, 10, 15, 8, 30, 0);

        var utc = _servicio.AUtc(local);

        Assert.Equal(TimeSpan.Zero, utc.Offset);
        Assert.Equal(2026, utc.Year);
        Assert.Equal(10, utc.Month);
        Assert.Equal(15, utc.Day);
        Assert.Equal(13, utc.Hour);
        Assert.Equal(30, utc.Minute);
    }

    [Fact]
    public void ObtenerFechaHoyLocal_DebeRetornarFechaEnZonaBogota()
    {
        var hoyLocal = _servicio.ObtenerFechaHoyLocal();

        var ahoraBogota = _servicio.AHoraLocal(DateTimeOffset.UtcNow);
        Assert.Equal(ahoraBogota.Date, hoyLocal);
        Assert.Equal(TimeSpan.Zero, hoyLocal.TimeOfDay);
    }
}
