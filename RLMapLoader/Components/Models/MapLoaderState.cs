namespace RLMapLoader.Components.Models
{
    public class MapLoaderState
    {
        public bool IsFirstTime { get; set; } = true;
        public bool IsMapLoaded { get; set; } = false;
        public string LoadedMapName { get; set; }

    }
}