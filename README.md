# TacTicA.Llama.PdfToText.Net

A .NET 9 CLI tool for converting PDFs to text files using Ollama.

![TacTicA.Llama.PdfToText.Net](https://img.shields.io/badge/Status-Ready%20for%20Deployment-green) ![.NET 9](https://img.shields.io/badge/.NET-9.0-purple) ![License](https://img.shields.io/badge/License-MIT-blue)

![1](https://github.com/tacticaxyz/tactica.llama.pdftotext.net/blob/main/images/tactica-pdf-to-text-small.png)

## Features

- Convert PDFs to text files locally, no token costs
- Use the latest multimodal models supported by Ollama
- Turn images and diagrams into detailed text descriptions
- Cross-platform support (Windows, Linux, macOS)

## Requirements

- .NET 9.0 or later
- Ollama installed and running locally

Optionally (DocumentProcessor is implemented in both ways using HttpClient or OllamaSharp):
- OllamaSharp
- Microsoft.Extensions.AI

## Installation

### From Source

```bash
git clone [repository-url]
cd TacTicA.Llama.PdfToText.Net
dotnet build -c Release
```

### Build and Install Globally

```bash
dotnet pack -c Release
dotnet tool install --global --add-source ./bin/Release TacTicA.Llama.PdfToText.Net
```

## Prerequisites

1. Install [Ollama](https://ollama.com/)
2. Pull the default model:
   ```bash
   ollama run qwen2.5vl:latest
   ```

## Usage

### Basic Usage

```bash
dotnet run -- path/to/your/file.pdf
```

### Process Specific Pages with Custom Width

```bash
dotnet run -- document.pdf --start 1 --end 5 --width 1000
```

### Use a Different Ollama Model

```bash
dotnet run -- document.pdf --model qwen2.5vl:3b
```

### Command Line Options

- `--output`, `-o`: Output directory (default: "output_YYYYMMDD_HHMMSS")
- `--model`, `-m`: Ollama model to use (default: "qwen2.5vl:latest")
- `--keep-images`, `-k`: Keep the intermediate image files (default: false)
- `--width`, `-w`: Width of the resized images (0 to skip resizing; default: 0)
- `--start`, `-s`: Start page number (default: 0, meaning all pages)
- `--end`, `-e`: End page number (default: 0, meaning all pages)
- `--stdout`: Write merged output to stdout (default: false)

### Examples

```bash
# Basic conversion
dotnet run -- document.pdf

# Convert pages 1-10 with custom output directory
dotnet run -- document.pdf --start 1 --end 10 --output my_output

# Resize images to 800px width and keep them
dotnet run -- document.pdf --width 800 --keep-images

# Use different model and output to stdout
dotnet run -- document.pdf --model llava:latest --stdout
```

## How It Works

1. **PDF to Images**: Converts each PDF page to PNG images using PDFtoImage library
2. **Image Processing**: Optionally resizes images to reduce processing time
3. **OCR with Ollama**: Sends each image to Ollama's multimodal models for transcription
4. **Text Consolidation**: Merges all transcribed text into a single output file

## Dependencies

- **System.CommandLine**: For CLI argument parsing
- **PDFtoImage**: For PDF to image conversion
- **SixLabors.ImageSharp**: For image processing and resizing
- **SkiaSharp**: For image format handling

## Architecture

The application is structured into several key components:

- `Constants.cs`: Configuration constants including Ollama URL and prompts
- `OllamaClient.cs`: HTTP client for communicating with Ollama API
- `PdfProcessor.cs`: PDF to image conversion and image resizing
- `Utils.cs`: Utility functions for file operations
- `DocumentProcessor.cs`: Main processing logic orchestrating the workflow
- `Program.cs`: CLI entry point and argument handling

## Error Handling

The application includes comprehensive error handling for:

- Missing PDF files
- Ollama server connectivity issues
- Invalid page ranges
- Image processing errors
- API communication failures

## Performance Considerations

- Images can be resized to reduce processing time
- Intermediate images can be deleted to save disk space
- Progress indicators show processing status
- Parallel processing could be added for better performance

## License

MIT License

Copyright (c) 2025 Anton Yarkov @ TacTicA