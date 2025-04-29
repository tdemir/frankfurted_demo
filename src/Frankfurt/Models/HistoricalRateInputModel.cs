using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Frankfurt.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Frankfurt.Models;

public class HistoricalRateInputModel
{
    //string baseCurrency, string startDate, DateTime endDate, int page
    // historical?base=EUR&startDate=2023-01-01&endDate=2023-01-31&page=1
    [FromQuery(Name = "base")]
    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    public string BaseCurrency { get; set; } = string.Empty;


    [Required]
    [FromQuery(Name = "startDate")]
    [DateValidation]
    public string StartDate { get; set; } = string.Empty;


    [Required]
    [FromQuery(Name = "endDate")]
    [DateValidation]
    [DateCompareValidation(nameof(StartDate))]
    public string EndDate { get; set; } = string.Empty;


    [Required]
    [Range(1, int.MaxValue)]
    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;
}