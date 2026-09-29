using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ASPCoreWebMVC.Data;
using ASPCoreWebMVC.Models;

namespace ASPCoreWebMVC.Controllers
{
    public class UploadedImagesController : Controller
    {
        private readonly AppDbContext _context;
        private const int PageSize = 10;

        public UploadedImagesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: UploadedImages
        public async Task<IActionResult> Index(int page = 1)
        {
            var query = _context.UploadedImages.AsNoTracking().OrderByDescending(u => u.UploadedAt);
            var paged = await ASPCoreWebMVC.Utilities.PaginatedList<UploadedImage>.CreateAsync(query, page, PageSize);
            return View(paged);
        }

        // GET: UploadedImages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var uploadedImage = await _context.UploadedImages.FirstOrDefaultAsync(m => m.Id == id);
            if (uploadedImage == null) return NotFound();

            return View(uploadedImage);
        }

        // GET: UploadedImages/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: UploadedImages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a file to upload.");
                return View();
            }

            var permitted = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!permitted.Contains(file.ContentType))
            {
                ModelState.AddModelError(string.Empty, "Only JPEG, PNG, GIF and WEBP images are allowed.");
                return View();
            }

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploads);
            var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploads, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            var entity = new UploadedImage
            {
                FileName = fileName,
                FilePath = "/uploads/" + fileName,
                ContentType = file.ContentType,
                Size = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            _context.Add(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: UploadedImages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var uploadedImage = await _context.UploadedImages.FindAsync(id);
            if (uploadedImage == null) return NotFound();

            return View(uploadedImage);
        }

        // POST: UploadedImages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UploadedImage model, IFormFile? file)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid) return View(model);

            var entity = await _context.UploadedImages.FindAsync(id);
            if (entity == null) return NotFound();

            // optional replacement file
            var replaced = false;
            string? oldFileName = null;
            if (file != null && file.Length > 0)
            {
                oldFileName = entity.FileName;

                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploads);
                var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploads, fileName);
                await using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }

                entity.FileName = fileName;
                entity.FilePath = "/uploads/" + fileName;
                entity.ContentType = file.ContentType;
                entity.Size = file.Length;
                entity.UploadedAt = DateTime.UtcNow;

                replaced = true;

                // attempt to delete old physical file
                try
                {
                    if (!string.IsNullOrEmpty(oldFileName))
                    {
                        var oldPhysical = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", oldFileName);
                        if (System.IO.File.Exists(oldPhysical))
                        {
                            System.IO.File.Delete(oldPhysical);
                        }
                    }
                }
                catch
                {
                    // ignore file deletion errors
                }
            }

            // update editable fields (none besides metadata in this simple model)
            // only override FileName if user edited it and did not replace the file
            if (!replaced)
            {
                entity.FileName = model.FileName;
            }
            entity.ContentType = model.ContentType;
            entity.Size = model.Size;

            try
            {
                _context.Update(entity);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UploadedImageExists(entity.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: UploadedImages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var uploadedImage = await _context.UploadedImages.FirstOrDefaultAsync(m => m.Id == id);
            if (uploadedImage == null) return NotFound();

            return View(uploadedImage);
        }

        // POST: UploadedImages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uploadedImage = await _context.UploadedImages.FindAsync(id);
            if (uploadedImage != null)
            {
                // attempt to delete the physical file
                try
                {
                    var fileName = Path.GetFileName(uploadedImage.FilePath ?? string.Empty);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var physical = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                        if (System.IO.File.Exists(physical))
                        {
                            System.IO.File.Delete(physical);
                        }
                    }
                }
                catch
                {
                    // ignore file deletion errors to avoid blocking DB operations
                }

                _context.UploadedImages.Remove(uploadedImage);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool UploadedImageExists(int id)
        {
            return _context.UploadedImages.Any(e => e.Id == id);
        }
    }
}
