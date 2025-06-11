using gestaotcc.Domain.Entities.Tcc;

namespace gestaotcc.Domain.Entities.TccCancellation;
public class TccCancellationEntityBuilder
{
    private long _id;
    private string _reason = string.Empty;
    private string _status = "PENDING"; // PENDING, APPROVED
    private long _tccId;

    public TccCancellationEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }
    public TccCancellationEntityBuilder WithReason(string reason)
    {
        _reason = reason;
        return this;
    }
    public TccCancellationEntityBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }
    public TccCancellationEntityBuilder WithTccId(long tccId)
    {
        _tccId = tccId;
        return this;
    }
    public TccCancellationEntity Build()
    {
        return new TccCancellationEntity(_id, _reason, _status, _tccId);
    }
}
