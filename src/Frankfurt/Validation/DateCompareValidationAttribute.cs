using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Frankfurt.Validation;

public class DateCompareValidationAttribute : ValidationAttribute
{
    private readonly string _comparisonProperty;

    public DateCompareValidationAttribute(string comparisonProperty)
    {
        _comparisonProperty = comparisonProperty;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success;

        var property = validationContext.ObjectType.GetProperty(_comparisonProperty);
        if (property == null)
        {
            throw new ArgumentException($"Property {_comparisonProperty} not found");
        }

        var comparisonValue = property.GetValue(validationContext.ObjectInstance);
        if (comparisonValue == null) return ValidationResult.Success;
        var str = value.ToString();

        if (ParseDates(comparisonValue.ToString()) > ParseDates(str))
        {
            return new ValidationResult("Start date must be lower than end date");
        }

        return ValidationResult.Success;
    }

    private DateTime ParseDates(string str)
    {
        if (DateTime.TryParseExact(str, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
        {
            return data;
        }
        return DateTime.MinValue;
    }
}