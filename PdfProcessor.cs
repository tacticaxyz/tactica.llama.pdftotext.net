using PDFtoImage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;

namespace TacTicA.Llama.PdfToText.Net
{
    public class PdfProcessor
    {
        /// <summary>
        /// Convert PDF pages to images and save them to the specified output directory.
        /// </summary>
        /// <param name="pdfPath">Path to the input PDF file.</param>
        /// <param name="outputDir">Directory where the images will be saved.</param>
        /// <param name="start">The start page number (1-based). If 0, starts from first page.</param>
        /// <param name="end">The end page number (1-based). If 0, goes until last page.</param>
        public static async Task PdfToImagesAsync(string pdfPath, string outputDir, int start = 0, int end = 0)
        {
            if (!File.Exists(pdfPath))
            {
                throw new FileNotFoundException($"PDF file not found: {pdfPath}");
            }

            Directory.CreateDirectory(outputDir);

            // Get total pages in PDF by reading the file as bytes first
            byte[] pdfBytes = await File.ReadAllBytesAsync(pdfPath);
            var images = Conversion.ToImages(pdfBytes);
            var totalPages = images.Count();

            // Validate page numbers
            if (start < 0 || (start > totalPages && start != 0))
            {
                throw new ArgumentException($"Start page number {start} is out of range. Document has {totalPages} pages.");
            }
            if (end < 0 || (end > totalPages && end != 0))
            {
                throw new ArgumentException($"End page number {end} is out of range. Document has {totalPages} pages.");
            }

            // Set default values for start and end
            start = start == 0 ? 1 : start;
            end = end == 0 ? totalPages : end;

            Console.WriteLine($"Converting pages {start} to {end} to images...");

            // Convert specified pages
            var pageImages = Conversion.ToImages(pdfBytes).Skip(start - 1).Take(end - start + 1);
            int pageNum = start;

            foreach (var skBitmap in pageImages)
            {
                var outputPath = Path.Combine(outputDir, $"page_{pageNum}.png");
                
                // Convert SKBitmap to byte array and save
                using var image = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
                await File.WriteAllBytesAsync(outputPath, image.ToArray());
                
                pageNum++;
            }

            Console.WriteLine($"Converted {end - start + 1} pages to images.");
        }

        /// <summary>
        /// Resize an image to the specified width while maintaining aspect ratio.
        /// </summary>
        /// <param name="inputPath">Path to the input image.</param>
        /// <param name="outputPath">Path to save the resized image.</param>
        /// <param name="width">Target width in pixels.</param>
        public static async Task ResizeImageAsync(string inputPath, string outputPath, int width)
        {
            using var image = await Image.LoadAsync(inputPath);
            
            var height = (int)(image.Height * ((double)width / image.Width));
            
            image.Mutate(x => x.Resize(width, height));
            
            await image.SaveAsPngAsync(outputPath);
        }
    }
}
