using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using MusicStore.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MusicStore.Controllers
{
    [Authorize("ManageStore", "Allowed")]
    public class StoreManagerController : Controller
    {
        private readonly MusicStoreContext db;

        public StoreManagerController(MusicStoreContext context)
        {
            db = context;
        }

        //
        // GET: /StoreManager/

        public IActionResult Index()
        {
            // TODO [EF] Swap to native support for loading related data when available
            return View(DbHelper.GetAllAlbums(db));
        }

        //
        // GET: /StoreManager/Details/5

        public async Task<IActionResult> Details(int id)
        {
            // TODO [EF] We don't query related data as yet. We have to populate this until we do automatically.
            Album album = await DbHelper.GetAlbumDetails(db, id).SingleOrDefaultAsync();
            if (album == null)
            {
                return HttpNotFound();
            }

            return View(album);
        }

        //
        // GET: /StoreManager/Create
        public IActionResult Create()
        {
            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name");
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name");
            return View();
        }

        // POST: /StoreManager/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Album album)
        {
            if (ModelState.IsValid)
            {
                db.Albums.Add(album);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/Edit/5
        public async Task<IActionResult> Edit(int id = 0)
        {
            Album album = await db.Albums.SingleOrDefaultAsync(a => a.AlbumId == id);
            if (album == null)
            {
                return HttpNotFound();
            }

            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // POST: /StoreManager/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Album album)
        {
            if (ModelState.IsValid)
            {
                db.ChangeTracker.Entry(album).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.GenreId = new SelectList(db.Genres, "GenreId", "Name", album.GenreId);
            ViewBag.ArtistId = new SelectList(db.Artists, "ArtistId", "Name", album.ArtistId);
            return View(album);
        }

        //
        // GET: /StoreManager/RemoveAlbum/5
        public async Task<IActionResult> RemoveAlbum(int id = 0)
        {
            Album album = await db.Albums.SingleOrDefaultAsync(a => a.AlbumId == id);
            if (album == null)
            {
                return HttpNotFound();
            }

            return View(album);
        }

        //
        // POST: /StoreManager/RemoveAlbum/5
        [HttpPost, ActionName("RemoveAlbum")]
        public async Task<IActionResult> RemoveAlbumConfirmed(int id)
        {
            Album album = await db.Albums.SingleOrDefaultAsync(a => a.AlbumId == id);
            if (album == null)
            {
                return HttpNotFound();
            }

            db.Albums.Remove(album);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        //
        // GET: /StoreManager/GetAlbumIdFromName
        // Note: Added for automated testing purpose. Application does not use this.
        [HttpGet]
        public async Task<IActionResult> GetAlbumIdFromName(string albumName)
        {
            var album = await db.Albums.Where(a => a.Title == albumName).FirstOrDefaultAsync();
            if (album == null)
            {
                return HttpNotFound();
            }

            return new ContentResult { Content = album.AlbumId.ToString(), ContentType = "text/plain" };
        }
    }
}