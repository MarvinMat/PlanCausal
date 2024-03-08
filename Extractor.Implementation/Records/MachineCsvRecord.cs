namespace Extractor.Implementation.Records;

using System;
using CsvHelper.Configuration.Attributes;

public class MachineCsvRecord
{
    [Name("Kostenstelle")]
    public int CostCenter { get; set; }

    [Name("APL")]
    public int WorkplaceId { get; set; }

    [Name("Jahr-KW")]
    public string YearWeek { get; set; }

    [Name("Jahr")]
    public int Year { get; set; }

    [Name("Monat")]
    public int Month { get; set; }

    [Name("Quartal")]
    public string Quarter { get; set; }

    [Name("Jahr-Monat")]
    public string YearMonth { get; set; }

    [Name("Jahr-Quartal")]
    public string YearQuarter { get; set; }

    [Name("leer1")]
    public string EmptyField { get; set; }

    [Name("Monat")]
    [Format("MM.yyy")]
    public DateTime MonthDate { get; set; }

    [Name("Datum")]
    [Format("dd.MM.yyyy")]
    public DateTime Date { get; set; }

    [Name("Interne ID des Arbeitsplatzes")]
    public long InternalWorkplaceId { get; set; }

    [Name("Arbeitsplatz")]
    public string Workplace { get; set; }

    [Name("Vorgangstext")]
    public string ProcessText { get; set; }

    [Name("Summe Stillstandszeiten")]
    public decimal TotalDowntime { get; set; }

    [Name("Stillstand Technischer Stop")]
    public decimal TechnicalStopDowntime { get; set; }

    [Name("Stillstand kein Personal")]
    public decimal NoPersonnelDowntime { get; set; }

    [Name("Stillstand unproduktive Zeit")]
    public decimal UnproductiveDowntime { get; set; }

    [Name("Geplante Stillstandszeiten")]
    public decimal PlannedDowntime { get; set; }

    [Name("Produktive Maschinenstunden")]
    public decimal ProductiveMachineHours { get; set; }

    [Name("Gruppe")]
    public string Group { get; set; }
}
