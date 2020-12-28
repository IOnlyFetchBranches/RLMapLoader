using System.Collections.Generic;

namespace RLMapLoader.Components.Interfaces
{
    public interface IInstallStrategy
    {
        public (bool ok, string msg) Execute(Dictionary<string, string> installData);
    }
}