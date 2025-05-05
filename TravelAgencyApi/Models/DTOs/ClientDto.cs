using System.ComponentModel.DataAnnotations;

namespace TravelAgencyApi.Models.DTOs; 

public class ClientDto
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(120)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(120)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [MaxLength(120)]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Telephone { get; set; }

    [Required(ErrorMessage = "PESEL is required.")]
    [MaxLength(120)]
    public string Pesel { get; set; } = string.Empty;
}