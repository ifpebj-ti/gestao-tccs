namespace gestaotcc.Domain.Dtos.Tcc;

public record FindTccWorkflowSignatureDTO(
    long DocumentId, 
    string AttachmentName, 
    List<FindTccWorkflowSignatureDetailsOnlyDocsDTO>? DetailsOnlyDocs,
    List<FindTccWorkflowSignatureDetailsNotOnlyDocsDTO>? DetailsNotOnlyDocs);