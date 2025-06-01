using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.UserTcc;

namespace gestaotcc.Domain.Entities.Tcc;

public class TccEntityBuilder
{
    private long _id;
    private string? _title;
    private string? _summary;
    private string _status = string.Empty;
    private string _step = string.Empty;
    private DateTime _creationDate;
    private ICollection<UserTccEntity> _userTccs = new List<UserTccEntity>();
    private ICollection<TccInviteEntity> _tccInvites = new List<TccInviteEntity>();
    private ICollection<DocumentEntity> _documents = new List<DocumentEntity>();

    public TccEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public TccEntityBuilder WithTitle(string? title)
    {
        _title = title;
        return this;
    }

    public TccEntityBuilder WithSummary(string? summary)
    {
        _summary = summary;
        return this;
    }

    public TccEntityBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public TccEntityBuilder WithStep(string step)
    {
        _step = step;
        return this;
    }

    public TccEntityBuilder WithCreationDate(DateTime creationDate)
    {
        _creationDate = creationDate;
        return this;
    }

    public TccEntityBuilder WithUserTccs(ICollection<UserTccEntity> userTccs)
    {
        _userTccs = userTccs;
        return this;
    }

    public TccEntityBuilder WithTccInvites(ICollection<TccInviteEntity> tccInvites)
    {
        _tccInvites = tccInvites;
        return this;
    }

    public TccEntityBuilder WithDocuments(ICollection<DocumentEntity> documents)
    {
        _documents = documents;
        return this;
    }

    public TccEntity Build()
    {
        return new TccEntity(_id, _title, _summary, _status, _step, _creationDate, _userTccs, _tccInvites, _documents);
    }
}