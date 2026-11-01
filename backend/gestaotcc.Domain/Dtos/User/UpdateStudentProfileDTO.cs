using System.Text.Json.Serialization;
using gestaotcc.Domain.Enums;

namespace gestaotcc.Domain.Dtos.User;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateStudentProfileDTO(
    string Name, string Phone, string Cpf, string Registration, string UserClass, ShiftType Shift);
