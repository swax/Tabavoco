using Microsoft.UI.Xaml.Controls.Primitives;

namespace TaBaVoCo.Views
{
    /// <summary>
    /// Volume control page for TaBaVoCo taskbar application.
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void OnVolumeChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var slider = sender as Slider;
            if (slider != null && VolumeLabel != null)
            {
                VolumeLabel.Text = $"{(int)slider.Value}%";
            }
        }
    }
}
