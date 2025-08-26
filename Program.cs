using System.CommandLine;
using TacTicA.Llama.PdfToText.Net;

var pdfPathArgument = new Argument<string>(
    name: "pdf-path",
    description: "Path to the input PDF file");

var outputOption = new Option<string>(
    name: "--output",
    description: "Output directory",
    getDefaultValue: () => $"output_{DateTime.Now:yyyyMMdd_HHmmss}");
outputOption.AddAlias("-o");

var modelOption = new Option<string>(
    name: "--model",
    description: "Ollama model to use",
    getDefaultValue: () => "qwen2.5vl:latest");
modelOption.AddAlias("-m");

var keepImagesOption = new Option<bool>(
    name: "--keep-images",
    description: "Keep the intermediate image files",
    getDefaultValue: () => false);
keepImagesOption.AddAlias("-k");

var widthOption = new Option<int>(
    name: "--width",
    description: "Width of the resized images. Set to 0 to skip resizing",
    getDefaultValue: () => 0);
widthOption.AddAlias("-w");

var startOption = new Option<int>(
    name: "--start",
    description: "Start page number",
    getDefaultValue: () => 0);
startOption.AddAlias("-s");

var endOption = new Option<int>(
    name: "--end",
    description: "End page number",
    getDefaultValue: () => 0);
endOption.AddAlias("-e");

var stdoutOption = new Option<bool>(
    name: "--stdout",
    description: "Write merged output to stdout",
    getDefaultValue: () => false);

var rootCommand = new RootCommand("Convert PDF pages to images and transcribe them using Ollama.");
rootCommand.AddArgument(pdfPathArgument);
rootCommand.AddOption(outputOption);
rootCommand.AddOption(modelOption);
rootCommand.AddOption(keepImagesOption);
rootCommand.AddOption(widthOption);
rootCommand.AddOption(startOption);
rootCommand.AddOption(endOption);
rootCommand.AddOption(stdoutOption);

rootCommand.SetHandler(async (pdfPath, output, model, keepImages, width, start, end, stdout) =>
{
    using var processor = new DocumentProcessor();
    await processor.ProcessPdfAsync(pdfPath, output, model, keepImages, width, start, end, stdout);
}, pdfPathArgument, outputOption, modelOption, keepImagesOption, widthOption, startOption, endOption, stdoutOption);

return await rootCommand.InvokeAsync(args);
