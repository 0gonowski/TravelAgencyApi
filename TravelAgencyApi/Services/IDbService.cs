using TravelAgencyApi.Models;
using TravelAgencyApi.Models.DTOs;

namespace TravelAgencyApi.Services; 

public interface IDbService
{

    Task<IEnumerable<TripDto>> GetTripsAsync();

    Task<IEnumerable<TripDto>> GetClientTripsAsync(int idClient);

    Task<int> AddClientAsync(ClientDto clientDto);
    
    Task AssignClientToTripAsync(int idClient, int idTrip);

    Task RemoveClientFromTripAsync(int idClient, int idTrip);
    
    Task<bool> DoesClientExistAsync(int idClient);
    
    Task<bool> DoesTripExistAsync(int idTrip);
    
    Task<bool> IsClientAlreadyRegisteredAsync(int idClient, int idTrip);

    Task<bool> IsPeselUniqueAsync(string pesel);
    
    Task<bool> IsEmailUniqueAsync(string email);

    Task<int> GetCurrentTripRegisteredCountAsync(int idTrip);
    
    Task<Trip?> GetTripDetailsByIdAsync(int idTrip); // Zwraca nullable Trip
}