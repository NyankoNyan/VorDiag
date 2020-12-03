using System.IO;
using System.Collections.Generic;

namespace VorDiag
{
    class DiagramBuildLogger : VoronoiDiagram.IBuildInfo
    {
        private string outputDir, filePrefix;
        private VoronoiDiagram.BoundingBox boundingBox;
        private int step = 1;

        public DiagramBuildLogger(string outputDir, string filePrefix, VoronoiDiagram.BoundingBox boundingBox)
        {
            this.outputDir = outputDir;
            this.filePrefix = filePrefix;
            this.boundingBox = boundingBox;
        }

        void VoronoiDiagram.IBuildInfo.SendState(
            IEnumerable<VoronoiDiagram.Border> borders,
            IEnumerable<VoronoiDiagram.BuildCircle> circles,
            IEnumerable<VoronoiDiagram.BuildArc> arcs,
            IEnumerable<VoronoiDiagram.Dot> pointsOfInterest, 
            float directrixY)
        {
            CanvasCreator canvasCreator = new CanvasCreator();

            canvasCreator.AddLine( 0, 1, -directrixY );

            foreach (var border in borders) {
                if (border.begin != null && border.end != null) {
                    canvasCreator.AddEdge( border.begin.Value.x, border.begin.Value.y, border.end.Value.x, border.end.Value.y );
                } else if (border.begin != null || border.end != null) {
                    var ray = border.GetRay();
                    canvasCreator.AddRay( ray.x, ray.y, ray.dx, ray.dy );
                } else {
                    var line = border.GetLine();
                    canvasCreator.AddLine( line.dx, line.dy, line.c );
                }
            }

            foreach (var circle in circles) {
                canvasCreator.AddCircle( circle.center.x, circle.center.y, circle.radius );
            }
            foreach (var arc in arcs) {
                canvasCreator.AddArc( arc.focus.x, arc.focus.y, arc.directrixY, arc.leftX, arc.rightX );
            }
            foreach (var dot in pointsOfInterest) {
                canvasCreator.AddDot( dot.x, dot.y );
            }

            canvasCreator.size = new CanvasCreator.Size() { x = 700, y = 700 };
            const float margin = 0.1f;
            canvasCreator.SetupCamera(
                boundingBox.x - boundingBox.sizeX * margin,
                boundingBox.x + boundingBox.sizeX * ( margin + 1f ),
                boundingBox.y - boundingBox.sizeY * margin,
                boundingBox.y + boundingBox.sizeY * ( margin + 1f ) );

            File.WriteAllText( $"{outputDir}{filePrefix}_{step}.html", canvasCreator.GetHtmlContent() );

            step++;
        }
    }
}
