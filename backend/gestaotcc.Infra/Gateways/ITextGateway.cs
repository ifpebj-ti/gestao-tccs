using gestaotcc.Application.Gateways;

namespace gestaotcc.Infra.Gateways;

public class ITextGateway : IITextGateway
{
    public Task<MemoryStream> FillPdf(Dictionary<string, string> fields, MemoryStream ms)
    {
        using var reader = new iText.Kernel.Pdf.PdfReader(ms);
        using var output = new MemoryStream();
        using var writer = new iText.Kernel.Pdf.PdfWriter(output);
        using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader, writer);
        var form = iText.Forms.PdfAcroForm.GetAcroForm(pdfDoc, true);

        foreach (var field in fields)
        {
            if (form.GetField(field.Key) != null) // só preenche se o campo existir
            {
                form.GetField(field.Key).SetValue(field.Value ?? "");
            }
        }
        pdfDoc.Close();
        
        output.Position = 0;
        
        return Task.FromResult(output);
    }
}