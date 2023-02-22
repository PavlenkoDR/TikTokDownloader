using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TikTokDownloader
{
    public class TabData
    {
        public int Position { get; set; }
        public string Text { get; set; }
        public Color TextColor { get; set; }
    }
    public class OnSwitchArgs
    {
        public int SelectedIndex { get; set; }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TabbedSwitch : ContentView
    {
        public new Color BackgroundColor
        {
            get => _BackgroundColor;
            set
            {
                _BackgroundColor = value;
                OnPropertyChanged("BackgroundColor");
            }
        }
        public Color _BackgroundColor = Color.FromHex("#eaeaea");
        public Color ThumbColor
        {
            get => _ThumbColor;
            set
            {
                _ThumbColor = value;
                OnPropertyChanged("ThumbColor");
            }
        }
        public Color _ThumbColor = Color.LightGray;
        public CornerRadius CornerRadius {
            get => _CornerRadius;
            set {
                _CornerRadius = value;
                OnPropertyChanged("CornerRadius");
            }
        }
        private CornerRadius _CornerRadius = 15;
        public List<TabData> Selectors
        {
            get => _Selectors;
            set
            {
                _Selectors = value;
                OnPropertyChanged("Selectors");
            }
        }
        public List<TabData> _Selectors = new List<TabData>();

        public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
            "SelectedIndex",
            typeof(int),
            typeof(TabbedSwitch),
            defaultBindingMode: BindingMode.OneWayToSource
        );

        public int SelectedIndex
        {
            get
            {
                return int.Parse(GetValue(SelectedIndexProperty).ToString());
            }
            set
            {
                SetValue(SelectedIndexProperty, value);
                OnPropertyChanged("SelectedIndex");
            }
        }
        public delegate void OnSwitchHandler(object sender, OnSwitchArgs e);
        public event OnSwitchHandler OnSwitch;
        public TabbedSwitch()
        {
            InitializeComponent();
        }
        private void SetSelectedBackgroundTransform()
        {
            if (SelectedIndex < selectorGrid.Children.Count())
            {
                selectedBackground.WidthRequest = (selectorGrid.Children[SelectedIndex] as Grid).Width;
                selectedBackground.TranslationX = (selectorGrid.Children[SelectedIndex] as Grid).X;
            }
            else if (selectorGrid.Children.Count > 0)
            {
                SelectedIndex = 0;
                selectedBackground.WidthRequest = selectorGrid.Children[0].Width;
            }
            else
            {
                selectedBackground.WidthRequest = selectorGrid.Width;
            }
        }
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            SetSelectedBackgroundTransform();
        }
        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            selectedBackground.WidthRequest = (sender as Grid).Width;
            selectedBackground.TranslateTo((sender as Grid).X, 0.0);
            SelectedIndex = selectorGrid.Children.IndexOf(sender as Grid);
            OnSwitch?.Invoke(this, new OnSwitchArgs() { SelectedIndex = SelectedIndex });
        }
    }
}