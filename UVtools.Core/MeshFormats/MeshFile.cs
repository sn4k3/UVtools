using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace UVtools.Core.MeshFormats
{
    public abstract class MeshFile
    {
        public string FilePath;
        public MeshFile(string filePath)
        {
            FilePath = filePath;
        }
        public abstract void Create();
        public abstract void WriteTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal);
        public abstract void Close();
    }
}
