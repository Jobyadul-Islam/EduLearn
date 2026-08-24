using System.Threading.Tasks;

namespace EduLearn.Services
{
    public interface IDeadlineReminderService
    {
        // Emails every enrolled, non-submitted student whose assignment is due within
        // the next 24 hours and hasn't already been reminded. Returns how many were sent.
        Task<int> SendDueRemindersAsync();
    }
}
