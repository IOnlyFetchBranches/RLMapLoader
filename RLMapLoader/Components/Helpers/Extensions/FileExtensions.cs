using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RLMapLoader.Components.Helpers.Extensions
{
    /// <summary>
    /// Not to be confused with .txt :)
    /// </summary>
    static class FileExtensions
    {
        public static byte[] GetAllBytes(this FileStream file) => File.ReadAllBytes(file.Name);
    }
}
