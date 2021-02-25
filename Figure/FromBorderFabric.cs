﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Figure
{
    public static class FromBorderFabric
    {
        public struct Border
        {
            public float x1, y1, x2, y2;
        }
        public static PolygonRepr Create(IEnumerable<VoronoiDiagram.Border> borders)
        {
            return Create( borders.Select( x => new Border { x1 = x.begin.Value.x, y1 = x.begin.Value.y, x2 = x.end.Value.x, y2 = x.end.Value.y } ).ToArray() );
        }
        public static PolygonRepr Create(Border[] borders)
        {
            PolygonRepr polyRepr = new PolygonRepr();

            foreach (Border border in borders) {
                Edge edge = new Edge();
                polyRepr.Edges.Add( edge );
                edge.begin = polyRepr.AddDot( border.x1, border.y1 );
                edge.begin.Edges.Add( edge );
                edge.end = polyRepr.AddDot( border.x2, border.y2 );
                edge.end.Edges.Add( edge );
            }

            foreach (var dot in polyRepr.Dots) {

                SortedList<float, Edge> edgeAngles = new SortedList<float, Edge>();

                foreach (var edge in dot.Edges) {
                    float dx, dy;
                    if (edge.begin == dot) {
                        dx = edge.end.X - edge.begin.X;
                        dy = edge.end.Y - edge.begin.Y;
                    } else {
                        dx = edge.begin.X - edge.end.X;
                        dy = edge.begin.Y - edge.end.Y;
                    }
                    edgeAngles.Add( GetAngleCompareValue( dx, dy ), edge );
                }

                dot.Edges.Clear();
                dot.Edges.AddRange( edgeAngles.Values );
            }

            foreach (var edge in polyRepr.Edges) {
                if (edge.left == null) {
                    polyRepr.Polygons.Add( MakePolygon( edge, edge.end ) );
                }
                if (edge.right == null) {
                    polyRepr.Polygons.Add( MakePolygon( edge, edge.begin ) );
                }
            }

            polyRepr.FillIDs();
            return polyRepr;
        }

        private static Polygon MakePolygon(Edge startEdge, Dot startDot)
        {
            Polygon polygon = new Polygon();
            Edge edge = startEdge;
            Dot dot = startDot;
            do {
                if (dot.Edges.Count <= 1) {
                    throw new Exception( "Dot link with less then two edges" );
                }
                if (edge.begin == edge.end) {
                    throw new Exception( "Edge link same dot" );
                }
                polygon.Dots.Add( dot );
                polygon.Edges.Add( edge );
                dot.Polygons.Add( polygon );
                
                if (edge.begin == dot) {
                    dot = edge.end;
                    edge.right = polygon;
                } else {
                    dot = edge.begin;
                    edge.left = polygon;
                }
                int edgeId = dot.Edges.IndexOf( edge );
                if (edgeId == -1) {
                    throw new Exception( "Broken dot found" );
                }
                int nextEdgeId = ( edgeId + 1 ) % dot.Edges.Count;
                edge = dot.Edges[nextEdgeId];

            } while (edge != startEdge);
            return polygon;
        }

        private static float GetAngleCompareValue(float x, float y)
        {
            if (MathF.Abs( x ) > MathF.Abs( y )) {
                return y / x + ( x < 0 ? 4f : 0 );
            } else {
                return -x / y + 2f * Math.Sign( y );
            }
        }
    }
}