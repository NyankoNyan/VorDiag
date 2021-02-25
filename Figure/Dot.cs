using System;
using System.Collections.Generic;

namespace Figure
{
    public class Dot
    {
        public float X { get; set; }
        public float Y { get; set; }
        public List<Edge> Edges { get; private set; }
        public List<Polygon> Polygons { get; private set; }
        public int Id { get; set; }
        public override string ToString()
        {
            return $"Dot {Id} ({X};{Y})";
        }

        public Dot()
        {
            Edges = new List<Edge>();
            Polygons = new List<Polygon>();
        }
    }
}