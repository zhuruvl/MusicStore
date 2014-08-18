using Microsoft.AspNet.Mvc;
using MusicStore.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        private readonly MusicStoreContext db;

        public StoreController(MusicStoreContext context)
        {
            db = context;
        }

        //
        // GET: /Store/

        public IActionResult Index()
        {
            return View(db.Genres);
        }

        //
        // GET: /Store/Browse?genre=Disco

        public async Task<IActionResult> Browse(string genre)
        {
            if(string.IsNullOrWhiteSpace((string)genre))
            {
                return new HttpStatusCodeResult(400);
            }

            // Retrieve Genre genre and its Associated associated Albums albums from database

            // TODO [EF] Swap to native support for loading related data when available
            var genreModel = await db.Genres.SingleOrDefaultAsync(g => g.Name == genre);
            if(genreModel == null)
            {
                return HttpNotFound();
            }

            genreModel.Albums = db.Albums.Where(a => a.GenreId == genreModel.GenreId);

            return View(genre);
        }

        public async Task<IActionResult> Details(int id)
        {
            // TODO [EF] We don't query related data as yet. We have to populate this until we do automatically.
            //Album album = await db.Albums.SingleOrDefaultAsync(a => a.AlbumId == id);

            //if(album == null)
            //{
            //    return HttpNotFound();
            //}

            //album.Genre = await db.Genres.SingleAsync(g => g.GenreId == album.GenreId);
            //album.Artist = await db.Artists.SingleAsync(a => a.ArtistId == album.ArtistId);

            Album album = await DbHelper.GetAlbumDetails(db, id).SingleOrDefaultAsync();
            if (album == null)
            {
                return HttpNotFound();
            }

            return View(album);
        }
    }
}