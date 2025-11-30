using System.ComponentModel.DataAnnotations;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentAssertions;
using observatorio.saude.Infra.Services;

namespace observatorio.saude.tests.Infra.Services;

public class TesteExportDto
{
    [Display(Name = "ID do Registro")] public int Id { get; set; }

    [Display(Name = "Nome Completo")] public string Nome { get; set; } = string.Empty;

    [Display(Name = "Valor Decimal")] public decimal Valor { get; set; }

    [Display(Name = "Data de Criação")] public DateTime Data { get; set; } = new(2025, 10, 17, 10, 30, 0);
}

public class FileExportServiceTest
{
    private readonly List<TesteExportDto> _mockData;
    private readonly FileExportService _service;

    public FileExportServiceTest()
    {
        _service = new FileExportService();
        _mockData =
        [
            new TesteExportDto { Id = 1, Nome = "Alice", Valor = 10.50M },
            new TesteExportDto { Id = 2, Nome = "Bob", Valor = 20.75M, Data = new DateTime(2024, 1, 1) },
            new TesteExportDto { Id = 3, Nome = "Charlie", Valor = 30.00M, Data = new DateTime(2023, 6, 15) }
        ];
    }


    [Fact]
    public void GenerateExcelDeveGerarCabecalhoEmNegritoApenasComDisplayAttribute()
    {
        var excelBytes = _service.GenerateExcel(_mockData);

        using var stream = new MemoryStream(excelBytes);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        worksheet.Cell("A1").GetValue<string>().Should().Be("ID do Registro");

        worksheet.Cell("B1").GetValue<string>().Should().Be("Nome Completo");
        worksheet.Cell("E1").IsEmpty().Should().BeTrue();

        worksheet.Row(1).Style.Font.Bold.Should().BeTrue();
    }

    [Fact]
    public void GenerateExcelDeveExportarTodosOsRegistrosCorretamenteAjustarColunas()
    {
        var excelBytes = _service.GenerateExcel(_mockData);

        using var stream = new MemoryStream(excelBytes);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        worksheet.Cell("A2").GetValue<int>().Should().Be(1);
        worksheet.Cell("B2").GetValue<string>().Should().Be("Alice");

        worksheet.Name.Should().Be(nameof(TesteExportDto).Replace("Dto", ""));

        worksheet.Column(2).Width.Should().BeGreaterThan(0);
    }


    [Fact]
    public async Task GenerateCsvStreamAsyncDeveGerarConteudoCorretoComDelimitadorPontoVirgula()
    {
        var asyncData = _mockData.ToAsyncEnumerable();
        await using var outputStream = new MemoryStream();

        await _service.GenerateCsvStreamAsync(asyncData, outputStream);

        outputStream.Position = 0;
        using var streamReader = new StreamReader(outputStream);
        var csvContent = await streamReader.ReadToEndAsync();

        var records = csvContent.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

        records[0].Should().Contain("ID do Registro;");
        records[1].Should().StartWith("1;Alice;10.50;");
    }


    [Fact]
    public async Task GenerateXlsxStreamAsyncDeveGerarPlanilhaComCabecalhoECelulasCorretas()
    {
        var asyncData = _mockData.ToAsyncEnumerable();
        await using var outputStream = new MemoryStream();

        await _service.GenerateXlsxStreamAsync(asyncData, outputStream);

        outputStream.Position = 0;
        using var spreadsheetDocument = SpreadsheetDocument.Open(outputStream, false);

        spreadsheetDocument.WorkbookPart.Should().NotBeNull();
        var workbookPart = spreadsheetDocument.WorkbookPart!;

        workbookPart.Workbook.Should().NotBeNull();
        workbookPart.Workbook.Sheets.Should().NotBeNull();

        var sheet = workbookPart.Workbook.Sheets!.First().Should().NotBeNull().And.BeOfType<Sheet>().Subject;
        sheet.Id.Should().NotBeNull();

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        worksheetPart.Should().NotBeNull();

        var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
        sheetData.Should().NotBeNull();

        var headerRow = sheetData.Elements<Row>().First();
        headerRow.Should().NotBeNull();
        headerRow.Elements<Cell>().ElementAt(0).InnerText.Should().Contain("ID do Registro");

        var dataRow1 = sheetData.Elements<Row>().ElementAt(1);
        dataRow1.Should().NotBeNull();

        dataRow1.Elements<Cell>().ElementAt(0).DataType.Should().NotBeNull();
        dataRow1.Elements<Cell>().ElementAt(0).DataType!.Value.Should().Be(CellValues.Number);

        dataRow1.Elements<Cell>().ElementAt(1).DataType.Should().NotBeNull();
        dataRow1.Elements<Cell>().ElementAt(1).DataType!.Value.Should().Be(CellValues.String);
    }
}

public static class EnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable)
    {
        foreach (var item in enumerable)
        {
            await Task.Yield();
            yield return item;
        }
    }
}