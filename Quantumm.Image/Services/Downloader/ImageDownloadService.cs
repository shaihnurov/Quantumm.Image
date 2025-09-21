using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Quantumm.Image.Services.Downloader
{
    /// <summary>
    /// Service for downloading images from the network and saving them to disk.
    /// Supports cancellation and safe handling of temporary files.
    /// </summary>
    /// <param name="httpClient">An instance of <see cref="HttpClient"/> provided by the user.</param>
    /// <param name="logger">Optional logger. Logs will be written only if provided.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="httpClient"/> is null.</exception>
    public class ImageDownloadService(HttpClient httpClient, ILogger<ImageDownloadService>? logger = null) : IImageDownloadService
    {
        private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        /// <inheritdoc />
        public async Task<string> DownloadImageAsync(string? url, string fileName, string folderPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentNullException(nameof(folderPath));

            Directory.CreateDirectory(folderPath);

            var targetPath = Path.Combine(folderPath, fileName);
            var tempPath = targetPath + ".tmp";

            try
            {
                logger?.LogInformation("Starting download: {Url}", url);

                await using var responseStream = await _httpClient
                    .GetStreamAsync(url, cancellationToken)
                    .ConfigureAwait(false);

                await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Delete, bufferSize: 81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await responseStream.CopyToAsync(fileStream, bufferSize: 81920, cancellationToken).ConfigureAwait(false);
                    await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                File.Move(tempPath, targetPath, overwrite: true);

                logger?.LogInformation("Image saved: {Path}", targetPath);
                return targetPath;
            }
            catch (OperationCanceledException ex)
            {
                logger?.LogWarning(ex, "Download canceled: {Url}", url);
                throw;
            }
            catch (HttpRequestException ex)
            {
                logger?.LogError(ex, "HTTP error while downloading: {Url}", url);
                throw;
            }
            catch (IOException ex)
            {
                logger?.LogError(ex, "File system error while saving: {FileName}", fileName);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                logger?.LogError(ex, "Access denied: {Path}", targetPath);
                throw;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unexpected error while downloading {Url}", url);
                throw;
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch (Exception deleteEx)
                    {
                        logger?.LogWarning(deleteEx, "Failed to delete temporary file: {TempPath}", tempPath);
                    }
                }
            }
        }
    }
}