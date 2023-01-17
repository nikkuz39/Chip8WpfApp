namespace Chip8WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer dispatcherTimerChip8Tick60Hz = new DispatcherTimer();

        private Chip8 chip8 = new Chip8();

        private Dictionary<Key, byte> keyMapping = new Dictionary<Key, byte>
        {
            { Key.D1, 0x0 },
            { Key.D2, 0x1 },
            { Key.D3, 0x2 },
            { Key.D4, 0x3 },
            { Key.Q, 0x4 },
            { Key.W, 0x5 },
            { Key.E, 0x6 },
            { Key.R, 0x7 },
            { Key.A, 0x8 },
            { Key.S, 0x9 },
            { Key.D, 0xA },
            { Key.F, 0xB },
            { Key.Z, 0xC },
            { Key.X, 0xD },
            { Key.C, 0xE },
            { Key.V, 0xF }
        };

        public MainWindow()
        {
            InitializeComponent();

            GetGameCatalog();

            KeyDown += SetKeyDown;
            KeyUp += SetKeyUp;

            dispatcherTimerChip8Tick60Hz.Interval = new TimeSpan(0, 0, 0, 0, 16);
            dispatcherTimerChip8Tick60Hz.Tick += Chip8Tick60Hz;
        }

        private void SetKeyDown(object sender, KeyEventArgs e)
        {
            if (keyMapping.TryGetValue(e.Key, out byte value))
                chip8.KeyDown(value);
        }

        private void SetKeyUp(object sender, KeyEventArgs e)
        {
            if (keyMapping.TryGetValue(e.Key, out byte value))
                chip8.KeyUp(value);
        }

        private void StartGame(string pathGame)
        {
            chip8.ResetIntepreter();

            chip8.LoadFonts();
            chip8.LoadROM(File.ReadAllBytes(pathGame));

            dispatcherTimerChip8Tick60Hz.Start();
        }

        private void Chip8Tick60Hz(object sender, object e)
        {
            byte loop = 0;

            while (loop < 16)
            {
                chip8.EmulateCycle();

                if (chip8.drawFrame)
                    DisplayImg.Source = ConvertBitmapToBitmapImage(DrawFrame(chip8.graphics));

                loop++;
            }
            chip8.UpdateTimers();
        }

        private unsafe Bitmap DrawFrame(byte[] graphics)
        {
            Bitmap bitmap = new Bitmap(0x40, 0x20);

            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            byte* pointer = (byte*)bitmapData.Scan0;

            for (ushort y = 0; y < bitmap.Height; y++)
            {
                for (ushort x = 0; x < bitmap.Width; x++)
                {
                    // ARGB = bytes [B,G,R,A]
                    pointer[0] = graphics[y * 0x40 + x] > 0 ? (byte)0XFF : (byte)0; // B
                    pointer[1] = graphics[y * 0x40 + x] > 0 ? (byte)0XFF : (byte)0; // G
                    pointer[2] = graphics[y * 0x40 + x] > 0 ? (byte)0XFF : (byte)0; // R
                    pointer[3] = graphics[y * 0x40 + x] > 0 ? (byte)0XFF : (byte)0; // A                          

                    pointer += 4;
                }
            }

            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }

        private BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            MemoryStream memoryStream = new MemoryStream();

            bitmap.Save(memoryStream, ImageFormat.Bmp);

            memoryStream.Position = 0;

            BitmapImage bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            return bitmapImage;
        }

        private void GetGameCatalog()
        {
            string pathGames = "Games/";

            DirectoryInfo directoryInfo = new DirectoryInfo(pathGames);

            if (directoryInfo.Exists)
            {
                FileInfo[] files = directoryInfo.GetFiles();

                foreach (FileInfo file in files)
                {
                    gamesListBox.Items.Add(file.Name);
                }
            }
        }

        private void SelectedGame(object sender, RoutedEventArgs e)
        {
            if (gamesListBox.SelectedItem != null)
            {
                string pathGame = "Games/" + gamesListBox.SelectedItem.ToString();

                StartGame(pathGame);
            }
        }
    }
}