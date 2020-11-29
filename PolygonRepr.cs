using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;

namespace Figure
{
    public class Polygon
    {
        public List<Dot> Dots { get; private set; }
        public List<Edge> Edges { get; private set; }
        public Polygon()
        {
            Dots = new List<Dot>();
            Edges = new List<Edge>();
        }
        public void Sort()
        {
            Dictionary<Dot, Edge> edgeSrch1 = Edges.ToDictionary( x => x.begin );
            Dictionary<Dot, Edge> edgeSrch2 = Edges.ToDictionary( x => x.end );
            Edge lastEdge = Edges[0];
            Dot dot;
            if (lastEdge.right == this) {
                dot = lastEdge.end;
            } else {
                dot = lastEdge.begin;
            }
            List<Dot> newDots = new List<Dot>();
            List<Edge> newEdges = new List<Edge>();
            for (int i = 0; i < Edges.Count; i++) {
                newDots.Add( dot );
                Edge edge = edgeSrch1[dot];
                if (edge == lastEdge) {
                    edge = edgeSrch2[dot];
                    dot = edge.begin;
                } else {
                    dot = edge.end;
                }
                if (edge == null) {
                    throw new Exception( "Polygon hasn't full edge loop" );
                }
                newEdges.Add( edge );
            }
            Dots = newDots;
            Edges = newEdges;
        }
    }

    public class Edge
    {
        public Dot begin, end;
        public Polygon left, right;

        public Edge(Dot begin, Dot end, Polygon left, Polygon right)
        {
            this.begin = begin;
            this.end = end;
            this.left = left;
            this.right = right;
            Connect();
        }

        public void Connect()
        {
            begin.Edges.Add( this );
            begin.Polygons.Add( left );
            begin.Polygons.Add( right );
            end.Edges.Add( this );
            end.Polygons.Add( left );
            end.Polygons.Add( right );
            left.Edges.Add( this );
            left.Dots.Add( begin );
            left.Dots.Add( end );
            right.Edges.Add( this );
            right.Dots.Add( begin );
            right.Dots.Add( end );
        }
    }

    public class Dot
    {
        public float X { get; set; }
        public float Y { get; set; }
        public List<Edge> Edges { get; private set; }
        public List<Polygon> Polygons { get; private set; }
        public Dot()
        {
            Edges = new List<Edge>();
            Polygons = new List<Polygon>();
        }
        public void Sort()
        {
            Edge prevEdge = Edges[0];
            List<Edge> newEdges = new List<Edge>();
            List<Polygon> newPolygons = new List<Polygon>();
            for (int i = 0; i < Edges.Count; i++) {
                newEdges.Add( prevEdge );
                Polygon polygon;
                if (prevEdge.begin == this) {
                    polygon = prevEdge.right;
                } else {
                    polygon = prevEdge.left;
                }
                if (polygon == null) {
                    throw new Exception( "Incomplete edge" );
                }
                newPolygons.Add( polygon );
                int prevPolyEdgeId = ( polygon.Edges.IndexOf( prevEdge ) + polygon.Edges.Count - 1 ) % polygon.Edges.Count;
                prevEdge = polygon.Edges[prevPolyEdgeId];
            }
        }
    }

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
            }
            return dot;
        }

        public void CompleteLoops()
        {
            foreach (Polygon polygon in Polygons) {
                polygon.Sort();
            }
            foreach(Dot dot in Dots) {
                dot.Sort();
            }
        }
    }

    public static class FromBorderFabric
    {
        public static PolygonRepr Create(IEnumerable<VoronoiDiagram.Border> borders, IEnumerable<VoronoiDiagram.Dot> dots)
        {
            Dictionary<PolygonRepr.Coord, Polygon> polygonSrch = new Dictionary<PolygonRepr.Coord, Polygon>();
            PolygonRepr polyRepr = new PolygonRepr();

            foreach (var border in borders) {
                if(border.begin==null || border.end == null) {
                    throw new Exception( "Unconsistent border" );
                }
                var dotBegin = polyRepr.AddDot( border.begin.Value.x, border.begin.Value.y );
                var dotEnd = polyRepr.AddDot( border.end.Value.x, border.end.Value.y );
                PolygonRepr.Coord dotLeft = new PolygonRepr.Coord( polyRepr, border.siteLeft.x, border.siteLeft.y );
                PolygonRepr.Coord dotRight = new PolygonRepr.Coord( polyRepr, border.siteRight.x, border.siteRight.y );
                Polygon polyLeft;
                Polygon polyRight;
                if (!polygonSrch.TryGetValue( dotLeft, out polyLeft )) {
                    polyLeft = new Polygon();
                    polyRepr.Polygons.Add( polyLeft );
                    polygonSrch.Add( dotLeft, polyLeft );
                }
                if (!polygonSrch.TryGetValue( dotRight, out polyRight )) {
                    polyRight = new Polygon();
                    polygonSrch.Add( dotRight, polyRight );
                    polyRepr.Polygons.Add( polyRight );
                }
                polyRepr.Edges.Add( new Edge( dotBegin, dotEnd, polyLeft, polyRight ) );
            }
            polyRepr.CompleteLoops();

            return polyRepr;
        }
    }
}
