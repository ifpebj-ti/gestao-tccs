using System.Text.RegularExpressions;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.Helpers;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;
using gestaotcc.Domain.Utils;

namespace gestaotcc.Application.UseCases.User;

public class MyProfileUseCase(IUserGateway userGateway)
{
    public async Task<ResultPattern<MyProfileDTO>> Find(long userId)
    {
        if (userId <= 0)
            return ResultPattern<MyProfileDTO>.FailureResult("Sessão inválida.", 401);

        var user = await userGateway.FindById(userId);
        return user is null
            ? ResultPattern<MyProfileDTO>.FailureResult("Conta não encontrada.", 404)
            : ResultPattern<MyProfileDTO>.SuccessResult(ToProfile(user));
    }

    public async Task<ResultPattern<MyProfileDTO>> UpdatePhone(long userId, UpdateMyProfileDTO data)
    {
        if (userId <= 0)
            return ResultPattern<MyProfileDTO>.FailureResult("Sessão inválida.", 401);

        var phone = data.Phone?.Trim() ?? string.Empty;
        if (phone.Length > 20 || !Regex.IsMatch(phone, @"^[0-9() +\-]+$"))
            return ResultPattern<MyProfileDTO>.FailureResult("Informe um telefone válido com DDD.", 400);

        var digits = Regex.Replace(phone, @"[^0-9]", string.Empty);
        if (!Regex.IsMatch(digits, @"^[0-9]{10,11}$"))
            return ResultPattern<MyProfileDTO>.FailureResult("O telefone deve ter DDD e 10 ou 11 dígitos.", 400);

        var user = await userGateway.FindById(userId);
        if (user is null)
            return ResultPattern<MyProfileDTO>.FailureResult("Conta não encontrada.", 404);

        // Somente o telefone da conta autenticada é alterado.
        if (!await userGateway.UpdatePhone(userId, digits))
            return ResultPattern<MyProfileDTO>.FailureResult("Conta não encontrada.", 404);
        user.Phone = digits;
        return ResultPattern<MyProfileDTO>.SuccessResult(ToProfile(user));
    }

    private static MyProfileDTO ToProfile(UserEntity user) => new(
        user.Id, user.Name, user.Email, user.Phone, user.Registration, user.SIAPE,
        user.CampiCourse?.Campi?.Name, user.CampiCourse?.Course?.Name,
        user.Profile?.Select(profile => profile.Role).ToArray() ?? [],
        user.Profile?.Any(profile => profile.Role == "STUDENT") == true ? user.CPF : null,
        user.UserClass, user.Shift);

    public async Task<ResultPattern<MyProfileDTO>> UpdateStudent(long userId, UpdateStudentProfileDTO data)
    {
        if (userId <= 0) return ResultPattern<MyProfileDTO>.FailureResult("Sessão inválida.", 401);
        if (!StudentProfileValidation.IsValid(data))
            return ResultPattern<MyProfileDTO>.FailureResult("Confira nome, CPF, matrícula, turma e turno.", 400);
        var phone = data.Phone?.Trim() ?? "";
        var digits = Regex.Replace(phone, @"[^0-9]", "");
        if (phone.Length > 20 || !Regex.IsMatch(phone, @"^[0-9() +\-]+$") || !Regex.IsMatch(digits, @"^[0-9]{10,11}$"))
            return ResultPattern<MyProfileDTO>.FailureResult("Informe um telefone válido com DDD.", 400);
        var user = await userGateway.FindById(userId);
        if (user is null) return ResultPattern<MyProfileDTO>.FailureResult("Conta não encontrada.", 404);
        if (user.Profile?.Any(profile => profile.Role == "STUDENT") != true)
            return ResultPattern<MyProfileDTO>.FailureResult("Edição disponível apenas para estudantes.", 403);
        var clean = data with { Name = data.Name.Trim(), Phone = digits,
            Cpf = data.Cpf.Trim(), Registration = data.Registration.Trim(), UserClass = data.UserClass.Trim() };
        var shift = EnumExtension.GetDescription(data.Shift);
        if (!await userGateway.UpdateStudentProfile(userId, clean, shift))
            return ResultPattern<MyProfileDTO>.FailureResult("A conta foi alterada. Atualize a página e tente novamente.", 409);
        user.Name = clean.Name;
        user.Phone = clean.Phone;
        user.CPF = clean.Cpf;
        user.Registration = clean.Registration;
        user.UserClass = clean.UserClass;
        user.Shift = shift;
        return ResultPattern<MyProfileDTO>.SuccessResult(ToProfile(user));
    }
}
