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
        Task<string> Save(byte[] data, string name, ContentType contentType);
        void ShareMediaFile(string title, string[] filesPath, string intentType);
        string getGalleryPath();
        string getMusicPath();
        Task<bool> CheckPermissions();
        bool OpenAppSettings();
        void PlayVideoFromLocalStorage(string filePath);
    }
}
