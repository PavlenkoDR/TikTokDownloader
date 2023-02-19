using System;

namespace TikTokDownloader
{
    public interface IClipBoardService
    {
        event EventHandler PrimaryClipChanged;
        void Set(string text);
        string Get();
        void AddPrimaryClipChanged(EventHandler handler);
    }
}
