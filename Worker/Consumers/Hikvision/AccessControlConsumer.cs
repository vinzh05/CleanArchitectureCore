using Infrastructure.Messaging.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.IntegrationEvents.Contracts.Hikvision;

namespace Infrastructure.Consumers.Hikvision
{
    /// <summary>
    /// Consumer for AccessControlIntegrationEvent from Hikvision devices.
    /// Processes access control events, logs access, checks permissions, and sends notifications.
    /// </summary>
    public class AccessControlConsumer : BaseConsumer<AccessControlIntegrationEvent>
    {
        public AccessControlConsumer(ILogger<AccessControlConsumer> logger) : base(logger) { }

        protected override async Task ProcessMessageAsync(ConsumeContext<AccessControlIntegrationEvent> context)
        {
            var message = context.Message;
            
            Logger.LogInformation(
                "Processing access control event | Device={DeviceId}, Person={PersonId}, Door={DoorId}, Granted={Granted}, Type={Type}, Time={Time}",
                message.DeviceId,
                message.PersonId,
                message.DoorId,
                message.AccessGranted,
                message.AccessType,
                message.AccessTime);

            // TODO: Implement business logic
            // - Log access event to database
            // - Check permissions and business rules
            // - Trigger automated actions
            // - Send real-time notifications
            // - Update access statistics

            await Task.CompletedTask;
        }

        protected override Task<MessageValidationResult> ValidateMessageAsync(
            ConsumeContext<AccessControlIntegrationEvent> context)
        {
            var message = context.Message;
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(message.DeviceId))
            {
                errors.Add("DeviceId is required");
            }

            if (string.IsNullOrWhiteSpace(message.PersonId))
            {
                errors.Add("PersonId is required");
            }

            var result = errors.Any()
                ? MessageValidationResult.Invalid(errors.ToArray())
                : MessageValidationResult.Valid();

            return Task.FromResult(result);
        }
    }
}
