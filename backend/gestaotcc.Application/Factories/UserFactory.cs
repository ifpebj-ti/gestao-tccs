﻿using gestaotcc.Application.Helpers;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Factories;
public class UserFactory
{
    public static UserEntity CreateUser(CreateUserDTO data, List<ProfileEntity> profile, CourseEntity course, AccessCodeEntity accessCode)
    {
        var randomPassword = PasswordHelper.GenerateRandomPassword();

        return new UserEntityBuilder()
            .WithName(data.Name)
            .WithEmail(data.Email)
            .WithRegistration(data.Registration ?? "")
            .WithCpf(data.CPF)
            .WithSiape(data.SIAPE ?? "")
            .WithPassword(randomPassword)
            .WithProfile(profile)
            .WithCourse(course)
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
            user.Course.Name
            );
    }
}
