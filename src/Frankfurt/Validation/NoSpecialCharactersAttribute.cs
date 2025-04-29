using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Frankfurt.Validation;

public class NoSpecialCharactersAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success;

        var str = value.ToString();
        if (Regex.IsMatch(str!, @"^[a-zA-Z0-9]+$"))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("Field contains invalid characters");
    }
}