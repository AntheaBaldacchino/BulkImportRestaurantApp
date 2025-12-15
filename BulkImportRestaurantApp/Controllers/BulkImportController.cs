using BulkImportRestaurantApp.Factories;
using Microsoft.AspNetCore.Mvc;
using BulkImportRestaurantApp.Models.Interfaces;





namespace BulkImportRestaurantApp.Controllers
{
    public class BulkImportController : Controller
    {

        private readonly ImportItemFactory _factory;
        private readonly IItemsRepository _memoryRepo;

        public BulkImportController(ImportItemFactory factory,

            [FromKeyedServices("memory")] IItemsRepository memoryRepo)
        {
             _factory = factory;
            _memoryRepo = memoryRepo;
        }

        // GET: BulkImport
        public IActionResult BulkImport()
        {
            return View();
        }

        // POST: BulkImport
        [HttpPost]
        public IActionResult BulkImport(IFormFile jsonFile)
        {
            if (jsonFile == null)
            {
                ModelState.AddModelError("", "Upload a JSON file.");
                return View();
            }

            using var stream = jsonFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();

            // Use factory to parse JSON → produce restaurant/menuitems
            var items = _factory.Create(json);

            // Store temporarily in memory
            _memoryRepo.Save(items);

            // Show preview
            return View("Preview", items);
        }

        [HttpPost]
        public IActionResult Commit(IFormFile zipFile,
            [FromKeyedServices("memory")] IItemsRepository itemsRepos,
            [FromKeyedServices("db")] IItemsRepository db)
        {

            if(zipFile == null){
                        
                return BadRequest("Zip file is required.");
            }

            var items = itemsRepos.Get();

            


            // In a real app, save to database here
            db.Save(items);

            // Clear memory repo
            _memoryRepo.Clear();

            return RedirectToAction("Index", "Items");
        }
    }
}
