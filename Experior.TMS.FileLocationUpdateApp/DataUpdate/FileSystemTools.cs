using System.IO;
using NLog;

namespace Experior.TMS.FileLocationUpdateApp.DataUpdate
{
    public class FileSystemTools
    {
        private readonly Logger _logger;

        public FileSystemTools()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        
        
        public bool TryMoveFile(string sourcePath, string destinationPath, string destinationCopyPath)
        {
            
            if (!File.Exists(sourcePath))
            {
                _logger.Warn("File not found: {0}", sourcePath);
                return false;
            }
            
            try
            {
                var directoryName = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);
                File.Move(sourcePath, destinationPath);
                _logger.Info("File moved successfully from {0} to {1}", sourcePath, destinationPath);
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "Error moving file {0} to {1}", sourcePath, destinationPath);
                return false;
            }

            try
            {
                var directoryName = Path.GetDirectoryName(destinationCopyPath);
                if (!Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);
                File.Copy(destinationPath, destinationCopyPath, true);
                _logger.Info("File copied successfully from {0} to {1}", destinationPath, destinationCopyPath);
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "Error copying file {0} to {1}", destinationPath, destinationCopyPath);
                return false;
            }
            return true;
        }
    }
}