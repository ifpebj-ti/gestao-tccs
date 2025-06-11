using System.Collections;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.TccCancellation;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.UserTcc;

namespace gestaotcc.Domain.Entities.Tcc;

public class TccEntity
{
    public long Id { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Step { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public ICollection<UserTccEntity> UserTccs { get; set; } = new List<UserTccEntity>();
    public ICollection<TccInviteEntity> TccInvites { get; set; } = new List<TccInviteEntity>();
    public ICollection<DocumentEntity> Documents { get; set; } = new List<DocumentEntity>();
    public TccCancellationEntity? TccCancellation { get; set; } = null;
    public TccEntity() { }

    public TccEntity(
        long id, 
        string? title, 
        string? summary, 
        string status, 
        string step,
        DateTime creationDate, 
        ICollection<UserTccEntity> userTccs, 
        ICollection<TccInviteEntity> tccInvites, 
        ICollection<DocumentEntity> documents)
    {
        this.Id = id;
        this.Title = title;
        this.Summary = summary;
        this.Status = status;
        this.Step = step;
        this.CreationDate = creationDate;
        this.UserTccs = userTccs;
        this.TccInvites = tccInvites;
        this.Documents = documents;
    }
}