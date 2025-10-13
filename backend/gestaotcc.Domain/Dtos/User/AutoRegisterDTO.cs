namespace gestaotcc.Domain.Dtos.User;

public record AutoRegisterDTO(
    string Name, 
    string Email, 
    string? Registration, 
    string CPF);