namespace gestaotcc.WebApi.InputModel;

public record EvaluateRegisteredTccInputModel(
    long TccId,
    decimal Grade,
    string? EvaluationComments,
    string? EvaluationDetails);
