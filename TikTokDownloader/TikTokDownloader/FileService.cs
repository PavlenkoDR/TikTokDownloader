using System.Threading.Tasks;

namespace TikTokDownloader
{
    public interface IFileService
    {
        Task Save(byte[] data, string name, bool isSaveToDownloads);
    }
}
