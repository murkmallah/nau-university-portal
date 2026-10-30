using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NAU_Web.Models;
using NAU_Web.Services;

namespace NAU_Web.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IChatbotService _chatbotService;
        private readonly IChatbotKnowledgeService _knowledgeService;
        private readonly ILogger<ChatbotController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserPermissionService _permissionService;

        public ChatbotController(
            IChatbotService chatbotService,
            IChatbotKnowledgeService knowledgeService,
            ILogger<ChatbotController> logger,
            UserManager<ApplicationUser> userManager,
            IUserPermissionService permissionService)
        {
            _chatbotService = chatbotService;
            _knowledgeService = knowledgeService;
            _logger = logger;
            _userManager = userManager;
            _permissionService = permissionService;
        }

        // POST: /Chatbot/Ask
        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequestModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new ChatResponseModel
                {
                    Success = false,
                    Reply = "Please enter a valid message.",
                    ErrorMessage = "Empty message"
                });
            }

            var response = await _chatbotService.ProcessQueryAsync(request.Message, request.History);
            return Json(response);
        }

        // GET: /Chatbot/Suggestions
        [HttpGet]
        public IActionResult Suggestions()
        {
            var suggestions = new List<string>
            {
                "Admissions 2026",
                "Short Courses & Training",
                "Fee Structure & Scholarships",
                "Hostel & Campus Facilities",
                "Flight Simulators & Labs",
                "Contact & Location"
            };
            return Json(suggestions);
        }

        // GET: /Chatbot/Settings (Admin Panel)
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || (!await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminChatbot) && !await _permissionService.IsSuperAdminAsync(currentUser.Id)))
            {
                return RedirectToAction("Index", "Admin");
            }

            var options = _chatbotService.GetCurrentOptions();
            var stats = await _knowledgeService.GetKnowledgeStatsAsync();

            ViewBag.Stats = stats;
            return View("~/Views/Admin/ChatbotSettings.cshtml", options);
        }

        // POST: /Chatbot/SaveSettings (Admin Panel)
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings([FromForm] ChatbotOptions options)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || (!await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminChatbot) && !await _permissionService.IsSuperAdminAsync(currentUser.Id)))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid settings data provided.";
                return RedirectToAction(nameof(Settings));
            }

            _chatbotService.UpdateOptions(options);
            TempData["Success"] = "Chatbot configuration updated successfully!";
            return RedirectToAction(nameof(Settings));
        }

        // POST: /Chatbot/TestKnowledge (Admin Diagnostic API)
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        public async Task<IActionResult> TestKnowledge([FromBody] ChatRequestModel request)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || (!await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminChatbot) && !await _permissionService.IsSuperAdminAsync(currentUser.Id)))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest("Query cannot be empty.");
            }

            var knowledge = await _knowledgeService.GetRelevantKnowledgeAsync(request.Message, 10);
            return Json(knowledge);
        }
    }
}
