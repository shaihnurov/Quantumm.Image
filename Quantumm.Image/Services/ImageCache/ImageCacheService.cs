using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Quantumm.Image.Services.ImageCache
{
    /// <summary>
    /// Image caching service using an LRU (Least Recently Used) policy and asynchronous loading.
    /// 
    /// This service keeps images in memory up to a configurable capacity.
    /// When the cache exceeds the capacity, the least recently used images are removed.
    /// Supports thread-safe access and prevents multiple parallel loads of the same image.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="capacity">Maximum number of images in cache (default 100).</param>
    public class ImageCacheService(ILogger<ImageCacheService> logger, int capacity = 100) : IImageCacheService
    {

        /// <summary>
        /// SemaphoreSlim used for thread-safe access to the cache and LRU structures.
        /// </summary>
        private readonly SemaphoreSlim _lock = new(1, 1);

        /// <summary>
        /// Dictionary mapping file paths to loaded Bitmaps.
        /// Acts as the main cache storage.
        /// </summary>
        private readonly Dictionary<string, Bitmap> _cache = [];

        /// <summary>
        /// Doubly-linked list to track the usage order for LRU.
        /// Most recently used items are at the front; least recently used at the back.
        /// </summary>
        private readonly LinkedList<string> _lruList = new();

        /// <summary>
        /// Maps cache keys (file paths) to nodes in the LRU linked list for quick updates.
        /// </summary>
        private readonly Dictionary<string, LinkedListNode<string>> _nodeMap = [];

        /// <summary>
        /// Tracks ongoing image load tasks to prevent loading the same file multiple times concurrently.
        /// Key: file path, Value: loading task returning the Bitmap.
        /// </summary>
        private readonly ConcurrentDictionary<string, Task<Bitmap>> _loadingTasks = new();

        /// <summary>
        /// Asynchronously loads an image from the cache or disk.
        /// If the image exists in the cache, it is returned immediately.
        /// Otherwise, the image is loaded from disk and added to the cache.
        /// Implements thread-safe access and updates the LRU order.
        /// </summary>
        /// <param name="path">Local file path of the image.</param>
        /// <returns>A <see cref="Bitmap"/> representing the loaded image.</returns>
        /// <exception cref="ArgumentException">Thrown when the path is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist on disk.</exception>
        public async Task<Bitmap> LoadImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Image path cannot be null or empty.", nameof(path));

            try
            {
                // 1. Check cache first
                await _lock.WaitAsync();
                try
                {
                    if (_cache.TryGetValue(path, out var cachedBitmap))
                    {
                        // Move node to front of LRU list
                        var node = _nodeMap[path];
                        _lruList.Remove(node);
                        _lruList.AddFirst(node);

                        return cachedBitmap;
                    }
                }
                finally
                {
                    _lock.Release();
                }

                // 2. Load image asynchronously (prevent duplicate loads)
                var loadTask = _loadingTasks.GetOrAdd(path, _ => Task.Run(() =>
                {
                    if (!File.Exists(path))
                        throw new FileNotFoundException($"Image file not found: {path}");

                    logger.LogDebug("Loading image {Path} from disk", path);
                    return new Bitmap(path);
                }));

                Bitmap newBitmap = await loadTask.ConfigureAwait(false);

                // Remove from loading tasks once finished
                _loadingTasks.TryRemove(path, out _);

                // 3. Add loaded image to cache
                await _lock.WaitAsync();
                try
                {
                    if (_cache.TryGetValue(path, out var existingBitmap))
                    {
                        // If another thread added it meanwhile, dispose duplicate
                        if (!ReferenceEquals(newBitmap, existingBitmap))
                            newBitmap.Dispose();

                        var node = _nodeMap[path];
                        _lruList.Remove(node);
                        _lruList.AddFirst(node);

                        return existingBitmap;
                    }

                    // Add new image to cache and LRU
                    var newNode = new LinkedListNode<string>(path);
                    _lruList.AddFirst(newNode);
                    _nodeMap[path] = newNode;
                    _cache[path] = newBitmap;

                    // Evict least recently used if cache exceeds capacity
                    if (_cache.Count > capacity && _lruList.Last != null)
                    {
                        string oldestKey = _lruList.Last.Value;
                        _lruList.RemoveLast();
                        _nodeMap.Remove(oldestKey);

                        if (_cache.Remove(oldestKey, out var oldBitmap) && oldBitmap != null)
                        {
                            try
                            {
                                oldBitmap.Dispose();
                                logger.LogDebug("Evicted least recently used image {Path} from cache", oldestKey);
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Error disposing old image {Path}", oldestKey);
                            }
                        }
                    }

                    return newBitmap;
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                _loadingTasks.TryRemove(path, out _);
                logger.LogError(ex, "Error loading image {Path}", path);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing image in the cache by loading a new version from disk.
        /// If the image is not in the cache, it will be added.
        /// Thread-safe and updates the LRU order.
        /// </summary>
        /// <param name="path">Local file path of the image to update.</param>
        /// <returns>The updated <see cref="Bitmap"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if path is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown if file does not exist.</exception>
        public async Task<Bitmap> UpdateImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Image path cannot be null or empty.", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException($"Image file not found: {path}");

            try
            {
                Bitmap newBitmap = await Task.Run(() => new Bitmap(path)).ConfigureAwait(false);

                await _lock.WaitAsync();
                try
                {
                    // Dispose old image if exists
                    if (_cache.TryGetValue(path, out var oldBitmap) && oldBitmap != null)
                    {
                        try
                        {
                            oldBitmap.Dispose();
                            logger.LogDebug("Updated image {Path} in cache", path);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Error disposing old image {Path}", path);
                        }
                    }

                    _cache[path] = newBitmap;

                    // Update LRU
                    if (_nodeMap.TryGetValue(path, out var existingNode))
                        _lruList.Remove(existingNode);

                    var newNode = new LinkedListNode<string>(path);
                    _lruList.AddFirst(newNode);
                    _nodeMap[path] = newNode;

                    return newBitmap;
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating image {Path}", path);
                throw;
            }
        }
    }
}