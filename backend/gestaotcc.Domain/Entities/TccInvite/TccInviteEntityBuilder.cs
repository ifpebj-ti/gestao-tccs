using gestaotcc.Domain.Entities.Tcc;

namespace gestaotcc.Domain.Entities.TccInvite;

public class TccInviteEntityBuilder
{
    private long _id;
    private string _email = string.Empty;
    private string _code = string.Empty;
    private TccEntity _tcc = null!;

    public TccInviteEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public TccInviteEntityBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public TccInviteEntityBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public TccInviteEntityBuilder WithTcc(TccEntity tcc)
    {
        _tcc = tcc;
        return this;
    }

    public TccInviteEntity Build()
    {
        return new TccInviteEntity(_id, _email, _code, _tcc);
    }
}