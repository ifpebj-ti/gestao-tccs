using gestaotcc.Domain.Dtos.InfosTcc;

namespace gestaotcc.Domain.Dtos.Tcc;
public record FindTccDTO(InfoTccDTO InfoTcc, List<InfoStudentDTO> InfoStudent, InfoAdvisorDTO InfoAdvisor, InfoBankingDTO InfoBanking, bool CancellationRequest);
