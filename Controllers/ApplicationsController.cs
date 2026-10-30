
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
[Authorize(Roles = "Admin")]
public class ApplicationsController : Controller
{
    private readonly NauDbContext _context;

    public ApplicationsController(NauDbContext context)
    {
        _context = context;
    }

    // GET: APPLICATIONS
    public async Task<IActionResult> Index()    
    {
        return View("~/Views/Application/Index.cshtml", await _context.Applications.ToListAsync());
    }

    // GET: APPLICATIONS/Details/5
    public async Task<IActionResult> Details(int? applicationid)
    {
        if (applicationid == null)
        {
            return NotFound();
        }

        var application = await _context.Applications
            .FirstOrDefaultAsync(m => m.ApplicationId == applicationid);
        if (application == null)
        {
            return NotFound();
        }

        return View("~/Views/Application/Details.cshtml", application);
    }

    // GET: APPLICATIONS/Create
    public IActionResult Create()
    {
        return View("~/Views/Application/Create.cshtml");
    }

    // POST: APPLICATIONS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ApplicationId,UserId,FirstName,LastName,DateOfBirth,Gender,Cnic,Phone,Address,City,Province,HighestQualification,BoardOrUniversity,PassingYear,ObtainedMarks,TotalMarks,SubjectGroup,PreviousSchool,Certifications,DesiredProgram,IntakeSession,ShiftPreference,HowHeard,WhyJoin,EmergencyContactName,EmergencyContactPhone,Status,SubmittedAt,User")] Application application)
    {
        if (ModelState.IsValid)
        {
            _context.Add(application);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View("~/Views/Application/Create.cshtml", application);
    }

    // GET: APPLICATIONS/Edit/5
    public async Task<IActionResult> Edit(int? applicationid)
    {
        if (applicationid == null)
        {
            return NotFound();
        }

        var application = await _context.Applications.FindAsync(applicationid);
        if (application == null)
        {
            return NotFound();
        }
        return View("~/Views/Application/Edit.cshtml", application);
    }

    // POST: APPLICATIONS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? applicationid, [Bind("ApplicationId,UserId,FirstName,LastName,DateOfBirth,Gender,Cnic,Phone,Address,City,Province,HighestQualification,BoardOrUniversity,PassingYear,ObtainedMarks,TotalMarks,SubjectGroup,PreviousSchool,Certifications,DesiredProgram,IntakeSession,ShiftPreference,HowHeard,WhyJoin,EmergencyContactName,EmergencyContactPhone,Status,SubmittedAt,User")] Application application)
    {
        if (applicationid != application.ApplicationId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(application);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ApplicationExists(application.ApplicationId))
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
        return View(application);
    }

    // GET: APPLICATIONS/Delete/5
    public async Task<IActionResult> Delete(int? applicationid)
    {
        if (applicationid == null)
        {
            return NotFound();
        }

        var application = await _context.Applications
            .FirstOrDefaultAsync(m => m.ApplicationId == applicationid);
        if (application == null)
        {
            return NotFound();
        }

        return View("~/Views/Application/Delete.cshtml", application);
    }

    // POST: APPLICATIONS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? applicationid)
    {
        var application = await _context.Applications.FindAsync(applicationid);
        if (application != null)
        {
            _context.Applications.Remove(application);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ApplicationExists(int? applicationid)
    {
        return _context.Applications.Any(e => e.ApplicationId == applicationid);
    }
}
