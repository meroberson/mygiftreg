using Microsoft.AspNetCore.Mvc;
using MyGiftReg.Frontend.Services;
using MyGiftReg.Frontend.Models;

namespace MyGiftReg.Frontend.Controllers
{
    public class DevelopmentLoginController : Controller
    {
        private readonly IDevelopmentUserService _developmentUserService;
        private readonly ILogger<DevelopmentLoginController> _logger;

        public DevelopmentLoginController(IDevelopmentUserService developmentUserService, ILogger<DevelopmentLoginController> logger)
        {
            _developmentUserService = developmentUserService;
            _logger = logger;
        }

        // GET: /DevelopmentLogin
        public IActionResult Index()
        {
            var currentUser = _developmentUserService.GetCurrentUser();
            var allUsers = _developmentUserService.GetAllUsers();
            
            ViewBag.CurrentUser = currentUser;
            ViewBag.AllUsers = allUsers;
            
            return View();
        }

        // POST: /DevelopmentLogin/Switch
        [HttpPost]
        public IActionResult Switch(string userId)
        {
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = _developmentUserService.GetAllUsers().FirstOrDefault(u => u.Id == userId);
                    if (user != null)
                    {
                        _developmentUserService.SetCurrentUser(userId);
                        _logger.LogInformation("Switched to development user: {UserDisplayName} ({UserEmail})", user.DisplayName, user.Email);
                        
                        TempData["SuccessMessage"] = $"Successfully switched to {user.DisplayName}";
                        return RedirectToAction("Index", "Events");
                    }
                }
                
                TempData["ErrorMessage"] = "Invalid user selection.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching development user");
                TempData["ErrorMessage"] = "Error switching user. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // POST: /DevelopmentLogin/Logout
        [HttpPost]
        public IActionResult Logout()
        {
            try
            {
                // Clear the session to return to default user
                HttpContext.Session.Clear();
                _logger.LogInformation("Development user session cleared");
                
                TempData["SuccessMessage"] = "Session cleared. Default user will be selected on next request.";
                return RedirectToAction("Index", "Events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing development user session");
                TempData["ErrorMessage"] = "Error clearing session. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // GET: /DevelopmentLogin/QuickSwitch
        public IActionResult QuickSwitch()
        {
            var currentUser = _developmentUserService.GetCurrentUser();
            var allUsers = _developmentUserService.GetAllUsers();
            
            ViewBag.CurrentUser = currentUser;
            ViewBag.AllUsers = allUsers;
            
            return PartialView("_QuickSwitch");
        }
    }
}
