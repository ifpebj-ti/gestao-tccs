namespace gestaotcc.Domain.Dtos.Campi;

public record FindAllCampiDTO(long Id, string Name, List<CourseDetailsForFindAllCampiDTO> Courses);