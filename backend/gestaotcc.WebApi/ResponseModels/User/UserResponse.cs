using gestaotcc.WebApi.ResponseModels.CampiCourse;
using gestaotcc.WebApi.ResponseModels.Course;
using gestaotcc.WebApi.ResponseModels.Profile;

namespace gestaotcc.WebApi.ResponseModels.User;

public record UserResponse(
    long Id, 
    string Name, 
    string Email, 
    string Registration, 
    string CPF, 
    string SIAPE, 
    string? Password, 
    string Status,
    string Phone,
    string? UserClass,
    string? Shift,
    string? Titration,
    ICollection<ProfileResponse> Profile, 
    CampiCourseResponse CampiCourse);
