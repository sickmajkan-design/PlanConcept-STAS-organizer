using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;

namespace Construction.Infrastructure.ExternalServices;

/// <summary>
/// <see cref="IPublicHolidaySource"/> backed by the Nager.Date public
/// holiday API (date.nager.at) — free, unauthenticated, and covers the
/// countries this company operates in. No settings/credentials to validate
/// at startup, unlike <c>EmailSettings</c>/<c>FirebaseSettings</c>, because
/// there is nothing to configure: the base address is fixed below and the
/// service needs no API key.
/// </summary>
public class NagerDatePublicHolidaySource : IPublicHolidaySource
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public NagerDatePublicHolidaySource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ExternalHoliday>> GetHolidaysAsync(
        string countryCode,
        int year,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        try
        {
            response = await _httpClient.GetAsync(
                $"PublicHolidays/{year}/{Uri.EscapeDataString(countryCode)}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ExternalServiceException(
                "Couldn't reach the public holiday service. Check the connection and try again.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("The public holiday service took too long to respond.");
        }

        // Nager answers 404 for a country code it doesn't recognise and 400
        // for a year outside the range it covers — both are the caller's
        // input, not an outage, but still something to say plainly rather
        // than let the generic "not successful" branch below swallow.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ExternalServiceException($"'{countryCode}' is not a country code the holiday service recognises.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ExternalServiceException($"The holiday service does not cover {year}.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalServiceException(
                $"The public holiday service returned an unexpected error ({(int)response.StatusCode}).");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<List<NagerHoliday>>(
            stream, JsonOptions, cancellationToken) ?? [];

        return payload
            .Select(holiday => new ExternalHoliday(
                DateOnly.Parse(holiday.Date), holiday.Name, holiday.LocalName))
            .ToList();
    }

    private class NagerHoliday
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = null!;

        [JsonPropertyName("localName")]
        public string LocalName { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }
}
