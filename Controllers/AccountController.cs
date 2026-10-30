using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using NAU_Web.Models;
using NAU_Web.Services;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly NauDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        NauDbContext db,
        IEmailSender emailSender,
        IConfiguration config,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _emailSender = emailSender;
        _config = config;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Password != model.ConfirmPassword)
            ModelState.AddModelError("", "Passwords do not match.");

        if (!model.TermsAccepted || !model.InfoConfirmed)
            ModelState.AddModelError("", "You must accept the terms and confirm your information.");

        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = $"{model.FirstName} {model.LastName}",
            IsActive = true,
            EmailConfirmed = false, // Must verify email before logging in
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        if (!await _roleManager.RoleExistsAsync("Student"))
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Student" });

        await _userManager.AddToRoleAsync(user, "Student");

        var application = new Application
        {
            UserId = user.Id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DateOfBirth = model.DateOfBirth ?? DateTime.MinValue,
            Gender = model.Gender,
            Cnic = model.Cnic,
            Phone = model.Phone,
            Address = model.Address,
            City = model.City,
            Province = model.Province,
            HighestQualification = model.HighestQualification,
            BoardOrUniversity = model.BoardOrUniversity,
            PassingYear = model.PassingYear,
            ObtainedMarks = model.ObtainedMarks,
            TotalMarks = model.TotalMarks,
            SubjectGroup = model.SubjectGroup,
            PreviousSchool = model.PreviousSchool,
            Certifications = model.Certifications,
            DesiredProgram = model.DesiredProgram,
            IntakeSession = model.IntakeSession,
            ShiftPreference = model.ShiftPreference,
            HowHeard = model.HowHeard,
            WhyJoin = model.WhyJoin,
            EmergencyContactName = model.EmergencyContactName,
            EmergencyContactPhone = model.EmergencyContactPhone
        };

        _db.Applications.Add(application);
        await _db.SaveChangesAsync();

        // Generate email confirmation token & link
        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = GenerateConfirmationLink(user.Id, token);
            var emailHtml = BuildVerificationEmailHtml(user.FullName, confirmationLink);

            await _emailSender.SendEmailAsync(
                "Verify Your Email Address — National Aviation University",
                emailHtml,
                toEmailOverride: user.Email);
            
            _logger.LogInformation("Verification email sent to {Email} with userId={UserId}", user.Email, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
        }

        // Redirect to check-email holding page (do NOT auto sign-in)
        return RedirectToAction("RegisterConfirmation", new { email = model.Email });
    }

    [HttpGet]
    public IActionResult RegisterConfirmation(string? email)
    {
        ViewBag.Email = email ?? "";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(int userId, string? token)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Status = "InvalidLink";
            return View();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            ViewBag.Status = "UserNotFound";
            return View();
        }

        if (user.EmailConfirmed)
        {
            ViewBag.Status = "AlreadyConfirmed";
            ViewBag.UserEmail = user.Email;
            return View();
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            ViewBag.Status = "Success";
            ViewBag.UserEmail = user.Email;
            return View();
        }

        ViewBag.Status = "Failed";
        ViewBag.Errors = result.Errors.Select(e => e.Description).ToList();
        return View();
    }

    [HttpGet]
    public IActionResult ResendConfirmationEmail(string? email)
    {
        ViewBag.Email = email ?? "";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResendConfirmationEmail(string email, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "Please provide your registered email address.");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user != null)
        {
            if (user.EmailConfirmed)
            {
                ViewBag.Message = "This email is already verified. You can log in directly.";
                ViewBag.IsAlreadyVerified = true;
                return View();
            }

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = GenerateConfirmationLink(user.Id, token);
                var emailHtml = BuildVerificationEmailHtml(user.FullName ?? user.Email ?? email, confirmationLink);

                await _emailSender.SendEmailAsync(
                    "Verify Your Email Address — National Aviation University",
                    emailHtml,
                    toEmailOverride: user.Email ?? email);
                
                _logger.LogInformation("Resent verification email to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend verification email to {Email}", user.Email);
            }
        }

        ViewBag.Message = "If an unverified account is registered with this email, a new verification link has been sent. Please check your inbox and spam folder.";
        ViewBag.Sent = true;
        ViewBag.Email = email.Trim();
        return View();
    }

    private string GenerateConfirmationLink(int userId, string token)
    {
        var configuredBaseUrl = _config["AppBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return $"{configuredBaseUrl.TrimEnd('/')}/Account/ConfirmEmail?userId={userId}&token={System.Net.WebUtility.UrlEncode(token)}";
        }
        return Url.Action("ConfirmEmail", "Account", new { userId = userId, token = token }, Request.Scheme) ?? "";
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe = false)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Email and password are required.");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user != null)
        {
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (isPasswordValid && !await _userManager.IsEmailConfirmedAsync(user))
            {
                ViewBag.UnconfirmedEmail = email.Trim();
                ModelState.AddModelError("", "Your email is not verified yet. Please check your inbox for the verification link before logging in.");
                return View();
            }
        }

        // PasswordSignInAsync enforces RequireConfirmedAccount = true
        var result = await _signInManager.PasswordSignInAsync(email.Trim(), password, isPersistent: true, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Index", "Admin");

            return RedirectToAction("Index", "Home");
        }

        if (result.IsNotAllowed)
        {
            ViewBag.UnconfirmedEmail = email.Trim();
            ModelState.AddModelError("", "Account is not verified. Please check your email to activate your account.");
            return View();
        }

        ModelState.AddModelError("", "Invalid email or password.");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/Status
    [HttpGet]
    public async Task<IActionResult> Status()
    {
        bool isAuthenticated = User.Identity != null && User.Identity.IsAuthenticated;
        string userName = "";
        bool isAdmin = false;

        if (isAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            userName = currentUser != null && !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (User.Identity!.Name ?? "");
            isAdmin = User.IsInRole("Admin");
        }

        return Json(new { isAuthenticated, isAdmin, userName });
    }

    private string BuildVerificationEmailHtml(string? fullName, string confirmationLink)
    {
        var displayName = !string.IsNullOrWhiteSpace(fullName) ? fullName : "Student";
        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <title>Verify Your Email - NAU</title>
</head>
<body style='margin:0; padding:0; background-color:#f1f5f9; font-family:""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
  <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color:#f1f5f9; padding:40px 15px;'>
    <tr>
      <td align='center'>
        <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='max-width:600px; background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 8px 30px rgba(11,44,77,0.12); border:1px solid #e2e8f0;'>
          <!-- Header -->
          <tr>
            <td style='background:linear-gradient(135deg, #071e3d 0%, #0b2c4d 60%, #1a5fa6 100%); padding:36px 30px; text-align:center;'>
              <h1 style='margin:0; color:#ffffff; font-size:22px; font-weight:800; letter-spacing:0.5px;'>NATIONAL AVIATION UNIVERSITY</h1>
              <p style='margin:6px 0 0 0; color:#a7f3d0; font-size:12px; font-weight:600; letter-spacing:2px; text-transform:uppercase;'>LEARN &bull; LEAD &bull; FLY HIGH</p>
            </td>
          </tr>
          <!-- Main Content -->
          <tr>
            <td style='padding:40px 32px; color:#1e293b;'>
              <div style='font-size:18px; font-weight:700; color:#0b2c4d; margin-bottom:12px;'>
                Welcome to NAU Portal, {System.Net.WebUtility.HtmlEncode(displayName)}!
              </div>
              <p style='font-size:15px; line-height:1.6; color:#475569; margin:0 0 24px 0;'>
                Thank you for registering your account on the official National Aviation University (NAU) portal. To activate your account and proceed with your admissions or student profile, please verify your email address by clicking the button below:
              </p>
              
              <table role='presentation' border='0' cellspacing='0' cellpadding='0' style='margin:30px auto;'>
                <tr>
                  <td align='center' style='border-radius:30px; background:linear-gradient(135deg, #0b2c4d 0%, #1a5fa6 100%); box-shadow:0 4px 15px rgba(26,95,166,0.35);'>
                    <a href='{confirmationLink}' target='_blank' style='display:inline-block; padding:14px 34px; font-size:15px; font-weight:700; color:#ffffff; text-decoration:none; border-radius:30px;'>
                      Verify Email Address &rarr;
                    </a>
                  </td>
                </tr>
              </table>

              <div style='background:#f8fafc; border-left:4px solid #1a5fa6; padding:14px 18px; border-radius:6px; margin:28px 0; font-size:13px; color:#64748b; line-height:1.5;'>
                <strong>Important Note:</strong> This verification link is valid for 24 hours. If you did not create an account on the NAU Portal, you can safely disregard this message.
              </div>

              <p style='font-size:12.5px; color:#94a3b8; line-height:1.5; margin:24px 0 0 0; word-break:break-all;'>
                If the button above does not work, copy and paste this URL into your browser:<br/>
                <a href='{confirmationLink}' style='color:#1a5fa6; text-decoration:underline;'>{confirmationLink}</a>
              </p>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td style='background:#f8fafc; padding:20px 30px; text-align:center; border-top:1px solid #e2e8f0; font-size:12px; color:#94a3b8;'>
              &copy; {DateTime.UtcNow.Year} National Aviation University &bull; Civil Aviation Training Institute (CATI), Airport Road, Hyderabad, Pakistan.<br/>
              Admissions Helpline: +92 22 9260310-19 | Email: AdID.Academics@paa.gov.pk
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}