namespace gestaotcc.Domain.Dtos.Tcc;

public record SubmitTccProposalDTO(
    string Title,
    string Summary,
    long AdvisorId
);
