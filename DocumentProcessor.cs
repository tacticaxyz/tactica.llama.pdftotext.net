namespace TacTicA.Llama.PdfToText.Net
{
    public class DocumentProcessor : IDisposable
    {
        private readonly OllamaClient _ollamaClient;

        public DocumentProcessor()
        {
            _ollamaClient = new OllamaClient();
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

            if (!await _ollamaClient.CheckForServerAsync())
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
                        var text = await _ollamaClient.TranscribeImageAsync(imageBase64, model);

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

        public void Dispose()
        {
            _ollamaClient?.Dispose();
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
