using gestaotcc.WebApi.ResponseModels.Course;
using gestaotcc.WebApi.ResponseModels.Profile;

namespace gestaotcc.WebApi.ResponseModels.User;

public record UserResponse(long Id, string Name, string Email, string Registration, string CPF, string SIAPE, string? Password, string Status, ICollection<ProfileResponse> Profile, CourseResponse Course);
