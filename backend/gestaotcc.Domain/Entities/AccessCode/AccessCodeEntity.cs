using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.AccessCode;

public class AccessCodeEntity
{
    public long Id { get;  set; }
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get;  set; } = true;
    public bool IsUserUpdatePassword { get;  set; } = false;
    public DateTime ExpirationDate { get;  set; }
    public long UserId { get; set; }
    public UserEntity User { get; set; }
    
    public AccessCodeEntity() { }
    
    public AccessCodeEntity(long id, string code, bool isActive, bool isUserUpdatePassword, DateTime expirationDate)
    {
        Id = id;
        Code = code;
        IsActive = isActive;
        IsUserUpdatePassword = isUserUpdatePassword;
        ExpirationDate = expirationDate;
    }
}