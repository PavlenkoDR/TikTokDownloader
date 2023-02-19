using System.Threading.Tasks;

namespace TikTokDownloader
{
    public interface IFileService
    {
        Task<string> Save(byte[] data, string name, bool isSaveToDownloads);
        void ShareMediaFile(string[] filesPath, string intentType);
    }
}
