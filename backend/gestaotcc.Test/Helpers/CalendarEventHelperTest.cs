using System;
using FluentAssertions;
using gestaotcc.Application.Helpers;
using Xunit;

namespace gestaotcc.Test.Helpers;

public class CalendarEventHelperTest
{
    [Fact]
    public void BuildIcsContent_ShouldGenerateValidIcsStructure()
    {
        // Arrange
        long tccId = 42;
        string title = "Sistema de Gestão de TCCs";
        string summary = "Apresentação final do trabalho de conclusão de curso com banca examinadora.";
        string location = "Auditório Central - Bloco B";
        DateTime scheduledDate = new DateTime(2026, 12, 10, 14, 0, 0, DateTimeKind.Utc);

        // Act
        var ics = CalendarEventHelper.BuildIcsContent(tccId, title, summary, location, scheduledDate);

        // Assert
        ics.Should().NotBeNullOrWhiteSpace();
        ics.Should().StartWith("BEGIN:VCALENDAR\r\n");
        ics.Should().EndWith("END:VCALENDAR\r\n");
        ics.Should().Contain("VERSION:2.0\r\n");
        ics.Should().Contain("METHOD:REQUEST\r\n");
        ics.Should().Contain("BEGIN:VEVENT\r\n");
        ics.Should().Contain($"UID:defesa-tcc-{tccId}@gradusflow.ifpe.edu.br\r\n");
        ics.Should().Contain("STATUS:CONFIRMED\r\n");
        ics.Should().Contain("SUMMARY:Defesa de TCC: Sistema de Gestão de TCCs\r\n");
        ics.Should().Contain("LOCATION:Auditório Central - Bloco B\r\n");
        ics.Should().Contain("DTSTART:20261210T140000Z\r\n");
        ics.Should().Contain("DTEND:20261210T160000Z\r\n");
        ics.Should().Contain("END:VEVENT\r\n");
    }

    [Fact]
    public void BuildIcsContent_WhenFieldsAreNullOrSpecial_ShouldEscapeProperly()
    {
        // Arrange
        long tccId = 99;
        string title = "Título; com vírgula, ponto e vírgula";
        string? summary = "Linha 1\nLinha 2\r\nLinha 3\\teste";
        string location = "Sala 101, Prédio 2; Campus";
        DateTime scheduledDate = new DateTime(2026, 5, 20, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var ics = CalendarEventHelper.BuildIcsContent(tccId, title, summary, location, scheduledDate);

        // Assert
        ics.Should().Contain(@"SUMMARY:Defesa de TCC: Título\; com vírgula\, ponto e vírgula");
        ics.Should().Contain(@"LOCATION:Sala 101\, Prédio 2\; Campus");
        ics.Should().Contain(@"Linha 1\nLinha 2\nLinha 3\\teste");
    }

    [Fact]
    public void BuildGoogleCalendarUrl_ShouldGenerateValidUrlWithParameters()
    {
        // Arrange
        string title = "Defesa de TCC";
        string description = "Apresentação para banca examinadora";
        string location = "Sala Virtual Meet";
        DateTime scheduledDate = new DateTime(2026, 11, 25, 9, 0, 0, DateTimeKind.Utc);

        // Act
        var url = CalendarEventHelper.BuildGoogleCalendarUrl(title, description, location, scheduledDate);

        // Assert
        url.Should().StartWith("https://calendar.google.com/calendar/render?action=TEMPLATE");
        url.Should().Contain("dates=20261125T090000Z/20261125T110000Z");
        url.Should().Contain("text=Defesa%20de%20TCC%3A%20Defesa%20de%20TCC");
        url.Should().Contain("location=Sala%20Virtual%20Meet");
    }

    [Fact]
    public void BuildOutlookCalendarUrl_ShouldGenerateValidUrlWithParameters()
    {
        // Arrange
        string title = "Defesa de TCC - Automação";
        string description = "Reunião de avaliação";
        string location = "Auditório";
        DateTime scheduledDate = new DateTime(2026, 11, 25, 15, 0, 0, DateTimeKind.Utc);

        // Act
        var url = CalendarEventHelper.BuildOutlookCalendarUrl(title, description, location, scheduledDate);

        // Assert
        url.Should().StartWith("https://outlook.live.com/calendar/0/deeplink/compose");
        url.Should().Contain("startdt=2026-11-25T15%3A00%3A00Z");
        url.Should().Contain("enddt=2026-11-25T17%3A00%3A00Z");
        url.Should().Contain("subject=Defesa%20de%20TCC%3A%20Defesa%20de%20TCC%20-%20Automa%C3%A7%C3%A3o");
        url.Should().Contain("location=Audit%C3%B3rio");
    }
}
