using System;
using System.Collections.Generic;

namespace Figure
{
    public class PolygonRepr
    {
        public List<Dot> Dots { get; private set; }
        public List<Edge> Edges { get; private set; }
        public List<Polygon> Polygons { get; private set; }
        public float Eps { get => eps; set { eps = value; epsSqr = value * value; } }
        public float EpsSqr { get => epsSqr; }

        private float eps;
        private float epsSqr;

        private Dictionary<Coord, Dot> dotSrch = new Dictionary<Coord, Dot>();

        internal struct Coord : IEquatable<Coord>
        {
            public float x, y;
            public PolygonRepr polyRepr;

            public Coord(PolygonRepr polyRepr, float x, float y)
            {
                this.polyRepr = polyRepr;
                this.x = x;
                this.y = y;
            }

            public bool Equals(Coord other)
            {
                float dx = other.x - x;
                float dy = other.y - y;
                return dx * dx + dy * dy <= polyRepr.EpsSqr;
            }
        }

        public PolygonRepr()
        {
            Eps = .00001f;
            Dots = new List<Dot>();
            Edges = new List<Edge>();
            Polygons = new List<Polygon>();
        }

        public Dot AddDot(float x, float y)
        {
            Coord coord = new Coord( this, x, y );
            Dot dot;
            if (!dotSrch.TryGetValue( coord, out dot )) {
                dot = new Dot() { X = x, Y = y };
                dotSrch.Add( coord, dot );
                Dots.Add( dot );
            }
            return dot;
        }

        public void FillIDs()
        {
            for (int i = 0; i < Dots.Count; i++) {
                Dots[i].Id = i + 1;
            }
            for (int i = 0; i < Edges.Count; i++) {
                Edges[i].Id = i + 1;
            }
            for (int i = 0; i < Polygons.Count; i++) {
                Polygons[i].Id = i + 1;
            }
        }
    }
}