using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Paint
{
    public class Form1 : Form
    {
        // Перечисление для инструментов
        private enum ToolType
        {
            Brush,
            Circle,
            Fill,
            Polygon,
            Text
        }

        // Основные переменные
        private Bitmap canvas;
        private Graphics graphics;
        private Point? lastPoint = null;
        private ToolType currentTool = ToolType.Brush;
        private Color currentColor = Color.Black;
        private int brushSize = 5;
        private bool isDrawing = false;
        private List<Point> polygonPoints = new List<Point>();
        private bool isPolygonComplete = false;
        private Font currentFont = new Font("Arial", 12);

        // Компоненты интерфейса
        private Panel toolPanel;
        private PictureBox pictureBox;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private NumericUpDown sizeNumeric;

        public Form1()
        {
            // НАСТРОЙКА ФОРМЫ
            this.Text = "Paint";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.LightGray;

            // ПАНЕЛЬ ИНСТРУМЕНТОВ
            toolPanel = new Panel();
            toolPanel.Dock = DockStyle.Top;
            toolPanel.Height = 100;
            toolPanel.BackColor = Color.FromArgb(45, 45, 48);

            // ХОЛСТ
            pictureBox = new PictureBox();
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.BackColor = Color.White;
            pictureBox.Cursor = Cursors.Cross;

            // СТАТУС БАР
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Готов к работе");
            statusStrip.Items.Add(statusLabel);

            // ДОБАВЛЯЕМ КНОПКИ
            string[] toolNames = { "Кисть", "Окружность", "Заливка", "Многоугольник", "Шрифты", "Очистить всё" };

            int xPos = 10;
            for (int i = 0; i < toolNames.Length; i++)
            {
                Button btn = new Button();
                btn.Text = toolNames[i];
                btn.Location = new Point(xPos, 10);
                btn.Size = new Size(100, 40);
                btn.BackColor = Color.FromArgb(63, 63, 70);
                btn.ForeColor = Color.White;
                btn.FlatStyle = FlatStyle.Flat;
                btn.Tag = i;

                if (toolNames[i] == "Очистить всё")
                    btn.Click += ClearButton_Click;
                else
                    btn.Click += ToolButton_Click;

                toolPanel.Controls.Add(btn);
                xPos += 110;
            }

            // КНОПКА ЦВЕТА
            Button colorBtn = new Button();
            colorBtn.Text = "Выбрать цвет";
            colorBtn.Location = new Point(xPos, 10);
            colorBtn.Size = new Size(100, 40);
            colorBtn.BackColor = Color.FromArgb(63, 63, 70);
            colorBtn.ForeColor = Color.White;
            colorBtn.FlatStyle = FlatStyle.Flat;
            colorBtn.Click += ColorButton_Click;
            toolPanel.Controls.Add(colorBtn);
            xPos += 110;

            // РАЗМЕР КИСТИ
            Label sizeLabel = new Label();
            sizeLabel.Text = "Размер:";
            sizeLabel.Location = new Point(xPos, 15);
            sizeLabel.Size = new Size(50, 20);
            sizeLabel.ForeColor = Color.White;
            toolPanel.Controls.Add(sizeLabel);

            sizeNumeric = new NumericUpDown();
            sizeNumeric.Location = new Point(xPos + 50, 12);
            sizeNumeric.Size = new Size(60, 25);
            sizeNumeric.Minimum = 1;
            sizeNumeric.Maximum = 50;
            sizeNumeric.Value = brushSize;
            sizeNumeric.BackColor = Color.FromArgb(63, 63, 70);
            sizeNumeric.ForeColor = Color.White;
            sizeNumeric.ValueChanged += (s, e) => brushSize = (int)sizeNumeric.Value;
            toolPanel.Controls.Add(sizeNumeric);

            // ДОБАВЛЯЕМ ВСЁ НА ФОРМУ
            this.Controls.Add(pictureBox);
            this.Controls.Add(toolPanel);
            this.Controls.Add(statusStrip);

            // ПОДПИСКА НА СОБЫТИЯ
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;
            pictureBox.Paint += PictureBox_Paint;
            pictureBox.Resize += PictureBox_Resize;

            // СОЗДАЁМ ХОЛСТ
            canvas = new Bitmap(pictureBox.Width, pictureBox.Height);
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.White);
            }
            pictureBox.Image = canvas;
            graphics = Graphics.FromImage(canvas);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
        }

        // ОБРАБОТЧИКИ СОБЫТИЙ
        private void PictureBox_Resize(object sender, EventArgs e)
        {
            if (pictureBox.Width > 0 && pictureBox.Height > 0 && canvas != null)
            {
                Bitmap newCanvas = new Bitmap(pictureBox.Width, pictureBox.Height);
                using (Graphics g = Graphics.FromImage(newCanvas))
                {
                    g.Clear(Color.White);
                    g.DrawImage(canvas, 0, 0);
                }
                canvas = newCanvas;
                graphics = Graphics.FromImage(canvas);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pictureBox.Image = canvas;
            }
        }

        private void ToolButton_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            currentTool = (ToolType)(int)btn.Tag;

            if (currentTool == ToolType.Polygon)
            {
                polygonPoints.Clear();
                isPolygonComplete = false;
            }

            statusLabel.Text = $"Выбран инструмент: {btn.Text}";
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.White);
            }
            pictureBox.Invalidate();
            polygonPoints.Clear();
            statusLabel.Text = "Холст очищен";
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                currentColor = colorDialog.Color;
            }
        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                switch (currentTool)
                {
                    case ToolType.Brush:
                        isDrawing = true;
                        lastPoint = e.Location;
                        break;

                    case ToolType.Circle:
                        DrawCircle(e.Location);
                        break;

                    case ToolType.Fill:
                        FloodFill(e.Location);
                        break;

                    case ToolType.Polygon:
                        if (!isPolygonComplete)
                        {
                            polygonPoints.Add(e.Location);
                            pictureBox.Invalidate();
                        }
                        break;

                    case ToolType.Text:
                        ShowTextDialog(e.Location);
                        break;
                }
            }
            else if (e.Button == MouseButtons.Right && currentTool == ToolType.Polygon)
            {
                if (polygonPoints.Count >= 3)
                {
                    CompletePolygon();
                }
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing && lastPoint.HasValue)
            {
                using (Pen pen = new Pen(currentColor, brushSize))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    graphics.DrawLine(pen, lastPoint.Value, e.Location);
                }
                lastPoint = e.Location;
                pictureBox.Invalidate();
            }

            statusLabel.Text = $"X: {e.X}, Y: {e.Y} | Инструмент: {currentTool}";
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isDrawing = false;
            lastPoint = null;
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (currentTool == ToolType.Polygon && polygonPoints.Count > 0 && !isPolygonComplete)
            {
                if (polygonPoints.Count >= 2)
                {
                    using (Pen pen = new Pen(Color.Gray, 1))
                    {
                        pen.DashStyle = DashStyle.Dash;
                        for (int i = 0; i < polygonPoints.Count - 1; i++)
                        {
                            e.Graphics.DrawLine(pen, polygonPoints[i], polygonPoints[i + 1]);
                        }
                    }
                }

                foreach (Point p in polygonPoints)
                {
                    e.Graphics.FillEllipse(Brushes.Red, p.X - 3, p.Y - 3, 6, 6);
                }
            }
        }

        private void DrawCircle(Point center)
        {
            Form inputForm = new Form();
            inputForm.Text = "Введите радиус";
            inputForm.Size = new Size(300, 150);
            inputForm.StartPosition = FormStartPosition.CenterParent;
            inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            inputForm.MaximizeBox = false;
            inputForm.MinimizeBox = false;

            Label label = new Label();
            label.Text = "Радиус окружности:";
            label.Location = new Point(10, 20);
            label.Size = new Size(150, 20);

            TextBox textBox = new TextBox();
            textBox.Location = new Point(10, 45);
            textBox.Size = new Size(150, 20);
            textBox.Text = "50";

            Button okButton = new Button();
            okButton.Text = "OK";
            okButton.Location = new Point(50, 75);
            okButton.Size = new Size(75, 30);
            okButton.DialogResult = DialogResult.OK;

            Button cancelButton = new Button();
            cancelButton.Text = "Отмена";
            cancelButton.Location = new Point(130, 75);
            cancelButton.Size = new Size(75, 30);
            cancelButton.DialogResult = DialogResult.Cancel;

            inputForm.Controls.Add(label);
            inputForm.Controls.Add(textBox);
            inputForm.Controls.Add(okButton);
            inputForm.Controls.Add(cancelButton);

            if (inputForm.ShowDialog() == DialogResult.OK)
            {
                if (int.TryParse(textBox.Text, out int radius) && radius > 0)
                {
                    using (Pen pen = new Pen(currentColor, brushSize))
                    {
                        graphics.DrawEllipse(pen, center.X - radius, center.Y - radius,
                                            radius * 2, radius * 2);
                    }
                    pictureBox.Invalidate();
                }
            }
        }

        private void FloodFill(Point point)
        {
            if (point.X < 0 || point.X >= canvas.Width || point.Y < 0 || point.Y >= canvas.Height)
                return;

            Color targetColor = canvas.GetPixel(point.X, point.Y);
            if (targetColor.ToArgb() != currentColor.ToArgb())
            {
                FloodFillAlgorithm(point.X, point.Y, targetColor, currentColor);
                pictureBox.Invalidate();
            }
        }

        // ИСПРАВЛЕННЫЙ МЕТОД ЗАЛИВКИ (без рекурсии)
        private void FloodFillAlgorithm(int startX, int startY, Color targetColor, Color fillColor)
        {
            if (targetColor.ToArgb() == fillColor.ToArgb())
                return;

            Stack<Point> pixels = new Stack<Point>();
            pixels.Push(new Point(startX, startY));

            while (pixels.Count > 0)
            {
                Point p = pixels.Pop();

                if (p.X < 0 || p.X >= canvas.Width || p.Y < 0 || p.Y >= canvas.Height)
                    continue;

                if (canvas.GetPixel(p.X, p.Y).ToArgb() != targetColor.ToArgb())
                    continue;

                canvas.SetPixel(p.X, p.Y, fillColor);

                pixels.Push(new Point(p.X + 1, p.Y));
                pixels.Push(new Point(p.X - 1, p.Y));
                pixels.Push(new Point(p.X, p.Y + 1));
                pixels.Push(new Point(p.X, p.Y - 1));
            }
        }

        private void CompletePolygon()
        {
            if (polygonPoints.Count >= 3)
            {
                using (Brush brush = new SolidBrush(currentColor))
                {
                    graphics.FillPolygon(brush, polygonPoints.ToArray());
                }
                pictureBox.Invalidate();
                isPolygonComplete = true;
                statusLabel.Text = "Многоугольник завершен";
            }
        }

        private void ShowTextDialog(Point location)
        {
            FontDialog fontDialog = new FontDialog();
            fontDialog.Font = currentFont;

            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                currentFont = fontDialog.Font;

                Form inputForm = new Form();
                inputForm.Text = "Введите текст";
                inputForm.Size = new Size(300, 150);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                Label label = new Label();
                label.Text = "Текст:";
                label.Location = new Point(10, 20);
                label.Size = new Size(50, 20);

                TextBox textBox = new TextBox();
                textBox.Location = new Point(10, 45);
                textBox.Size = new Size(200, 20);

                Button okButton = new Button();
                okButton.Text = "OK";
                okButton.Location = new Point(60, 75);
                okButton.Size = new Size(75, 30);
                okButton.DialogResult = DialogResult.OK;

                Button cancelButton = new Button();
                cancelButton.Text = "Отмена";
                cancelButton.Location = new Point(140, 75);
                cancelButton.Size = new Size(75, 30);
                cancelButton.DialogResult = DialogResult.Cancel;

                inputForm.Controls.Add(label);
                inputForm.Controls.Add(textBox);
                inputForm.Controls.Add(okButton);
                inputForm.Controls.Add(cancelButton);

                if (inputForm.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(textBox.Text))
                {
                    using (Brush brush = new SolidBrush(currentColor))
                    {
                        graphics.DrawString(textBox.Text, currentFont, brush, location);
                    }
                    pictureBox.Invalidate();
                }
            }
        }
    }
}