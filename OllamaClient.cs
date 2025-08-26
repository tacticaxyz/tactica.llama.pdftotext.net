using System.Text;
using System.Text.Json;

namespace TacTicA.Llama.PdfToText.Net
{
    public class OllamaClient : IDisposable
    {
        private readonly HttpClient _httpClient;

        public OllamaClient()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Check if the Ollama server is running.
        /// </summary>
        /// <returns>True if server is running, false otherwise.</returns>
        public async Task<bool> CheckForServerAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{Constants.OllamaBaseUrl}/tags");
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }

        /// <summary>
        /// Transcribe an image using the specified model.
        /// </summary>
        /// <param name="imageBase64">Base64 encoded image data.</param>
        /// <param name="model">The model to use for transcription.</param>
        /// <returns>The transcribed text.</returns>
        public async Task<string> TranscribeImageAsync(string imageBase64, string model)
        {
            var payload = new
            {
                model = model,
                prompt = Constants.TranscriptionPrompt,
                stream = false,
                images = new[] { imageBase64 }
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{Constants.OllamaBaseUrl}/generate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(responseContent);
                return jsonDocument.RootElement.GetProperty("response").GetString() ?? "";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API call failed with status code {response.StatusCode}: {errorContent}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
