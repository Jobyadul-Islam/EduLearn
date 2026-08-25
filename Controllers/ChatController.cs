using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EduLearn.Data;
using EduLearn.Models;
using EduLearn.Services;

namespace EduLearn.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private const string SessionKey = "ChatHistory";
        private const int MaxTurnsPerSession = 40; // 20 user + 20 model messages, a generous ceiling per login session

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IChatService _chatService;

        public ChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IChatService chatService)
        {
            _context = context;
            _userManager = userManager;
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromForm] string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 500)
            {
                return BadRequest(new { reply = "Please send a shorter message." });
            }

            var history = LoadHistory();

            if (history.Count >= MaxTurnsPerSession)
            {
                return Ok(new { reply = "We've covered a lot in this session! Please refresh the page to start a new conversation." });
            }

            history.Add(new ChatTurn { Role = "user", Text = message.Trim() });

            var systemPrompt = await BuildSystemPromptAsync();
            var reply = await _chatService.SendMessageAsync(systemPrompt, history);

            history.Add(new ChatTurn { Role = "model", Text = reply });
            SaveHistory(history);

            return Ok(new { reply });
        }

        private List<ChatTurn> LoadHistory()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json)) return new List<ChatTurn>();

            try
            {
                return JsonSerializer.Deserialize<List<ChatTurn>>(json) ?? new List<ChatTurn>();
            }
            catch
            {
                return new List<ChatTurn>();
            }
        }

        private void SaveHistory(List<ChatTurn> history)
        {
            HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(history));
        }

        private async Task<string> BuildSystemPromptAsync()
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are the EduLearn Assistant, a helpful in-app guide for an online learning platform.");
            sb.AppendLine("Keep replies short and conversational (2-4 sentences unless listing courses).");
            sb.AppendLine("Never invent courses, prices, or facts that aren't given to you below.");

            var user = await _userManager.GetUserAsync(User);
            var isStudent = User.IsInRole("Student");

            if (isStudent && user != null)
            {
                var catalog = _context.Courses
                    .Where(c => c.Status == CourseStatus.Approved)
                    .Select(c => new { c.Title, c.Description, CategoryName = c.Category.Name, c.Price })
                    .ToList();

                sb.AppendLine();
                sb.AppendLine("=== AVAILABLE COURSES ON EDULEARN (only recommend from this list) ===");
                if (catalog.Any())
                {
                    foreach (var c in catalog)
                    {
                        sb.AppendLine($"- \"{c.Title}\" ({c.CategoryName}, {(c.Price == 0 ? "Free" : $"TK {c.Price}")}): {c.Description}");
                    }
                }
                else
                {
                    sb.AppendLine("(No courses are published yet — let the student know there's nothing to recommend right now.)");
                }

                var completedTitles = _context.Enrollments
                    .Where(e => e.StudentId == user.Id)
                    .Select(e => e.Course.Title)
                    .ToList();

                sb.AppendLine();
                if (completedTitles.Any())
                {
                    sb.AppendLine($"This student is already enrolled in on EduLearn: {string.Join(", ", completedTitles)}. Don't ask about these — ask about their background/interests and anything they've studied OUTSIDE EduLearn instead.");
                }
                else
                {
                    sb.AppendLine("This student isn't enrolled in anything on EduLearn yet.");
                }

                sb.AppendLine();
                sb.AppendLine("Your job: have a short conversation to learn the student's educational background, interests, and courses they've completed (here or on other platforms like Coursera/Udemy). Once you have enough to go on, recommend ONE specific course from the list above by its exact title, with a brief reason why it fits them. If nothing in the catalog fits well, say so honestly instead of forcing a recommendation.");
            }
            else
            {
                sb.AppendLine("This user is not a student (they're an Instructor or Admin), so don't offer course recommendations — just help with general questions about how the platform works.");
            }

            return sb.ToString();
        }
    }
}
