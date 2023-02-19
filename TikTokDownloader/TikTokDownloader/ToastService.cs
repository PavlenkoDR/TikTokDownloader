using System.Threading.Tasks;

namespace TikTokDownloader
{
    public enum ToastLength
    {
        Short = 0,
        Long = 1
    }
    public interface IToastService
    {
        void MakeText(string text, ToastLength length = ToastLength.Short);
    }
}
