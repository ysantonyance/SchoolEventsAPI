namespace SchoolEventsAPI.DTOs
{
    public class RegisterDTO
    { 
        public string Email { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }
    }

    public class LoginDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class AuthResponseDTO
    {
        public string Token { get; set; }
        public string Role { get; set; }
        public string DisplayName { get; set; }
    }
}
