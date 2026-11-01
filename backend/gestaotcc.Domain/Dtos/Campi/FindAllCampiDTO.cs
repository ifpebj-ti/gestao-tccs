namespace gestaotcc.Domain.Dtos.Campi;

public record FindAllCampiDTO(long Id, string Name, string City, List<CourseDetailsForFindAllCampiDTO> Courses);