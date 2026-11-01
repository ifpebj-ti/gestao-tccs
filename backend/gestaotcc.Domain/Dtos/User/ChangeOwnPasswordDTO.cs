using System.Text.Json.Serialization;

namespace gestaotcc.Domain.Dtos.User;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record ChangeOwnPasswordDTO(string CurrentPassword, string NewPassword);
