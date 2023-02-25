using System.Threading.Tasks;

namespace TikTokDownloader
{
    public interface IFileService
    {
        Task<string> Save(byte[] data, string name, bool isSaveToDownloads);
        void ShareMediaFile(string title, string[] filesPath, string intentType);
        Task<string> getGalleryPath();
        Task<string> getDownloadsPath();
        Task<bool> CheckPermissions();
        void OpenAppSettings();
    }
}
