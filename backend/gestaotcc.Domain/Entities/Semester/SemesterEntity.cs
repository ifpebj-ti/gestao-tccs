using gestaotcc.Domain.Entities.Tcc;

namespace gestaotcc.Domain.Entities.Semester;

public class SemesterEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<TccEntity> Tccs { get; set; } = new List<TccEntity>();

    public SemesterEntity() { }

    public SemesterEntity(long id, string name, DateTime startDate, DateTime endDate, bool isActive)
    {
        Id = id;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }

    public void Update(string name, DateTime startDate, DateTime endDate, bool isActive)
    {
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }
}
