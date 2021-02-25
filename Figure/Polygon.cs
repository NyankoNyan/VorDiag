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
    }
}