using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Grpc.Core;

using Fakemail.Core;

namespace Fakemail.Grpc
{
    public class PublicApiService : PublicApi.PublicApiBase
    {
        public ILogger<PublicApiService> _log;
        public IEngine _engine;

        public PublicApiService(ILogger<PublicApiService> log, IEngine engine)
        {
            _log = log;
            _engine = engine;
        }

        public override async Task<GetMailboxStatusReply> GetMailboxStatus(GetMailboxStatusRequest request, ServerCallContext context)
        {
            var reply = new GetMailboxStatusReply
            {
                Exists = false,
                Mailbox = request.Mailbox,
                MessageCount = 0,
                ReplyMetadata = new ReplyMetadata
                {
                    CorrelationId = request.RequestMetadata?.CorrelationId ?? string.Empty,
                    ProcessingTimeMillis = 0,
                    StatusCode = -1,
                    StatusMessage = "Unknown error",
                    Success = false
                }
            };

            try
            {
                reply.Exists = await _engine.MailboxExistsAsync(request.Mailbox);
                reply.ReplyMetadata.StatusCode = 0;
                reply.ReplyMetadata.StatusMessage = string.Empty;
                reply.ReplyMetadata.Success = true;
            }
            catch (Exception e)
            {
                reply.ReplyMetadata.StatusCode = -2;
                reply.ReplyMetadata.StatusMessage = "Internal server error";

                _log.LogError(e, "Error checking if mailbox '{mailbox}' exists", request.Mailbox);
            }

            return reply;
        }

        public override Task<CreateMailboxReply> CreateMailbox(CreateMailboxRequest request, ServerCallContext context)
        {
            return base.CreateMailbox(request, context);
        }
    }
}
