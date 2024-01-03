using System.Threading.Tasks;

namespace TikTokDownloader
{
    public enum ContentType
    {
        VIDEO,
        IMAGE,
        MUSIC
    }
    public interface IFileService
    {
        Task<string> Save(byte[] data, string name, bool isSaveToDownloads, ContentType contentType);
        void ShareMediaFile(string title, string[] filesPath, string intentType);
        Task<string> getGalleryPath();
        Task<string> getDownloadsPath();
        Task<string> getMusicPath();
        Task<bool> CheckPermissions();
        bool OpenAppSettings();
    }
}
