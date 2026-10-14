using System.Text.RegularExpressions;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Enums;

namespace gestaotcc.Application.Helpers;

public static class StudentProfileValidation
{
    public static bool IsValid(UpdateStudentProfileDTO data) =>
        ValidText(data.Name, 3, 100) && ValidText(data.Registration, 1, 50) &&
        ValidText(data.UserClass, 1, 50) && Enum.IsDefined(typeof(ShiftType), data.Shift) &&
        IsValidCpf(data.Cpf);

    private static bool ValidText(string? value, int min, int max) =>
        value?.Trim().Length >= min && value.Trim().Length <= max && !value.Any(char.IsControl);

    public static bool IsValidCpf(string? value)
    {
        if (value is null || !Regex.IsMatch(value, @"^(\d{11}|\d{3}\.\d{3}\.\d{3}-\d{2})$")) return false;
        var digits = Regex.Replace(value, @"[^0-9]", "");
        if (digits.All(digit => digit == digits[0])) return false;
        for (var size = 9; size <= 10; size++)
        {
            var sum = Enumerable.Range(0, size).Sum(index => (digits[index] - '0') * (size + 1 - index));
            var check = sum * 10 % 11;
            if (check == 10) check = 0;
            if (check != digits[size] - '0') return false;
        }
        return true;
    }
}
