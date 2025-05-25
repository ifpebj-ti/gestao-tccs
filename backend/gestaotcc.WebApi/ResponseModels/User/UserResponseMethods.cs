using gestaotcc.Domain.Entities.User;
using gestaotcc.WebApi.ResponseModels.Course;
using gestaotcc.WebApi.ResponseModels.Profile;

namespace gestaotcc.WebApi.ResponseModels.User;

public class UserResponseMethods
{
    public static UserResponse CreateUserResponse(UserEntity user)
    {
        return new UserResponseBuilder()
            .WithId(user.Id)
            .WithName(user.Name)
            .WithEmail(user.Email)
            .WithStatus(user.Status)
            .WithProfile(ProfileResponseMethods.CreateProfileResponse(user.Profile))
            .WithCourse(CourseResponseMethods.CreateCourseResponse(user.Course))
            .Build();
    }
}
