using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthDotNet9.Entities
{
    [Table("pl_user")]
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; }=string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}
