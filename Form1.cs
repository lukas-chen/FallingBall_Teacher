using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FallingBall
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        const int GridWidth = 10;
        const int GridHeight = 16;
        const int GridSize = BallSize + 4;
        const int BallSize = 32;

        int MenuHeight = 0;

        Color[,] balls = new Color[GridWidth, GridHeight];
        Color[] colors = { Color.Red, Color.Blue/*, Color.Orange, Color.Green*/ };
        Stack<Undo> undo = new Stack<Undo>();
        private void Form1_Load(object sender, EventArgs e)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            MenuHeight = menuStrip1.Height;
            this.ClientSize = new Size(GridWidth * GridSize + 1, GridHeight * GridSize + 1 + MenuHeight);

            NewGame();
        }

        private void NewGame()
        {
            Random rnd = new Random();

            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    balls[x, y] = colors[rnd.Next(colors.Length)];
                }
            }
            this.Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            for(int x = 0;x <= GridWidth; x++)
                e.Graphics.DrawLine(Pens.Black, x * GridSize, MenuHeight, x * GridSize, GridHeight * GridSize + MenuHeight);

            for (int y = 0; y <= GridHeight; y++)
                e.Graphics.DrawLine(Pens.Black, 0, y * GridSize + MenuHeight, GridWidth * GridSize, y * GridSize + MenuHeight);

            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    Brush b = new SolidBrush(balls[x, y]);
                    e.Graphics.FillEllipse(b, x * GridSize + 2, y * GridSize + 2 + MenuHeight, BallSize, BallSize);
                }
            }

        }

        void CheckPoint(int x, int y, Color c, List<Point> points)
        {
            Point p = new Point(x, y);
            if (!points.Contains(p) && x >= 0 && x < GridWidth && y >= 0 && y < GridHeight && balls[x, y] == c)
            {
                points.Add(p);
                CheckPoint(x, y - 1, c, points);
                CheckPoint(x, y + 1, c, points);
                CheckPoint(x - 1, y, c, points);
                CheckPoint(x + 1, y, c, points);
            }
        }

        void Wait()
        {
            this.Invalidate();
            Application.DoEvents();
            Thread.Sleep(50);
        }

        bool inMouseDown = false;
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (!inMouseDown)
            {
                inMouseDown = true;
                this.Cursor = Cursors.WaitCursor;

                int x = e.X / GridSize;
                int y = (e.Y - MenuHeight) / GridSize;

                if (x < GridWidth && y < GridHeight && balls[x, y] != Color.Empty)
                {
                    Color c = balls[x, y];

                    List<Point> points = new List<Point>();
                    CheckPoint(x, y, c, points);
                    if (points.Count >= 3)
                    {
                        Undo u = new Undo();

                        foreach (Point p in points)
                            balls[p.X, p.Y] = Color.Empty;

                        var sorted =
                            from p in points
                            orderby p.X, p.Y
                            select p;
                        u.Points = new Stack<Point>();
                        foreach (var p in sorted)
                        {
                            u.Points.Push(p);
                            for (int j = p.Y; j > 0; j--)
                                balls[p.X, j] = balls[p.X, j - 1];
                            balls[p.X, 0] = Color.Empty;

                            Wait();
                        }

                        int i = 0;
                        var a = balls.Cast<Color>().Where(f =>
                        {
                            i++;
                            return i % GridHeight == 0;
                        }).Reverse().SkipWhile(f => f == Color.Empty).Reverse().ToArray();

                        List<int> list1 = new List<int>();
                        for (int k = 0; k < a.Length; k++)
                        {
                            if (a[k] == Color.Empty)
                                list1.Add(k);
                        }
                        u.Columns = new Stack<int>();

                        int count = 0;
                        foreach (int k in list1)
                        {
                            int col = k - count;
                            u.Columns.Push(col);

                            for (int m = col; m < GridWidth - 1; m++)
                                for (int n = 0; n < GridHeight; n++)
                                    balls[m, n] = balls[m + 1, n];

                            for (int n = 0; n < GridHeight; n++)
                                balls[GridWidth - 1, n] = Color.Empty;

                            count++;
                            Wait();
                        }

                        u.Color = c;
                        undo.Push(u);
                    }

                    this.Invalidate();
                }
                this.Cursor = Cursors.Default;
                inMouseDown = false;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            balls[2, GridHeight - 1] = Color.Empty;
            balls[3, GridHeight - 1] = Color.Empty;
            balls[6, GridHeight - 1] = Color.Empty;
            balls[GridWidth - 1, GridHeight - 1] = Color.Empty;
            balls[GridWidth - 2, GridHeight - 1] = Color.Empty;

            int i = 0;
            var a = balls.Cast<Color>().Where(c =>
            {
                i++;
                return i % GridHeight == 0;
            }).Reverse().SkipWhile(c => c == Color.Empty).Reverse().ToArray();

            List<int> list1 = new List<int>();
            for(int k=0;k<a.Length;k++)
            {
                if (a[k] == Color.Empty)
                    list1.Add(k);
            }



        }

        bool inKeyDown = false;
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!inKeyDown && e.Control && e.KeyCode == Keys.Z && undo.Count > 0)
            {
                inKeyDown = true;
                Undo u = undo.Pop();

                while (u.Columns.Count > 0)
                {
                    int col = u.Columns.Pop();

                    for (int x = GridWidth - 1; x > col; x--)
                        for (int y = 0; y < GridHeight; y++)
                            balls[x, y] = balls[x - 1, y];

                    for (int y = 0; y < GridHeight; y++)
                        balls[col, y] = Color.Empty;

                    Wait();

                }

                Point[] points = u.Points.ToArray();

                while (u.Points.Count > 0)
                {
                    Point p = u.Points.Pop();
                    for (int y = 0; y < p.Y; y++)
                        balls[p.X, y] = balls[p.X, y + 1];
                    
                    balls[p.X, p.Y] = Color.Empty;
                    Wait();
                }
                foreach (Point pt in points)
                    balls[pt.X, pt.Y] = u.Color;
                Wait();
                inKeyDown = false;
            }
        }

        private void menuNew_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否要啟動新遊戲?", "開啟新遊戲", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                NewGame();
            }
            
        }

        private void menuOpen_Click(object sender, EventArgs e)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = new FileStream(spath + @"\colors.bin", FileMode.Open))
            {
                balls = (Color[,])bf.Deserialize(fs);
            }

            using (FileStream fs = new FileStream(spath + @"\undo.bin", FileMode.Open))
            {
                undo = (Stack<Undo>)bf.Deserialize(fs);
            }
            this.Invalidate();
        }

        string spath = Application.StartupPath + "\\SaveData";

        private void menuSave_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(spath))
                Directory.CreateDirectory(spath);

            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = new FileStream(spath + @"\colors.bin", FileMode.Create))
            {
                bf.Serialize(fs, balls);
            }

            using (FileStream fs = new FileStream(spath + @"\undo.bin", FileMode.Create))
            {
                bf.Serialize(fs, undo);
            }

            MessageBox.Show("DONE");
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you wanna leave??", "Exit", MessageBoxButtons.YesNo) == DialogResult.Yes)
                Application.Exit();
        }
    }
}
