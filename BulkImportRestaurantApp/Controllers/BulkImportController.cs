using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BulkImportRestaurantApp.Factories;
using BulkImportRestaurantApp.Models;
using BulkImportRestaurantApp.Models.Interfaces;
using BulkImportRestaurantApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BulkImportRestaurantApp.Controllers
{
    public class BulkImportController : Controller
    {
        private readonly ImportItemFactory _factory;
        private readonly ILogger<BulkImportController> _logger;

        public BulkImportController(
            ImportItemFactory factory,
            ILogger<BulkImportController> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        // STEP 1: Show upload page
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // STEP 2: Upload JSON, parse, store in memory, show preview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            IFormFile jsonFile,
            [FromKeyedServices("memory")] IItemsRepository memoryRepository)
        {
            if (jsonFile == null || jsonFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please upload a JSON file.");
                return View();
            }

            string json;
            using (var reader = new StreamReader(jsonFile.OpenReadStream()))
            {
                json = await reader.ReadToEndAsync();
            }

            List<IItemValidating> items;

            try
            {
                items = _factory.Create(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse JSON during bulk import.");
                ModelState.AddModelError(string.Empty, "Invalid JSON format.");
                return View();
            }

            // Store parsed items in memory (pending, not yet in DB)
            await memoryRepository.SaveAsync(items);

            // Show preview view strongly typed to IEnumerable<IItemValidating>
            return View("Preview", items);
        }

        // STEP 3: Generate ZIP of default images (one folder per item)
        [HttpGet]
        public async Task<IActionResult> DownloadImageTemplate(
            [FromKeyedServices("memory")] IItemsRepository memoryRepository)
        {
            var items = await memoryRepository.GetAsync();
            if (items == null || items.Count == 0)
            {
                return BadRequest("No items in memory. Upload a JSON file first.");
            }

            var zipBytes = GenerateDefaultImagesZip(items);
            return File(zipBytes, "application/zip", "items-images-template.zip");
        }

        // STEP 4: Commit – upload images ZIP, save to DB, clear memory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Commit(
            IFormFile imagesZip,
            [FromKeyedServices("memory")] ItemsInMemoryRepository memoryRepository,
            [FromKeyedServices("database")] ItemsDbRepository dbRepository,
            [FromServices] IWebHostEnvironment env)
        {
            if (imagesZip == null || imagesZip.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please upload a ZIP file with images.");
                return RedirectToAction("Index");
            }

            var pendingItems = await memoryRepository.GetAsync();
            if (pendingItems == null || pendingItems.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "No pending items found. Upload JSON first.");
                return RedirectToAction("Index");
            }

            var webRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var imagesRoot = Path.Combine(webRoot, "images", "items");
            Directory.CreateDirectory(imagesRoot);

            using var zipStream = imagesZip.OpenReadStream();
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            // Take all file entries, sorted, and map to items by index
            var imageEntries = archive.Entries
                .Where(e => !string.IsNullOrEmpty(e.Name))
                .OrderBy(e => e.FullName)
                .ToList();

            var index = 0;
            foreach (var item in pendingItems)
            {
                if (index >= imageEntries.Count)
                {
                    break;
                }

                var entry = imageEntries[index];
                index++;

                var extension = Path.GetExtension(entry.Name);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    extension = ".jpg";
                }

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var itemFolder = Path.Combine(imagesRoot, $"item-{index}");
                Directory.CreateDirectory(itemFolder);

                var physicalPath = Path.Combine(itemFolder, uniqueFileName);

                await using (var fileStream = System.IO.File.Create(physicalPath))
                await using (var entryStream = entry.Open())
                {
                    await entryStream.CopyToAsync(fileStream);
                }

                var relativePath = Path.Combine("images", "items", $"item-{index}", uniqueFileName)
                    .Replace("\\", "/");

                // For now, only Restaurant has ImagePath in your DB schema.
                if (item is Restaurant restaurant)
                {
                    restaurant.ImagePath = relativePath;
                }

                // If you later add ImagePath to MenuItem, set it here too.
            }

            await dbRepository.SaveAsync(pendingItems);
            await memoryRepository.ClearAsync();

            // Later you'll redirect to ItemsController.Catalog
            return RedirectToAction("Index", "Home");
        }

        // Helper: create default.jpg folders zip
        private static byte[] GenerateDefaultImagesZip(IReadOnlyList<IItemValidating> items)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (var i = 0; i < items.Count; i++)
                {
                    // Example: item-1/default.jpg
                    var entry = archive.CreateEntry($"item-{i + 1}/default.jpg");
                    using var entryStream = entry.Open();
                    // empty file is fine as placeholder
                }
            }

            return memoryStream.ToArray();
        }
    }
}
