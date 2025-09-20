using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Notification
{
    public class BulkNotificationRequest
    {
        public List<string> UserIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
