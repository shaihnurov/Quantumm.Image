using Avalonia.Media.Imaging;
using System.Threading.Tasks;

namespace Quantumm.Image.Services.ImageCache
{
    /// <summary>
    /// Interface for an image caching service.
    /// Provides methods for loading images from disk or cache,
    /// and updating images in the cache.
    /// 
    /// This interface abstracts the caching mechanism, allowing
    /// implementations to use memory caching, LRU policies, or 
    /// other caching strategies without changing consuming code.
    /// </summary>
    public interface IImageCacheService
    {
        /// <summary>
        /// Loads an image from a given local file path.
        /// 
        /// The method first attempts to retrieve the image from the cache.
        /// If the image is not in the cache, it will load it from the file system.
        /// This operation is asynchronous to avoid blocking the UI or main thread.
        /// </summary>
        /// <param name="filePath">
        /// The local file path of the image to load.
        /// Must not be null or empty. The file should exist on disk.
        /// </param>
        /// <returns>
        /// A <see cref="Bitmap"/> representing the loaded image.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> is null or empty.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the file specified by <paramref name="filePath"/> does not exist.
        /// </exception>
        Task<Bitmap> LoadImage(string filePath);

        /// <summary>
        /// Updates an image in the cache by loading a new image from the specified path.
        /// 
        /// If an image with the same path already exists in the cache, it will be replaced
        /// with the new image. This method is useful when the underlying file has changed
        /// and the cached version needs to be refreshed.
        /// 
        /// The operation is asynchronous and ensures thread-safe access to the cache.
        /// </summary>
        /// <param name="path">
        /// The local file path of the new image to load into the cache.
        /// Must not be null or empty. The file should exist on disk.
        /// </param>
        /// <returns>
        /// A <see cref="Bitmap"/> representing the updated image.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="path"/> is null or empty.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the file specified by <paramref name="path"/> does not exist.
        /// </exception>
        Task<Bitmap> UpdateImage(string path);
    }
}