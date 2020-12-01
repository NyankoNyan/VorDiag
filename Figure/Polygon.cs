using System;
using System.Collections.Generic;
using System.Linq;

namespace Figure
{
    public class Polygon
    {
        public List<Dot> Dots { get; private set; }
        public List<Edge> Edges { get; private set; }
        public int Id { get; set; }
        public override string ToString()
        {
            return $"Polygon {Id}";
        }

        public Polygon()
        {
            Dots = new List<Dot>();
            Edges = new List<Edge>();
        }

        public float GetInnerAnglesSum()
        {
            float angleSum = 0;
            Dot prevDot = Dots[Dots.Count - 1];
            float prevAngle = GetAngle( Dots[Dots.Count - 2], Dots[Dots.Count - 1] );
            foreach (var dot in Dots) {
                float angle = GetAngle( prevDot, dot );
                float delta = prevAngle - angle;
                if (delta > MathF.PI) {
                    delta -= MathF.PI;
                }else if(delta < -MathF.PI) {
                    delta += MathF.PI;
                }
                angleSum += delta;
                prevDot = dot;
                prevAngle = angle;
            }
            return angleSum;
        }

        private static float GetAngle(Dot dot1, Dot dot2)
        {
            float dx = dot2.X - dot1.X;
            float dy = dot2.Y - dot1.Y;
            return MathF.Atan2( dy, dx );
        }

        //public void Sort()
        //{
        //    Dictionary<Dot, Edge> edgeSrch1 = Edges.ToDictionary( x => x.right == this ? x.begin : x.end );
        //    Dictionary<Dot, Edge> edgeSrch2 = Edges.ToDictionary( x => x.right == this ? x.end : x.begin );
        //    Edge lastEdge = Edges[0];
        //    Dot dot;
        //    if (lastEdge.right == this) {
        //        dot = lastEdge.end;
        //    } else {
        //        dot = lastEdge.begin;
        //    }
        //    List<Dot> newDots = new List<Dot>();
        //    List<Edge> newEdges = new List<Edge>();
        //    for (int i = 0; i < Edges.Count; i++) {
        //        newDots.Add( dot );
        //        Edge edge = edgeSrch1[dot];
        //        if (edge == lastEdge) {
        //            edge = edgeSrch2[dot];
        //            dot = edge.begin;
        //        } else {
        //            dot = edge.end;
        //        }
        //        if (edge == null) {
        //            throw new Exception( "Polygon hasn't full edge loop" );
        //        }
        //        newEdges.Add( edge );
        //    }
        //    Dots = newDots;
        //    Edges = newEdges;
        //}
    }
}