namespace gestaotcc.Domain.Dtos.Tcc;

public record ReformulateTccProposalDTO(
    string Title,
    string Summary,
    long? AdvisorId = null
);
