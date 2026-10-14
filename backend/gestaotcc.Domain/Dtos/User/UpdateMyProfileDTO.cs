using System.Text.Json.Serialization;

namespace gestaotcc.Domain.Dtos.User;

// Recusa campos adicionais: nome, identidade e permissões não são editáveis aqui.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateMyProfileDTO(string Phone);
