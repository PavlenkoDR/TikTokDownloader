using System;

namespace TikTokDownloader
{
    public interface IClipBoardService
    {
        void Set(string text);
        string Get();
        void AddPrimaryClipChanged(EventHandler handler);
    }
}
