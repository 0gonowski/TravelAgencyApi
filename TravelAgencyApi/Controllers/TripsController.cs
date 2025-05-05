using Microsoft.AspNetCore.Mvc;
using TravelAgencyApi.Services;

namespace TravelAgencyApi.Controllers;

[ApiController]
[Route("api/trips")]
public class TripsController(IDbService dbService) : ControllerBase
{
    private readonly IDbService _dbService = dbService;

    // Endpoint 1: GET /api/trips
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTrips()
    {
        try
        {
            var trips = await _dbService.GetTripsAsync();
            return Ok(trips);
        }
        catch (Exception e)
        {
            // Logowanie błędu
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred retrieving trips: {e.Message}");
        }
    }
}