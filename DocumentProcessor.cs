using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TacTicA.Llama.PdfToText.Net
{
    public class DocumentProcessor : IDisposable
    {
        private readonly OllamaClient _ollamaHttpClient;
        private readonly IOllamaApiClient _ollamaClient;

        public DocumentProcessor()
        {
            // 2 instances of client: basically both doing the same thing, but one is native...
            _ollamaHttpClient = new OllamaClient();

            // ...and another is from OlamaSharp lib
            _ollamaClient = new OllamaApiClient(new Uri(Constants.OllamaBaseUrl), Constants.OllamaModel);
        }

        /// <summary>
        /// Process a PDF file, converting pages to images and transcribing them.
        /// </summary>
        /// <param name="pdfPath">The path to the PDF file.</param>
        /// <param name="outputDir">The directory to save the output.</param>
        /// <param name="model">The model to use for transcription.</param>
        /// <param name="keepImages">Whether to keep the images after processing.</param>
        /// <param name="width">The width of the resized images.</param>
        /// <param name="start">The start page number.</param>
        /// <param name="end">The end page number.</param>
        /// <param name="stdout">Whether to output to stdout.</param>
        public async Task ProcessPdfAsync(
            string pdfPath,
            string outputDir,
            string model,
            bool keepImages,
            int width,
            int start,
            int end,
            bool stdout)
        {
            if (!File.Exists(pdfPath))
            {
                Console.Error.WriteLine($"Error: PDF file not found: {pdfPath}");
                Environment.Exit(1);
            }


            //if (!await _ollamaHttpClient.CheckForServerAsync()) // Uncomment if using OllamaClient
            //{
            //    Console.Error.WriteLine("Error: Ollama server not running. Please start the server and try again.");
            //    Environment.Exit(1);
            //}

            string version = await _ollamaClient.GetVersionAsync(); // Uncomment if using OlamaSharp.OllamaApiClient
            if (string.IsNullOrWhiteSpace(version))
            {
                Console.Error.WriteLine("Error: Ollama server not running. Please start the server and try again.");
                Environment.Exit(1);
            }

            try
            {
                // Setup output directories
                var (imageDir, textDir) = Utils.SetupOutputDirs(outputDir);

                // Convert PDF to images
                await PdfProcessor.PdfToImagesAsync(pdfPath, imageDir, start, end);

                // Get all image files
                var imageFiles = Directory.GetFiles(imageDir, "page_*.png")
                    .OrderBy(f => ExtractPageNumber(Path.GetFileNameWithoutExtension(f)))
                    .ToArray();

                var totalPages = imageFiles.Length;

                // Resize images if width is specified
                if (width > 0)
                {
                    Console.WriteLine("Resizing images...");
                    for (int i = 0; i < imageFiles.Length; i++)
                    {
                        var imageFile = imageFiles[i];
                        await PdfProcessor.ResizeImageAsync(imageFile, imageFile, width);
                        Console.Write($"\rResizing images: {i + 1}/{totalPages}");
                    }
                    Console.WriteLine();
                }

                // Process each page
                Console.WriteLine("Transcribing pages...");
                for (int i = 0; i < imageFiles.Length; i++)
                {
                    var imageFile = imageFiles[i];
                    var pageNumber = i + 1;

                    try
                    {
                        // Convert image to base64
                        var imageBase64 = await Utils.ImageToBase64Async(imageFile);

                        // Transcribe the image
                        //var text = await _ollamaClient.TranscribeImageAsync(imageBase64, model); // Uncomment if using OllamaClient
                        var text = await TranscribeImageAsync(imageBase64, model); // Uncomment if using OlamaSharp.OllamaApiClient

                        // Save transcription
                        var textFile = Path.Combine(textDir, $"{Path.GetFileNameWithoutExtension(imageFile)}.txt");
                        await File.WriteAllTextAsync(textFile, text);

                        Console.Write($"\rTranscribing pages: {pageNumber}/{totalPages}");
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"Error processing page {pageNumber}: {e.Message}");
                    }

                    // Clean up image if not keeping them
                    if (!keepImages)
                    {
                        File.Delete(imageFile);
                    }
                }
                Console.WriteLine();

                // Merge text files
                var mergedFile = await Utils.MergeTextFilesAsync(textDir);

                if (stdout)
                {
                    var content = await File.ReadAllTextAsync(mergedFile);
                    Console.WriteLine(content);
                }

                Console.Error.WriteLine($"Processing complete! Output saved to: {outputDir}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error: {e.Message}");
                Environment.Exit(1);
            }
        }

        private async Task<string> TranscribeImageAsync(string imageBase64, string model)
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

            await foreach (var stream in _ollamaClient.GenerateAsync(
                new OllamaSharp.Models.GenerateRequest
                {
                    Model = model,
                    Prompt = Constants.TranscriptionPrompt,
                    Stream = false,
                    Images = new[] { imageBase64 }
                }))
            {
                if (stream.Done)
                {
                    return stream.Response ?? string.Empty;
                }
                else
                {
                    throw new Exception($"Generate API call failed: {stream.Response}");
                }
            }

            return string.Empty;
        }

        public void Dispose()
        {
            _ollamaHttpClient?.Dispose();
        }

        /// <summary>
        /// Extract page number from filename like "page_1" -> 1
        /// </summary>
        private static int ExtractPageNumber(string filename)
        {
            var parts = filename.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int pageNum))
            {
                return pageNum;
            }
            return 0;
        }
    }
}
