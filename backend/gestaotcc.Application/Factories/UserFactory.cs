using gestaotcc.Application.Helpers;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Utils;

namespace gestaotcc.Application.Factories;
public class UserFactory
{
    //verificar
    public static UserEntity CreateUser(CreateUserDTO data, List<ProfileEntity> profile, CampiCourseEntity campiCourse, AccessCodeEntity accessCode)
    {
        var randomPassword = PasswordHelper.GenerateRandomPassword();

        return new UserEntityBuilder()
            .WithName(data.Name)
            .WithEmail(data.Email)
            .WithRegistration(data.Registration ?? "")
            .WithCpf(data.CPF)
            .WithSiape(data.SIAPE ?? "")
            .WithPhone(data.Phone)
            .WithUserClass(data.UserClass)
            .WithShift(EnumExtension.GetDescription(data.Shift))
            .WithTitration(data.Titration)
            .WithPassword(randomPassword)
            .WithProfile(profile)
            .WithCampiCourse(campiCourse)
            .WithAccessCode(accessCode)
            .Build();
    }
    public static UserEntity CreateUser(AutoRegisterDTO data, List<ProfileEntity> profile, CampiCourseEntity campiCourse, AccessCodeEntity accessCode)
    {
        var randomPassword = PasswordHelper.GenerateRandomPassword();

        return new UserEntityBuilder()
            .WithName(data.Name)
            .WithEmail(data.Email)
            .WithRegistration(data.Registration ?? "")
            .WithCpf(data.CPF)
            .WithPassword(randomPassword)
            .WithPhone(data.Phone)
            .WithUserClass(data.UserClass)
            .WithShift(EnumExtension.GetDescription(data.Shift))
            .WithProfile(profile)
            .WithCampiCourse(campiCourse)
            .WithAccessCode(accessCode)
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
