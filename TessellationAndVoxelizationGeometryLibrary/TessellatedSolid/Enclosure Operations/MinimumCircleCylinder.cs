﻿// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-17-2015
//
// Last Modified By : Matt
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="ConvexHull2D.cs" company="Design Engineering Lab">
//     Copyright ?  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     The MinimumEnclosure class includes static functions for defining smallest enclosures for a
    ///     tessellated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {
        /// <summary>
        ///     Finds the minimum bounding circle
        /// </summary>
        public static BoundingCircle MinimumCircle(IList<Point> points)
        {
            return MinimumCircle(points.Select(p => new PointLight(p)).ToList());
        }

        /// <summary>
        ///     Finds the minimum bounding circle
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception">Bounding circle failed to converge</exception>
        /// <references>
        ///     Based on Emo Welzl's "move-to-front heuristic" and this paper (algorithm 1).
        ///     http://www.inf.ethz.ch/personal/gaertner/texts/own_work/esa99_final.pdf
        ///     This algorithm runs in near linear time. Visiting most points just a few times.
        ///     Though a linear algorithm was found by Meggi do, this algorithm is more robust
        ///     (doesn't care about multiple points on a line and fewer rounding functions)
        ///     and directly applicable to multiple dimensions (in our case, just 2 and 3 D).
        /// </references>
        public static BoundingCircle MinimumCircle(IList<PointLight> points)
        {
            #region Algorithm 1

            ////Randomize the list of points
            //var r = new Random();
            //var randomPoints = new List<PointLight>(points.OrderBy(p => r.Next()));

            //if (randomPoints.Count < 2) return new BoundingCircle(0.0, points[0]);
            ////Get any two points in the list points.
            //var point1 = randomPoints[0];
            //var point2 = randomPoints[1];
            //var previousPoints = new HashSet<PointLight>();
            //var circle = new InternalCircle(point1, point2);
            //var stallCounter = 0;
            //var i = 0;

            //while (i < randomPoints.Count && stallCounter < points.Count * 2)
            //{
            //    var currentPoint = randomPoints[i];
            //    //If the current point is part of the circle or inside the circle, go to the next iteration
            //    if (circle.Point0.Equals(currentPoint) ||
            //        circle.Point1.Equals(currentPoint) || 
            //        circle.Point2.Equals(currentPoint) || 
            //        circle.IsPointInsideCircle(currentPoint))
            //    {
            //        i++;
            //        continue;
            //    }

            //    //Else if the currentPoint is a previousPoint, increase dimension
            //    if (previousPoints.Contains(currentPoint))
            //    {
            //        //Make a new circle from the current two-point circle and the current point
            //        circle = new InternalCircle(circle.Point0, circle.Point1, currentPoint);
            //        previousPoints.Remove(currentPoint);
            //        i++;
            //    }
            //    else
            //    {
            //        //Find the point in the circle furthest from new point. 
            //        circle.Furthest(currentPoint, out var furthestPoint, ref previousPoints);
            //        //Make a new circle from the furthest point and current point
            //        circle = new InternalCircle(currentPoint, furthestPoint);
            //        //Add previousPoints to the front of the list
            //        foreach (var previousPoint in previousPoints)
            //        {
            //            randomPoints.Remove(previousPoint);
            //            randomPoints.Insert(0, previousPoint);
            //        }
            //        //Restart the search
            //        stallCounter++;
            //        i = 0;
            //    }
            //}

            #endregion

            #region Algorithm 2: Furthest Point
            //var r = new Random();
            //var randomPoints = new List<PointLight>(points.OrderBy(p => r.Next()));

            //Algorithm 2
            //I tried using the extremes (X, Y, and also tried Sum, Diff) to do a first pass at the circle
            //or to define the starting circle for max dX or dY, but all of these were slower do to the extra
            //for loop at the onset. The current approach is faster and simpler; just start with some arbitrary points.
            var circle = new InternalCircle(points[0], points[points.Count / 2]);
            var dummyPoint = new PointLight(double.NaN, double.NaN);
            var stallCounter = 0;
            var successful = false;
            var stallLimit = points.Count * 1.5;
            if (stallLimit < 100) stallLimit = 100;
            var centerX = circle.CenterX;
            var centerY = circle.CenterY;
            var sqTolerance = Math.Sqrt(Constants.BaseTolerance);
            var sqRadiusPlusTolerance = circle.SqRadius + sqTolerance;
            var nextPoint = dummyPoint;
            var priorRadius = circle.SqRadius;
            while (!successful && stallCounter < stallLimit)
            {
                //If stallCounter is getting big, add a bit extra to the circle radius to ensure convergence
                if (stallCounter > stallLimit / 2)
                    sqRadiusPlusTolerance = sqRadiusPlusTolerance + Constants.BaseTolerance * sqRadiusPlusTolerance;
                //Add it a second time if stallCounter is even bigger
                if (stallCounter > stallLimit * 2 / 3)
                    sqRadiusPlusTolerance = sqRadiusPlusTolerance + Constants.BaseTolerance * sqRadiusPlusTolerance;

                //Find the furthest point from the center point
                var maxDistancePlusTolerance = sqRadiusPlusTolerance;
                var nextPointIsSet = false;
                foreach (var point in points)
                {
                    var dx = centerX - point.X;
                    var dy = centerY - point.Y;
                    var squareDistanceToPoint = dx * dx + dy * dy;
                    //If the square distance is less than or equal to the max square distance, continue.
                    if (squareDistanceToPoint < maxDistancePlusTolerance) continue;
                    //Otherwise, set this as the next point to go to.
                    maxDistancePlusTolerance = squareDistanceToPoint + sqTolerance;
                    nextPoint = point;
                    nextPointIsSet = true;
                }
                if (!nextPointIsSet)
                {
                    successful = true;
                    continue;
                }

                //Create a new circle with 2 points
                //Find the point in the circle furthest from new point.
                circle.Furthest(nextPoint, out var furthestPoint, out var previousPoint1,
                    out var previousPoint2, out var numPreviousPoints);
                //Make a new circle from the furthest point and current point
                circle = new InternalCircle(nextPoint, furthestPoint);
                centerX = circle.CenterX;
                centerY = circle.CenterY;
                sqRadiusPlusTolerance = circle.SqRadius + sqTolerance;

                //Now check if the previous points are outside this circle.
                //To be outside the circle, it must be further out than the specified tolerance. 
                //Otherwise, the loop can get caught in a loop due to rounding error 
                //If you wanted to use a tighter tolerance, you would need to take the square roots to get the radius.   
                //If they are, increase the dimension and use three points in a circle
                var dxP1 = centerX - previousPoint1.X;
                var dyP1 = centerY - previousPoint1.Y;
                var squareDistanceP1 = dxP1 * dxP1 + dyP1 * dyP1;
                if (squareDistanceP1 > sqRadiusPlusTolerance)
                {
                    //Make a new circle from the current two-point circle and the current point
                    circle = new InternalCircle(circle.Point0, circle.Point1, previousPoint1);
                    centerX = circle.CenterX;
                    centerY = circle.CenterY;
                    sqRadiusPlusTolerance = circle.SqRadius + sqTolerance;
                }
                else if (numPreviousPoints == 2)
                {
                    var dxP2 = centerX - previousPoint2.X;
                    var dyP2 = centerY - previousPoint2.Y;
                    var squareDistanceP2 = dxP2 * dxP2 + dyP2 * dyP2;
                    if (squareDistanceP2 > sqRadiusPlusTolerance)
                    {
                        //Make a new circle from the current two-point circle and the current point
                        circle = new InternalCircle(circle.Point0, circle.Point1, previousPoint2);
                        centerX = circle.CenterX;
                        centerY = circle.CenterY;
                        sqRadiusPlusTolerance = circle.SqRadius + sqTolerance;
                    }
                }

                if(circle.SqRadius < priorRadius)
                {
                    Debug.WriteLine("Bounding circle got smaller during this iteration");
                }
                priorRadius = circle.SqRadius;
                stallCounter++;
            }
            if (stallCounter >= stallLimit) Debug.WriteLine("Bounding circle failed to converge to within " + (Constants.BaseTolerance * circle.SqRadius * 2));

            #endregion

            #region Algorithm 3: Meggiddo's Linear-Time Algorithm

            //Pair up points into n/2 pairs. 
            //If an odd number of points.....

            //Construct a bisecting line for each pair of points. This sets their slope.

            //Order the slopes.
            //Find the median (halfway in the set) slope of the bisector lines 


            //Test

            #endregion

            var radius = circle.SqRadius.IsNegligible() ? 0 : Math.Sqrt(circle.SqRadius);
            return new BoundingCircle(radius, new PointLight(centerX, centerY));
        }

        /// <summary>
        ///     Gets the maximum inner circle given a group of polygons and a center point.
        ///     If there are no negative polygons, the function will return a negligible Bounding Circle
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public static BoundingCircle MaximumInnerCircle(IList<List<PointLight>> paths, PointLight centerPoint)
        {
            var polygons = paths.Select(path => new Polygon(path.Select(p => new Point(p)))).ToList();
            return MaximumInnerCircle(polygons, new Point(centerPoint));
        }

        /// <summary>
        ///     Gets the maximum inner circle given a group of polygons and a center point.
        ///     If there are no negative polygons, the function will return a negligible Bounding Circle
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public static BoundingCircle MaximumInnerCircle(IList<PolygonLight> paths, PointLight centerPoint)
        {
            var polygons = paths.Select(path => new Polygon(path)).ToList();
            return MaximumInnerCircle(polygons, new Point(centerPoint));
        }

        /// <summary>
        ///     Gets the maximum inner circle given a group of polygons and a center point.
        ///     If there are no negative polygons, the function will return a negligible Bounding Circle
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public static BoundingCircle MaximumInnerCircle(IList<List<Point>> paths, Point centerPoint)
        {
            var polygons = paths.Select(path => new Polygon(path)).ToList();
            return MaximumInnerCircle(polygons, centerPoint);
        }

        /// <summary>
        ///     Gets the maximum inner circle given a group of polygons and a center point.
        ///     The circle will either be inside a negative polygon or outside a positive polygon (e.g. C channel). 
        ///     Else it returns a negligible Bounding Circle
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public static BoundingCircle MaximumInnerCircle(List<Polygon> polygons, Point centerPoint)
        {
            var negativePolygons = new List<Polygon>();
            var positivePolygons = new List<Polygon>();
            foreach (var polygon in polygons)
            {
                if(polygon.PathLines == null) polygon.SetPathLines();
                if (polygon.IsPositive) positivePolygons.Add(polygon);
                else negativePolygons.Add(polygon);
            }
            //Check the distance from every line and point of every polygon in the group. 
            //Note: this function could possible be improved by determining which polygon is closest, 
            //but that did not seem to be a faster method. Also, a inner circle does not necessarily
            //need to be contained in a bounding box for positive solids (e.g. a C shape).
            var polygonsOfInterest = new List<Polygon>();

            //First, check if the point is inside any negative polygon.
            var minDistance = double.MaxValue;
            Polygon closestContainingPolygon = null;
            foreach (var negativePoly in negativePolygons)
            {
                if (!MiscFunctions.IsPointInsidePolygon(negativePoly, centerPoint, out var closestLineAbove,
                    out _, out var onBoundary)) continue;
                if (onBoundary) return new BoundingCircle(0.0, centerPoint.Light); //Null solution.
                var d = closestLineAbove.YGivenX(centerPoint.X) - centerPoint.Y; //Not negligible because not on Boundary
                if (d < minDistance)
                {
                    minDistance = d;
                    closestContainingPolygon = negativePoly;
                }
            }

            //If the point is inside the polygon.
            if (closestContainingPolygon != null)
            {
                polygonsOfInterest.Add(closestContainingPolygon);
            }
            //If not inside a negative polygon, check if the point is inside any positive polygons. If it is return null.
            else
            {
                foreach (var positivePoly in positivePolygons)
                {
                    if (MiscFunctions.IsPointInsidePolygon(positivePoly, centerPoint, out _,
                        out _, out _)) return new BoundingCircle(0.0, centerPoint.Light);
                    polygonsOfInterest.Add(positivePoly);
                }
            }
            
            //Lastly, determine how big the inner circle can be.
            var shortestDistance = double.MaxValue;
            var smallestBoundingCircle = new BoundingCircle(0.0, centerPoint.Light);
            foreach (var polygon in polygonsOfInterest)
            {
                var boundingCircle = MaximumInnerCircleInHole(polygon, centerPoint);
                if (boundingCircle.Radius < shortestDistance)
                {
                    shortestDistance = boundingCircle.Radius;
                    smallestBoundingCircle = boundingCircle;
                }
            }

            return smallestBoundingCircle;
        }

        private static BoundingCircle MaximumInnerCircleInHole(Polygon polygon, Point centerPoint)
        {
            var shortestDistance = double.MaxValue;
            //1. For every line on the path, get the closest point on the edge to the center point. 
            //   Skip if min distance to line (perpendicular) forms a point not on the line.
            foreach (var line in polygon.PathLines)
            {
                var v1 = line.ToPoint-line.FromPoint;
                //Correctly ordering the points should yield a negative area if the circle is inside a hole or outside a positive polygon.
                //Note also that zero area will occur when the points line up, which we want to ignore (the line ends will be checked anyways)
                if (!MiscFunctions.AreaOfPolygon(new List<Point> { line.FromPoint, line.ToPoint, centerPoint }).IsLessThanNonNegligible())
                    continue;

                //Figure out how far the center point is away from the line
                var d = MiscFunctions.DistancePointToLine(centerPoint, line.FromPoint, v1, out var pointOnLine);
                if (d > shortestDistance) continue;

                //Now we need to figure out if the lines intersect
                var tempLine = new Line(centerPoint, pointOnLine, false);
                if (!MiscFunctions.LineLineIntersection(line, tempLine, out _)) continue;
                //if(intersectionPoint != tempPoint) throw new Exception("Error in implementation. This should always be true.");
                shortestDistance = d;
            }

            //2. For every point in path and every closest edge point find distance to center.
            //   The shortest distance determines the diameter of the inner circle.
            foreach (var point in polygon.Path)
            {
                var d = MiscFunctions.DistancePointToPoint(point, centerPoint);
                if (d < shortestDistance) shortestDistance = d;
            }

            if (shortestDistance.IsPracticallySame(double.MaxValue)) return new BoundingCircle(0.0, centerPoint.Light); //Not inside any hole or outside any positive polygon
            return new BoundingCircle(shortestDistance, centerPoint.Light);
        }

        /// <summary>
        ///     Takes a set of elements and a metric for comparing them pairwise, and returns the median of the elements.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>Point.</returns>
        public static Point Median(List<Point> points)
        {
            return points[0];
        }

        /// <summary>
        ///     Takes a set of points and a line, and determines which side of the line the center of the MEC of  the points lies
        ///     on.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="line">The line.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool MEC_Center(List<Point> points, double[] line)
        {
            return false;
        }

        /// <summary>
        ///     Gets the minimum bounding cylinder using 13 guesses for the depth direction
        /// </summary>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <returns>BoundingBox.</returns>
        public static BoundingBox MinimumBoundingCylinder(IList<Vertex> convexHullVertices)
        {
            // here we create 13 directions. just like for bounding box
            var directions = new List<double[]>();
            for (var i = -1; i <= 1; i++)
                for (var j = -1; j <= 1; j++)
                    directions.Add(new[] {1.0, i, j});
            directions.Add(new[] {0.0, 0, 1});
            directions.Add(new[] {0.0, 1, 0});
            directions.Add(new[] {0.0, 1, 1});
            directions.Add(new[] {0.0, -1, 1});

            var boxes = directions.Select(v => new BoundingBox
            {
                Directions = new[] {v},
                Volume = double.PositiveInfinity
            }).ToList();
            for (var i = 0; i < 13; i++)
            {
                boxes[i] = Find_via_ChanTan_AABB_Approach(convexHullVertices, boxes[i]);
                for (var j = 0; j < 3; j++)
                {
                    var pointsOnFace_i = MiscFunctions.Get2DProjectionPoints(convexHullVertices, boxes[i].Directions[j]);
                }
            }
            var minVol = boxes.Min(box => box.Volume);
            return boxes.First(box => box.Volume == minVol);
        }

        /// <summary>
        ///     Adjusts the orthogonal rotations.
        /// </summary>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <param name="minOBB">The minimum obb.</param>
        /// <returns>BoundingBox.</returns>
        private static BoundingBox AdjustOrthogonalRotations(IList<Vertex> convexHullVertices, BoundingBox minOBB)
        {
            var failedConsecutiveRotations = 0;
            var k = 0;
            var i = 0;
            do
            {
                //Find new OBB along OBB.direction2 and OBB.direction3, keeping the best OBB.
                var newObb = FindOBBAlongDirection(convexHullVertices, minOBB.Directions[i++]);
                if (newObb.Volume.IsLessThanNonNegligible(minOBB.Volume))
                {
                    minOBB = newObb;
                    failedConsecutiveRotations = 0;
                }
                else failedConsecutiveRotations++;
                if (i == 3) i = 0;
                k++;
            } while (failedConsecutiveRotations < 3 && k < MaxRotationsForOBB);
            return minOBB;
        }

        /// <summary>
        ///     Class Bisector.
        /// </summary>
        internal class Bisector
        {
            #region Constructor

            /// <summary>
            ///     Initializes a new instance of the <see cref="Bisector" /> class.
            /// </summary>
            /// <param name="point1">The point1.</param>
            /// <param name="point2">The point2.</param>
            internal Bisector(Point point1, Point point2)
            {
            }

            #endregion

            #region Properties

            /// <summary>
            ///     Gets the slope.
            /// </summary>
            /// <value>The slope.</value>
            internal double Slope { get; private set; }

            #endregion
        }

        /// <summary>
        ///     Class InternalCircle.
        /// </summary>
        internal class InternalCircle
        {
            #region Constructor

            /// <summary>
            ///     Create a new circle from 3 points
            /// </summary>
            /// <param name="p0"></param>
            /// <param name="p1"></param>
            /// <param name="p2"></param>
            internal InternalCircle(PointLight p0, PointLight p1, PointLight p2)
            {
                NumPointsDefiningCircle = 3;
                Point0 = p0;
                Point1 = p1;
                Point2 = p2;
                var point0X = Point0.X;
                var point0Y = Point0.Y;
                var point1X = Point1.X;
                var point1Y = Point1.Y;
                var point2X = Point2.X;
                var point2Y = Point2.Y;

                //Find Circle center and radius
                //Assume NO two points are exactly the same 
                var rise1 = point1Y - point0Y;
                var run1 = point1X - point0X;
                var rise2 = point2Y - point1Y;
                var run2 = point2X - point1X;
                double x;
                double y;
                //Check for special cases of vertical or horizontal lines
                if (rise1.IsNegligible(Constants.BaseTolerance)) //If rise is zero, x can be found directly
                {
                    x = (point0X + point1X)/2;
                    //If run of other line is approximately zero as well, y can be found directly
                    if (run2.IsNegligible(Constants.BaseTolerance))
                        //If run is approximately zero, y can be found directly
                    {
                        y = (point1Y + point2Y)/2;
                    }
                    else
                    {
                        //Find perpendicular slope, and midpoint of line 2. 
                        //Then use the midpoint to find "b" and solve y = mx+b
                        //This is condensed into a single line because VS rounds the numbers 
                        //during division.
                        y = (point1Y + point2Y)/2 + -run2/rise2*(x - (point1X + point2X)/2);
                    }
                }
                else if (rise2.IsNegligible(Constants.BaseTolerance))
                    //If rise is approximately zero, x can be found directly
                {
                    x = (point1X + point2X)/2;
                    //If run of other line is approximately zero as well, y can be found directly
                    if (run1.IsNegligible(Constants.BaseTolerance))
                        //If run is approximately zero, y can be found directly
                    {
                        y = (point0Y + point1Y)/2;
                    }
                    else
                    {
                        //Find perpendicular slope, and midpoint of line 2. 
                        //Then use the midpoint to find "b" and solve y = mx+b
                        //This is condensed into a single line because VS rounds the numbers 
                        //during division.
                        y = (point0Y + point1Y)/2 + -run1/rise1*(x - (point0X + point1X)/2);
                    }
                }
                else if (run1.IsNegligible(Constants.BaseTolerance))
                    //If run is approximately zero, y can be found directly
                {
                    y = (point0Y + point1Y)/2;
                    //Find perpendicular slope, and midpoint of line 2. 
                    //Then use the midpoint to find "b" and solve y = mx+b
                    //This is condensed into a single line because VS rounds the numbers 
                    //during division.
                    x = (y - ((point1Y + point2Y)/2 - -run2/rise2*
                              (point1X + point2X)/2))/(-run2/rise2);
                }
                else if (run2.IsNegligible(Constants.BaseTolerance))
                    //If run is approximately zero, y can be found directly
                {
                    y = (point1Y + point2Y)/2;
                    //Find perpendicular slope, and midpoint of line 2. 
                    //Then use the midpoint to find "b" and solve y = mx+b
                    //This is condensed into a single line because VS rounds the numbers 
                    //during division.
                    x = (y - ((point1Y + point0Y)/2 - -run1/rise1*
                              (point1X + point0X)/2))/(-run1/rise1);
                }
                else
                {
                    //Didn't solve for slopes first because of rounding error in division
                    //ToDo: This does not always find a good center. Figure out why.
                    x = (rise1 / run1 * (rise2 / run2) * (point2Y - point0Y) +
                         rise1 / run1 * (point1X + point2X) -
                         rise2 / run2 * (point0X + point1X)) / (2 * (rise1 / run1 - rise2 / run2));
                    y = -(1 / (rise1 / run1)) * (x - (point0X + point1X) / 2) +
                        (point0Y + point1Y) / 2;
                }

                var dx = x - point0X;
                var dy = y - point0Y;
                CenterX = x;
                CenterY = y;
                SqRadius = dx * dx + dy * dy;
            }

            internal InternalCircle(PointLight p0, PointLight p1)
            {
                NumPointsDefiningCircle = 2;
                Point0 = p0;
                Point1 = p1;
                var point0X = p0.X;
                var point0Y = p0.Y;
                CenterX = point0X + (Point1.X - point0X) / 2;
                CenterY = point0Y + (Point1.Y - point0Y) / 2;
                var dx = CenterX - point0X;
                var dy = CenterY - point0Y;
                SqRadius = dx * dx + dy * dy;
            }
            #endregion

            private readonly PointLight _dummyPoint = new PointLight(double.NaN, double.NaN);

            /// <summary>
            ///     Finds the furthest the specified point.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <param name="furthestPoint">The furthest point.</param>
            /// <param name="previousPoint1"></param>
            /// <param name="previousPoint2"></param>
            /// <exception cref="ArgumentNullException">previousPoints cannot be null</exception>
            internal void Furthest(PointLight point, out PointLight furthestPoint, out PointLight previousPoint1, out PointLight previousPoint2, out int numPreviousPoints)
            {
                //Distance between point and center is greater than radius, it is outside the circle
                //DO P0, then P1, then P2
                numPreviousPoints = (NumPointsDefiningCircle == 3) ? 2 : 1;
                previousPoint2 = _dummyPoint;
                var p0SquareDistance =  Math.Pow(Point0.X - point.X, 2) + Math.Pow(Point0.Y - point.Y, 2);
                var p1SquareDistance = Math.Pow(Point1.X - point.X, 2) + Math.Pow(Point1.Y - point.Y, 2);              
                if (p0SquareDistance > p1SquareDistance)
                {
                    previousPoint1 = Point1;
                    if (NumPointsDefiningCircle == 3)
                    {
                        var p2SquareDistance = Math.Pow(Point2.X - point.X, 2) + Math.Pow(Point2.Y - point.Y, 2);
                        if (p0SquareDistance > p2SquareDistance)
                        {
                            furthestPoint = Point0;
                            previousPoint2 = Point2;
                        }
                        else
                        {
                            //If P2 > P0 and P0 > P1, P2 must also be greater than P1.
                            furthestPoint = Point2;
                            previousPoint2 = Point0;
                        }        
                    }
                    else
                    {
                        furthestPoint = Point0;                
                    }
                }
                else
                {
                    previousPoint1 = Point0;
                    if (NumPointsDefiningCircle == 3)
                    {
                        var p2SquareDistance = Math.Pow(Point2.X - point.X, 2) + Math.Pow(Point2.Y - point.Y, 2);
                        if (p1SquareDistance > p2SquareDistance)
                        {
                            furthestPoint = Point1;
                            previousPoint2 = Point2;
                        }
                        else
                        {
                            furthestPoint = Point2;
                            previousPoint2 = Point1;
                        }
                    }
                    else
                    {
                        furthestPoint = Point1;
                    }
                } 
            }

            #region Properties

            /// <summary>
            ///     Gets one point of the circle.
            /// </summary>
            /// <value>The points.</value>
            internal PointLight Point0 { get; }

            /// <summary>
            ///     Gets one point of the circle.
            /// </summary>
            /// <value>The points.</value>
            internal PointLight Point1 { get; }

            /// <summary>
            ///     Gets one point of the circle. This point may not exist.
            /// </summary>
            /// <value>The points.</value>
            internal PointLight Point2 { get; }

            /// <summary>
            ///     Gets the number of points that define the circle. 2 or 3.
            /// </summary>
            /// <value>The points.</value>
            internal int NumPointsDefiningCircle { get; }

            internal double CenterX { get; }

            internal double CenterY { get; }

            internal double SqRadius { get; set; }

            #endregion
        }
    }
}