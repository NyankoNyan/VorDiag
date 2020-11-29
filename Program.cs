using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Runtime.InteropServices;

namespace VorDiag
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (string filePath in InputFiles()) {
                string fileNameCore = Path.GetFileNameWithoutExtension( filePath );
                //if (fileNameCore != "test2b") {
                //    continue;//todo delete
                //}
                var inputRepr = InputRepr.FromFile( filePath );
                var borders = VoronoiDiagram.CreateEdges( inputRepr.dots.Select( d => new VoronoiDiagram.Dot() { x = d[0], y = d[1] } ) );

                borders = VoronoiDiagram.ApplyBoundingBox( borders, new VoronoiDiagram.BoundingBox() {
                    x = inputRepr.boundingBox.leftX,
                    y = inputRepr.boundingBox.bottomY,
                    sizeX = inputRepr.boundingBox.width,
                    sizeY = inputRepr.boundingBox.height
                } );

                CanvasCreator canvasCreator = new CanvasCreator();
                canvasCreator.size = new CanvasCreator.Size() { x = 700, y = 700 };

                const float margin = 0.1f;
                canvasCreator.SetupCamera(
                    inputRepr.boundingBox.leftX - inputRepr.boundingBox.width * margin,
                    inputRepr.boundingBox.leftX + inputRepr.boundingBox.width * ( margin + 1f ),
                    inputRepr.boundingBox.bottomY - inputRepr.boundingBox.height * margin,
                    inputRepr.boundingBox.bottomY + inputRepr.boundingBox.height * ( margin + 1f ) );

                foreach (var border in borders) {
                    if (border.begin != null && border.end != null) {
                        canvasCreator.AddEdge( border.begin.Value.x, border.begin.Value.y, border.end.Value.x, border.end.Value.y );
                    } else if (border.begin != null || border.end != null) {
                        var ray = border.GetRay();
                        canvasCreator.AddRay( ray.x, ray.y, ray.dx, ray.dy );
                    }
                }
                foreach (var dot in inputRepr.dots) {
                    canvasCreator.AddDot( dot[0], dot[1] );
                }

                File.WriteAllText( $"..\\..\\..\\tests\\out\\{fileNameCore}.html", canvasCreator.GetHtmlContent() );
            }
        }

        static string[] InputFiles()
        {
            return Directory.GetFiles( "..\\..\\..\\tests\\in\\" );
        }
    }
}
