using System;
using System.IO;

namespace PrusaSL1Reader
{
    public abstract class FileFormat : IFileFormat
    {
        public string FileFullPath { get; protected set; }
        public abstract string FileExtension { get; }
        public abstract string FileExtensionName { get; }
        public abstract void Load(string fileFullPath);
        public abstract float GetHeightFromLayer(uint layerNum);

        public void FileValidation(string fileFullPath)
        {
            if (ReferenceEquals(fileFullPath, null)) throw new ArgumentNullException(nameof(FileFullPath), "fullFilePath can't be null.");
            if (!File.Exists(fileFullPath)) throw new FileNotFoundException("The specified file does not exists.", fileFullPath);
            if (!Path.GetExtension(fileFullPath).Equals($".{FileExtension}")) throw new FileLoadException($"The specified file is not valid, can only open {FileExtension} files.", fileFullPath);
        }
    }
}
