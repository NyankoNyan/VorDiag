using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

class InputRepr
{
    public BoudingBox boundingBox { get; set; }
    public float[][] dots { get; set; }
    public InputRepr()
    {
        boundingBox = new BoudingBox();
    }
    public class BoudingBox
    {
        public float leftX { get; set; }
        public float bottomY { get; set; }
        public float width { get; set; }
        public float height { get; set; }
    }
    public static InputRepr FromFile(string filePath)
    {
        return JsonSerializer.Deserialize<InputRepr>( File.ReadAllText( filePath ) );
    }
    public string GetContent()
    {
        return JsonSerializer.Serialize( this, new JsonSerializerOptions() { WriteIndented = true } );
    }
}
