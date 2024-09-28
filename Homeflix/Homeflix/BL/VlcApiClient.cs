using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Homeflix.BL
{
    public class VlcApiClient
    {
        private readonly HttpClient _httpClient;
        private const string VlcStatusUrl = "http://127.0.0.1:8080/requests/status.xml";
        private const string Password = "1234";

        public VlcApiClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<(int? CurrentTime, int? MaxLength)> GetPlaybackStatusAsync()
        {
            try
            {
                var byteArray = Encoding.ASCII.GetBytes($":{Password}"); // Assuming no username, just password
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                // Send a GET request to the VLC API
                var response = await _httpClient.GetAsync(VlcStatusUrl);

                // If the response is not successful, return default TimeSpan values
                if (!response.IsSuccessStatusCode)
                {
                    return (null, null);
                }

                // Read the response content as a string
                var content = await response.Content.ReadAsStringAsync();

                // Parse the XML content
                var xml = XDocument.Parse(content);
                var currentTimeElement = xml.Root.Element("time");
                var maxLengthElement = xml.Root.Element("length");

                // Parse the time values
                var currentTime = int.Parse(currentTimeElement?.Value ?? null);
                var maxLength = int.Parse(maxLengthElement?.Value ?? null);

                return (currentTime, maxLength);
            }
            catch (Exception)
            {
                // Return default TimeSpan values in case of an exception
                return (null, null);
            }
        }
    }
}
