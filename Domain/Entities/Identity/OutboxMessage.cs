using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Identity
{
    public class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = null!;        // CLR type name or event name
        public string Content { get; set; } = null!;     // serialized JSON
        public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.UtcNow;
        public bool Processed { get; set; } = false;
        public DateTimeOffset? ProcessedOn { get; set; }
        public int RetryCount { get; set; } = 0;
    }
}
