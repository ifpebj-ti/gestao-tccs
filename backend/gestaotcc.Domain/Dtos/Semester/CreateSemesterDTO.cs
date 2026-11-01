namespace gestaotcc.Domain.Dtos.Semester;

public class CreateSemesterDTO
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
