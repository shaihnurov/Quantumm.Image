# Quantumm.Image

[![NuGet](https://img.shields.io/nuget/v/Quantumm.Image.svg)](https://www.nuget.org/packages/Quantumm.Image)  

**Quantumm.Image** is an asynchronous, thread-safe, LRU-aware image caching library designed for **Avalonia** applications. It provides fast and memory-efficient image loading with support for dependency injection and duplicate download prevention.  

---

## Features

- Asynchronous image loading from disk  
- Memory-limited **LRU (Least Recently Used)** caching  
- Thread-safe access to cached images  
- Automatic eviction of least recently used images  
- Prevents multiple concurrent loads of the same image  
- Easy **dependency injection** integration with `Microsoft.Extensions.DependencyInjection`  
- Detailed XML documentation for all public interfaces  
- Supports Avalonia 11 and .NET 9  

---

## Installation

Install via NuGet:

```bash
dotnet add package Quantumm.Image --version 1.1
```

## Usage

1. Register the service in your DI container
   ```csharp
   using Microsoft.Extensions.DependencyInjection;
   using Quantumm.Image.Services.DependencyInjection;

   var services = new ServiceCollection();

   // Register ImageCacheService with optional capacity (default 100)
   services.AddImageCache(capacity: 200);

   var serviceProvider = services.BuildServiceProvider();
   ```
2. Resolve and use the service
   ```csharp
    using Quantumm.Image.Services.ImageCache;
    using Avalonia.Media.Imaging;
    
    var imageCache = serviceProvider.GetRequiredService<IImageCacheService>();
    
    // Load an image asynchronously
    Bitmap image = await imageCache.LoadImage("path/to/image.png");
    
    // Update an image in cache if the file has changed
    Bitmap updatedImage = await imageCache.UpdateImage("path/to/image.png");
   ```

## API

 IImageCacheService

 - Task<Bitmap> LoadImage(string filePath)
Loads an image from cache or disk asynchronously. Adds it to the cache if it wasn't already present.

 - Task<Bitmap> UpdateImage(string path)
Updates an existing image in the cache by loading a new version from disk. Adds it if it does not exist.

## Logging

 ImageCacheService supports logging via Microsoft.Extensions.Logging. Useful events include:

 - Image loading
 - Image eviction
 - Errors during image load or disposal
  ```csharp
  services.AddLogging(builder => builder.AddConsole());
  ```

## Supported Platforms
 - Avalonia 11
 - .NET 9.0

## License
MIT License. See [LICENSE](LICENSE.txt) for details.

## Contributors

[See all contributors](https://github.com/shaihnurov/Quantumm.Image/graphs/contributors)
