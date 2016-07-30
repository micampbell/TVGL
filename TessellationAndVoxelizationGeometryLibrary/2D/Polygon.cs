using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// A list of 2D points
    /// </summary>
    public class Polygon
    {
        /// <summary>
        /// The list of 2D points that make up a polygon.
        /// </summary>
        public IList<Point> Path;
        /// <summary>
        /// A list of the polygons inside this polygon.
        /// </summary>
        public List<Polygon> Childern;
        /// <summary>
        /// The polygon that this polygon is inside of.
        /// </summary>
        public Polygon Parent;
        /// <summary>
        /// The index of this child in its parent's child list.
        /// </summary>
        public int Index;
        /// <summary>
        /// Gets whether the polygon has an open path.
        /// </summary>
        public readonly bool IsOpen;

        /// <summary>
        /// Gets whether the path is CCW positive == not a hole.
        /// </summary>
        public bool IsPositive => !(Area < 0);

        /// <summary>
        /// Gets whether the path is CCW positive == not a hole.
        /// </summary>
        public bool IsConvex;

        /// <summary>
        /// Gets the area of the polygon. Negative Area for holes.
        /// </summary>
        public double Area;

        /// <summary>
        /// Gets whether the path is self intersecting.
        /// </summary>
        public bool IsSelfIntersecting;

        internal Polygon()
        {
        }

        internal Polygon(IEnumerable<Point> points, bool isOpen = false)
        {
            Path = new List<Point>(points);
            IsOpen = isOpen;
            IsConvex = IsThisConvex();
            IsSelfIntersecting = IsThisSelfIntersecting();
            Area = CalculateArea();
        }

        //Gets whether this polygon is a hole, based on its position
        //In the polygon tree. 
        //ToDo: Confirm this function, since mine is opposite from Clipper for some reason.
        internal bool IsHole()
        {
            var result = false;
            var parent = Parent;
            while (parent != null)
            {
                result = !result;
                parent = parent.Parent;
            }
            //If it has no parent, then it must be NOT be a hole
            return result;
        }

        internal void AddChild(Polygon child)
        {
            var count = Childern.Count;
            Childern.Add(child);
            child.Parent = this;
            child.Index = count;
        }

        /// <summary>
        /// Sets a polygon to counter clock wise positive
        /// </summary>
        public void SetToCCWPositive()
        {
            var polygon = new List<Point>(Path);
            if (!IsPositive) polygon.Reverse();
            Path = polygon;
            //Invert the area.
            Area = -Area;
        }


        private double CalculateArea()
        {
            return MiscFunctions.AreaOfPolygon(Path.ToArray());
        }

        /// <summary>
        /// Gets whether the polygon is convex.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <assumptions>
        /// 1. the polygon is closed
        /// 2. the last point is not repeated.
        /// 3. the polygon is simple (does not intersect itself or have holes)
        /// </assumptions>
        /// /// <source>
        /// http://debian.fmi.uni-sofia.bg/~sergei/cgsr/docs/clockwise.htm
        /// </source>
        private bool IsThisConvex()
        {
            var n = Path.Count;
            var flag = 0;

            if (n < 3) return false;

            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                var k = (i + 2) % n;
                var z = (Path[j].X - Path[i].X) * (Path[k].Y - Path[j].Y);
                z -= (Path[j].Y - Path[i].Y) * (Path[k].X - Path[j].X);
                if (z < 0)
                    flag |= 1;
                else if (z > 0)
                    flag |= 2;
                if (flag == 3)
                    return (true);
            }
            if (flag != 0)
            {
                return (false);
            }
            return false;
           // throw new Exception("Concavity could not be determined. May be due to colinear points. Add functionality to code to account for this.");
        }

        /// <summary>
        /// Returns a list of lines that make up the path of this polygon
        /// </summary>
        /// <returns></returns>
        public List<Line> PathLines()
        {
            var lines = new List<Line>();
            var n = Path.Count;
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                lines.Add(new Line(Path[i], Path[j], true));
            }
            return lines;
        }

        private bool IsThisSelfIntersecting()
        {
            var orderedLoop = Path;
            const int precision = 15;
            var sortedPoints = orderedLoop.OrderBy(point => Math.Round(point.Y, precision)).ThenBy(point => Math.Round(point.X, precision)).ToList();

            //inititallize lineList 
            var lineList = new HashSet<Line>();
            for (var i = 0; i < sortedPoints.Count; i++)
            {
                //Add to or remove lines from Red-Black Tree
                foreach (var line in sortedPoints[i].Lines)
                {
                    if (lineList.Contains(line))
                    {
                        lineList.Remove(line);
                    }
                    else
                    {
                        lineList.Add(line);
                    }
                }
                //Create a second lineList in a perpendicular direction
                var unsortedPointSet2 = new HashSet<Point>();
                foreach (var line in lineList)
                {
                    if (!unsortedPointSet2.Contains(line.ToPoint)) unsortedPointSet2.Add(line.ToPoint);
                    if (!unsortedPointSet2.Contains(line.FromPoint)) unsortedPointSet2.Add(line.FromPoint);
                }
                var sortedPointSet2 =
                    unsortedPointSet2.OrderBy(point => Math.Round(point.X, precision))
                        .ThenBy(point => Math.Round(point.Y, precision))
                        .ToList();
                var lineList2 = new HashSet<Line>();
                for (var j = 0; j < sortedPointSet2.Count; j++)
                {
                    //Add to or remove lines from Red-Black Tree
                    foreach (var line in sortedPointSet2[j].Lines.Where(line => lineList.Contains(line)))
                    {
                        if (lineList2.Contains(line))
                        {
                            lineList2.Remove(line);
                        }
                        else
                        {
                            lineList2.Add(line);
                        }
                    }

                    //Check if any of the lines are crossing 
                    foreach (var line1 in lineList2)
                    {
                        foreach (var line2 in lineList2)
                        {
                            if (line2 == line1) continue;
                            //Only consider them, if they don't share any points.
                            if (line1.FromPoint == line2.FromPoint || line1.ToPoint == line2.FromPoint ||
                                line1.FromPoint == line2.ToPoint || line1.ToPoint == line2.ToPoint) continue;
                            Point insectionPoint;
                            var linesIntersect = LineLineIntersection(line1, line2, out insectionPoint);
                            if (linesIntersect && !insectionPoint.Equals(line1.ToPoint) && !insectionPoint.Equals(line1.FromPoint) &&
                                !insectionPoint.Equals(line2.ToPoint) && !insectionPoint.Equals(line2.FromPoint)) return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Detemines if Two Lines intersect. Outputs intersection point if they do.
        /// If two lines are colinear, they are not considered intersecting.
        /// </summary>
        /// <param name="line1"></param>
        /// <param name="line2"></param>
        /// <param name="intersectionPoint"></param>
        /// <param name="considerCollinearOverlapAsIntersect"></param>
        /// <source>
        /// http://www.codeproject.com/Tips/862988/Find-the-Intersection-Point-of-Two-Line-Segments
        /// </source>
        /// <returns></returns>
        public bool LineLineIntersection(Line line1, Line line2, out Point intersectionPoint, bool considerCollinearOverlapAsIntersect = false)
        {
            var p = line1.FromPoint.Position2D;
            var p2 = line1.ToPoint.Position2D;
            var q = line2.FromPoint.Position2D;
            var q2 = line2.ToPoint.Position2D;
            intersectionPoint = null;
            var r = p2.subtract(p);
            var s = q2.subtract(q);
            var rxs = r[0]*s[1]-r[1]*s[0]; //2D cross product
            var qp = q.subtract(p);
            var qpxr = qp[0]*r[1] - qp[1] * r[0];//2D cross product

            // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
            if (rxs.IsNegligible() && qpxr.IsNegligible())
            {
                // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
                // then the two lines are overlapping,
                // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
                // then the two lines are collinear but disjoint.
                if (!considerCollinearOverlapAsIntersect) return false;
                var qpr = qp[0]*r[0] + qp[1]*r[1];
                var pqs = p.subtract(q)[0] * s[0] + p.subtract(q)[1] * s[1];
                return (0 <= qpr && qpr <= r[0] * r[0] + r[1] * r[1]) || (0 <= pqs && pqs <= s[0] * s[0] + s[1] + s[1]);
            }

            // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
            if (rxs.IsNegligible() && !qpxr.IsNegligible()) return false;

            // t = (q - p) x s / (r x s)
            //Note, the output of this will be t = [0,0,#] since this is a 2D cross product.
            var t = q.subtract(p).crossProduct(s).divide(rxs);

            // u = (q - p) x r / (r x s)
            //Note, the output of this will be u = [0,0,#] since this is a 2D cross product.
            var u = q.subtract(p).crossProduct(r).divide(rxs);

            // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
            // the two line segments meet at the point p + t r = q + u s.
            if (!rxs.IsNegligible() && (0 <= t[2] && t[2] <= 1) && (0 <= u[2] && u[2] <= 1))
            {
                // We can calculate the intersection point using either t or u.
                var x = p[0] + t[0] * r[0];
                var y = p[1] + t[1] * r[1];
                intersectionPoint = new Point(x, y);

                // An intersection was found.
                return true;
            }

            // 5. Otherwise, the two line segments are not parallel but do not intersect.
            return false;
        }
    }
}


