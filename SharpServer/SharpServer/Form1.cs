using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using static System.Windows.Forms.LinkLabel;
using System.Collections.Generic;
using System.Security.Cryptography;
using static UdpServer.ServerForm;
using System.Security.Policy;

namespace UdpServer
{
    public partial class ServerForm : Form
    {
        private const int ServerPort = 12345;
        private UdpClient udpListener;
        private static int rotation = 0;

        private List<Lines> lines = new List<Lines>();
        private List<Pixels> pixels = new List<Pixels>();
        private List<Rectangles> rectangles = new List<Rectangles>();
        private List<FillRectangles> fillrectangles = new List<FillRectangles>();
        private List<Ellipses> ellipses = new List<Ellipses>();
        private List<FillEllipses> fillellipses = new List<FillEllipses>();
        private List<Sprite> sprites = new List<Sprite>();
        private List<Ticker> tickers = new List<Ticker>();
        public ServerForm()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        private void server_Load(object sender, EventArgs e)
        {
            udpListener = new UdpClient(ServerPort);
            udpListener.BeginReceive(ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, ServerPort);
                byte[] data = udpListener.EndReceive(ar, ref clientEndPoint);
                string message = Encoding.UTF8.GetString(data);
                Invoke(new Action(() =>
                {
                    string[] parts = message.Split('|');
                    if (parts.Length == 2)
                    {
                        string command = parts[0].Trim();
                        string parameters = parts[1].Trim();

                        switch (command)
                        {
                            case "clear display":
                                string colorName = parameters.Trim();
                                Color color = ParseColor(colorName);
                                ChangeBackgroundColor(color);
                                
                                lblCommand.Text = $"Команда: clear display";
                                lblParam.Text = $"Параметры: color={colorName}";
                                break;
                            case "draw line":
                                DrawLine(parameters);
                                break;
                            case "draw pixel":
                                DrawPixel(parameters);
                                break;
                            case "draw rectangle":
                                DrawRectangle(parameters);
                                break;
                            case "fill rectangle":
                                FillRectangle(parameters);
                                break;
                            case "draw ellipse":
                                DrawEllipse(parameters);
                                break;
                            case "fill ellipse":
                                FillEllipse(parameters);
                                break;
                            case "set orientation":
                                SetOrientation(parameters);
                                break;
                            case "get width":
                                SendCommandResult(GetFormWidth().ToString(), clientEndPoint);
                                break;
                            case "get height":
                                SendCommandResult(GetFormHeight().ToString(), clientEndPoint);
                                break;
                            case "load sprite":
                                LoadSprite(parameters);
                                break;
                            case "show sprite":
                                ShowSprite(parameters);
                                break;
                            case "ticker":
                                ShowTicker(parameters);
                                break;

                            default:
                                break;
                        }
       

                        lblCommand.Text = $"Команда: {command}";
                        lblParam.Text = $"Параметры: {parameters}";
                    }

                   

                }));
                udpListener.BeginReceive(ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при приеме данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowTicker(string parameters)
        {
            string[] paramParts = parameters.Split(' ');
            if (paramParts.Length == 6)
            {
                int x, y, fontSize, speed;
                if (int.TryParse(paramParts[0].Trim(), out x) &&
                    int.TryParse(paramParts[1].Trim(), out y) &&
                    int.TryParse(paramParts[3].Trim(), out fontSize) &&
                    int.TryParse(paramParts[5].Trim(), out speed))
                {
                    string text = paramParts[2].Trim();
                    string colorName = paramParts[4].Trim();
                    Color color = ParseColor(colorName);

                    Ticker ticker = new Ticker(x, y, text, fontSize, color, speed);
                    tickers.Add(ticker);

                    ShowTickerOnForm(ticker);

                    lblCommand.Text = $"Команда: ticker";
                    lblParam.Text = $"Параметры: x={x}, y={y}, text={text}, fontSize={fontSize}, color={colorName}, speed={speed}";
                }
                else
                {
                    MessageBox.Show("Ошибка разбора параметров команды 'ticker'", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void ShowTickerOnForm(Ticker ticker)
        {
            Graphics graphics = this.CreateGraphics();
            Font font = new Font("Arial", ticker.FontSize);
            Brush brush = new SolidBrush(ticker.Color);
            RectangleF rect = new RectangleF(ticker.X, ticker.Y, this.Width, this.Height);

            Timer tickerTimer = new Timer();
            tickerTimer.Interval = ticker.Speed;
            tickerTimer.Tick += (sender, e) =>
            {
                ticker.X += 1;

                graphics.Clear(BackColor);

                graphics.DrawString(ticker.Text, font, brush, (int)ticker.X, ticker.Y);

                if (ticker.X > this.Width)
                {
                    ticker.X = -(int)graphics.MeasureString(ticker.Text, font).Width;
                }
            };
            tickerTimer.Start();

        }




        private void SetOrientation(string parameters)
        {
            int orientation;
            if (int.TryParse(parameters, out orientation))
            {
                rotation = orientation * 90;
                OnPaint();
                lblCommand.Text = $"Команда: set orientation";
                lblParam.Text = $"Параметры: orientation={orientation}";
            }
            else
            {
                MessageBox.Show("Ошибка разбора параметров команды 'set orientation'", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private Color ParseColor(string colorStr)
        {
            if (colorStr.StartsWith("#") && (colorStr.Length == 7 || colorStr.Length == 4))
            {
                try
                {
                    int startIndex = colorStr.StartsWith("#") ? 1 : 0;
                    int r = int.Parse(colorStr.Substring(startIndex, 2), System.Globalization.NumberStyles.HexNumber);
                    int g = int.Parse(colorStr.Substring(startIndex + 2, 2), System.Globalization.NumberStyles.HexNumber);
                    int b = int.Parse(colorStr.Substring(startIndex + 4, 2), System.Globalization.NumberStyles.HexNumber);

                    return Color.FromArgb(r, g, b);
                }
                catch (Exception)
                {
                    return Color.White;
                }
            }

            if (Enum.TryParse(colorStr, out KnownColor knownColor))
            {
                return Color.FromKnownColor(knownColor);
            }
            return Color.White;
        }



        public class Ticker
        {
            public int X { get; set; }
            public int Y { get; set; }
            public string Text { get; set; }
            public int FontSize { get; set; }
            public Color Color { get; set; }
            public int Speed { get; set; }

            public Ticker(int x, int y, string text, int fontSize, Color color, int speed)
            {
                X = x;
                Y = y;
                Text = text;
                FontSize = fontSize;
                Color = color;
                Speed = speed;
            }
        }

        public class Sprite
        {
            public int Index { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public string FilePath { get; set; }
            public Image Image { get; set; }

            public Sprite(int index, int width, int height, string filePath)
            {
                Index = index;
                Width = width;
                Height = height;
                FilePath = filePath;
                Image = Image.FromFile(filePath);
            }
        }

        private int GetFormWidth()
        {
            return this.Width;
        }
        private int GetFormHeight()
        {
            return this.Height;
        }

        private void SendCommandResult(string result, IPEndPoint clientEndPoint)
        {
            byte[] data = Encoding.UTF8.GetBytes(result);
            udpListener.Send(data, data.Length, clientEndPoint);
        }


        public class Pixels
        {
            public int x0;
            public int y0;
            public Color colorName;
            public Pixels(int _x0, int _y0, Color _colorName)
            {
                this.x0 = _x0;
                this.y0 = _y0;
                this.colorName = _colorName;
            }
        }

        public class Lines
        {
            public int x0;
            public int y0;
            public int x1;
            public int y1;
            public Color colorName;

            public Lines(int _x0, int _y0, int _x1, int _y1, Color _colorName)
            {
                this.x0 = _x0;
                this.y0 = _y0;
                this.x1 = _x1;
                this.y1 = _y1;
                this.colorName = _colorName;
            }
        }

        public class Rectangles
        {
            public int x0;
            public int y0;
            public int w;
            public int h;
            public Color colorName;
            public Rectangles(int _x0, int _y0, int _w, int _h, Color _colorName)
            {
                this.x0 = _x0;
                this.y0 = _y0;
                this.w = _w;
                this.h = _h;
                this.colorName = _colorName;
            }
        }
        public class FillRectangles
        {
            public int x0;
            public int y0;
            public int w;
            public int h;
            public Color colorName;
            public FillRectangles(int _x0, int _y0, int _w, int _h, Color _colorName)
            {
                this.x0 = _x0;
                this.y0 = _y0;
                this.w = _w;
                this.h = _h;
                this.colorName = _colorName;
            }
        }

        public class Ellipses
        {
            public int x0;
            public int y0;
            public int radiusX; public int radiusY;
            public Color colorName;
            public bool isfilled;
            public Ellipses(int _x0, int _y0, int _radiusX, int _radiusY, Color _colorName)
            {
                this.x0 = _x0;
                this.y0 = _y0;
                this.radiusX = _radiusX;
                this.radiusY = _radiusY;
                this.colorName = _colorName;
            }
        }
        public class FillEllipses
        {
            public int x0;
            public int y0;
            public int radiusX; public int radiusY;
            public Color colorName;
            public bool isfilled;
            public FillEllipses(int _x0, int _y0, int _radiusX, int _radiusY, Color _colorName)
            {
                this.x0 = _x0;
                this.y0 = _y0;
                this.radiusX = _radiusX;
                this.radiusY = _radiusY;
                this.colorName = _colorName;
            }
        }



        private void DrawLine(string parameters)
        {
            string[] paramParts = parameters.Split(' ');
            if (paramParts.Length == 5)
            {
                int x0, y0, x1, y1;
                if (int.TryParse(paramParts[0].Trim(), out x0) &&
                    int.TryParse(paramParts[1].Trim(), out y0) &&
                    int.TryParse(paramParts[2].Trim(), out x1) &&
                    int.TryParse(paramParts[3].Trim(), out y1))
                {
                    string colorName = paramParts[4].Trim();
                    Color color = ParseColor(colorName);

                    lines.Add(new Lines(x0, y0, x1, y1, color));
                    OnPaint(); 

                }
            }
        }

        private void DrawPixel(string parameters)
        {
            string[] paramParts = parameters.Split(' ');
            if (paramParts.Length == 3)
            {
                int x0, y0;
                if (int.TryParse(paramParts[0], out x0) &&
                    int.TryParse(paramParts[1], out y0))
                {
                    string colorName = paramParts[2].Trim();
                    Color color = ParseColor(colorName);

                    pixels.Add(new Pixels(x0, y0, color));
                    OnPaint();
                }
            }
        }

        private void DrawRectangle(string parameters)
        {
            string[] paramParts = parameters.Split(' ');
            if (paramParts.Length == 5)
            {
                int x0, y0, w, h;
                if (int.TryParse(paramParts[0].Trim(), out x0) &&
                    int.TryParse(paramParts[1].Trim(), out y0) &&
                    int.TryParse(paramParts[2].Trim(), out w) &&
                    int.TryParse(paramParts[3].Trim(), out h))
                {
                    string colorName = paramParts[4].Trim();
                    Color color = ParseColor(colorName);

                    rectangles.Add(new Rectangles(x0, y0, w, h, color));
                    OnPaint();

                }
            }
        }
        private void FillRectangle(string parameters)
        {
            string[] paramParts = parameters.Split(' ');
            if (paramParts.Length == 5)
            {
                int x0, y0, w, h;
                if (int.TryParse(paramParts[0].Trim(), out x0) &&
                    int.TryParse(paramParts[1].Trim(), out y0) &&
                    int.TryParse(paramParts[2].Trim(), out w) &&
                    int.TryParse(paramParts[3].Trim(), out h))
                {
                    string colorName = paramParts[4].Trim();
                    Color color = ParseColor(colorName);

                    fillrectangles.Add(new FillRectangles(x0, y0, w, h, color));
                    OnPaint();
                }
            }
        }

        private void DrawEllipse(string parameters)
        {
            string[] paramParts = parameters.Split(' ');
            if (paramParts.Length == 5)
            {
                int x, y, radiusX, radiusY;
                if (int.TryParse(paramParts[0].Trim(), out x) &&
                    int.TryParse(paramParts[1].Trim(), out y) &&
                    int.TryParse(paramParts[2].Trim(), out radiusX) &&
                    int.TryParse(paramParts[3].Trim(), out radiusY))
                {
                    string colorName = paramParts[4].Trim();
                    Color color = ParseColor(colorName);

                    ellipses.Add(new Ellipses(x, y, radiusX, radiusY, color));
                    OnPaint();
                }
            }
        }
        private void FillEllipse(string parameters)
        {
            string[] paramParts = parameters.Split(' ');
            if (paramParts.Length == 5)
            {
                int x, y, radiusX, radiusY;
                if (int.TryParse(paramParts[0].Trim(), out x) &&
                    int.TryParse(paramParts[1].Trim(), out y) &&
                    int.TryParse(paramParts[2].Trim(), out radiusX) &&
                    int.TryParse(paramParts[3].Trim(), out radiusY))
                {
                    string colorName = paramParts[4].Trim();
                    Color color = ParseColor(colorName);

                    fillellipses.Add(new FillEllipses(x, y, radiusX, radiusY, color));
                    OnPaint();
                }
            }
        }


        private void LoadSprite(string parameters)
        {
            string[] paramParts = parameters.Split(' ');
            if (paramParts.Length == 4)
            {
                int index, width, height;
                if (int.TryParse(paramParts[0].Trim(), out index) &&
                    int.TryParse(paramParts[1].Trim(), out width) &&
                    int.TryParse(paramParts[2].Trim(), out height))
                {
                    string filePath = paramParts[3].Trim();
                    Sprite sprite = new Sprite(index, width, height, filePath);
                    sprites.Add(sprite);

                    lblCommand.Text = $"Команда: load_sprite";
                    lblParam.Text = $"Параметры: index={index}, width={width}, height={height}, filePath={filePath}";
                }
            }
        }

        private void ShowSprite(string parameters)
        {
            string[] paramParts = parameters.Split(' ');
            if (paramParts.Length == 3)
            {
                int index, x, y;
                if (int.TryParse(paramParts[0].Trim(), out index) &&
                    int.TryParse(paramParts[1].Trim(), out x) &&
                    int.TryParse(paramParts[2].Trim(), out y))
                {
                    Sprite sprite = sprites.Find(s => s.Index == index);
                    if (sprite != null)
                    {
                        Graphics graphics = this.CreateGraphics();
                        graphics.DrawImage(sprite.Image, x, y, sprite.Width, sprite.Height);

                        lblCommand.Text = $"Команда: show_sprite";
                        lblParam.Text = $"Параметры: index={index}, x={x}, y={y}";
                    }
                }
            }
        }




        private void OnPaint()
        {
            Graphics graphics = this.CreateGraphics();
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.TranslateTransform(this.Width / 2, this.Height / 2);
            graphics.RotateTransform(rotation);
            graphics.TranslateTransform(-this.Width / 2, -this.Height / 2);
            graphics.Clear(BackColor);

            foreach (Lines line in lines)
            {
                using (Pen pen = new Pen(line.colorName))
                {
                    graphics.DrawLine(pen, line.x0, line.y0, line.x1, line.y1);
                }
            }
            foreach (var pixel in pixels.ToArray())
            {
                using (SolidBrush brush = new SolidBrush(pixel.colorName))
                {
                    graphics.FillEllipse(brush, pixel.x0, pixel.y0, 1, 1); 
                }
            }
            foreach (Rectangles rectangle in rectangles)
            {
                using (Pen pen = new Pen(rectangle.colorName))
                {
                    graphics.DrawRectangle(pen, rectangle.x0, rectangle.y0, rectangle.h, rectangle.w); 
                }
            }
            foreach (FillRectangles fillRectangle in fillrectangles)
            {
                using (SolidBrush brush = new SolidBrush(fillRectangle.colorName))
                {
                    graphics.FillRectangle(brush, fillRectangle.x0, fillRectangle.y0, fillRectangle.h, fillRectangle.w); 
                }
            }
            foreach (Ellipses ellipse in ellipses)
            {
                using (Pen pen = new Pen(ellipse.colorName))
                {
                    graphics.DrawEllipse(pen, ellipse.x0, ellipse.y0, ellipse.radiusX, ellipse.radiusY);
                }
            }
            foreach (FillEllipses fillellipse in fillellipses)
            {
                using (SolidBrush brush = new SolidBrush(fillellipse.colorName))
                {
                    graphics.FillEllipse(brush, fillellipse.x0, fillellipse.y0, fillellipse.radiusX, fillellipse.radiusY);
                }
            }

        }





        private void ChangeBackgroundColor(Color color)
        {
            this.BackColor = color;
        }
    }
}
