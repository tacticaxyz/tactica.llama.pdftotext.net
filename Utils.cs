namespace TacTicA.Llama.PdfToText.Net
{
    public class Utils
    {
        /// <summary>
        /// Setup output directories for images and text files.
        /// </summary>
        /// <param name="outputBase">Base output directory.</param>
        /// <returns>Tuple containing image directory and text directory paths.</returns>
        public static (string imageDir, string textDir) SetupOutputDirs(string outputBase)
        {
            var imageDir = Path.Combine(outputBase, "images");
            var textDir = Path.Combine(outputBase, "text");

            Directory.CreateDirectory(imageDir);
            Directory.CreateDirectory(textDir);

            return (imageDir, textDir);
        }

        /// <summary>
        /// Merge all text files in a directory into a single file.
        /// </summary>
        /// <param name="textDir">Directory containing text files.</param>
        /// <returns>Path to the merged text file.</returns>
        public static async Task<string> MergeTextFilesAsync(string textDir)
        {
            var mergedFile = Path.Combine(Directory.GetParent(textDir)!.FullName, "merged_output.txt");
            var textFiles = Directory.GetFiles(textDir, "*.txt")
                .OrderBy(f => ExtractPageNumber(Path.GetFileNameWithoutExtension(f)))
                .ToArray();

            using var writer = new StreamWriter(mergedFile);
            
            foreach (var textFile in textFiles)
            {
                var content = await File.ReadAllTextAsync(textFile);
                await writer.WriteLineAsync(content);
                await writer.WriteLineAsync(); // Add a blank line between pages
            }

            return mergedFile;
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

        /// <summary>
        /// Convert image file to base64 string.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <returns>Base64 encoded image data.</returns>
        public static async Task<string> ImageToBase64Async(string imagePath)
        {
            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            return Convert.ToBase64String(imageBytes);
        }
    }
}
