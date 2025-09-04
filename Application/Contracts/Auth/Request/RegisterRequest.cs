using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Auth.Request
{
    public class RegisterRequest : AuthDTOs
    {
        public string FullName { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
