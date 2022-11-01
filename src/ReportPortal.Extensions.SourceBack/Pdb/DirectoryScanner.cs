using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReportPortal.Extensions.SourceBack.Pdb
{
    class DirectoryScanner
    {
        public static IEnumerable<string> FindPdbPaths(string path)
        {
            var dir = new DirectoryInfo(path);

            if (!dir.Exists) throw new ArgumentException($"Directory not found by '{path}' path.");

            var dllFiles = dir.GetFiles("*.dll");

            return dllFiles.Select(f => f.FullName);
        }
    }
}
