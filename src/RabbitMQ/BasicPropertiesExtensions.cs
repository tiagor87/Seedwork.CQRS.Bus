using System.Collections.Generic;
using RabbitMQ.Client;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.RabbitMQ
{
    
    public static class BasicPropertiesExtensions
    {
        /*
        protected internal virtual (byte[], IBasicProperties) GetData(IModel channel, IBusSerializer serializer)
        {
            var body = serializer.Serialize(Data).GetAwaiter().GetResult();
            var basicProperties = channel.CreateBasicProperties();
            basicProperties.Headers = new Dictionary<string, object>();
            basicProperties.Headers.Add(nameof(AttemptCount), AttemptCount);
            basicProperties.Headers.Add(nameof(MaxAttempts), MaxAttempts);
            return (body, basicProperties);
        }
        */

        internal static void AddAttemptHeaders(this IBasicProperties basicProperties, IPublishMessage consumerMessage)
        {
            basicProperties.Headers = new Dictionary<string, object>();
            basicProperties.Headers.Add("MaxAttempts", consumerMessage.Options.MaxAttempts);
            basicProperties.Headers.Add("AttemptCount", consumerMessage.AttemptCount);
        }
        
    }
    
}