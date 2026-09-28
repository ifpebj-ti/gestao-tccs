namespace gestaotcc.Domain.Dtos.Email;

public class EmailAttachmentDTO
{
    public string FileName { get; set; }
    public byte[] FileBytes { get; set; }
    public string ContentType { get; set; }

    public EmailAttachmentDTO(string fileName, byte[] fileBytes, string contentType)
    {
        FileName = fileName;
        FileBytes = fileBytes;
        ContentType = contentType;
    }
}
