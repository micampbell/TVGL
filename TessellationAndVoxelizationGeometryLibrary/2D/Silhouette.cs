using System;
using System.Collections.Generic;
using System.Linq;
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
        public static List<List<Point>> Run2(TessellatedSolid ts, double[] normal)
        {
            //Get the negative faces
            var negativeFaces = new List<PolygonalFace>();
            foreach (var face in ts.Faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (Math.Sign(dot) < 0)
                {
                    negativeFaces.Add(face);
                }
            }
            
            //For each negative face.
            //1. Project it onto a plane perpendicular to the given normal.
            //2. Union it with the prior negative faces.
            var startFace = negativeFaces[0];
            negativeFaces.RemoveAt(0);
            var startPolygon = MiscFunctions.Get2DProjectionPoints(startFace.Vertices, normal, false).ToList();
            //Make this polygon positive CCW
            startPolygon = CCWPositive(startPolygon);
            var polygonList = new List<List<Point>> { startPolygon };

            while (negativeFaces.Any())
            {
                var negativeFace = negativeFaces[0];
                negativeFaces.RemoveAt(0);
                var nextPolygon = MiscFunctions.Get2DProjectionPoints(negativeFace.Vertices, normal, false).ToList();
                //Make this polygon positive CCW
                nextPolygon = CCWPositive(nextPolygon);
                polygonList = Union.Run(polygonList, nextPolygon);
            }
            return polygonList;
        }

        /// <summary>
        /// Gets the silhouette of a solid along a given normal.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static List<List<Point>> Run(TessellatedSolid ts, double[] normal)
        {
            //Get the negative faces into a dictionary
            var negativeFaceDict = new Dictionary<int, PolygonalFace>();
            var negativeFaces = new List<PolygonalFace>();
            foreach (var face in ts.Faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (Math.Sign(dot) < 0)
                {
                    negativeFaceDict.Add(face.IndexInList, face);
                }
            }

            var unusedNegativeFaces = new Dictionary<int, PolygonalFace>(negativeFaceDict);
            var seperateSurfaces = new List<List<PolygonalFace>>();
            while (unusedNegativeFaces.Any())
            {
                var surface = new HashSet<PolygonalFace>();
                var stack = new Stack<PolygonalFace>(new[] { unusedNegativeFaces.ElementAt(0).Value });
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
                        if (!negativeFaceDict.ContainsKey(adjacentFace.IndexInList)) continue;//Ignore if not negative
                        stack.Push(adjacentFace);
                    }
                }
                seperateSurfaces.Add(surface.ToList());
            }

            var loops = new List<List<Vertex>>();
            //Get the surface inner and outer edges
            foreach (var surface in seperateSurfaces)
            {
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
                        foreach (var edge2 in outerEdges)
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
                            if (edge2 == outerEdges.Last() && !isReversed)
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
                            else if (edge2 == outerEdges.Last() && isReversed)
                            {
                                //Artifically close the loop.
                                vertex = startVertex;
                            }
                        }
                    } while (vertex != startVertex && outerEdges.Any()) ;
                    loops.Add(loop);
                }
            }

            //For now, assume all loops are positive CCW
            var allPolygons = loops.Select(loop => CCWPositive(MiscFunctions.Get2DProjectionPoints(loop, normal, false).ToList())).ToList();

            var polygonList = Union.Run(allPolygons);
            var newPolygonList = polygonList.Select(CCWPositive).ToList();
            polygonList = Union.Run(newPolygonList);
            return polygonList;
        }

        //Sets a convex polygon to counter clock wise positive
        // It is assumed that
        // 1. the polygon is closed
        // 2. the last point is not repeated.
        // 3. the polygon is simple (does not intersect itself or have holes)
        //http://debian.fmi.uni-sofia.bg/~sergei/cgsr/docs/clockwise.htm
        private static List<Point> CCWPositive(IList<Point> p)
        {
            var polygon = new List<Point>(p);
            var n = p.Count;
            var count = 0;

            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                var k = (i + 2) % n;
                var z = (p[j].X - p[i].X) * (p[k].Y - p[j].Y);
                z -= (p[j].Y - p[i].Y) * (p[k].X - p[j].X);
                if (z < 0)
                    count--;
                else if (z > 0)
                    count++;
            }
            //The polygon has a CW winding if count is negative
            if (count < 0) polygon.Reverse();
            return polygon;
        } 
    }
}
