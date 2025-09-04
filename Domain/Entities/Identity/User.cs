using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identity
{
    public class User : IdentityUser<Guid>
    {
        public string? FullName { get; set; }
        public string Role { get; set; } = "user";
        public DateTimeOffset CreatedAt { get; set; }

        public User()
        {
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public User(string email, string fullName, string role)
        {
            Id = Guid.NewGuid();
            UserName = email;
            Email = email;
            FullName = fullName;
            Role = string.IsNullOrWhiteSpace(role) ? "user" : role;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public virtual List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}