
namespace gestaotcc.Domain.Dtos.User;
public record CampiDetailsForFindAllUserDTO(
    long Id,
    string Name,
    CourseDetailsForFindAllUserDTO Course
    );
