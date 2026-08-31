namespace EduLearn.Models.ViewModels
{
    public class UserListItemViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public bool IsActive { get; set; }
    }
}
