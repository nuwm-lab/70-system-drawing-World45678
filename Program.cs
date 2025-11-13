using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace GraphDrawingLab
{
    // --- 1. MODEL (Дані та Математика) ---
    // Цей клас нічого не знає про форму чи Graphics. Тільки чиста логіка.
    public class GraphModel
    {
        public struct DataPoint
        {
            public double T { get; }
            public double Y { get; }
            public DataPoint(double t, double y) { T = t; Y = y; }
        }

        public List<DataPoint> Points { get; private set; }
        
        // Використовуємо PascalCase для констант (Code Convention)
        private const double TStart = 2.3;
        private const double TEnd = 7.2;
        private const double TStep = 0.8;

        public GraphModel()
        {
            CalculateData();
        }

        private void CalculateData()
        {
            Points = new List<DataPoint>();
            for (double t = TStart; t <= TEnd; t += TStep)
            {
                // y = (cos^3(t^2)) / (1.5t + 2)
                double numerator = Math.Pow(Math.Cos(t * t), 3);
                double denominator = (1.5 * t) + 2.0;

                // Захист від ділення на нуль у самій формулі (хоча за умовою t >= 2.3, це не станеться)
                if (Math.Abs(denominator) < 1e-9) denominator = 1e-9;

                double y = numerator / denominator;
                Points.Add(new DataPoint(t, y));
            }
        }

        public (double MinT, double MaxT, double MinY, double MaxY) GetBounds()
        {
            if (Points == null || Points.Count == 0) return (0, 0, 0, 0);
            return (Points.Min(p => p.T), Points.Max(p => p.T), 
                    Points.Min(p => p.Y), Points.Max(p => p.Y));
        }
    }

    // --- 2. RENDERER (Логіка малювання) ---
    // Відповідає за перетворення координат і малювання. Реалізує IDisposable для очищення GDI ресурсів.
    public class GraphRenderer : IDisposable
    {
        private readonly Font _mainFont;
        private readonly Pen _axisPen;
        private readonly Pen _graphPen;
        private readonly Brush _pointBrush;
        private readonly Brush _textBrush;

        public bool ShowLabels { get; set; } = true;
        public bool IsScatterPlot { get; set; } = false; // Line vs Scatter

        public GraphRenderer()
        {
            // Ініціалізація ресурсів ОДИН РАЗ
            _mainFont = new Font("Segoe UI", 8);
            _axisPen = new Pen(Color.Gray, 1) { DashStyle = DashStyle.Dash };
            _graphPen = new Pen(Color.RoyalBlue, 2);
            _pointBrush = new SolidBrush(Color.Crimson);
            _textBrush = new SolidBrush(Color.Black);
        }

        public void Draw(Graphics g, RectangleF drawArea, GraphModel model)
        {
            if (model.Points.Count < 2) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Отримуємо межі даних
            var bounds = model.GetBounds();
            double rangeT = bounds.MaxT - bounds.MinT;
            double rangeY = bounds.MaxY - bounds.MinY;

            // ЗАХИСТ: Перевірка на нульовий діапазон (ділення на нуль)
            if (Math.Abs(rangeT) < 1e-9) rangeT = 1.0; 
            if (Math.Abs(rangeY) < 1e-9) rangeY = 1.0;

            // 2. Функції конвертації (World -> Screen)
            float GetScreenX(double t) =>
                drawArea.Left + (float)((t - bounds.MinT) / rangeT * drawArea.Width);

            float GetScreenY(double y) =>
                drawArea.Bottom - (float)((y - bounds.MinY) / rangeY * drawArea.Height);

            // 3. Малюємо рамку
            g.DrawRectangle(_axisPen, drawArea.X, drawArea.Y, drawArea.Width, drawArea.Height);

            // 4. Підготовка точок для швидкого малювання
            PointF[] screenPoints = model.Points
                .Select(p => new PointF(GetScreenX(p.T), GetScreenY(p.Y)))
                .ToArray();

            // 5. Малювання графіка
            if (!IsScatterPlot)
            {
                // ОПТИМІЗАЦІЯ: DrawLines набагато швидше за цикл DrawLine
                g.DrawLines(_graphPen, screenPoints);
            }

            // 6. Малювання точок та підписів (якщо потрібно)
            float pointRadius = IsScatterPlot ? 4 : 2.5f;

            for (int i = 0; i < screenPoints.Length; i++)
            {
                float x = screenPoints[i].X;
                float y = screenPoints[i].Y;

                // Малюємо точку
                g.FillEllipse(_pointBrush, x - pointRadius, y - pointRadius, pointRadius * 2, pointRadius * 2);

                // Малюємо текст (опціонально, щоб не засмічувати графік)
                if (ShowLabels)
                {
                    // Пропускаємо деякі підписи, якщо точок дуже багато, або малюємо всі, якщо їх мало (як у завданні)
                    string label = $"({model.Points[i].T:F1}; {model.Points[i].Y:F3})";
                    // Зсув тексту, щоб не перекривав точку
                    g.DrawString(label, _mainFont, _textBrush, x + 3, y - 15);
                }
            }
            
            // Заголовок
            g.DrawString("y = cos^3(t^2) / (1.5t + 2)", new Font(_mainFont.FontFamily, 12, FontStyle.Bold), Brushes.DarkSlateGray, drawArea.Left, drawArea.Top - 25);
        }

        public void Dispose()
        {
            _mainFont?.Dispose();
            _axisPen?.Dispose();
            _graphPen?.Dispose();
            _pointBrush?.Dispose();
            _textBrush?.Dispose();
        }
    }

    // --- 3. FORM (UI & Controller) ---
    public class MainForm : Form
    {
        private readonly GraphModel _model;
        private readonly GraphRenderer _renderer;
        
        // UI Controls
        private Panel _controlsPanel;
        private CheckBox _chkShowLabels;
        private RadioButton _rbLine;
        private RadioButton _rbScatter;

        public MainForm()
        {
            // Налаштування форми
            this.Text = "Лабораторна: System.Drawing (Refactored)";
            this.Size = new Size(900, 600);
            this.MinimumSize = new Size(500, 400);
            
            // Уникнення миготіння
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;

            // Ініціалізація компонентів
            _model = new GraphModel();
            _renderer = new GraphRenderer();

            InitializeCustomControls();
        }

        private void InitializeCustomControls()
        {
            _controlsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(10),
                BackColor = Color.WhiteSmoke
            };

            _rbLine = new RadioButton { Text = "Лінійний графік", Checked = true, AutoSize = true, Location = new Point(10, 10) };
            _rbScatter = new RadioButton { Text = "Точковий (Scatter)", AutoSize = true, Location = new Point(130, 10) };
            _chkShowLabels = new CheckBox { Text = "Показувати координати", Checked = true, AutoSize = true, Location = new Point(270, 10) };

            // Підписка на події
            _rbLine.CheckedChanged += (s, e) => { _renderer.IsScatterPlot = !_rbLine.Checked; this.Invalidate(); };
            _chkShowLabels.CheckedChanged += (s, e) => { _renderer.ShowLabels = _chkShowLabels.Checked; this.Invalidate(); };

            _controlsPanel.Controls.Add(_rbLine);
            _controlsPanel.Controls.Add(_rbScatter);
            _controlsPanel.Controls.Add(_chkShowLabels);
            this.Controls.Add(_controlsPanel);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Відступ для області малювання (враховуючи панель керування)
            float padding = 40f;
            RectangleF drawRect = new RectangleF(
                padding, 
                _controlsPanel.Height + padding, // Зсув вниз через панель
                this.ClientSize.Width - 2 * padding, 
                this.ClientSize.Height - _controlsPanel.Height - 2 * padding
            );

            // Перевірка на коректність розмірів вікна (захист від мінусової ширини/висоти)
            if (drawRect.Width <= 0 || drawRect.Height <= 0) return;

            // Делегуємо малювання рендереру
            _renderer.Draw(e.Graphics, drawRect, _model);
        }

        // Важливо: очищення ресурсів при закритті форми
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _renderer.Dispose();
            base.OnFormClosing(e);
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
