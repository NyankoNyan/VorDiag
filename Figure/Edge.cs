namespace Figure
{
    public class Edge
    {
        public Dot begin, end;
        public Polygon left, right;
        public int Id { get; set; }
        public override string ToString()
        {
            return $"Edge {Id} {begin.ToString()} - {end.ToString()}";
        }

        public Edge()
        {
            
        }

        //public Edge(Dot begin, Dot end, Polygon left, Polygon right)
        //{
        //    this.begin = begin;
        //    this.end = end;
        //    this.left = left;
        //    this.right = right;
        //    Connect();
        //}

        //public void Connect()
        //{
        //    begin.Edges.Add( this );
        //    begin.Polygons.Add( left );
        //    begin.Polygons.Add( right );
        //    end.Edges.Add( this );
        //    end.Polygons.Add( left );
        //    end.Polygons.Add( right );
        //    left.Edges.Add( this );
        //    left.Dots.Add( begin );
        //    left.Dots.Add( end );
        //    right.Edges.Add( this );
        //    right.Dots.Add( begin );
        //    right.Dots.Add( end );
        //}
    }
}