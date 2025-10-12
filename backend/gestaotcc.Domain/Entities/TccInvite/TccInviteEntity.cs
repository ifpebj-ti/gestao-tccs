using gestaotcc.Domain.Entities.Tcc;

namespace gestaotcc.Domain.Entities.TccInvite;

public class TccInviteEntity
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsValidCode { get; set; } = true;
    public long CampiId { get; set; }
    public long CourseId { get; set; }
    public TccEntity Tcc { get; set; } = null!;
    public long TccId { get; set; }
    public TccInviteEntity() { }

    public TccInviteEntity(long id, string email, string code, TccEntity tcc, long campiId, long courseId)
    {
        Id = id;
        Email = email;
        Code = code;
        Tcc = tcc;
        CampiId = campiId;
        CourseId = courseId;
    }
}