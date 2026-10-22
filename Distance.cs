using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Do
{
    class Distance
    {
        public static int Get(double x, double X, double y, double Y)
        {
           return (int)Math.Sqrt(((x - X) * (x - X)) + ((y - Y) * (y - Y)));
        }
    }
}
