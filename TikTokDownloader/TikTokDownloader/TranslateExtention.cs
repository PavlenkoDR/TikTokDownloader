using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static System.Net.Mime.MediaTypeNames;

namespace TikTokDownloader
{
    [ContentProperty("TextID")]
    public class TranslateExtension : IMarkupExtension<BindingBase>, INotifyPropertyChanged
    {
        const string ResourceId = "TikTokDownloader.Resource";

        public TranslateExtension()
        {
        }

        public string TextID { get; set; } = "Instance";

        public static TranslateExtension Instance { get; } = new TranslateExtension();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Invalidate()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return (this as IMarkupExtension<BindingBase>).ProvideValue(serviceProvider);
        }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding
            {
                Mode = BindingMode.OneWay,
                Source = Instance,
                Path = $"[{TextID}]"
            };
        }
        public string this[string textId]
        {
            get
            {
                return GetText(textId);
            }
        }

        public static string GetText(string textID)
        {
            if (textID == null)
            {
                return "";
            }

            ResourceManager resmgr = new ResourceManager(ResourceId,
                        typeof(TranslateExtension).GetTypeInfo().Assembly);
            var text = resmgr.GetString(textID, CultureInfo.CurrentCulture) ?? textID;
            return text;
        }
    }
}
