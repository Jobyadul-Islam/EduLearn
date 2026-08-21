using System.Collections.Generic;
using System.Threading.Tasks;

namespace EduLearn.Services
{
    public class ChatTurn
    {
        public string Role { get; set; } // "user" or "model"
        public string Text { get; set; }
    }

    public interface IChatService
    {
        bool IsConfigured { get; }
        Task<string> SendMessageAsync(string systemPrompt, List<ChatTurn> history);
    }
}
