namespace Bloxstrap.Models.Persistable
{
    public class CustomGradientStop
    {
        public double Offset { get; set; }
        public byte A { get; set; } = 255;
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public CustomGradientStop() { }

        public CustomGradientStop(double offset, System.Windows.Media.Color color)
        {
            Offset = offset;
            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;
        }

        public System.Windows.Media.Color ToColor() => System.Windows.Media.Color.FromArgb(A, R, G, B);
    }
}
