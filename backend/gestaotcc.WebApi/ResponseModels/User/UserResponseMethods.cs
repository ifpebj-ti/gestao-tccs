using gestaotcc.Domain.Entities.User;
using gestaotcc.WebApi.ResponseModels.CampiCourse;
using gestaotcc.WebApi.ResponseModels.Course;
using gestaotcc.WebApi.ResponseModels.Profile;

namespace gestaotcc.WebApi.ResponseModels.User;

public class UserResponseMethods
{
    //verificar
    public static UserResponse CreateUserResponse(UserEntity user)
    {
        var campiCourse = new CampiCourseResponse(
            user.CampiCourse.CampiId,
            user.CampiCourse.Campi.Name,
            user.CampiCourse.CourseId,
            user.CampiCourse.Course.Name
            );
        
        return new UserResponseBuilder()
            .WithId(user.Id)
            .WithName(user.Name)
            .WithEmail(user.Email)
            .WithRegistration(user.Registration)
            .WithCpf(user.CPF)
            .WithSiape(user.SIAPE)
            .WithStatus(user.Status)
            .WithProfile(ProfileResponseMethods.CreateProfileResponse(user.Profile))
            .WithCampiCourses(campiCourse)
            .Build();
    }
}
