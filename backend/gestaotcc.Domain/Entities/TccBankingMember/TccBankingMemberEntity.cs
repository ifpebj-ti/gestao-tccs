using gestaotcc.Domain.Entities.Tcc;

namespace gestaotcc.Domain.Entities.TccBankingMember;

public class TccBankingMemberEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // e.g. "Examinador Interno", "Examinador Externo"
    public string AccessToken { get; set; } = string.Empty;
    public DateTime? TokenExpiryDate { get; set; }
    public long TccId { get; set; }
    public TccEntity Tcc { get; set; } = null!;

    public TccBankingMemberEntity() { }

    public TccBankingMemberEntity(string name, string email, string role, string accessToken, long tccId)
    {
        Name = name;
        Email = email;
        Role = role;
        AccessToken = accessToken;
        TccId = tccId;
    }
}
