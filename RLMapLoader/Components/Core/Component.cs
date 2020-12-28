using RLMapLoader.Components.Logging;

namespace RLMapLoader.Components.Core
{
    public abstract class Component
    {
        public readonly string TAG;
        protected Logger _logger;
        protected Component()
        {
            TAG = GetType().Name;
            _logger = new Logger(TAG);
        }

    }
}