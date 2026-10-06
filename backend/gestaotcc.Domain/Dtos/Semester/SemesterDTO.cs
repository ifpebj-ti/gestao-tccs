namespace gestaotcc.Domain.Dtos.Semester;

public class SemesterDTO
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    
    public SemesterDTO(long id, string name, DateTime startDate, DateTime endDate, bool isActive)
    {
        Id = id;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = isActive;
    }
}
