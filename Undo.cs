using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FallingBall
{
    struct Undo
    {
        public Color Color { get; set; }
        public Stack<Point> Points { get; set; }
        public Stack<int> Columns { get; set; }
    }
}
