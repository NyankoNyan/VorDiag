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

        //public void Sort()
        //{
        //    Edge prevEdge = Edges[0];
        //    List<Edge> newEdges = new List<Edge>();
        //    List<Polygon> newPolygons = new List<Polygon>();
        //    for (int i = 0; i < Edges.Count; i++) {
        //        newEdges.Add( prevEdge );
        //        Polygon polygon;
        //        if (prevEdge.begin == this) {
        //            polygon = prevEdge.right;
        //        } else {
        //            polygon = prevEdge.left;
        //        }
        //        if (polygon == null) {
        //            throw new Exception( "Incomplete edge" );
        //        }
        //        newPolygons.Add( polygon );
        //        int prevPolyEdgeId = ( polygon.Edges.IndexOf( prevEdge ) + polygon.Edges.Count - 1 ) % polygon.Edges.Count;
        //        prevEdge = polygon.Edges[prevPolyEdgeId];
        //    }
        //    Edges = newEdges;
        //    Polygons = newPolygons;
        //}
    }
}