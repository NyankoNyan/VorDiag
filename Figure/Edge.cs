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
    }
}