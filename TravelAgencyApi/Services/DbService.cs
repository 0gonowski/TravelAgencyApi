using System.Text;
using Microsoft.Data.SqlClient;
using TravelAgencyApi.Exceptions;
using TravelAgencyApi.Models;
using TravelAgencyApi.Models.DTOs;

namespace TravelAgencyApi.Services; 

public class DbService(IConfiguration config) : IDbService
{
    private readonly string? _connectionString = config.GetConnectionString("Default");

    public async Task<IEnumerable<TripDto>> GetTripsAsync()
    {
        var trips = new List<TripDto>();
        var tripCountries = new Dictionary<int, List<CountryDto>>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string tripsSql = "SELECT IdTrip, Name, Description, DateFrom, DateTo, MaxPeople FROM Trip ORDER BY DateFrom DESC";
        await using (var tripsCommand = new SqlCommand(tripsSql, connection))
        await using (var tripsReader = await tripsCommand.ExecuteReaderAsync())
        {
            while (await tripsReader.ReadAsync())
            {
                var trip = new TripDto
                {
                    IdTrip = tripsReader.GetInt32(0),
                    Name = tripsReader.GetString(1),
                    Description = tripsReader.IsDBNull(2) ? null : tripsReader.GetString(2),
                    DateFrom = tripsReader.GetDateTime(3), // Zakładamy DATETIME2 w bazie
                    DateTo = tripsReader.GetDateTime(4),   // Zakładamy DATETIME2 w bazie
                    MaxPeople = tripsReader.GetInt32(5)
                };
                trips.Add(trip);
                tripCountries.Add(trip.IdTrip, new List<CountryDto>());
            }
        }

        if (trips.Any())
        {
             var tripIds = trips.Select(t => t.IdTrip).ToArray();
             var parameters = new string[tripIds.Length];
             var countriesCommand = new SqlCommand();
             var sqlBuilder = new StringBuilder(@"
                 SELECT ct.IdTrip, c.Name
                 FROM Country c
                 JOIN Country_Trip ct ON c.IdCountry = ct.IdCountry
                 WHERE ct.IdTrip IN (");

            for (int i = 0; i < tripIds.Length; i++)
            {
                parameters[i] = $"@IdTrip{i}";
                sqlBuilder.Append(parameters[i]);
                if(i < tripIds.Length -1) sqlBuilder.Append(", ");
                countriesCommand.Parameters.AddWithValue(parameters[i], tripIds[i]);
            }
            sqlBuilder.Append(") ORDER BY ct.IdTrip");

            countriesCommand.CommandText = sqlBuilder.ToString();
            countriesCommand.Connection = connection;

            await using (countriesCommand)
            await using (var countriesReader = await countriesCommand.ExecuteReaderAsync())
            {
                while (await countriesReader.ReadAsync())
                {
                    int tripId = countriesReader.GetInt32(0);
                    string countryName = countriesReader.GetString(1);
                    if (tripCountries.ContainsKey(tripId))
                    {
                        tripCountries[tripId].Add(new CountryDto { Name = countryName });
                    }
                }
            }

            foreach (var tripDto in trips)
            {
                if (tripCountries.ContainsKey(tripDto.IdTrip))
                {
                    tripDto.Countries = tripCountries[tripDto.IdTrip];
                }
            }
        }

        return trips;
    }

    public async Task<int> AddClientAsync(ClientDto clientDto)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
            OUTPUT INSERTED.IdClient
            VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FirstName", clientDto.FirstName);
        command.Parameters.AddWithValue("@LastName", clientDto.LastName);
        command.Parameters.AddWithValue("@Email", clientDto.Email);
        command.Parameters.AddWithValue("@Telephone", (object?)clientDto.Telephone ?? DBNull.Value);
        command.Parameters.AddWithValue("@Pesel", clientDto.Pesel);

        await connection.OpenAsync();
        var id = await command.ExecuteScalarAsync();

        if (id == null) throw new Exception("Failed to create client.");
        return Convert.ToInt32(id);
    }

    public async Task AssignClientToTripAsync(int idClient, int idTrip)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
            VALUES (@IdClient, @IdTrip, @RegisteredAt, NULL);";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdClient", idClient);
        command.Parameters.AddWithValue("@IdTrip", idTrip);
        command.Parameters.AddWithValue("@RegisteredAt", DateTime.Now);

        await connection.OpenAsync();
        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0) throw new Exception("Failed to assign client to trip. Zero rows affected.");
    }

    public async Task RemoveClientFromTripAsync(int idClient, int idTrip)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdClient", idClient);
        command.Parameters.AddWithValue("@IdTrip", idTrip);

        await connection.OpenAsync();
        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0) throw new NotFoundException($"Registration for Client Id {idClient} on Trip Id {idTrip} not found or already deleted.");
    }

     public async Task<IEnumerable<TripDto>> GetClientTripsAsync(int idClient)
     {
        var trips = new List<TripDto>();
        var tripCountries = new Dictionary<int, List<CountryDto>>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string tripsSql = @"
            SELECT T.IdTrip, T.Name, T.Description, T.DateFrom, T.DateTo, T.MaxPeople
            FROM Trip T
            JOIN Client_Trip CT ON T.IdTrip = CT.IdTrip
            WHERE CT.IdClient = @IdClient
            ORDER BY T.DateFrom DESC";
        await using (var tripsCommand = new SqlCommand(tripsSql, connection))
        {
            tripsCommand.Parameters.AddWithValue("@IdClient", idClient);
            await using (var tripsReader = await tripsCommand.ExecuteReaderAsync())
            {
                while (await tripsReader.ReadAsync())
                {
                    var trip = new TripDto
                    {
                        IdTrip = tripsReader.GetInt32(0),
                        Name = tripsReader.GetString(1),
                        Description = tripsReader.IsDBNull(2) ? null : tripsReader.GetString(2),
                        DateFrom = tripsReader.GetDateTime(3),
                        DateTo = tripsReader.GetDateTime(4),
                        MaxPeople = tripsReader.GetInt32(5)
                    };
                    trips.Add(trip);
                    tripCountries.Add(trip.IdTrip, new List<CountryDto>());
                }
            }
        }

        if (trips.Any())
        {
             var tripIds = trips.Select(t => t.IdTrip).ToArray();
             var parameters = new string[tripIds.Length];
             var countriesCommand = new SqlCommand();
             var sqlBuilder = new StringBuilder(@"
                 SELECT ct.IdTrip, c.Name
                 FROM Country c
                 JOIN Country_Trip ct ON c.IdCountry = ct.IdCountry
                 WHERE ct.IdTrip IN (");

            for (int i = 0; i < tripIds.Length; i++)
            {
                parameters[i] = $"@IdTrip{i}";
                sqlBuilder.Append(parameters[i]);
                 if(i < tripIds.Length -1) sqlBuilder.Append(", ");
                countriesCommand.Parameters.AddWithValue(parameters[i], tripIds[i]);
            }
             sqlBuilder.Append(") ORDER BY ct.IdTrip");

            countriesCommand.CommandText = sqlBuilder.ToString();
            countriesCommand.Connection = connection;

            await using (countriesCommand)
            await using (var countriesReader = await countriesCommand.ExecuteReaderAsync())
            {
                while (await countriesReader.ReadAsync())
                {
                    int tripId = countriesReader.GetInt32(0);
                    string countryName = countriesReader.GetString(1);
                    if (tripCountries.ContainsKey(tripId))
                    {
                        tripCountries[tripId].Add(new CountryDto { Name = countryName });
                    }
                }
            }

            foreach (var tripDto in trips)
            {
                if (tripCountries.ContainsKey(tripDto.IdTrip))
                {
                    tripDto.Countries = tripCountries[tripDto.IdTrip];
                }
            }
        }
        return trips;
     }


    // --- Implementacja Metod Pomocniczych ---

    public async Task<bool> DoesClientExistAsync(int idClient)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdClient", idClient);
        await connection.OpenAsync();
        return await command.ExecuteScalarAsync() != null;
    }

    public async Task<bool> DoesTripExistAsync(int idTrip)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "SELECT 1 FROM Trip WHERE IdTrip = @IdTrip";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdTrip", idTrip);
        await connection.OpenAsync();
        return await command.ExecuteScalarAsync() != null;
    }

    public async Task<bool> IsClientAlreadyRegisteredAsync(int idClient, int idTrip)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdClient", idClient);
        command.Parameters.AddWithValue("@IdTrip", idTrip);
        await connection.OpenAsync();
        return await command.ExecuteScalarAsync() != null;
    }

    public async Task<bool> IsPeselUniqueAsync(string pesel)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "SELECT 1 FROM Client WHERE Pesel = @Pesel";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Pesel", pesel);
        await connection.OpenAsync();
        return await command.ExecuteScalarAsync() == null;
    }

     public async Task<bool> IsEmailUniqueAsync(string email)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "SELECT 1 FROM Client WHERE Email = @Email";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", email);
        await connection.OpenAsync();
        return await command.ExecuteScalarAsync() == null;
    }

     public async Task<int> GetCurrentTripRegisteredCountAsync(int idTrip)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "SELECT COUNT(IdClient) FROM Client_Trip WHERE IdTrip = @IdTrip";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdTrip", idTrip);
        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();
        return result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

     public async Task<Trip?> GetTripDetailsByIdAsync(int idTrip) // Zwraca model Trip lub null
     {
         await using var connection = new SqlConnection(_connectionString);
         const string sql = "SELECT IdTrip, Name, Description, DateFrom, DateTo, MaxPeople FROM Trip WHERE IdTrip = @IdTrip";
         await using var command = new SqlCommand(sql, connection);
         command.Parameters.AddWithValue("@IdTrip", idTrip);
         await connection.OpenAsync();
         await using var reader = await command.ExecuteReaderAsync();

         if (await reader.ReadAsync())
         {
             return new Trip
             {
                 IdTrip = reader.GetInt32(0),
                 Name = reader.GetString(1),
                 Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                 DateFrom = reader.GetDateTime(3),
                 DateTo = reader.GetDateTime(4),
                 MaxPeople = reader.GetInt32(5)
             };
         }
         return null;
     }
}