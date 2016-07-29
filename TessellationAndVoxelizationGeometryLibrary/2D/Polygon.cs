using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TVGL._2D
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
        public bool IsPositive;

        /// <summary>
        /// Gets whether the path is CCW positive == not a hole.
        /// </summary>
        public bool IsConvex;

        internal Polygon()
        {
        }

        internal Polygon(IEnumerable<Point> points, bool isOpen = false)
        {
            Path = new List<Point>(points);
            IsOpen = isOpen;
            IsPositive = IsCCWPositive();
            IsConvex = IsThisConvex();
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
            var isPositive = IsCCWPositive();
            if (!isPositive) polygon.Reverse();
            Path = polygon;
        }

        /// <summary>
        /// Gets whether a Polygon is Positive CCW
        /// </summary>
        /// <assumptions>
        /// 1. the polygon is closed
        /// 2. the last point is not repeated.
        /// 3. the polygon is simple (does not intersect itself or have holes)
        /// </assumptions>
        /// <source>
        /// http://debian.fmi.uni-sofia.bg/~sergei/cgsr/docs/clockwise.htm
        /// </source>
        private bool IsCCWPositive()
        {
            var n = Path.Count;
            var count = 0;

            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                var k = (i + 2) % n;
                var z = (Path[j].X - Path[i].X) * (Path[k].Y - Path[j].Y);
                z -= (Path[j].Y - Path[i].Y) * (Path[k].X - Path[j].X);
                if (z < 0)
                    count--;
                else if (z > 0)
                    count++;
            }
            //The polygon has a CW winding if count is negative
            return count >= 0;
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
            throw new Exception("Concavity could not be determined. May be due to colinear points. Add functionality to code to account for this.");
        }

        private bool IsSelfIntersecting()
        {
            var successful = false;
            var attempts = 0;
            while (successful == false && attempts < 4)
            {
                try
                {
                    //Change point X and Y coordinates to be changed to mostly random primary axis
                    //Removed random value to make function repeatable for debugging.
                    var values = new List<double>() {0.82348, 0.13905, 0.78932, 0.37510};
                    var theta = values[attempts - 1];

                    var pHighest = double.NegativeInfinity;
                    foreach (var point in Path)
                    {
                        point.X = point.X*Math.Cos(theta) - point.Y*Math.Sin(theta);
                        point.Y = point.X*Math.Sin(theta) + point.Y*Math.Cos(theta);
                        if (point.Y > pHighest)
                        {
                            pHighest = point.Y;
                        }
                    }


                    successful = true;
                }
                catch
                {
                    attempts ++;
                }
            }
            if(!successful) throw new Exception("Failed to determine if polygon is self intersecting");


            var linesInLoops = new List<List<TVGL.Line>>();
        }
    }
}


