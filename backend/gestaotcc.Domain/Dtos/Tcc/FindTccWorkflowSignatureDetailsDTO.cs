namespace gestaotcc.Domain.Dtos.Tcc;

public record FindTccWorkflowSignatureDetailsDTO(long UserId, string UserProfile, string UserName, bool IsSigned);