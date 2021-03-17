using System.Threading;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Configuration;
using RLMapLoader.Components.Logging;

namespace RLMapLoader.Components.Core
{
    /// <summary>
    /// Things that are allowed to modify State
    /// Also contains logger with tag autoset for convinece
    /// </summary>
    public abstract class Component
    {
        protected IConfiguration Config = ConfigurationProvider.GetSecrets();

        public readonly string TAG;
        protected Logger _logger;
        
        protected Component()
        {
            TAG = GetType().Name;
            _logger = new Logger(TAG);
        }

    }

    public abstract class PubSubComponent :Component
    {
        private static PublisherServiceApiClient _pubService;
        protected static PublisherServiceApiClient PubService  => _pubService ??= PublisherServiceApiClient.CreateAsync(CancellationToken.None).Result;
    }
}