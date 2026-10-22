using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Do
{
    /// <summary>
    /// Вспомогательный класс для вычисления расстояния между двумя точками на карте.
    /// </summary>
    class Distance
    {
        /// <summary>
        /// Вычисляет евклидово расстояние между двумя точками (x, y) и (X, Y).
        /// </summary>
        /// <param name="x">Координата X первой точки.</param>
        /// <param name="X">Координата X второй точки.</param>
        /// <param name="y">Координата Y первой точки.</param>
        /// <param name="Y">Координата Y второй точки.</param>
        /// <returns>Целочисленное расстояние между точками.</returns>
        public static int Get(double x, double X, double y, double Y)
        {
           return (int)Math.Sqrt(((x - X) * (x - X)) + ((y - Y) * (y - Y)));
        }
    }
}
