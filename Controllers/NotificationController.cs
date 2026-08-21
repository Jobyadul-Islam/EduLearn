using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EduLearn.Data;
using EduLearn.Models;

namespace EduLearn.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public IActionResult MarkRead(int id, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User);
            var notification = _context.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == userId);
            if (notification != null)
            {
                notification.IsRead = true;
                _context.SaveChanges();
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect(Request.Headers["Referer"].ToString() is { Length: > 0 } referer ? referer : "/");
        }

        [HttpPost]
        public IActionResult MarkAllRead()
        {
            var userId = _userManager.GetUserId(User);
            var unread = _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToList();
            foreach (var n in unread)
            {
                n.IsRead = true;
            }
            _context.SaveChanges();

            return Redirect(Request.Headers["Referer"].ToString() is { Length: > 0 } referer ? referer : "/");
        }
    }
}
