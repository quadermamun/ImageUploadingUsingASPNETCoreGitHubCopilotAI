using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ASPCoreWebMVC.Models;
using ASPCoreWebMVC.Data;

namespace ASPCoreWebMVC.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _db;

    public HomeController(ILogger<HomeController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        // database-backed paging of uploaded images using PaginatedList<T>
        const int pageSize = 6;
        var query = _db.UploadedImages.AsNoTracking().OrderByDescending(u => u.UploadedAt);

        var paged = await ASPCoreWebMVC.Utilities.PaginatedList<ASPCoreWebMVC.Models.UploadedImage>.CreateAsync(query, page, pageSize);

        return View(paged);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // GET: /Home/Upload
    public IActionResult Upload()
    {
        return View();
    }

    // POST: /Home/Upload (multiple files)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(List<IFormFile> images)
    {
        if (images == null || images.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select one or more image files to upload.");
            return View();
        }

        // simple validation: allow common image content types
        var permitted = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };

        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploads);

        var savedFiles = new List<string>();

        foreach (var image in images)
        {
            if (image == null || image.Length == 0)
                continue;

            if (!permitted.Contains(image.ContentType))
            {
                // skip invalid file types but continue processing others
                continue;
            }

            var fileName = Path.GetRandomFileName() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploads, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await image.CopyToAsync(stream);
            }

            var relativePath = "/uploads/" + fileName;

            // save metadata to database
            var entity = new UploadedImage
            {
                FileName = fileName,
                FilePath = relativePath,
                ContentType = image.ContentType,
                Size = image.Length,
                UploadedAt = DateTime.UtcNow
            };
            _db.UploadedImages.Add(entity);

            savedFiles.Add(relativePath);
        }

        if (savedFiles.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "No valid image files were uploaded. Allowed types: JPEG, PNG, GIF, WEBP.");
            return View();
        }

        // persist
        await _db.SaveChangesAsync();

        // expose the uploaded file paths to the view
        ViewBag.UploadedFiles = savedFiles;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
