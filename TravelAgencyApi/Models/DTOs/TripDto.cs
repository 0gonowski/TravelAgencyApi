namespace TravelAgencyApi.Models.DTOs;

public class TripDto
{
    public int IdTrip { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public List<CountryDto> Countries { get; set; } = new List<CountryDto>();
}