using MecaManage.Application.Features.Invoices.Queries;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MecaManage.API.Pdf;

public class InvoicePdfDocument : IDocument
{
    private readonly InvoicePdfDto _inv;

    public InvoicePdfDocument(InvoicePdfDto inv) => _inv = inv;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    /// <summary>Generates the PDF and returns it as a byte array.</summary>
    public byte[] GeneratePdf() => Document.Create(Compose).GeneratePdf();

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

            page.Header().Element(Header);
            page.Content().PaddingTop(20).Element(Content);
            page.Footer().Element(Footer);
        });
    }

    private void Header(IContainer c)
    {
        c.Row(row =>
        {
            // Left: Garage info
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_inv.GarageName)
                    .Bold().FontSize(22).FontColor(Colors.Indigo.Darken3);
                col.Item().Text(_inv.GarageAddress).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"{_inv.GarageCity}  ·  {_inv.GaragePhone}").FontColor(Colors.Grey.Darken1);
            });

            // Right: Invoice meta
            row.ConstantItem(170).Column(col =>
            {
                col.Item().AlignRight().Text("DEVIS / FACTURE")
                    .Bold().FontSize(14).FontColor(Colors.Indigo.Darken2);
                col.Item().AlignRight().Text($"N° {_inv.InvoiceNumber}")
                    .Bold().FontSize(12).FontColor(Colors.Grey.Darken3);
                col.Item().AlignRight().Text($"Date : {_inv.CreatedAt:dd/MM/yyyy}")
                    .FontColor(Colors.Grey.Darken1);
                col.Item().AlignRight()
                    .Text($"Statut : {StatusLabel(_inv.Status)}")
                    .FontColor(StatusColor(_inv.Status));
            });
        });

        c.PaddingTop(8).LineHorizontal(1).LineColor(Colors.Indigo.Lighten3);
    }

    private void Content(IContainer c)
    {
        c.Column(col =>
        {
            // Client info
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(12).Column(inner =>
                {
                    inner.Item().Text("FACTURER À").Bold().FontSize(8)
                        .FontColor(Colors.Grey.Medium).LetterSpacing(0.05f);
                    inner.Item().PaddingTop(4).Text(_inv.ClientName)
                        .Bold().FontSize(12);
                    inner.Item().Text(_inv.ClientEmail).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrWhiteSpace(_inv.ClientPhone))
                        inner.Item().Text(_inv.ClientPhone).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(20);
                row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(12).Column(inner =>
                {
                    inner.Item().Text("DÉTAILS FACTURE").Bold().FontSize(8)
                        .FontColor(Colors.Grey.Medium).LetterSpacing(0.05f);
                    inner.Item().PaddingTop(4).Text($"Émise le : {_inv.CreatedAt:dd/MM/yyyy}");
                    if (_inv.FinalizedAt.HasValue)
                        inner.Item().Text($"Finalisée le : {_inv.FinalizedAt:dd/MM/yyyy}");
                });
            });

            // Line items table
            col.Item().PaddingTop(20).Element(LineItemsTable);

            // Totals
            col.Item().PaddingTop(8).AlignRight().Column(totals =>
            {
                totals.Item().Row(r =>
                {
                    r.ConstantItem(200).AlignRight().Text("Main d'œuvre :").FontColor(Colors.Grey.Darken1);
                    r.ConstantItem(100).AlignRight().Text($"{_inv.ServiceFee:F2} €").FontColor(Colors.Grey.Darken2);
                });
                totals.Item().Row(r =>
                {
                    r.ConstantItem(200).AlignRight().Text("Pièces :").FontColor(Colors.Grey.Darken1);
                    r.ConstantItem(100).AlignRight().Text($"{_inv.PartsTotal:F2} €").FontColor(Colors.Grey.Darken2);
                });
                if (_inv.TaxAmount.GetValueOrDefault() > 0)
                {
                    totals.Item().Row(r =>
                    {
                        r.ConstantItem(200).AlignRight().Text("TVA :").FontColor(Colors.Grey.Darken1);
                        r.ConstantItem(100).AlignRight().Text($"{_inv.TaxAmount:F2} €").FontColor(Colors.Grey.Darken2);
                    });
                }
                totals.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Indigo.Lighten3);
                totals.Item().PaddingTop(4).Row(r =>
                {
                    r.ConstantItem(200).AlignRight().Text("TOTAL TTC :").Bold().FontSize(13);
                    r.ConstantItem(100).AlignRight().Text($"{_inv.TotalAmount:F2} €")
                        .Bold().FontSize(13).FontColor(Colors.Indigo.Darken2);
                });
            });
        });
    }

    private void LineItemsTable(IContainer c)
    {
        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(4);   // Description
                cols.RelativeColumn(1);   // Qty
                cols.RelativeColumn(1.5f); // Unit price
                cols.RelativeColumn(1.5f); // Total
            });

            // Header row
            table.Header(header =>
            {
                header.Cell().Background(Colors.Indigo.Darken2).Padding(6)
                    .Text("Description").Bold().FontColor(Colors.White).FontSize(9);
                header.Cell().Background(Colors.Indigo.Darken2).Padding(6)
                    .AlignCenter().Text("Qté").Bold().FontColor(Colors.White).FontSize(9);
                header.Cell().Background(Colors.Indigo.Darken2).Padding(6)
                    .AlignRight().Text("P.U. HT").Bold().FontColor(Colors.White).FontSize(9);
                header.Cell().Background(Colors.Indigo.Darken2).Padding(6)
                    .AlignRight().Text("Total").Bold().FontColor(Colors.White).FontSize(9);
            });

            bool even = false;
            foreach (var li in _inv.LineItems)
            {
                var bg = even ? Colors.White : Colors.Grey.Lighten5;
                even = !even;
                table.Cell().Background(bg).Padding(6).Text(li.Description);
                table.Cell().Background(bg).Padding(6).AlignCenter().Text(li.Quantity.ToString());
                table.Cell().Background(bg).Padding(6).AlignRight().Text($"{li.UnitPrice:F2} €");
                table.Cell().Background(bg).Padding(6).AlignRight().Text($"{li.LineTotal:F2} €");
            }
        });
    }

    private void Footer(IContainer c)
    {
        c.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text("Merci de votre confiance.")
                    .FontColor(Colors.Grey.Medium).Italic();
                row.ConstantItem(80).AlignRight().Text(x =>
                {
                    x.Span("Page ").FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontColor(Colors.Grey.Medium);
                    x.Span(" / ").FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontColor(Colors.Grey.Medium);
                });
            });
        });
    }

    private static string StatusLabel(string status) => status switch
    {
        "Draft"           => "Brouillon",
        "AwaitingApproval"=> "En attente d'approbation",
        "Approved"        => "Approuvé",
        "Rejected"        => "Refusé",
        "Paid"            => "Payé",
        _                 => status,
    };

    private static string StatusColor(string status) => status switch
    {
        "Approved" or "Paid" => Colors.Green.Darken2,
        "Rejected"           => Colors.Red.Darken2,
        "AwaitingApproval"   => Colors.Orange.Darken2,
        _                    => Colors.Grey.Darken1,
    };
}

