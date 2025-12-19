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
using Microsoft.AspNetCore.Hosting;

namespace BulkImportRestaurantApp.Controllers
{
    public class BulkImportController : Controller
    {
        private readonly ImportItemFactory _factory;
        private readonly ILogger<BulkImportController> _logger;
        private readonly IWebHostEnvironment _env;

        public BulkImportController(
          ImportItemFactory factory,
          ILogger<BulkImportController> logger,
          IWebHostEnvironment env)
        {
            _factory = factory;
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

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

            await memoryRepository.SaveAsync(items);

            return View("Preview", items);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Commit(
     IFormFile imagesZip,
     [FromKeyedServices("memory")] ItemsInMemoryRepository memoryRepository,
     [FromKeyedServices("database")] ItemsDbRepository dbRepository)

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

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var imagesRoot = Path.Combine(webRoot, "images", "items");
            Directory.CreateDirectory(imagesRoot);

            using var zipStream = imagesZip.OpenReadStream();
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

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

     
                if (item is Restaurant restaurant)
                {
                    restaurant.ImagePath = relativePath;
                }

            }

            await dbRepository.SaveAsync(pendingItems);
            if (memoryRepository is ItemsInMemoryRepository memRepo)
            {
                await memRepo.ClearAsync();
            }
            return RedirectToAction("Index", "Home");
        }

  
        private static byte[] GenerateDefaultImagesZip(IReadOnlyList<IItemValidating> items)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (var i = 0; i < items.Count; i++)
                {
       
                    var entry = archive.CreateEntry($"item-{i + 1}/default.jpg");
                    using var entryStream = entry.Open();

                }
            }

            return memoryStream.ToArray();
        }
    }
}
