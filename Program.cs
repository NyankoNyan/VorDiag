using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VorDiag
{

    class Program
    {
        static bool drawDebug = false;
        static bool test = true;
        static string specificTestName = "test10";
        static int rndGenerationsCount = 1;
        static int rndDotCountMin = 1000;
        static int rndDotCountMax = 1200;
        static int resolutionX = 700;
        static int resolutionY = 700;

        static void Main(string[] args)
        {
            if (test) {
                BorderConditionTests();
            } else {
                RandomGenerationTests( rndGenerationsCount, rndDotCountMin, rndDotCountMax );
            }
        }

        static void RandomGenerationTests(int count, int minDots, int maxDots)
        {
            Random random = new Random();
            for (int iter = 0; iter < count; iter++) {
                VoronoiDiagram.BoundingBox boundingBox = new VoronoiDiagram.BoundingBox() {
                    x = 0,
                    y = 0,
                    sizeX = 1000,
                    sizeY = 1000
                };
                var dots = GetRandomDots( random.Next( minDots, maxDots ), 100, 900, 100, 900 );

                InputRepr inputRepr = new InputRepr();

                inputRepr.boundingBox.leftX = boundingBox.x;
                inputRepr.boundingBox.bottomY = boundingBox.y;
                inputRepr.boundingBox.width = boundingBox.sizeX;
                inputRepr.boundingBox.height = boundingBox.sizeY;
                inputRepr.dots = dots.Select( dot => new float[2] { dot.x, dot.y } ).ToArray();
                File.WriteAllText( $"input_{iter.ToString()}.json", inputRepr.GetContent() );

                DiagramBuildLogger logger = null;
                if (drawDebug) {
                    logger = new DiagramBuildLogger( "", $"output_{iter.ToString()}_debug", boundingBox, resolutionX, resolutionY );
                }

                var borders = VoronoiDiagram.CreateEdges( dots, logger );

                borders = VoronoiDiagram.ApplyBoundingBox( borders, boundingBox );

                //CanvasCreator canvasCreator = GetSimpleCanvas( dots, borders );
                CanvasCreator canvasCreator = GetPolygonalCanvas( dots, borders );

                SetupCanvas( canvasCreator, boundingBox );

                File.WriteAllText( $"output_{iter.ToString()}.html", canvasCreator.GetHtmlContent() );
            }
        }

        static void SetupCanvas(CanvasCreator canvasCreator, VoronoiDiagram.BoundingBox boundingBox)
        {
            canvasCreator.size = new CanvasCreator.Size() { x = resolutionX, y = resolutionY };

            const float margin = 0.1f;
            canvasCreator.SetupCamera(
                boundingBox.x - boundingBox.sizeX * margin,
                boundingBox.x + boundingBox.sizeX * ( margin + 1f ),
                boundingBox.y - boundingBox.sizeY * margin,
                boundingBox.y + boundingBox.sizeY * ( margin + 1f ) );
        }

        static List<VoronoiDiagram.Dot> GetRandomDots(int count, float minX, float maxX, float minY, float maxY)
        {
            List<VoronoiDiagram.Dot> dots = new List<VoronoiDiagram.Dot>();
            Random random = new Random();
            for (int iter = 0; iter < count; iter++) {
                dots.Add( new VoronoiDiagram.Dot() {
                    x = minX + (float)random.NextDouble() * ( maxX - minX ),
                    y = minY + (float)random.NextDouble() * ( maxY - minY )
                } );
            }
            return dots;
        }

        static void BorderConditionTests()
        {
            foreach (string filePath in InputFiles()) {
                string fileNameCore = Path.GetFileNameWithoutExtension( filePath );
                if (specificTestName != "" && fileNameCore != specificTestName) {
                    continue;//todo delete
                }
                var inputRepr = InputRepr.FromFile( filePath );

                var boundingBox = new VoronoiDiagram.BoundingBox() {
                    x = inputRepr.boundingBox.leftX,
                    y = inputRepr.boundingBox.bottomY,
                    sizeX = inputRepr.boundingBox.width,
                    sizeY = inputRepr.boundingBox.height
                };

                DiagramBuildLogger logger = null;
                if (drawDebug) {
                    logger = new DiagramBuildLogger( "", fileNameCore + "_debug", boundingBox, resolutionX, resolutionY );
                }

                var dots = inputRepr.dots.Select( d => new VoronoiDiagram.Dot() { x = d[0], y = d[1] } );
                var borders = VoronoiDiagram.CreateEdges( dots, logger );

                CanvasCreator canvasCreator;

                canvasCreator = GetSimpleCanvas( dots, borders );
                SetupCanvas( canvasCreator, boundingBox );
                File.WriteAllText( $"..\\..\\..\\tests\\out\\{fileNameCore}.html", canvasCreator.GetHtmlContent() );

                borders = VoronoiDiagram.ApplyBoundingBox( borders, boundingBox );

                canvasCreator = GetPolygonalCanvas( dots, borders );
                SetupCanvas( canvasCreator, boundingBox );

                File.WriteAllText( $"..\\..\\..\\tests\\out\\{fileNameCore}_p.html", canvasCreator.GetHtmlContent() );
            }
        }

        static CanvasCreator GetSimpleCanvas(IEnumerable<VoronoiDiagram.Dot> dots, IEnumerable<VoronoiDiagram.Border> borders)
        {
            CanvasCreator canvasCreator = new CanvasCreator();

            foreach (var border in borders) {
                if (border.begin != null && border.end != null) {
                    canvasCreator.AddEdge( border.begin.Value.x, border.begin.Value.y, border.end.Value.x, border.end.Value.y );
                } else if (border.begin != null || border.end != null) {
                    var ray = border.GetRay();
                    canvasCreator.AddRay( ray.x, ray.y, ray.dx, ray.dy );
                }
            }
            foreach (var dot in dots) {
                canvasCreator.AddDot( dot.x, dot.y );
            }
            return canvasCreator;
        }

        static CanvasCreator GetPolygonalCanvas(IEnumerable<VoronoiDiagram.Dot> dots, IEnumerable<VoronoiDiagram.Border> borders)
        {
            CanvasCreator canvasCreator = new CanvasCreator();

            var polyRepr = Figure.FromBorderFabric.Create( borders );

            foreach (var polygon in polyRepr.Polygons) {
                if (polygon.GetInnerAnglesSum() > 0) {
                    // Only normal polygons without border polygon
                    canvasCreator.AddPolygon( polygon.Dots.SelectMany( dot => new float[] { dot.X, dot.Y } ).ToArray() );
                }
            }
            foreach (var dot in dots) {
                canvasCreator.AddDot( dot.x, dot.y );
            }
            return canvasCreator;
        }

        static string[] InputFiles()
        {
            return Directory.GetFiles( "..\\..\\..\\tests\\in\\" );
        }
    }
}
