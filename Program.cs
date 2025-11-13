using System;
using System.Collections.Generic;
using System.Drawing; // Основний простір імен для графіки
using System.Drawing.Drawing2D; // Для згладжування ліній
using System.Windows.Forms;
using System.Linq;

namespace GraphDrawingLab
{
    // Головний клас форми
    public class GraphForm : Form
    {
        // Структура для зберігання точок графіка
        private struct GraphPoint
        {
            public double T { get; set; } // Координата X (час t)
            public double Y { get; set; } // Координата Y (значення функції)
        }

        private List<GraphPoint> _points;
        private const double T_START = 2.3;
        private const double T_END = 7.2;
        private const double T_STEP = 0.8;

        public GraphForm()
        {
            // Налаштування вікна
            this.Text = "Лабораторна: Графік System.Drawing";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(400, 300);
            
            // Вмикаємо подвійну буферизацію, щоб графік не мерехтів при ресайзі
            this.DoubleBuffered = true;
            this.ResizeRedraw = true; // Автоматично викликає перерисовку при зміні розміру

            CalculateData();
        }

        /// <summary>
        /// Обчислення таблиці значень функції
        /// </summary>
        private void CalculateData()
        {
            _points = new List<GraphPoint>();

            // Цикл згідно з завданням: від 2.3 до 7.2 з кроком 0.8
            for (double t = T_START; t <= T_END; t += T_STEP)
            {
                // Формула: y = (cos^3(t^2)) / (1.5t + 2)
                double numerator = Math.Pow(Math.Cos(t * t), 3);
                double denominator = (1.5 * t) + 2.0;
                
                double y = numerator / denominator;

                _points.Add(new GraphPoint { T = t, Y = y });
            }
        }

        /// <summary>
        /// Основний метод для малювання (перевизначення методу базового класу)
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Отримуємо об'єкт графіки
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Увімкнути згладжування

            // Якщо точок немає або їх замало - не малюємо
            if (_points == null || _points.Count < 2) return;

            // 1. Визначаємо межі (Min/Max) для масштабування
            double minT = _points.Min(p => p.T);
            double maxT = _points.Max(p => p.T);
            double minY = _points.Min(p => p.Y);
            double maxY = _points.Max(p => p.Y);

            // Додамо невеликі відступи, щоб графік не прилипав до країв форми
            float padding = 50f;
            float drawWidth = this.ClientSize.Width - 2 * padding;
            float drawHeight = this.ClientSize.Height - 2 * padding;

            // 2. Функції перетворення координат (Математика -> Пікселі)
            // X: (t - minT) / (maxT - minT) * ширина
            // Y: (y - minY) / (maxY - minY) * висота. 
            // УВАГА: У комп'ютерній графіці вісь Y йде ВНИЗ. Тому Y треба інвертувати.

            float GetScreenX(double t)
            {
                return padding + (float)((t - minT) / (maxT - minT) * drawWidth);
            }

            float GetScreenY(double y)
            {
                // Інвертуємо Y: віднімаємо нормоване значення від нижньої межі області малювання
                return (this.ClientSize.Height - padding) - (float)((y - minY) / (maxY - minY) * drawHeight);
            }

            // 3. Малювання осей (спрощено, просто рамка області графіка)
            using (Pen axisPen = new Pen(Color.Gray, 1) { DashStyle = DashStyle.Dash })
            {
                g.DrawRectangle(axisPen, padding, padding, drawWidth, drawHeight);
            }

            // 4. Малювання самого графіка та точок
            using (Pen graphPen = new Pen(Color.Blue, 2))
            using (Brush pointBrush = new SolidBrush(Color.Red))
            using (Font font = new Font("Arial", 8))
            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                for (int i = 0; i < _points.Count - 1; i++)
                {
                    // Поточна точка
                    float x1 = GetScreenX(_points[i].T);
                    float y1 = GetScreenY(_points[i].Y);

                    // Наступна точка
                    float x2 = GetScreenX(_points[i+1].T);
                    float y2 = GetScreenY(_points[i+1].Y);

                    // Малюємо лінію між точками
                    g.DrawLine(graphPen, x1, y1, x2, y2);

                    // Малюємо кружечок на точці (щоб було видно крок 0.8)
                    float r = 3; // радіус точки
                    g.FillEllipse(pointBrush, x1 - r, y1 - r, 2 * r, 2 * r);
                    
                    // Підписуємо координати (опціонально)
                    string label = $"({_points[i].T:F1}; {_points[i].Y:F3})";
                    g.DrawString(label, font, textBrush, x1, y1 - 20);
                }

                // Малюємо останню точку окремо (бо цикл йде до передостанньої)
                var lastPoint = _points.Last();
                float xL = GetScreenX(lastPoint.T);
                float yL = GetScreenY(lastPoint.Y);
                g.FillEllipse(pointBrush, xL - 3, yL - 3, 6, 6);
                g.DrawString($"({lastPoint.T:F1}; {lastPoint.Y:F3})", font, textBrush, xL, yL - 20);
            }
            
            // Заголовок
            g.DrawString("Графік функції y = (cos^3(t^2)) / (1.5t + 2)", 
                         new Font("Arial", 14, FontStyle.Bold), 
                         Brushes.DarkSlateGray, 
                         padding, 10);
        }

        // Точка входу в програму
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GraphForm());
        }
    }
}
