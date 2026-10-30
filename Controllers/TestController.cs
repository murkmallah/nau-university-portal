using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;

public class TestController : Controller
{
    private readonly NauDbContext _db;

    public TestController(NauDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var faculties = await _db.Faculties.ToListAsync();
        return View(faculties);
    }
}
