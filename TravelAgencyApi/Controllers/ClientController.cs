using Microsoft.AspNetCore.Mvc;
using TravelAgencyApi.Exceptions;
using TravelAgencyApi.Models.DTOs;
using TravelAgencyApi.Services;

namespace TravelAgencyApi.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController(IDbService dbService) : ControllerBase
{
    private readonly IDbService _dbService = dbService;

    // Endpoint 3: POST /api/clients
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddClient([FromBody] ClientDto clientDto)
    {
        if (!await _dbService.IsEmailUniqueAsync(clientDto.Email))
        {
            return BadRequest($"Client with email '{clientDto.Email}' already exists.");
        }
        if (!await _dbService.IsPeselUniqueAsync(clientDto.Pesel))
        {
             return BadRequest($"Client with PESEL '{clientDto.Pesel}' already exists.");
        }
        try
        {
            var newClientId = await _dbService.AddClientAsync(clientDto);
            return StatusCode(StatusCodes.Status201Created);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while adding the client: {e.Message}");
        }
    }

    // Endpoint 4: POST /api/clients/{idClient}/trips/{idTrip}
    [HttpPost("{idClient}/trips/{idTrip}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignTripToClient(int idClient, int idTrip)
    {
        if (!await _dbService.DoesClientExistAsync(idClient))
            return NotFound($"Client with id {idClient} not found.");
        var trip = await _dbService.GetTripDetailsByIdAsync(idTrip);
        if (trip == null)
            return NotFound($"Trip with id {idTrip} not found.");
        if (trip.DateFrom <= DateTime.Now)
            return BadRequest("Cannot assign client to a trip that has already started or is in the past.");
        if (await _dbService.IsClientAlreadyRegisteredAsync(idClient, idTrip))
            return BadRequest("Client is already registered for this trip.");
        var registeredCount = await _dbService.GetCurrentTripRegisteredCountAsync(idTrip);
        if (registeredCount >= trip.MaxPeople)
            return BadRequest("Trip is full. No available slots.");
        try
        {
            await _dbService.AssignClientToTripAsync(idClient, idTrip);
            return StatusCode(StatusCodes.Status201Created);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred assigning client to trip: {e.Message}");
        }
    }

    // Endpoint 5: DELETE /api/clients/{idClient}/trips/{idTrip}
    [HttpDelete("{idClient}/trips/{idTrip}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveClientFromTrip(int idClient, int idTrip)
    {
         if (!await _dbService.DoesClientExistAsync(idClient))
             return NotFound($"Client with id {idClient} not found.");
         if (!await _dbService.DoesTripExistAsync(idTrip))
            return NotFound($"Trip with id {idTrip} not found.");
        try
        {
            await _dbService.RemoveClientFromTripAsync(idClient, idTrip);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred removing client registration: {e.Message}");
        }
    }

    // Endpoint 2: GET /api/clients/{idClient}/trips
    [HttpGet("{idClient}/trips")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTripsForClient(int idClient)
    {
        if (!await _dbService.DoesClientExistAsync(idClient))
        {
            return NotFound($"Client with id {idClient} not found.");
        }
        try
        {
            var trips = await _dbService.GetClientTripsAsync(idClient);
            return Ok(trips);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred retrieving client trips: {e.Message}");
        }
    }
}