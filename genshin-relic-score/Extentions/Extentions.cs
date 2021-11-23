using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace genshin.relic.score.Extentions
{
    public static class Extention
    {
        public static int Distance(this Point self, Point point)
        {
            var x1 = self.X;
            var x2 = point.X;
            var y1 = self.Y;
            var y2 = point.Y;

            var distance = Math.Sqrt((Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)));

            return Convert.ToInt32(distance);
        }
    }
}
