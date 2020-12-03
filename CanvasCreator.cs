using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;

public class CanvasCreator
{
    public Size size { get; set; }
    public float[][] camera { get; set; }
    public Edge[] edges { get => _edges.ToArray(); }
    private List<Edge> _edges = new List<Edge>();
    public Ray[] rays { get => _rays.ToArray(); }
    private List<Ray> _rays = new List<Ray>();
    public Dot[] dots { get => _dots.ToArray(); }
    private List<Dot> _dots = new List<Dot>();
    public Polygon[] polygons { get => _polygons.ToArray(); }
    private List<Polygon> _polygons = new List<Polygon>();
    public Arc[] arcs { get => _arcs.ToArray(); }
    private List<Arc> _arcs = new List<Arc>();
    public Circle[] circles { get => _circles.ToArray(); }
    private List<Circle> _circles = new List<Circle>();
    public Line[] lines { get => _lines.ToArray(); }
    private List<Line> _lines = new List<Line>();

    private static string template;
    private const string templateFile = "canvas_template.txt";

    static CanvasCreator()
    {
        template = File.ReadAllText( templateFile );
    }
    public CanvasCreator()
    {
        camera = new float[][]{
            new float[] { 1, 0, 0 },
            new float[] { 0, 1, 0 },
            new float[] { 0, 0, 1 } };
        size = new Size() { x = 300, y = 300 };
    }

    public void SetupCamera(float left, float right, float bottom, float top)
    {
        camera[0][0] = size.x / ( right - left );
        camera[1][1] = size.y / ( bottom - top );
        camera[2][0] = -camera[0][0] * left;
        camera[2][1] = -camera[1][1] * top;
    }

    public string GetHtmlContent()
    {
        string canvasContent = JsonSerializer.Serialize( this );
        return template.Replace( "%JSON_OUTPUT%", canvasContent );
    }

    public class Size
    {
        public float x { get; set; }
        public float y { get; set; }
    }

    public class Edge
    {
        public float x1 { get; set; }
        public float y1 { get; set; }
        public float x2 { get; set; }
        public float y2 { get; set; }
    }

    public struct Ray
    {
        public float x { get; set; }
        public float y { get; set; }
        public float dx { get; set; }
        public float dy { get; set; }
    }

    public struct Dot
    {
        public float x { get; set; }
        public float y { get; set; }
    }

    public struct Arc
    {
        public float fX { get; set; }
        public float fY { get; set; }
        public float dirY { get; set; }
        public float minX { get; set; }
        public float maxX { get; set; }
    }

    public struct Polygon
    {
        public Dot[] dots { get; set; }
    }

    public struct Circle
    {
        public float x { get; set; }
        public float y { get; set; }
        public float r { get; set; }
    }

    public struct Line
    {
        public float dx { get; set; }
        public float dy { get; set; }
        public float c { get; set; }
    }

    public void AddEdge(float x1, float y1, float x2, float y2)
    {
        _edges.Add( new Edge() { x1 = x1, y1 = y1, x2 = x2, y2 = y2 } );
    }

    public void AddRay(float x, float y, float dx, float dy)
    {
        _rays.Add( new Ray() { x = x, y = y, dx = dx, dy = dy } );
    }

    public void AddDot(float x, float y)
    {
        _dots.Add( new Dot() { x = x, y = y } );
    }

    public void AddPolygon(float[] dots)
    {
        Polygon polygon = new Polygon();
        polygon.dots = new Dot[dots.Length / 2];
        for (int i = 0; i < polygon.dots.Length; i++) {
            polygon.dots[i].x = dots[i * 2];
            polygon.dots[i].y = dots[i * 2 + 1];
        }
        _polygons.Add( polygon );
    }

    public void AddArc(float fX, float fY, float dirY, float minX, float maxX)
    {
        //const float infFloat = 100000f;
        _arcs.Add( new Arc() { dirY = dirY, fX = fX, fY = fY, minX = minX, maxX = maxX } );
    }

    public void AddCircle(float x, float y, float r)
    {
        _circles.Add( new Circle() { x = x, y = y, r = r } );
    }

    public void AddLine(float dx,float dy,float c)
    {
        _lines.Add( new Line() { dx = dx, dy = dy, c = c } );
    }
}
