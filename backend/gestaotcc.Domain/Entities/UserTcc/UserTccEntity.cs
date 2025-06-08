using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.UserTcc;

public class UserTccEntity
{
    public long Id { get; set; }
    public UserEntity User { get; set; }
    public long UserId { get; set; }
    public TccEntity Tcc { get; set; }
    public long TccId { get; set; }
    public ProfileEntity Profile { get; set; }
    public long ProfileId { get; set; }
    public DateTime BindingDate { get; set; }
    public UserTccEntity() { }

    public UserTccEntity(UserEntity user, TccEntity tcc, ProfileEntity profile, DateTime bindingDate)
    {
        this.User = user;
        this.Tcc = tcc;
        this.Profile = profile;
        this.BindingDate = bindingDate;
    }
}