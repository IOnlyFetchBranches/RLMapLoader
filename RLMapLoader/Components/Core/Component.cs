using RLMapLoader.Components.Logging;

namespace RLMapLoader.Components.Core
{
    /// <summary>
    /// Things that are allowed to modify State
    /// Also contains logger with tag autoset for convinece
    /// </summary>
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