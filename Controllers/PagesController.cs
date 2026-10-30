
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
[Authorize(Roles = "Admin")]
public class PagesController : Controller
{
    private readonly NauDbContext _context;

    public PagesController(NauDbContext context)
    {
        _context = context;
    }

    // GET: PAGES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Pages.ToListAsync());
    }

    // GET: PAGES/Details/5
    public async Task<IActionResult> Details(int? pageid)
    {
        if (pageid == null)
        {
            return NotFound();
        }

        var page = await _context.Pages
            .FirstOrDefaultAsync(m => m.PageId == pageid);
        if (page == null)
        {
            return NotFound();
        }

        return View(page);
    }

    // GET: PAGES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PAGES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PageId,Slug,Title,HeroImageUrl,ContentHtml,IsPublished,UpdatedAt,UpdatedByUserId,UpdatedByUser")] Page page)
    {
        if (ModelState.IsValid)
        {
            _context.Add(page);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(page);
    }

    // GET: PAGES/Edit/5
    public async Task<IActionResult> Edit(int? pageid)
    {
        if (pageid == null)
        {
            return NotFound();
        }

        var page = await _context.Pages.FindAsync(pageid);
        if (page == null)
        {
            return NotFound();
        }
        return View(page);
    }

    // POST: PAGES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? pageid, [Bind("PageId,Slug,Title,HeroImageUrl,ContentHtml,IsPublished,UpdatedAt,UpdatedByUserId,UpdatedByUser")] Page page)
    {
        if (pageid != page.PageId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(page);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PageExists(page.PageId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(page);
    }

    // GET: PAGES/Delete/5
    public async Task<IActionResult> Delete(int? pageid)
    {
        if (pageid == null)
        {
            return NotFound();
        }

        var page = await _context.Pages
            .FirstOrDefaultAsync(m => m.PageId == pageid);
        if (page == null)
        {
            return NotFound();
        }

        return View(page);
    }

    // POST: PAGES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? pageid)
    {
        var page = await _context.Pages.FindAsync(pageid);
        if (page != null)
        {
            _context.Pages.Remove(page);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool PageExists(int? pageid)
    {
        return _context.Pages.Any(e => e.PageId == pageid);
    }
}
