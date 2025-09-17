using gestaotcc.Application.Gateways;
using iText.Kernel.Pdf;
using iText.Forms;
namespace gestaotcc.Infra.Gateways;

public class ITextGateway : IITextGateway
{
    public Task<MemoryStream> FillPdf(Dictionary<string, string> fields, MemoryStream ms)
    {
        ms.Position = 0;

        var output = new MemoryStream();

        var reader = new PdfReader(ms);
        var writer = new PdfWriter(output);
        var pdfDoc = new PdfDocument(reader, writer);

        var form = PdfAcroForm.GetAcroForm(pdfDoc, true);

        foreach (var field in fields)
        {
            var pdfField = form.GetField(field.Key);
            if (pdfField != null)
            {
                pdfField.SetValue(field.Value ?? "");
            }
        }

        form.SetNeedAppearances(true);

        pdfDoc.Close(); // fecha apenas o pdf, não o MemoryStream

        return Task.FromResult(output);
    }
}