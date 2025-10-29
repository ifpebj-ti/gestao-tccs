using gestaotcc.Application.Gateways;
using iText.Kernel.Pdf;
using iText.Forms;
using iText.Signatures;

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
    
    public Task<(string hashToSignB64, byte[] preparedPdf)> PrepareEnvelopedSignature(byte[] pdfDocument)
        {
            using (var pdfStream = new MemoryStream(pdfDocument))
            using (var reader = new PdfReader(pdfStream))
            using (var outputStream = new MemoryStream())
            {
                // Usamos UseAppendMode para adicionar a assinatura sem invalidar o conteúdo existente
                var signer = new PdfSigner(reader, outputStream, new StampingProperties().UseAppendMode());

                // Define a aparência da assinatura (invisível, neste caso)
                // Um placeholder é necessário para que o iText saiba onde alocar a assinatura.
                signer.GetSignatureAppearance()
                    .SetPageNumber(1) // Pode ser qualquer página
                    .SetPageRect(new iText.Kernel.Geom.Rectangle(0, 0, 0, 0)); // Retângulo de tamanho 0x0

                // Objeto para calcular o digest (hash)
                // Requer o pacote nuget itext7.bouncycastle
                IExternalDigest digest = new BouncyCastleDigest();
                
                // Objeto que representa uma assinatura "em branco" (que será preenchida depois)
                IExternalSignature signature = new ExternalBlankSignature(PdfName.Adobe_PPKLite, PdfName.Adbe_pkcs7_detached);
                
                // 8192 bytes é um tamanho padrão reservado para o placeholder da assinatura
                int estimatedSize = 8192; 

                // Este é o método-chave. Ele faz 3 coisas:
                // 1. Prepara o 'outputStream' com o PDF contendo o placeholder (dicionário de assinatura vazio).
                // 2. Calcula os bytes-ranges corretos do 'outputStream'.
                // 3. Calcula o hash SHA-256 desses bytes-ranges e o retorna.
                // Embora "obsoleto" em favor do fluxo de container, é o método exato para *seu* fluxo desacoplado.
                byte[] hash = signer.ComputeHash(digest, signature, estimatedSize);
                
                // Fecha o signer, finalizando o 'outputStream'
                signer.Close();

                // Converte o hash para Base64
                string hashB64 = Convert.ToBase64String(hash);

                // Retorna o hash (para a API do Gov) e o PDF preparado (para o passo de Embutir)
                return Task.FromResult((hashB64, outputStream.ToArray()));
            }
        }

        /// <summary>
        /// PASSO 2 do Fluxo de Assinatura: Embuti a assinatura (obtida do Gov.br)
        /// no PDF que foi preparado pelo método anterior.
        /// </summary>
        public Task<byte[]> EmbedEnvelopedSignature(byte[] preparedPdf, string base64Signature)
        {
            byte[] signatureBytes = Convert.FromBase64String(base64Signature);

            using (var preparedStream = new MemoryStream(preparedPdf))
            using (var reader = new PdfReader(preparedStream))
            using (var outputStream = new MemoryStream())
            {
                // Abre o PDF preparado para escrita, em modo de apêndice
                using (var pdfDoc = new PdfDocument(reader, new PdfWriter(outputStream), new StampingProperties().UseAppendMode()))
                {
                    // Encontra o último dicionário de assinatura (o que acabamos de criar no 'Prepare')
                    PdfDictionary sigDict = pdfDoc.GetLastModifiedSignatureDictionary();
                    if (sigDict == null)
                    {
                        throw new InvalidOperationException("O PDF preparado não contém um dicionário de assinatura válido.");
                    }

                    // Cria o objeto PdfString para a assinatura (o PKCS#7)
                    // SetHexWriting(true) é crucial para formatar corretamente
                    // o binário da assinatura dentro do PDF.
                    PdfString contents = new PdfString(signatureBytes).SetHexWriting(true);

                    // Injeta a assinatura no placeholder /Contents
                    sigDict.Put(PdfName.Contents, contents);
                    
                    // Fecha o documento, salvando as alterações no 'outputStream'
                    pdfDoc.Close();
                }

                return Task.FromResult(outputStream.ToArray());
            }
        }
}