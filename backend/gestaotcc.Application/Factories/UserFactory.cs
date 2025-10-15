using gestaotcc.Application.Helpers;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Factories;
public class UserFactory
{
    public static UserEntity CreateUser(CreateUserDTO data, List<ProfileEntity> profile, CampiCourseEntity campiCourse)
    {
        return new UserEntityBuilder()
            .WithName(data.Name)
            .WithEmail(data.Email)
            .WithRegistration(data.Registration ?? "")
            .WithCpf(data.CPF)
            .WithSiape(data.SIAPE ?? "")
            .WithProfile(profile)
            .WithCampiCourse(campiCourse)
            .Build();
    }
    public static UserEntity CreateUser(AutoRegisterDTO data, List<ProfileEntity> profile, CampiCourseEntity campiCourse)
    {
        return new UserEntityBuilder()
            .WithName(data.Name)
            .WithEmail(data.Email)
            .WithRegistration(data.Registration ?? "")
            .WithCpf(data.CPF)
            .WithProfile(profile)
            .WithCampiCourse(campiCourse)
            .Build();
    }
    
    public static FindAllUserByFilterDTO CreateFindAllUserByFilterDTO(UserEntity user)
    {
        return new FindAllUserByFilterDTO(
            user.Id,
            user.Name,
            user.Email,
            user.Profile.FirstOrDefault(x => x.Users.Any(x => x.Id == user.Id)).Role,
            user.CampiCourse.Course.Name
            );
    }
}
