using System.Threading;
using System.Threading.Tasks;

namespace Quantumm.Image.Services.Downloader
{
    /// <summary>
    /// Interface for a service that downloads images from a URL
    /// and saves them to a specified folder.
    /// </summary>
    public interface IImageDownloadService
    {
        /// <summary>
        /// Asynchronously downloads an image from the given URL and saves it to the specified folder.
        /// </summary>
        /// <param name="url">The URL of the image.</param>
        /// <param name="fileName">The file name (without path).</param>
        /// <param name="folderPath">The folder path where the image will be saved.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The full path of the saved file.</returns>
        Task<string> DownloadImageAsync(string? url, string fileName, string folderPath, CancellationToken cancellationToken = default);
    }
}