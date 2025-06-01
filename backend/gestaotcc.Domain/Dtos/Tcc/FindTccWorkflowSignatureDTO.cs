namespace gestaotcc.Domain.Dtos.Tcc;

public record FindTccWorkflowSignatureDTO(long DocumentId, string AttachmentName, List<FindTccWorkflowSignatureDetailsDTO> Details);