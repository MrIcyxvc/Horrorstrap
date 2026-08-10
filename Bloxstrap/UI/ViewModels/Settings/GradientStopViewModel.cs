using System.ComponentModel;
using System.Windows.Media;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class GradientStopViewModel : INotifyPropertyChanged
    {
        private double _offset;
        private Color _color;

        public double Offset
        {
            get => _offset;
            set
            {
                _offset = Math.Clamp(value, 0, 1);
                OnPropertyChanged(nameof(Offset));
                OnPropertyChanged(nameof(HexColor));
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged(nameof(Color));
                OnPropertyChanged(nameof(HexColor));
            }
        }

        public string HexColor
        {
            get => $"#{Color.A:X2}{Color.R:X2}{Color.G:X2}{Color.B:X2}";
            set
            {
                try
                {
                    Color = (Color)ColorConverter.ConvertFromString(value);
                }
                catch
                {
                    // ignore invalid input until valid
                }
            }
        }

        public GradientStopViewModel(double offset, Color color)
        {
            _offset = offset;
            _color = color;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
