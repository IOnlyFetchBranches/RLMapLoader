namespace RLMapLoader.Components.Models
{
    public class MapLoaderState
    {
        public bool IsFirstTime { get; set; } = true;
        public bool IsMapLoaded { get; set; } = false;
        public string LoadedMapName { get; set; } = null;
        public string LastKnownMapName { get; set; } = null;

        public bool IsLoggedOn { get; set; } = false;
        public string UserId { get; set; }
        public string PrivateUserEmail { get; set; }
    }
}