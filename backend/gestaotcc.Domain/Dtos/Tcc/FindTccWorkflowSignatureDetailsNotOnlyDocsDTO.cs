namespace gestaotcc.Domain.Dtos.Tcc;

public record FindTccWorkflowSignatureDetailsNotOnlyDocsDTO(
    long UserId, 
    string UserProfile, 
    string UserName, 
    bool IsSigned, 
    List<FindTccWorkflowSignatureDetailsNotOnlyDocsOtherSignaturesDTO> OtherSignatures);