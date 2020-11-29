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
}
