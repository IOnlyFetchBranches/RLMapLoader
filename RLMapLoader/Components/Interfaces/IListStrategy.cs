using System.Collections.Generic;

namespace RLMapLoader.Components.Interfaces
{
    public interface IListStrategy
    {
        public (List<MapInfo> infos, string msg) Execute();
    }
}