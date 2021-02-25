using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

public class VoronoiDiagram
{
    private const float eps = 0.0001f;
    private const float eps2 = eps * eps;

    public interface IBuildInfo
    {
        public void SendState(IEnumerable<Border> borders, IEnumerable<BuildCircle> circles, IEnumerable<BuildArc> arcs, IEnumerable<Dot> pointsOfInterest, float directrixY);
    }

    public static float AdEps(float v1, float v2)
    {
        return MathF.Abs( v1 + v2 ) * eps;
    }

    public static List<Border> ApplyBoundingBox(List<Border> borders, BoundingBox box)
    {
        List<Border> outBorders;
        outBorders = LineSplit( new VoronoiDiagram.Line() { dx = 1, c = -box.x }, borders );
        outBorders = LineSplit( new VoronoiDiagram.Line() { dx = -1, c = box.x + box.sizeX }, outBorders );
        outBorders = LineSplit( new VoronoiDiagram.Line() { dy = 1, c = -box.y }, outBorders );
        outBorders = LineSplit( new VoronoiDiagram.Line() { dy = -1, c = box.y + box.sizeY }, outBorders );
        //return outBorders;
        return outBorders.Where( x => x.begin != null && x.end != null ).ToList();
    }

    private class BeachLine
    {
        public void SearchBeachPart(float x)
        {

        }
    }

    public static List<Border> CreateEdges(IEnumerable<Dot> dots, IBuildInfo buildInfo = null)
    {
        LinkedList<Event> events = new LinkedList<Event>( dots.OrderBy( dot => dot, new DotPreciceComparerY() ).Distinct().Select( dot => new Event( dot ) ) );

        LinkedList<Dot> arcs = new LinkedList<Dot>();
        List<Corner> corners = new List<Corner>();
        Dictionary<LinkedListNode<Dot>, LinkedListNode<Event>> circleEvents = new Dictionary<LinkedListNode<Dot>, LinkedListNode<Event>>();
        List<Border> borders = new List<Border>();
        BorderSrch borderSrch = new BorderSrch();

        Border border;

        float startDirectrix = events.First.Value.dot.y;
        float prevDirectrix = startDirectrix;

        while (events.Count > 0) {
            Event simpleEvent = events.First.Value;
            events.RemoveFirst();

            if (!SameValue( simpleEvent.directrix, prevDirectrix )) {
                // Recalc directrix
                corners.Clear();
                for (var currentArcNode = arcs.First;
                    currentArcNode != null;
                    currentArcNode = currentArcNode.Next) {
                    if (currentArcNode.Previous != null) {
                        int intersectIndex = currentArcNode.Previous.Value.y < currentArcNode.Value.y ? 0 : 1;
                        corners.Add( new Corner() {
                            dot = GetParabolasIntersect(
                                currentArcNode.Previous.Value,
                                currentArcNode.Value,
                                simpleEvent.directrix )[intersectIndex],
                            leftArcNode = currentArcNode.Previous,
                            rightArcNode = currentArcNode
                        } );
                    }
                }
                prevDirectrix = simpleEvent.directrix;
            }

            if (buildInfo != null) {
                LogBuildInfo( buildInfo, corners, circleEvents, borders, simpleEvent, dots );
            }

            switch (simpleEvent.eventType) {
                case Event.EventType.Dot:

                    if (SameValue( simpleEvent.directrix, startDirectrix )) {
                        // This is first dot and dots with same Y coord
                        if (arcs.Count == 0) {
                            arcs.AddFirst( simpleEvent.dot );
                        } else {
                            LinkedListNode<Dot> arcNode;
                            for (arcNode = arcs.First;
                                arcNode != null && arcNode.Value.x < simpleEvent.dot.x;
                                arcNode = arcNode.Next) { }
                            if (arcNode == null) {
                                border = new Border() { siteLeft = arcs.Last.Value, siteRight = simpleEvent.dot };
                                borders.Add( border );
                                borderSrch.Add( border );
                                arcs.AddLast( simpleEvent.dot );
                            } else {
                                border = new Border() { siteLeft = simpleEvent.dot, siteRight = arcNode.Value };
                                borders.Add( border );
                                borderSrch.Add( border );
                                arcs.AddBefore( arcNode, simpleEvent.dot );
                            }
                        }
                        // We don't adding any circles here, cause three dots on one line can't lie on same circle 

                    } else {
                        // This is not first dot and at list one arc with non-zero focus already exists
                        bool cornerFound = false;
                        for (int cornerId = 0; cornerId < corners.Count; cornerId++) {
                            Corner corner = corners[cornerId];
                            if (SameValue( corner.dot.x, simpleEvent.dot.x )) {
                                RemoveCircle( corner.leftArcNode, events, circleEvents );
                                RemoveCircle( corner.rightArcNode, events, circleEvents );

                                border = borderSrch.Search( corner.leftArcNode.Value, corner.rightArcNode.Value );
                                if (border.siteLeft.Equals( corner.leftArcNode.Value )) {
                                    border.end = corner.dot;
                                } else {
                                    border.begin = corner.dot;
                                }

                                border = new Border() {
                                    siteLeft = corner.leftArcNode.Value,
                                    siteRight = simpleEvent.dot,
                                    begin = corner.dot
                                };
                                borders.Add( border );
                                borderSrch.Add( border );

                                border = new Border() {
                                    siteLeft = simpleEvent.dot,
                                    siteRight = corner.rightArcNode.Value,
                                    begin = corner.dot
                                };
                                borders.Add( border );
                                borderSrch.Add( border );

                                var newArc = arcs.AddAfter( corner.leftArcNode, simpleEvent.dot );

                                corners[cornerId] = new Corner() {
                                    dot = corner.dot,
                                    leftArcNode = corner.leftArcNode,
                                    rightArcNode = newArc
                                };
                                corners.Insert( cornerId + 1, new Corner {
                                    dot = corner.dot,
                                    leftArcNode = newArc,
                                    rightArcNode = corner.rightArcNode
                                } );

                                // We don't insert circle for new arc, cause we just processed it
                                if (cornerId > 0) {
                                    InsertCircleIfPossible( corners[cornerId - 1], corners[cornerId], simpleEvent, events, circleEvents );
                                }
                                if (cornerId + 2 < corners.Count) {
                                    InsertCircleIfPossible( corners[cornerId + 1], corners[cornerId + 2], simpleEvent, events, circleEvents );
                                }

                                cornerFound = true;
                                break;

                            } else if (corner.dot.x > simpleEvent.dot.x) {
                                RemoveCircle( corner.leftArcNode, events, circleEvents );

                                border = new Border() { siteLeft = corner.leftArcNode.Value, siteRight = simpleEvent.dot };
                                borders.Add( border );
                                borderSrch.Add( border );

                                Dot cornerDot = GetParabolaCoord( corner.leftArcNode.Value, simpleEvent.directrix, simpleEvent.dot.x );

                                var newLeftArc = arcs.AddBefore( corner.leftArcNode, corner.leftArcNode.Value );
                                arcs.AddBefore( corner.leftArcNode, simpleEvent.dot );

                                if (cornerId > 0) {
                                    var replaceCorner = corners[cornerId - 1];
                                    replaceCorner.rightArcNode = newLeftArc;
                                    corners[cornerId - 1] = replaceCorner;
                                }
                                corners.InsertRange( cornerId, new Corner[2] {
                                    new Corner(){
                                        dot = cornerDot,
                                        leftArcNode = corner.leftArcNode.Previous.Previous,
                                        rightArcNode = corner.leftArcNode.Previous },
                                    new Corner(){
                                        dot = cornerDot,
                                        leftArcNode = corner.leftArcNode.Previous,
                                        rightArcNode = corner.leftArcNode }
                                } );

                                // We don't insert circle for new arc, cause it intersect only two focus-points
                                if (cornerId > 0) {
                                    InsertCircleIfPossible( corners[cornerId - 1], corners[cornerId], simpleEvent, events, circleEvents );
                                }
                                if (cornerId + 2 < corners.Count) {
                                    InsertCircleIfPossible( corners[cornerId + 1], corners[cornerId + 2], simpleEvent, events, circleEvents );
                                }

                                cornerFound = true;
                                break;
                            }
                        }
                        if (!cornerFound) {
                            // No corners from right side
                            border = new Border() { siteLeft = arcs.Last.Value, siteRight = simpleEvent.dot };
                            borders.Add( border );
                            borderSrch.Add( border );

                            Dot cornerDot = GetParabolaCoord( arcs.Last.Value, simpleEvent.directrix, simpleEvent.dot.x );

                            arcs.AddLast( arcs.Last.Value );
                            arcs.AddBefore( arcs.Last, simpleEvent.dot );

                            corners.AddRange( new Corner[2] {
                                new Corner(){ dot = cornerDot, leftArcNode = arcs.Last.Previous.Previous, rightArcNode = arcs.Last.Previous },
                                new Corner(){ dot = cornerDot, leftArcNode = arcs.Last.Previous, rightArcNode = arcs.Last }
                            } );

                            // We don't insert circle for new arc, cause it intersect only two focus-points
                            // And we don't insert circle for right arc, cause it last
                            if (corners.Count >= 3) {
                                InsertCircleIfPossible( corners[^3], corners[^2], simpleEvent, events, circleEvents );
                            }
                        }
                    }
                    break;

                case Event.EventType.Circle:

                    Circle circle = SearchCircle( simpleEvent.circle.arc, circleEvents );

                    RemoveCircle( circle.arc, events, circleEvents );

                    var leftArcNode = circle.arc.Previous;
                    var rightArcNode = circle.arc.Next;

                    Border leftBorder = borderSrch.Search( leftArcNode.Value, circle.arc.Value );
                    Border rightBorder = borderSrch.Search( rightArcNode.Value, circle.arc.Value );

                    if (leftBorder.siteLeft.Equals( leftArcNode.Value )) {
                        leftBorder.end = simpleEvent.circle.center;
                    } else {
                        leftBorder.begin = simpleEvent.circle.center;
                    }
                    if (rightBorder.siteRight.Equals( rightArcNode.Value )) {
                        rightBorder.end = simpleEvent.circle.center;
                    } else {
                        rightBorder.begin = simpleEvent.circle.center;
                    }

                    border = new Border() { siteLeft = leftArcNode.Value, siteRight = rightArcNode.Value, begin = simpleEvent.circle.center };
                    borders.Add( border );
                    borderSrch.Add( border );

                    RemoveCircle( leftArcNode, events, circleEvents );
                    RemoveCircle( rightArcNode, events, circleEvents );

                    arcs.Remove( circle.arc );

                    // Merge corners
                    int leftCornerId = corners.FindIndex( x => x.leftArcNode == leftArcNode && x.rightArcNode == circle.arc );
                    Corner mergedCorner = new Corner() { leftArcNode = leftArcNode, rightArcNode = rightArcNode, dot = circle.center };
                    corners[leftCornerId] = mergedCorner;
                    corners.RemoveAt( leftCornerId + 1 );

                    // Add new circle events
                    if (leftCornerId > 0) {
                        InsertCircleIfPossible( corners[leftCornerId - 1], corners[leftCornerId], simpleEvent, events, circleEvents );
                    }
                    if (leftCornerId + 1 < corners.Count) {
                        InsertCircleIfPossible( corners[leftCornerId], corners[leftCornerId + 1], simpleEvent, events, circleEvents );
                    }

                    break;
            }
        }

        return borders.Where( x => x.begin == null || x.end == null || x.SqrLength() > eps ).ToList();
    }

    public static List<Border> LineSplit(Line line, List<Border> borders)
    {
        List<Border> newBorders = new List<Border>();
        var borderSrch = new AxisSrch<Border>();
        Dictionary<Dot, DotLineSurroundings> surroundings = new Dictionary<Dot, DotLineSurroundings>();

        foreach (Border border in borders) {
            Dot intersection;
            //todo if border on line
            try {
                intersection = LineIntersect( line, border.GetLine() );
            } catch {
                if (border.begin != null) {
                    float lineSide = line.WhereDot( border.begin.Value );
                    if (MathF.Abs( lineSide ) < AdEps( border.begin.Value.x, border.begin.Value.y )) {
                        // Just ignore border
                    } else if (lineSide > 0) {
                        newBorders.Add( border );
                    }
                } else if (border.end != null) {
                    float lineSide = line.WhereDot( border.end.Value );
                    if (MathF.Abs( lineSide ) < AdEps( border.end.Value.x, border.end.Value.y )) {
                        // Just ignore border
                    } else if (lineSide > 0) {
                        newBorders.Add( border );
                    }
                } else {

                    // todo what will we do with lines?
                }
                continue;
            }

            Line intLine = border.GetLine();
            // Cross product 
            float cross = line.dx * intLine.dy - line.dy * intLine.dx;

            if (border.begin != null && border.end != null) {

                float toBegin = DistanceSqr( intersection, border.begin.Value );
                float toEnd = DistanceSqr( intersection, border.end.Value );
                float edgeSize = DistanceSqr( border.begin.Value, border.end.Value );

                if (edgeSize - toBegin > -eps && edgeSize - toEnd > -eps) {

                    Border newBorder = new Border( border );
                    if (line.WhereDot( border.begin.Value ) > eps) {
                        newBorder.end = intersection;
                    } else {
                        newBorder.begin = intersection;
                    }

                    if (!newBorder.begin.Equals( newBorder.end )
                        && ( line.WhereDot( newBorder.begin.Value ) > eps
                            || line.WhereDot( newBorder.end.Value ) > eps )) {
                        newBorders.Add( newBorder );
                        AddOnLineBorders( line, borderSrch, surroundings, intersection, cross, newBorder );
                    }

                } else {
                    // Intersection isn't on edge
                    if (line.WhereDot( border.begin.Value ) > -eps) {
                        newBorders.Add( new Border( border ) );
                    } else {
                        // Edge in dead zone
                    }
                }
            } else {
                try {
                    Ray ray = border.GetRay();
                    bool rayBeginInZone = line.WhereDot( new Dot() { x = ray.x, y = ray.y } ) > 0;
                    bool rayDirectedToZone = line.WhereDot( new Dot() { x = intersection.x + ray.dx, y = intersection.y + ray.dy } ) > 0;

                    if (rayBeginInZone) {
                        if (!rayDirectedToZone) {
                            Border newBorder = new Border( border );
                            if (newBorder.begin == null) {
                                newBorder.begin = intersection;
                            } else {
                                newBorder.end = intersection;
                            }

                            if (!newBorder.begin.Equals( newBorder.end )) {
                                newBorders.Add( newBorder );
                                AddOnLineBorders( line, borderSrch, surroundings, intersection, cross, newBorder );
                            }

                        } else {
                            // Ray don't intersect line and must be save
                            newBorders.Add( new Border( border ) );
                        }
                    } else {
                        if (rayDirectedToZone) {
                            Border newBorder = new Border( border );
                            if (newBorder.begin != null) {
                                newBorder.begin = intersection;
                            } else {
                                newBorder.end = intersection;
                            }
                            newBorders.Add( newBorder );

                            AddOnLineBorders( line, borderSrch, surroundings, intersection, cross, newBorder );
                        } else {
                            // Ray don't intersect line
                        }
                    }
                } catch {
                    //this is line
                    Border newBorder = new Border( border );

                    if (cross > 0) {
                        newBorder.end = intersection;
                    } else {
                        newBorder.begin = intersection;
                    }
                    newBorders.Add( newBorder );

                    AddOnLineBorders( line, borderSrch, surroundings, intersection, cross, newBorder );
                }
            }
        }

        foreach (var kvpSurr in surroundings) {
            if (kvpSurr.Value.sideBorders.Count > 1) {

                LinkedList<Tuple<Dot, Dot>> leftRightLinks = new LinkedList<Tuple<Dot, Dot>>(
                    kvpSurr.Value.sideBorders.Select( x =>
                        x.end.Equals( kvpSurr.Value.dot )
                        ? new Tuple<Dot, Dot>( x.siteLeft, x.siteRight )
                        : new Tuple<Dot, Dot>( x.siteRight, x.siteLeft ) ) );
                Dot leftSite = leftRightLinks.First.Value.Item1;
                Dot rightSite = leftRightLinks.First.Value.Item2;
                leftRightLinks.RemoveFirst();
                while (leftRightLinks.Count > 0) {
                    bool someFound = false;
                    for (var linkNode = leftRightLinks.First; linkNode != null; linkNode = linkNode.Next) {
                        if (linkNode.Value.Item1.Equals( rightSite )) {
                            rightSite = linkNode.Value.Item2;
                            someFound = true;
                            leftRightLinks.Remove( linkNode );
                        } else if (linkNode.Value.Item2.Equals( leftSite )) {
                            leftSite = linkNode.Value.Item1;
                            someFound = true;
                            leftRightLinks.Remove( linkNode );
                        }
                    }
                    if (!someFound) {
                        throw new Exception( "Unconsistent dot surroundings" );
                    }
                }
                borderSrch.Search( kvpSurr.Value.axisVal ).siteRight = rightSite;
                borderSrch.SearchPrev( kvpSurr.Value.axisVal ).siteRight = leftSite;
            }
        }

        foreach (Border border in borderSrch.GetAll()) {
            // Create fake sites (it works cause dx and dy have specific scale)
            if (line.WhereDot( border.siteRight ) > 0) {
                border.siteLeft = GetOppositeDot( line, border.siteRight );
            } else {
                border.siteLeft = border.siteRight;
                border.siteRight = GetOppositeDot( line, border.siteRight );
            }
            newBorders.Add( border );
        }

        return newBorders;
    }

    public static float SqrLength(Dot dot1, Dot dot2)
    {
        float dx = dot1.x - dot2.x;
        float dy = dot1.y - dot2.y;
        return dx * dx + dy * dy;
    }

    private static void AddOnLineBorders(Line line, AxisSrch<Border> borderSrch, Dictionary<Dot, DotLineSurroundings> surroundings, Dot intersection, float cross, Border newBorder)
    {
        float axisIntersection;
        if (MathF.Abs( line.dx ) > MathF.Abs( line.dy )) {
            axisIntersection = intersection.y * line.dx;
        } else {
            axisIntersection = -intersection.x * line.dy;
        }

        Border border1, border2;
        if (borderSrch.Count == 0) {

            border1 = new Border() { begin = intersection };
            border2 = new Border() { end = intersection };

            if (cross < 0) {
                border1.siteRight = newBorder.siteRight;
                border2.siteRight = newBorder.siteLeft;
            } else {
                border1.siteRight = newBorder.siteLeft;
                border2.siteRight = newBorder.siteRight;
            }

            borderSrch.Add( axisIntersection, border1 );
            borderSrch.Add( float.NegativeInfinity, border2 );
        } else {
            border1 = borderSrch.Search( axisIntersection );

            if (!intersection.Equals( border1.end )) {
                border2 = new Border() { begin = intersection, end = border1.end };
                border1.end = intersection;
                if (cross > 0) {
                    border1.siteRight = newBorder.siteRight;
                    border2.siteRight = newBorder.siteLeft;
                } else {
                    border1.siteRight = newBorder.siteLeft;
                    border2.siteRight = newBorder.siteRight;
                }
                borderSrch.Add( axisIntersection, border2 );
            }
        }

        DotLineSurroundings dotSurr;
        if (!surroundings.TryGetValue( intersection, out dotSurr )) {
            dotSurr = new DotLineSurroundings();
            surroundings.Add( intersection, dotSurr );
            dotSurr.dot = intersection;
        }
        dotSurr.sideBorders.Add( newBorder );
    }

    private static float Distance(Dot dot1, Dot dot2)
    {
        return MathF.Sqrt( DistanceSqr( dot1, dot2 ) );
    }

    private static float DistanceSqr(Dot dot1, Dot dot2)
    {
        float dx = dot2.x - dot1.x;
        float dy = dot2.y - dot1.y;
        return dx * dx + dy * dy;
    }

    private static Dot Get3DotsCircleCenter(Dot v1, Dot v2, Dot v3)
    {
        float diff1 = v2.x - v1.x;
        float diff2 = v3.x - v2.x;
        float diff3 = v1.x - v3.x;
        Dot center;
        if (MathF.Abs( diff1 ) < eps) {
            if (MathF.Abs( diff2 ) < eps || MathF.Abs( diff3 ) < eps) {
                throw new Exception( "Dots on one line" );
            } else {
                return Get3DotsCircleCenter( v2, v3, v1 );
            }
        } else if (MathF.Abs( diff2 ) < eps) {
            if (MathF.Abs( diff3 ) < eps) {
                throw new Exception( "Dots on one line" );
            } else {
                return Get3DotsCircleCenter( v3, v1, v2 );
            }
        } else {
            float m1 = ( v2.y - v1.y ) / diff1;
            float m2 = ( v3.y - v2.y ) / diff2;
            if (MathF.Abs( m1 - m2 ) < eps) {
                throw new Exception( "Dots on one line" );
            }
            center.x = .5f * ( m1 * m2 * ( v1.y - v3.y ) + m2 * ( v1.x + v2.x ) - m1 * ( v2.x + v3.x ) ) / ( m2 - m1 );
            if (MathF.Abs( m1 ) > eps) {
                center.y = .5f * ( v1.y + v2.y ) - ( center.x - .5f * ( v1.x + v2.x ) ) / m1;
            } else {
                center.y = .5f * ( v2.y + v3.y ) - ( center.x - .5f * ( v2.x + v3.x ) ) / m2;
            }
            return center;
        }
    }

    private static Dot GetOppositeDot(Line line, Dot dot)
    {
        Dot result;
        if (MathF.Abs( line.dx ) > eps) {
            float k1 = line.dy * line.dy / line.dx;
            result.x = ( dot.x * ( line.dx - k1 ) + 2f * dot.y * line.dy + 2f * line.c ) / ( -line.dx - k1 );
            result.y = dot.y + ( result.x - dot.x ) * line.dy / line.dx;
        } else if (MathF.Abs( line.dy ) > eps) {
            float k1 = line.dx * line.dx / line.dy;
            result.y = ( dot.y * ( line.dy - k1 ) + 2f * dot.x * line.dx + 2f * line.c ) / ( -line.dy - k1 );
            result.x = dot.x + ( result.y - dot.y ) * line.dx / line.dy;
        } else {
            throw new Exception( "Bad line dx and dy" );
        }
        return result;
    }

    private static Dot GetParabolaCoord(Dot focus, float directrix, float line)
    {
        if (MathF.Abs( focus.y - directrix ) < eps) {
            throw new Exception( "Bad parabola parameters" );
        }
        Dot result = new Dot();
        result.x = line;
        result.y = .5f * ( ( line - focus.x ) * ( line - focus.x ) + focus.y * focus.y - directrix * directrix ) / ( focus.y - directrix );
        return result;
    }

    private static Dot[] GetParabolasIntersect(Dot v1, Dot v2, float directrix)
    {
        if (MathF.Abs( v1.y - directrix ) < AdEps( v1.y, directrix )) {
            Dot retDot = GetParabolaCoord( v2, directrix, v1.x );
            return new Dot[2] { retDot, retDot };
        }
        if (MathF.Abs( v2.y - directrix ) < AdEps( v2.y, directrix )) {
            Dot retDot = GetParabolaCoord( v1, directrix, v2.x );
            return new Dot[2] { retDot, retDot };
        }
        float divider1 = 1f / ( v1.y - directrix );
        float divider2 = 1f / ( v2.y - directrix );
        float a = divider1 - divider2;
        float b = 2f * ( v2.x * divider2 - v1.x * divider1 );
        float c = divider1 * ( v1.x * v1.x + v1.y * v1.y - directrix * directrix )
            - divider2 * ( v2.x * v2.x + v2.y * v2.y - directrix * directrix );
        Func<float, float, float, float, float> xFunc = (a, b, D, i) => .5f * ( -b + i * (float)Math.Sqrt( D ) ) / a;
        Func<float, Dot, float, float> yFunc = (x, dot, dir) =>
            .5f / ( dot.y - dir ) * ( ( x - dot.x ) * ( x - dot.x ) + dot.y * dot.y - dir * dir );
        if (MathF.Abs( a ) > AdEps( divider1, divider2 )) {
            float D = b * b - 4f * a * c;
            if (D < 0) {
                throw new Exception( $"Parabolas never itersect with v1={v1}, v2={v2} and directrix={directrix}" );
            }
            Dot intersect1, intersect2;

            intersect1.x = xFunc( a, b, D, 1 );
            intersect1.y = yFunc( intersect1.x, v1, directrix );
            intersect2.x = xFunc( a, b, D, -1 );
            intersect2.y = yFunc( intersect2.x, v1, directrix );
            if (intersect1.x < intersect2.x) {
                return new Dot[2] { intersect1, intersect2 };
            } else {
                return new Dot[2] { intersect2, intersect1 };
            }
        } else {
            Dot intersect;
            if (MathF.Abs( b ) < eps) {
                throw new Exception( $"Parabolas never itersect with v1={v1}, v2={v2} and directrix={directrix}" );
            }
            intersect.x = -c / b;
            intersect.y = yFunc( intersect.x, v1, directrix );
            return new Dot[2] { intersect, intersect };
        }
    }

    private static bool InsertCircleIfPossible(Corner leftCorner, Corner rightCorner, Event prevEvent, LinkedList<Event> events, Dictionary<LinkedListNode<Dot>, LinkedListNode<Event>> circleEvents)
    {
        LinkedListNode<Dot> arc = leftCorner.rightArcNode;
        if (arc != rightCorner.leftArcNode) {
            throw new Exception( "Bad corners" );
        }
        if (!arc.Previous.Value.Equals( arc.Next.Value )) {
            Circle circle;
            try {
                circle = new Circle( arc );
            } catch {
                return false;
            }
            if (( circle.v1.Equals( prevEvent.circle.v1 )
                    && circle.v2.Equals( prevEvent.circle.v3 )
                    && circle.v3.Equals( prevEvent.circle.v2 ) )
                || ( circle.v1.Equals( prevEvent.circle.v2 )
                    && circle.v2.Equals( prevEvent.circle.v1 )
                    && circle.v3.Equals( prevEvent.circle.v3 ) )) {
                // In specific condition when leftCorner and rightCorner too near, IsOverTangent may return incorrect result
                // I beleave we can avoid this problem with this check.
                return false;
            }
            // If circle center is over tangent of arc in coordinate of corner then corners of arc come together with directix increase. Else corners diverge and circle is false.
            if (IsOverTangent( circle.center, arc.Value, prevEvent.directrix, leftCorner.dot )
                && IsOverTangent( circle.center, arc.Value, prevEvent.directrix, rightCorner.dot )) {
                var newEvent = new Event() {
                    eventType = Event.EventType.Circle,
                    circle = circle,
                    directrix = circle.directrix
                };
                circleEvents.Add( arc, InsertEvent( events, newEvent ) );
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }

    private static LinkedListNode<Event> InsertEvent(LinkedList<Event> events, Event newEvent)
    {
        for (var currEvent = events.First; currEvent != null; currEvent = currEvent.Next) {
            if (currEvent.Value.directrix >= newEvent.directrix) {
                return events.AddBefore( currEvent, newEvent );
            }
        }
        return events.AddLast( newEvent );
    }

    private static bool IsOverTangent(Dot cmpDot, Dot arcFocus, float directrix, Dot arcDot)
    {
        // Derivative of prabola is y=ax+b
        float dy = arcFocus.y - directrix;
        float a = 1f / dy;
        float b = -arcFocus.x / dy;
        // Tangent of parabola in coord arcDot is y=kx+c
        float k = a * arcDot.x + b;
        float c = arcDot.y - k * arcDot.x;
        // Y coord of dot on tangent with same X coord as cmpDot
        float cmpDotY = k * cmpDot.x + c;
        return cmpDotY - cmpDot.y < AdEps( cmpDotY, cmpDot.y );
    }

    private static Dot LineIntersect(Line line1, Line line2)
    {

        if (MathF.Abs( line1.dy ) > eps) {
            float k1 = line1.dy * line2.dy;
            float k2 = line2.dx - k1 * line1.dx;
            if (MathF.Abs( k2 ) > eps) {
                float x = ( k1 * line1.c - line2.c ) / k2;
                return new Dot() { x = x, y = ( -line1.c - x * line1.dx ) / line1.dy };
            }
        }
        if (MathF.Abs( line2.dy ) > eps) {
            float k1 = line1.dy * line2.dy;
            float k2 = line1.dx - k1 * line2.dx;
            if (MathF.Abs( k2 ) > eps) {
                float x = ( k1 * line2.c - line1.c ) / k2;
                return new Dot() { x = x, y = ( -line2.c - x * line2.dx ) / line2.dy };
            }
        }
        throw new Exception( "Lines is parallel" );
    }

    private static void LogBuildInfo(IBuildInfo buildInfo, List<Corner> corners, Dictionary<LinkedListNode<Dot>, LinkedListNode<Event>> circleEvents, List<Border> borders, Event simpleEvent, IEnumerable<Dot> dots)
    {
        BuildCircle[] buildCircles = circleEvents.Values.Select( x =>
            new BuildCircle() {
                center = x.Value.circle.center,
                radius = x.Value.circle.directrix - x.Value.circle.center.y
            } ).ToArray();
        BuildArc[] buildArcs;
        if (corners.Count > 0) {
            buildArcs = new BuildArc[corners.Count + 1];
            for (int cornerId = 0; cornerId < corners.Count; cornerId++) {
                buildArcs[cornerId] = new BuildArc() {
                    focus = corners[cornerId].leftArcNode.Value,
                    leftX = cornerId > 0 ? corners[cornerId - 1].dot.x : -float.MaxValue,
                    rightX = corners[cornerId].dot.x,
                    directrixY = simpleEvent.directrix
                };
            }
            buildArcs[corners.Count] = new BuildArc() {
                focus = corners[corners.Count - 1].rightArcNode.Value,
                leftX = corners[corners.Count - 1].dot.x,
                rightX = float.MaxValue,
                directrixY = simpleEvent.directrix
            };
        } else {
            buildArcs = new BuildArc[0];
        }
        List<Dot> interestPoints = new List<Dot>();
        switch (simpleEvent.eventType) {
            case Event.EventType.Dot:
                interestPoints.Add( simpleEvent.dot );
                break;
            case Event.EventType.Circle:
                interestPoints.Add( simpleEvent.circle.center );
                interestPoints.Add( simpleEvent.circle.v1 );
                interestPoints.Add( simpleEvent.circle.v2 );
                interestPoints.Add( simpleEvent.circle.v3 );
                break;
        }
        foreach (var dot in dots) {
            interestPoints.Add( dot );
        }
        buildInfo.SendState( borders, buildCircles, buildArcs, interestPoints, simpleEvent.directrix );
    }

    private static void RemoveCircle(LinkedListNode<Dot> arc, LinkedList<Event> events, Dictionary<LinkedListNode<Dot>, LinkedListNode<Event>> circleEvents)
    {
        LinkedListNode<Event> eventNode;
        if (circleEvents.TryGetValue( arc, out eventNode )) {
            try {
                events.Remove( eventNode );
            } catch (InvalidOperationException) { }
            circleEvents.Remove( arc );
        }
    }

    private static bool SameValue(float a, float b)
    {
        return MathF.Abs( a - b ) < eps;
    }

    private static Circle SearchCircle(LinkedListNode<Dot> arc, Dictionary<LinkedListNode<Dot>, LinkedListNode<Event>> circleEvents)
    {
        return circleEvents[arc].Value.circle;
    }

    public struct AxisSrchNode<StoreType>
    {
        public float axisVal;
        public StoreType value;
    }

    public struct BoundingBox
    {
        /// <summary>
        /// (X;Y) - coord of left bottom corner
        /// </summary>
        public float x, y, sizeX, sizeY;
    }

    public struct BuildArc
    {
        public Dot focus;
        public float leftX, rightX, directrixY;
    }

    public struct BuildCircle
    {
        public Dot center;
        public float radius;
    }
    public struct Corner
    {
        public Dot dot;
        public LinkedListNode<Dot> leftArcNode, rightArcNode;
    }

    public struct Dot : IEquatable<Dot>
    {
        public float x, y;

        bool IEquatable<Dot>.Equals(Dot other)
        {
            float dx = other.x - x;
            float dy = other.y - y;
            return ( dx * dx + dy * dy ) < eps2;
        }
        public override string ToString()
        {
            return $"({x};{y})";
        }
    }
    /// <summary>
    /// Storage for line with formula dx * x + dy * y + c
    /// </summary>
    public struct Line
    {
        public float dx, dy, c;
        public float WhereDot(Dot dot)
        {
            return dx * dot.x + dy * dot.y + c;
        }
    }

    public struct Ray
    {
        public float dx, dy;
        public float x, y;
    }

    private struct Circle
    {
        public LinkedListNode<Dot> arc;
        public Dot center;
        public float directrix;
        public Dot v1, v2, v3;
        public Circle(Dot v1, Dot v2, Dot v3, LinkedListNode<Dot> arc)
        {
            this.arc = arc;
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
            center = Get3DotsCircleCenter( v1, v2, v3 );
            directrix = Distance( center, v1 );
        }

        public Circle(LinkedListNode<Dot> arc)
        {
            if (arc.Next == null || arc.Previous == null) {
                throw new Exception( "Can't create circle for arc" );
            }
            this.arc = arc;
            v1 = arc.Previous.Value;
            v2 = arc.Value;
            v3 = arc.Next.Value;
            center = Get3DotsCircleCenter( v1, v2, v3 );
            directrix = center.y + Distance( center, v1 );
        }
    }

    private struct Event
    {
        public Circle circle;

        public float directrix;

        public Dot dot;

        public EventType eventType;

        public Event(Dot dot)
        {
            eventType = EventType.Dot;
            this.dot = dot;
            directrix = dot.y;
            circle = new Circle();
        }

        public enum EventType
        {
            Dot,
            Circle
        }
    }

    public class AxisSrch<StoreType>
    {
        private LinkedList<AxisSrchNode<StoreType>> storage = new LinkedList<AxisSrchNode<StoreType>>();
        public int Count { get => storage.Count; }
        public void Add(float axisVal, StoreType value)
        {
            var node = SearchNode( axisVal );
            var srchNode = new AxisSrchNode<StoreType>() { axisVal = axisVal, value = value };
            if (node != null) {
                storage.AddAfter( node, srchNode );
            } else {
                storage.AddFirst( srchNode );
            }
        }

        public IEnumerable<StoreType> GetAll()
        {
            return storage.Select( x => x.value );
        }

        public StoreType Search(float axisValue)
        {
            var node = SearchNode( axisValue );

            if (node != null) {
                return node.Value.value;
            } else {
                throw new Exception( "Value not found" );
            }
        }
        public StoreType SearchPrev(float axisValue)
        {
            var node = SearchNode( axisValue );

            if (node != null && node.Previous != null) {
                return node.Previous.Value.value;
            } else {
                throw new Exception( "Value not found" );
            }
        }
        private LinkedListNode<AxisSrchNode<StoreType>> SearchNode(float axisValue)
        {
            for (var node = storage.First; node != null; node = node.Next) {
                if (eps > axisValue - node.Value.axisVal) {
                    return node.Previous;
                }
            }
            return storage.Last;
        }
    }

    public class Border
    {
        public Dot? begin, end;
        public Dot siteLeft, siteRight;
        public Border()
        {

        }
        public Border(Border other) : base()
        {
            siteLeft = other.siteLeft;
            siteRight = other.siteRight;
            begin = other.begin;
            end = other.end;
        }
        public Line GetLine()
        {
            return new Line() {
                dx = 2f * ( siteLeft.x - siteRight.x ),
                dy = 2f * ( siteLeft.y - siteRight.y ),
                c = siteRight.x * siteRight.x + siteRight.y * siteRight.y - siteLeft.x * siteLeft.x - siteLeft.y * siteLeft.y
            };
        }

        public Ray GetRay()
        {
            Ray ray;
            float dx = siteLeft.x - siteRight.x;
            float dy = siteLeft.y - siteRight.y;

            if (begin != null) {
                if (end != null) {
                    throw new Exception( "Can't convert edge into ray" );
                }
                ray.x = begin.Value.x;
                ray.y = begin.Value.y;
                ray.dx = dy;
                ray.dy = -dx;
            } else if (end != null) {
                ray.x = end.Value.x;
                ray.y = end.Value.y;
                ray.dx = -dy;
                ray.dy = dx;
            } else {
                throw new Exception( "Can't convert line into ray" );
            }
            return ray;
        }
        public float Length()
        {
            return (float)Math.Sqrt( SqrLength() );
        }

        public float SqrLength()
        {
            if (begin == null || end == null) {
                throw new Exception( "This is not edge" );
            }
            return VoronoiDiagram.SqrLength( begin.Value, end.Value );
        }
    }

    public class DotComparerYPriority : IComparer<Dot>
    {
        int IComparer<Dot>.Compare(Dot d1, Dot d2)
        {
            if (MathF.Abs( d1.y - d2.y ) < AdEps( d1.y, d2.y )) {
                if (MathF.Abs( d1.x - d2.x ) < AdEps( d1.x, d2.x )) {
                    return 0;
                } else {
                    return d1.x > d2.x ? 1 : -1;
                }
            } else {
                return d1.y > d2.y ? 1 : -1;
            }
        }
    }
    public class DotPreciceComparerY : IComparer<Dot>
    {
        public int Compare(Dot d1, Dot d2)
        {
            if (d1.y == d2.y) {
                return d1.x > d2.x ? 1 : -1;
            } else {
                return d1.y > d2.y ? 1 : -1;
            }
        }
    }
    private class BorderSrch
    {
        private readonly Dictionary<Navigation, Border> srch = new Dictionary<Navigation, Border>();

        public void Add(Border border) => srch.Add( new Navigation( border.siteLeft, border.siteRight ), border );

        public Border Search(Dot site1, Dot site2)
        {
            Border border;
            if (srch.TryGetValue( new Navigation( site1, site2 ), out border )
                || srch.TryGetValue( new Navigation( site2, site1 ), out border )) {
                return border;
            } else {
                throw new Exception( "Border not found" );
            }
        }

        private struct Navigation
        {
            public Dot right, left;
            public Navigation(Dot left, Dot right)
            {
                this.left = left;
                this.right = right;
            }
        }
    }

    private class DotLineSurroundings
    {
        public float axisVal;
        public Dot dot;
        public List<Border> sideBorders = new List<Border>();
    }
}
