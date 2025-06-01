namespace gestaotcc.Domain.Dtos.Tcc;

public record FindTccWorkflowDTO(long TccId, long Step, List<FindTccWorkflowSignatureDTO> Signatures);