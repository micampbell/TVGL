using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using StarMathLib;
using TVGL._2D.Clipper;

namespace TVGL._2D
{
    /// <summary>
    /// The outline of a solid from a particular direction.
    /// </summary>
    public static class Silhouette
    {
        /// <summary>
        /// Gets the silhouette of a solid along a given normal.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static List<List<Point>> Run(TessellatedSolid ts, double[] normal)
        {
            //Get the negative faces
            var negativeFaceDict = new Dictionary<int, PolygonalFace>();
            foreach (var face in ts.Faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (Math.Sign(dot) < 0)
                {
                    negativeFaceDict.Add(face.IndexInList, face);
                }
            }

            //For each negative face.
            //1. Project it onto a plane perpendicular to the given normal. 
            var startFace = negativeFaceDict.First().Value;
            negativeFaceDict.Remove(startFace.IndexInList);
            var startPath = MiscFunctions.Get2DProjectionPoints(startFace.Vertices, normal, false).ToList();
            var alreadyProjectedPoints = startPath.ToDictionary(point => point.References.First().IndexInList);
            
            //Make this polygon positive CCW
            startPath = PolygonOperations.CCWPositive(startPath);
            var polygonList = new List<List<Point>> { startPath};
            while (negativeFaceDict.Any())
            {
                var negativeFace = negativeFaceDict.First().Value;
                negativeFaceDict.Remove(negativeFace.IndexInList);
                var nextPolygon = new List<Point>();
                //Only project vertices that have not already been projected.
                //ToDo: This should allow better joining when IntPoints get removed from the Union function.
                foreach (var vertex in negativeFace.Vertices)
                {

                    if (alreadyProjectedPoints.ContainsKey(vertex.IndexInList))
                    {
                        nextPolygon.Add(alreadyProjectedPoints[vertex.IndexInList]);
                    }
                    else
                    {
                        var newPoint = MiscFunctions.Get2DProjectionPoints(new List<Vertex> { vertex }, normal, false).First();
                        nextPolygon.Add(newPoint);
                        alreadyProjectedPoints.Add(vertex.IndexInList, newPoint);
                    }
                }
                //Make this polygon positive CCW
                nextPolygon = PolygonOperations.CCWPositive(nextPolygon);
                //2. Union the polygons one by one. This performed better than one large union.
                polygonList = Union.Run(polygonList, nextPolygon);
            }

            //ToDo: Fix holes. The issue with this method is that the projection is not perfect, so small holes form on the part.
            //ToDo: Removing small holes is not enough, because holes can break up two polygons that would otherwise be joined during the union.
            //ToDo: This issue is likely caused by rounding error in the projection transform (e.g. it uses tan(theta))
            //ToDo: if empty space is defined by two disconnected positive polygons, rather than a negative polygon, connect the two polygons.
            //Remove all tiny polygons
            var allSignificantPolygons = new List<List<Point>>();
            foreach (var polygon in polygonList)
            {
                if (!MiscFunctions.AreaOfPolygon(polygon.ToArray()).IsNegligible(0.0001)) allSignificantPolygons.Add(polygon);
            }

            return allSignificantPolygons;
        }

        /// <summary>
        /// Gets the silhouette of a solid along a given normal. Much faster than the method above.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static List<List<Point>> Run2(TessellatedSolid ts, double[] normal)
        {
            //Get the negative faces into a dictionary
            var negativeFaceDict = new Dictionary<int, PolygonalFace>();
            foreach (var face in ts.Faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (Math.Sign(dot) < 0)
                {
                    negativeFaceDict.Add(face.IndexInList, face);
                }
            }

            var unusedNegativeFaces = new Dictionary<int, PolygonalFace>(negativeFaceDict);
            var seperateSurfaces = new List<HashSet<PolygonalFace>>();

            while (unusedNegativeFaces.Any())
            {
                var surface = new HashSet<PolygonalFace>();
                var stack = new Stack<PolygonalFace>(new[] {unusedNegativeFaces.ElementAt(0).Value});
                while (stack.Any())
                {
                    var face = stack.Pop();
                    if (surface.Contains(face)) continue;
                    surface.Add(face);
                    unusedNegativeFaces.Remove(face.IndexInList);
                    //Only push adjacent faces that are also negative
                    foreach (var adjacentFace in face.AdjacentFaces)
                    {
                        if (adjacentFace == null) continue; //This is an error. Handle it in the error function.
                        if (!negativeFaceDict.ContainsKey(adjacentFace.IndexInList)) continue; //Ignore if not negative
                        stack.Push(adjacentFace);
                    }
                }
                seperateSurfaces.Add(surface);
            }

            var solution = new List<List<Point>>();
            //Get the surface inner and outer edges
            //Check whether one of the loops's face centers is inside our outside the polygon, once it is projected down onto the plane.
            //may whish to check multiple points in case one does not work well. Ideally, choose the face with the highest dot product (closest to 1).
            //If the point is inside, the loop should be positive. Otherwise, the loop is negative.
            foreach (var surface in seperateSurfaces)
            {
                var surfaceSetOfLoops = new List<List<Vertex>>();
                //Get the surface inner and outer edges
                var outerEdges = new HashSet<Edge>();
                var innerEdges = new HashSet<Edge>();
                foreach (var face in surface)
                {
                    if (face.Edges.Count != 3) throw new Exception();
                    foreach (var edge in face.Edges)
                    {
                        //if (innerEdges.Contains(edge)) continue;
                        if (!outerEdges.Contains(edge)) outerEdges.Add(edge);
                        else if (outerEdges.Contains(edge))
                        {
                            innerEdges.Add(edge);
                            outerEdges.Remove(edge);
                        }
                        else throw new Exception();
                    }

                }


                //The inner edges may form 0 to many negative (CW) loops
                while (outerEdges.Any())
                {
                    var isReversed = false;
                    var startEdge = outerEdges.First();
                    outerEdges.Remove(startEdge);
                    var startVertex = startEdge.From;
                    var vertex = startEdge.To;
                    var loop = new List<Vertex> {vertex};
                    do
                    {
                        foreach (var edge2 in vertex.Edges.Where(edge2 => outerEdges.Contains(edge2)))
                        {
                            if (edge2.From == vertex)
                            {
                                outerEdges.Remove(edge2);
                                vertex = edge2.To;
                                loop.Add(vertex);
                                break;
                            }
                            else if (edge2.To == vertex)
                            {
                                outerEdges.Remove(edge2);
                                vertex = edge2.From;
                                loop.Add(vertex);
                                break;
                            }
                            if (edge2 == vertex.Edges.Last() && !isReversed)
                            {
                                //Swap the vertices were interested in and
                                //Reverse the loop
                                var tempVertex = startVertex;
                                startVertex = vertex;
                                vertex = tempVertex;
                                loop.Reverse();
                                loop.Add(vertex);
                                isReversed = true;
                            }
                            else if (edge2 == vertex.Edges.Last() && isReversed)
                            {
                                //Artifically close the loop.
                                vertex = startVertex;
                            }
                        }
                    } while (vertex != startVertex && outerEdges.Any());
                    surfaceSetOfLoops.Add(loop);
                }

                //Get the paths, build polygons, and determine whether each polygon should be positive or negative
                var surfacePaths =
                    surfaceSetOfLoops.Select(loop => MiscFunctions.Get2DProjectionPoints(loop, normal, false).ToList())
                        .ToList();

                //ToDO: Must simplify, because loops could be self intersecting, which means a section of the loop may reference a hole an the other section may reference a solid.
                var simplifiedPaths = new List<List<Point>>();
                foreach (var path in surfacePaths)
                {
                    simplifiedPaths.AddRange(PolygonOperations.Simplify(path));
                }

                //var surfaceUnion = Union.Run(innerPaths, outerPath);

                solution.AddRange(simplifiedPaths);
                break;
            }


            //ToDO: This section does not work because the selected faces center is not gauranteed to actually be inside the polygon if it is positive.
            //ToDO: This is because of overlapping faces
            ////Now we need to determine whether the loop should be positive or negative.
            ////To do this, get the faces that made this loops and get one with a 
            ////normal that is at least close lining up with the given direction
            ////Once we have a good enough candidate, stop and make the projected point.
            ////Lastly, check to see wheterh this point is inside the polygon. 
            ////If it is inside, the path should be CCW+ else it should be CW-.
            //var orderedPaths = new List<List<Point>>();
            //foreach (var path in simplifiedPaths)
            //{
            //    var minDot = 1.0;
            //    double[] idealFaceCenter = null;
            //    bool pointIsGoodEnough = false;
            //    foreach (var point in path.Where(point => point.References?.First() != null && point.References.Count >= 1))
            //    {
            //        foreach (var face in point.References.First().Faces)
            //        {
            //            if (!negativeFaceDict.ContainsKey(face.IndexInList)) continue;
            //            var dot = normal.dotProduct(face.Normal);
            //            {
            //                if (dot < minDot)
            //                {
            //                    minDot = dot;
            //                    idealFaceCenter = face.Center;
            //                }
            //                //If this face is good enough, we are finished with this path
            //                if (dot.IsPracticallySame(-1, 0.3))
            //                {
            //                    pointIsGoodEnough = true;
            //                    break;
            //                }
            //            }
            //        }
            //        if (pointIsGoodEnough) break;
            //    }
            //    if (idealFaceCenter == null)
            //    {
            //        //Assume it is positive. Better to error on the side of positives rather than holes
            //        orderedPaths.Add(PolygonOperations.CCWPositive(path));
            //    }
            //    else
            //    {
            //        var idealFaceVertex = new Vertex(idealFaceCenter);
            //        var pointOnFace = MiscFunctions.Get2DProjectionPoints(new List<Vertex> {idealFaceVertex}, normal, false).First();
            //        if (MiscFunctions.IsPointInsidePolygon(path, pointOnFace, false))
            //        {
            //            orderedPaths.Add(PolygonOperations.CCWPositive(path));
            //        }
            //        else
            //        {
            //            orderedPaths.Add(PolygonOperations.CWNegative(path));
            //        }
            //    }

            //}

            //var allPolygons = new List<Polygon>();
            //var allOrderedPaths = new List<List<Point>>();
            //for (var i = 0; i < loops.Count; i++)
            //{
            //    var path = MiscFunctions.Get2DProjectionPoints(loops[i], normal, false).ToList();
            //    var idealFaceVertex = new Vertex(idealFaceCenters[i]);
            //    var idealFaceCenter = MiscFunctions.Get2DProjectionPoints(new List<Vertex> {idealFaceVertex}, normal).First();
            //    var polygon = new Polygon(path);
            //    if (MiscFunctions.IsPointInsidePolygon(path, idealFaceCenter, false))
            //    {
            //        polygon.IsPositive = true;
            //    }
            //    else
            //    {
            //        polygon.IsPositive = false;
            //    }
            //    allPolygons.Add(polygon);
            //    allOrderedPaths.Add(new List<Point>(polygon.Path));
            //}

            //var solution = Union.Run(orderedPaths);

            ////Remove all tiny polygons
            //var allSignificantPolygons = new List<List<Point>>();
            //foreach (var polygon in solution)
            //{
            //    if(!MiscFunctions.AreaOfPolygon(polygon.ToArray()).IsNegligible(0.01)) allSignificantPolygons.Add(polygon);
            //}
            return solution;
        }

        
    }
}
