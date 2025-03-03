﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarMathLib;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public partial class VoxelizedSolid:Solid, IEnumerable<int[]>
    {
        #region Public Methods that Branch
        /// <summary>
        /// Gets or sets the <see cref="System.Boolean"/> at the specified coordinate.
        /// true corresponds to "on" and false to "off".
        /// </summary>
        /// <value>
        /// The <see cref="System.Boolean"/>.
        /// </value>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <returns></returns>
        public bool this[int xCoord, int yCoord, int zCoord]
        {
            get => voxels[yCoord + zMultiplier * zCoord][xCoord];
            set => voxels[yCoord + zMultiplier * zCoord][xCoord] = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Boolean"/> at the specified coordinate.
        /// true corresponds to "on" and false to "off".
        /// </summary>
        /// <value>
        /// The <see cref="System.Boolean"/>.
        /// </value>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <returns></returns>
        public bool this[int[] coordinates]
        {
            get => voxels[coordinates[1] + zMultiplier * coordinates[2]][coordinates[0]];
            set => voxels[coordinates[1] + zMultiplier * coordinates[2]][coordinates[0]] = value;
        }

        /// <summary>
        /// Gets the neighbors of the specified voxel position (even if specified is an off-voxel).
        /// The result is true if there are neighbors and false if there are none.
        /// the neighbors array is the coordinates or nulls. Where the null represents off-voxels.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <param name="neighbors">The neighbors.</param>
        /// <returns></returns>
        public bool GetNeighbors(int xCoord, int yCoord, int zCoord, out int[][] neighbors)
        {
            neighbors = GetNeighbors(xCoord, yCoord, zCoord);
            return !neighbors.Any(n => n != null);
        }
        
        /// <summary>
        /// Gets the neighbors of the specified voxel position (even if specified is an off-voxel).
        /// The result is an array of coordinates or nulls. Where the null represents off-voxels.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <returns></returns>
        public int[][] GetNeighbors(int xCoord, int yCoord, int zCoord)
        {
            var neighbors = new int[][] { null, null, null, null, null, null };
            var xNeighbors = voxels[yCoord + zMultiplier * zCoord].GetNeighbors(xCoord);
            if (xNeighbors.Item1)
                neighbors[0] = new[] { xCoord - 1, yCoord, zCoord };
            if (xNeighbors.Item2)
                neighbors[1] = new[] { xCoord + 1, yCoord, zCoord };

            if (yCoord > 0 && voxels[yCoord - 1 + zMultiplier * zCoord][xCoord])
                neighbors[2] = new[] { xCoord, yCoord - 1, zCoord };
            if (yCoord + 1 < numVoxelsY && voxels[yCoord + 1 + zMultiplier * zCoord][xCoord])
                neighbors[3] = new[] { xCoord, yCoord + 1, zCoord };

            if (zCoord > 0 && voxels[yCoord + zMultiplier * (zCoord - 1)][xCoord])
                neighbors[4] = new[] { xCoord, yCoord, zCoord - 1 };
            if (zCoord + 1 < numVoxelsZ && voxels[yCoord + zMultiplier * (zCoord + 1)][xCoord])
                neighbors[5] = new[] { xCoord, yCoord, zCoord + 1 };

            return neighbors;
        }


        public int NumNeighbors(int xCoord, int yCoord, int zCoord)
        {
            var neighbors = 0;

            var xNeighbors = voxels[yCoord + zMultiplier * zCoord].GetNeighbors(xCoord);
            if (xNeighbors.Item1) neighbors++;
            if (xNeighbors.Item2) neighbors++;

            if (yCoord != 0 && voxels[yCoord - 1 + zMultiplier * zCoord][xCoord]) neighbors++;
            if (yCoord + 1 < numVoxelsY && voxels[yCoord + 1 + zMultiplier * zCoord][xCoord]) neighbors++;

            if (zCoord != 0 && voxels[yCoord + zMultiplier * (zCoord - 1)][xCoord]) neighbors++;
            if (zCoord + 1 < numVoxelsZ && voxels[yCoord + zMultiplier * (zCoord + 1)][xCoord]) neighbors++;

            return neighbors;
        }
        #endregion

        #region Set/update properties
        public void UpdateProperties()
        {
            Count = 0;
            var xTotal = 0.0;
            var yTotal = 0.0;
            var zTotal = 0.0;
            for (int j = 0; j < numVoxelsY; j++)
                for (int k = 0; k < numVoxelsZ; k++)
                {
                    var voxelRow = voxels[j + zMultiplier * k];
                    var rowCount = voxelRow.Count;
                    xTotal += rowCount * voxelRow.AverageXPosition();
                    yTotal += rowCount * j;
                    zTotal += rowCount * k;
                    Count += rowCount;
                }
            Volume = Count * Math.Pow(VoxelSideLength, 3);
            Center = new double[]
            {
                VoxelSideLength*xTotal / Count,
                VoxelSideLength*yTotal /Count,
                VoxelSideLength*zTotal / Count
            };
        }



        /*
        var neighbors = new ConcurrentDictionary<int, int>();
        //Parallel.For(0, xLim, i =>
        for (int i = 0; i < numVoxelsX; i++)
        {
            var neighborCount = 0;
            for (var j = 0; j < numVoxelsY; j++)
            {
                for (var k = 0; k < numVoxelsZ; k++)
                {
                    if (!this[i, j, k]) continue;
                    var num = NumNeighbors(i, j, k, numVoxelsX, numVoxelsY, numVoxelsZ);
                    neighborCount += num;
                }
            }
            neighbors.TryAdd(i, neighborCount);
        } //);
        long totalNeighbors = 0;
        foreach (var v in neighbors.Values)
            totalNeighbors += v;
        SurfaceArea = 6 * (Count - totalNeighbors / 6) * Math.Pow(VoxelSideLength, 2);
        */

        #endregion

        #region Boolean functions
        // NOT A
        public VoxelizedSolid InvertToNewSolid()
        {
            var vs = (VoxelizedSolid)Copy();
            vs.Invert();
            return vs;
        }
        // NOT A
        public void Invert()
        {
            UpdateToAllSparse();
            Parallel.ForEach(voxels, vx => vx.Invert());
            UpdateProperties();
        }

        // A OR B
        public VoxelizedSolid UnionToNewSolid(params VoxelizedSolid[] solids)
        {
            var vs = (VoxelizedSolid)Copy();
            vs.Union(solids);
            return vs;
        }
        // A OR B
        public void Union(params VoxelizedSolid[] solids)
        {
            UpdateToAllSparse();
            foreach (var solid in solids)
                solid.UpdateToAllSparse();
            Parallel.For(0, numVoxelsY * numVoxelsZ, i =>
            {
                voxels[i].Union(solids.Select(s => s.voxels[i]).ToArray());
            });
            UpdateProperties();
        }

        // A AND B
        public VoxelizedSolid IntersectToNewSolid(params VoxelizedSolid[] solids)
        {
            var vs = (VoxelizedSolid)Copy();
            vs.Intersect(solids);
            return vs;
        }
        // A AND B
        public void Intersect(params VoxelizedSolid[] solids)
        {
            UpdateToAllSparse();
            foreach (var solid in solids)
                solid.UpdateToAllSparse();
            Parallel.For(0, numVoxelsY * numVoxelsZ, i =>
            {
                voxels[i].Intersect(solids.Select(s => s.voxels[i]).ToArray());
            });
            UpdateProperties();
        }

        // A AND (NOT B)
        public VoxelizedSolid SubtractToNewSolid(params VoxelizedSolid[] subtrahends)
        {
            var vs = (VoxelizedSolid)Copy();
            vs.Subtract(subtrahends);
            return vs;
        }

        // A AND (NOT B)
        public void Subtract(params VoxelizedSolid[] subtrahends)
        {
            UpdateToAllSparse();
            foreach (var solid in subtrahends)
                solid.UpdateToAllSparse();
            Parallel.For(0, numVoxelsY * numVoxelsZ, i =>
            {
                voxels[i].Subtract(subtrahends.Select(s => s.voxels[i]).ToArray());
            });
            UpdateProperties();
        }
        #endregion

        #region Slice Voxel Solids
        // If direction is negative, the negative side solid is in position one of return tuple
        // If direction is positive, the positive side solid is in position one of return tuple
        // distance is the zero-based index of voxel-plane to cut before
        // i.e. distance = 8, would yield one solid with voxels 0 to 7, and one with 8 to end
        // 0 < distance < VoxelsPerSide[cut direction]
        public (VoxelizedSolid, VoxelizedSolid) SliceOnFlat(CartesianDirections vd, int distance)
        {
            if (distance >= VoxelsPerSide[Math.Abs((int)vd) - 1] || distance < 1)
                throw new ArgumentOutOfRangeException();
            ushort uCutBefore = (ushort)distance;
            ushort top = (ushort)numVoxelsX;
            var vs1 = (VoxelizedSolid)Copy();
            var vs2 = (VoxelizedSolid)Copy();
            switch (vd)
            {
                case CartesianDirections.XPositive:
                    Parallel.ForEach(vs1.voxels, row => row.TurnOffRange(0, uCutBefore));
                    Parallel.ForEach(vs2.voxels, row => row.TurnOffRange(uCutBefore, (ushort)numVoxelsX));
                    break;
                case CartesianDirections.YPositive:
                    Parallel.For(0, numVoxelsZ, k =>
                    {
                        for (var j = 0; j < distance; j++)
                            vs1.voxels[j + zMultiplier * k].Clear();
                        for (var j = distance; j < numVoxelsY; j++)
                            vs2.voxels[j + zMultiplier * k].Clear();
                    });
                    break;
                case CartesianDirections.ZPositive:
                    Parallel.For(0, VoxelsPerSide[1] * distance, i => vs1.voxels[i].Clear());
                    Parallel.For(VoxelsPerSide[1] * distance, numVoxelsY * numVoxelsZ,
                        i => vs2.voxels[i].Clear());
                    break;
                case CartesianDirections.XNegative:
                    Parallel.ForEach(vs2.voxels, row => row.TurnOffRange(0, uCutBefore));
                    Parallel.ForEach(vs1.voxels, row => row.TurnOffRange(uCutBefore, (ushort)numVoxelsX));
                    break;
                case CartesianDirections.YNegative:
                    Parallel.For(0, numVoxelsZ, k =>
                    {
                        for (var j = 0; j < distance; j++)
                            vs2.voxels[j + zMultiplier * k].Clear();
                        for (var j = distance; j < numVoxelsY; j++)
                            vs1.voxels[j + zMultiplier * k].Clear();
                    });
                    break;
                case CartesianDirections.ZNegative:
                    Parallel.For(0, zMultiplier * distance, i => vs2.voxels[i].Clear());
                    Parallel.For(zMultiplier * distance, numVoxelsY * numVoxelsZ,
                        i => vs1.voxels[i].Clear());
                    break;
            }
            vs1.UpdateProperties();
            vs2.UpdateProperties();
            return (vs1, vs2);
        }

        // Solid on positive side of flat is in position one of return tuple
        // Voxels exactly on the plane are assigned to the positive side
        public (VoxelizedSolid, VoxelizedSolid) SliceOnFlat(Flat plane)
        {
            var vs1 = (VoxelizedSolid)Copy();
            var vs2 = (VoxelizedSolid)Copy();

            var normalOfPlane = plane.Normal;
            var distOfPlane = plane.DistanceToOrigin;

            var xOff = Offset[0];
            var yOff = Offset[1];
            var zOff = Offset[2];
            if (normalOfPlane[0].IsNegligible()) //since no x component. we simply clear rows
                Parallel.For(0, numVoxelsZ, k =>
                {
                    for (var j = 0; j < VoxelsPerSide[1]; j++)
                    {
                        var y = yOff + (j + .5) * VoxelSideLength;
                        var z = zOff + (k + .5) * VoxelSideLength;
                        var d = MiscFunctions.DistancePointToPlane(new[] { 0, y, z }, normalOfPlane, distOfPlane);
                        if (d < 0)
                            vs1.voxels[j + zMultiplier * k].Clear();
                        else vs2.voxels[j + zMultiplier * k].Clear();
                    }
                });
            else
            {
                Parallel.For(0, numVoxelsZ, k =>
                //for (int k = 0; k < numVoxelsZ; k++)
                {
                    var z = zOff + (k + .5) * VoxelSideLength;
                    var zComponent = distOfPlane - z * normalOfPlane[2];
                    for (var j = 0; j < VoxelsPerSide[1]; j++)
                    {
                        var y = yOff + (j + .5) * VoxelSideLength;
                        var x = (zComponent - y * normalOfPlane[1]) / normalOfPlane[0];
                        var xIndex = (x - xOff) / VoxelSideLength - 0.5;
                        if (xIndex < 0)
                            vs2.voxels[j + zMultiplier * k].Clear();
                        else if (xIndex > numVoxelsX)
                            vs1.voxels[j + zMultiplier * k].Clear();
                        else
                        {
                            vs1.voxels[j + zMultiplier * k].TurnOffRange(0, (ushort)xIndex);
                            vs2.voxels[j + zMultiplier * k].TurnOffRange((ushort)xIndex,
                                (ushort)(numVoxelsX + 1));
                        }
                    }
                });
            }
            vs1.UpdateProperties();
            vs2.UpdateProperties();
            return (vs1, vs2);
        }
        #endregion

        #region Draft in VoxelDirection
        /// <summary>
        /// Drafts or extrudes the solid in specified direction. This means that the side of the
        /// part opposite the direction will be like the origial and the side facing the direction
        /// will be flat - as if extrude (playdoh style) in the specified direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public VoxelizedSolid DraftToNewSolid(CartesianDirections direction)
        {
            var vs = (VoxelizedSolid)Copy();
            vs.Draft(direction);
            return vs;
        }
        /// <summary>
        /// Drafts or extrudes the solid in specified direction. This means that the side of the
        /// part opposite the direction will be like the origial and the side facing the direction
        /// will be flat - as if extrude (playdoh style) in the specified direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        public void Draft(CartesianDirections direction)
        {
            UpdateToAllSparse();
            if (direction == CartesianDirections.XPositive)
                Parallel.For(0, numVoxelsY * numVoxelsZ, i =>
                {
                    var rowIndices = ((VoxelRowSparse)voxels[i]).indices;
                    if (rowIndices.Any())
                    {
                        var start = rowIndices[0];
                        rowIndices.Clear();
                        rowIndices.Add(start);
                        rowIndices.Add((ushort)(numVoxelsX + 1));
                    }
                });
            if (direction == CartesianDirections.XNegative)
                Parallel.For(0, numVoxelsY * numVoxelsZ, i =>
                {
                    var rowIndices = ((VoxelRowSparse)voxels[i]).indices;
                    if (rowIndices.Any())
                    {
                        var end = rowIndices.Last();
                        rowIndices.Clear();
                        rowIndices.Add(0);
                        rowIndices.Add(end);
                    }
                });
            if (direction == CartesianDirections.YPositive)
                Parallel.For(0, numVoxelsX, i =>
                {
                    for (var k = 0; k < numVoxelsZ; k++)
                    {
                        var j = 0;
                        while (j < numVoxelsY && !this[i, j, k]) j++;
                        for (; j < numVoxelsY; j++)
                            this[i, j, k] = true;
                    }
                });
            if (direction == CartesianDirections.YNegative)
                Parallel.For(0, numVoxelsX, i =>
                {
                    for (var k = 0; k < numVoxelsZ; k++)
                    {
                        var j = numVoxelsY - 1;
                        while (j >= 0 && !this[i, j, k]) j--;
                        for (; j >= 0; j--)
                            this[i, j, k] = true;
                    }
                });
            if (direction == CartesianDirections.ZPositive)
                Parallel.For(0, numVoxelsX, i =>
                {
                    for (var j = 0; j < numVoxelsY; j++)
                    {
                        var k = 0;
                        while (k < numVoxelsZ && !this[i, j, k]) k++;
                        for (; k < numVoxelsZ; k++)
                            this[i, j, k] = true;
                    }
                });
            if (direction == CartesianDirections.ZNegative)
                Parallel.For(0, numVoxelsX, i =>
                {
                    for (var j = 0; j < numVoxelsY; j++)
                    {
                        var k = numVoxelsZ - 1;
                        while (k >= 0 && !this[i, j, k]) k--;
                        for (; k >= 0; k--)
                            this[i, j, k] = true;
                    }
                });
            UpdateProperties();
        }
        #endregion

        #region Voxel erosion
        /// <summary>
        /// Erodes the solid in the supplied direction until the mask contacts the constraint solid.
        /// This creates a new solid. The orinal is unaltered.
        /// </summary>
        /// <param name="constraintSolid">The constraint solid.</param>
        /// <param name="dir">The dir.</param>
        /// <param name="tLimit">The t limit.</param>
        /// <param name="maskSize">Size of the mask.</param>
        /// <param name="maskOptions">The mask options.</param>
        /// <returns></returns>
        public VoxelizedSolid DirectionalErodeToConstraintToNewSolid(in VoxelizedSolid constraintSolid, double[] dir,
            double tLimit = 0, double maskSize = 0, params string[] maskOptions)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.DirectionalErodeToConstraint(constraintSolid, dir.normalize(3), tLimit, maskSize, maskOptions);
            return copy;
        }

        /// <summary>
        /// Erodes the solid in the supplied direction until the mask contacts the constraint solid.
        /// This creates a new solid. The orinal is unaltered.
        /// </summary>
        /// <param name="constraintSolid">The constraint solid.</param>
        /// <param name="dir">The dir.</param>
        /// <param name="tLimit">The t limit.</param>
        /// <param name="maskSize">Size of the mask.</param>
        /// <param name="maskOptions">The mask options.</param>
        /// <returns></returns>
        public VoxelizedSolid DirectionalErodeToConstraintToNewSolid(in VoxelizedSolid constraintSolid, CartesianDirections dir,
            double tLimit = 0, double maskSize = 0, params string[] maskOptions)
        {
            var copy = (VoxelizedSolid)Copy();

            var tDir = new[] { .0, .0, .0 };
            tDir[Math.Abs((int)dir) - 1] = Math.Sign((int)dir);

            copy.DirectionalErodeToConstraint(constraintSolid, tDir.normalize(3), tLimit, maskSize, maskOptions);
            return copy;
        }

        /// <summary>
        /// Erodes the solid in the supplied direction until the mask contacts the constraint solid.
        /// </summary>
        /// <param name="constraintSolid">The constraint solid.</param>
        /// <param name="dir">The dir.</param>
        /// <param name="tLimit">The t limit.</param>
        /// <param name="maskSize">The tool dia.</param>
        /// <param name="toolOptions">The tool options.</param>
        private void DirectionalErodeToConstraint(VoxelizedSolid constraintSolid, double[] dir,
            double tLimit, double maskSize, params string[] maskOptions)
        {
            var dirX = dir[0];
            var dirY = dir[1];
            var dirZ = dir[2];
            var signX = (byte)(Math.Sign(dirX) + 1);
            var signY = (byte)(Math.Sign(dirY) + 1);
            var signZ = (byte)(Math.Sign(dirZ) + 1);
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];

            tLimit = tLimit <= 0 ? VoxelsPerSide.norm2() : tLimit / VoxelSideLength;
            var mLimit = tLimit + VoxelsPerSide.norm2();
            var mask = CreateProjectionMask(dir, mLimit);
            var starts = GetAllVoxelsOnBoundingSurfaces(dirX, dirY, dirZ, maskSize);
            var sliceMask = ThickenMask(mask[0], dir, maskSize, maskOptions);

            Parallel.ForEach(starts, vox =>
                ErodeMask(constraintSolid, mask, signX, signY, signZ, xLim, yLim, zLim, sliceMask, vox));
            //foreach (var vox in starts)
            //    ErodeMask(constraintSolid, mask, signX, signY, signZ, xLim, yLim, zLim, sliceMask, vox);
            UpdateProperties();
        }

        private static IEnumerable<CartesianDirections> GetVoxelDirections(double dirX, double dirY, double dirZ)
        {
            var dirs = new List<CartesianDirections>();
            var signedDir = new[] { Math.Sign(dirX), Math.Sign(dirY), Math.Sign(dirZ) };
            for (var i = 0; i < 3; i++)
            {
                if (signedDir[i] == 0) continue;
                dirs.Add((CartesianDirections)((i + 1) * -1 * signedDir[i]));
            }
            return dirs.ToArray();
        }

        private IEnumerable<int[]> GetAllVoxelsOnBoundingSurfaces(double dirX, double dirY, double dirZ,
            double toolDia)
        {
            var voxels = new HashSet<int[]>(new SameCoordinates());
            var directions = GetVoxelDirections(dirX, dirY, dirZ);
            foreach (var direction in directions)
            {
                var voxel = GetAllVoxelsOnBoundingSurface(direction, toolDia);
                foreach (var vox in voxel)
                    voxels.Add(vox);
            }
            return voxels;
        }

        private IEnumerable<int[]> GetAllVoxelsOnBoundingSurface(CartesianDirections dir, double toolDia)
        {
            var limit = new int[3][];
            var offset = (int)Math.Ceiling(0.5 * toolDia / VoxelSideLength);

            limit[0] = new[] { 0 - offset, VoxelsPerSide[0] + offset };
            limit[1] = new[] { 0 - offset, VoxelsPerSide[1] + offset };
            limit[2] = new[] { 0 - offset, VoxelsPerSide[2] + offset };

            var ind = Math.Abs((int)dir) - 1;
            if (Math.Sign((int)dir) == 1)
                limit[ind][0] = limit[ind][1] - 1;
            else
                limit[ind][1] = limit[ind][0] + 1;

            var arraySize = (limit[0][1] - limit[0][0]) * (limit[1][1] - limit[1][0]) * (limit[2][1] - limit[2][0]);
            var surfaceVoxels = new int[arraySize][];
            var m = 0;

            for (var i = limit[0][0]; i < limit[0][1]; i++)
                for (var j = limit[1][0]; j < limit[1][1]; j++)
                    for (var k = limit[2][0]; k < limit[2][1]; k++)
                    {
                        surfaceVoxels[m] = new[] { i, j, k };
                        m++;
                    }

            return surfaceVoxels;
        }

        private void ErodeMask(VoxelizedSolid constraintSolid, int[][] mask, byte signX, byte signY,
            byte signZ, int xLim, int yLim, int zLim, int[][] sliceMask, int[] start)
        {
            //var sliceMaskCount = sliceMask.Length;
            var xMask = mask[0][0];
            var yMask = mask[0][1];
            var zMask = mask[0][2];
            var xShift = start[0] - xMask;
            var yShift = start[1] - yMask;
            var zShift = start[2] - zMask;

            var insidePart = false;

            //foreach depth or timestep
            foreach (var initCoord in mask)
            {
                var xStartCoord = initCoord[0] + xShift;
                var yStartCoord = initCoord[1] + yShift;
                var zStartCoord = initCoord[2] + zShift;

                var xTShift = xStartCoord - xMask;
                var yTShift = yStartCoord - yMask;
                var zTShift = zStartCoord - zMask;

                //Iterate over the template of the slice mask
                //to move them to the appropriate location but 
                //need to be sure that we are in the space (not negative)
                //var succeedCounter = 0;
                //var precedeCounter = 0;
                var succeeds = true;
                var precedes = true;
                var outOfBounds = false;

                foreach (var voxCoord in sliceMask)
                {
                    var coordX = voxCoord[0] + xTShift;
                    var coordY = voxCoord[1] + yTShift;
                    var coordZ = voxCoord[2] + zTShift;

                    // 0 is negative dir, 1 is zero, and 2 is positive. E.g. for signX:
                    // 0: [-0.577  0.577  0.577]
                    // 1: [ 0      0.707  0.707]
                    // 2: [ 0.577  0.577  0.577]
                    if (!insidePart && ((signX == 0 && coordX >= xLim) || (signX == 2 && coordX < 0) ||
                                        (signX == 1 && (coordX >= xLim || coordX < 0)) ||
                                        (signY == 0 && coordY >= yLim) || (signY == 2 && coordY < 0) ||
                                        (signY == 1 && (coordY >= yLim || coordY < 0)) ||
                                        (signZ == 0 && coordZ >= zLim) || (signZ == 2 && coordZ < 0) ||
                                        (signZ == 1 && (coordZ >= zLim || coordZ < 0))))
                    {
                        outOfBounds = true;
                        continue;
                    }
                    precedes = false;

                    if (coordX < 0 || coordY < 0 || coordZ < 0 || coordX >= xLim || coordY >= yLim || coordZ >= zLim)
                    {
                        outOfBounds = true;
                        // Return if you've left the part
                        continue;
                    }
                    succeeds = false;
                    if (constraintSolid[coordX, coordY, coordZ]) return;
                }

                if (!insidePart && precedes) continue;
                if (succeeds) return;
                if (!insidePart)
                    insidePart = true;

                foreach (var voxCoord in sliceMask)
                {
                    var coordX = voxCoord[0] + xTShift;
                    var coordY = voxCoord[1] + yTShift;
                    var coordZ = voxCoord[2] + zTShift;
                    if (outOfBounds && (coordX < 0 || coordY < 0 || coordZ < 0 || coordX >= xLim || coordY >= yLim ||
                                        coordZ >= zLim)) continue;
                    this[coordX, coordY, coordZ] = false;
                }
            }
        }
        #endregion


        #region Functions for Dilation (3D Offsetting)
        private static int[][] GetVoxelsWithinCircle(double[] center, double[] dir, double radius, bool edge = false)
        {
            var voxels = new HashSet<int[]>(new SameCoordinates());

            var radii = new List<double>();
            if (!edge)
                for (var i = .0; i < radius; i += 0.5)
                    radii.Add(i);
            radii.Add(radius);
            var a = Math.Abs(dir[0]) < 1e-5
                ? new[] { .0, -dir[2], dir[1] }.normalize(3)
                : new[] { dir[1], -dir[0], 0 }.normalize(3);
            var b = a.crossProduct(dir);

            foreach (var r in radii)
            {
                var step = 2 * Math.PI / Math.Ceiling(Math.PI * 2 * r / 0.5);
                for (var t = .0; t < 2 * Math.PI; t += step)
                {
                    var x = (int)Math.Floor(center[0] + 0.5 + r * Math.Cos(t) * a[0] + r * Math.Sin(t) * b[0]);
                    var y = (int)Math.Floor(center[1] + 0.5 + r * Math.Cos(t) * a[1] + r * Math.Sin(t) * b[1]);
                    var z = (int)Math.Floor(center[2] + 0.5 + r * Math.Cos(t) * a[2] + r * Math.Sin(t) * b[2]);
                    voxels.Add(new[] { x, y, z });
                }
            }

            return voxels.ToArray();
        }

        private static int[][] GetVoxelsOnCone(int[] center, double[] dir, double radius, double angle)
        {
            var voxels = new HashSet<int[]>(new[] { center.ToArray() }, new SameCoordinates());

            var a = angle * (Math.PI / 180) / 2;
            var l = radius / Math.Sin(a);
            var numSteps = (int)Math.Ceiling(l / 0.5);
            var lStep = l / numSteps;
            var tStep = lStep * Math.Cos(a);
            var rStep = lStep * Math.Sin(a);

            var centerDouble = new double[] { center[0], center[1], center[2] };
            var c = centerDouble.ToArray();
            var cStep = dir.multiply(tStep, 3);

            for (var i = 1; i <= numSteps; i++)
            {
                var r = rStep * i;
                c = c.subtract(cStep, 3);
                var voxelsOnCircle = GetVoxelsWithinCircle(c, dir, r, true);
                foreach (var voxel in voxelsOnCircle)
                    voxels.Add(voxel);
            }

            return voxels.ToArray();
        }

        private static int[][] GetVoxelsOnHemisphere(int[] center, double[] dir, double radius)
        {
            var voxels = new HashSet<int[]>(new[] { center.ToArray() }, new SameCoordinates());

            var centerDouble = new double[] { center[0], center[1], center[2] };

            var numSteps = (int)Math.Ceiling(Math.PI * radius / 2 / 0.5);
            var aStep = Math.PI / 2 / numSteps;

            for (var i = 1; i <= numSteps; i++)
            {
                var a = aStep * i;
                var r = radius * Math.Sin(a);
                var tStep = radius * (1 - Math.Cos(a));
                var c = centerDouble.subtract(dir.multiply(tStep, 3), 3);
                var voxelsOnCircle = GetVoxelsWithinCircle(c, dir, r, true);
                foreach (var voxel in voxelsOnCircle)
                    voxels.Add(voxel);
            }

            return voxels.ToArray();
        }

        private int[][] ThickenMask(int[] vox, double[] dir, double toolDia, params string[] maskOptions)
        {
            if (toolDia <= 0) return new[] { vox };

            var radius = 0.5 * toolDia / VoxelSideLength;
            maskOptions = maskOptions.Length == 0 ? new[] { "flat" } : maskOptions;

            switch (maskOptions[0])
            {
                case "ball":
                    return GetVoxelsOnHemisphere(vox, dir, radius);
                case "cone":
                    double angle;
                    if (maskOptions.Length < 2) angle = 118;
                    else if (!double.TryParse(maskOptions[1], out angle))
                        angle = 118;
                    return GetVoxelsOnCone(vox, dir, radius, angle);
                default:
                    var voxDouble = new double[] { vox[0], vox[1], vox[2] };
                    return GetVoxelsWithinCircle(voxDouble, dir, radius);
            }
        }

        private int[][] CreateProjectionMask(double[] dir, double tLimit)
        {
            var initCoord = new[] { 0, 0, 0 };
            for (var i = 0; i < 3; i++)
                if (dir[i] < 0) initCoord[i] = VoxelsPerSide[i] - 1;
            var voxels = new List<int[]>(new[] { initCoord });
            var c = initCoord.add(new[] { 0.5, 0.5, 0.5 }, 3);
            var ts = FindIntersectionDistances(c, dir, tLimit);
            foreach (var t in ts)
            {
                var cInt = c.add(dir.multiply(t, 3), 3);
                for (var i = 0; i < 3; i++) cInt[i] = Math.Round(cInt[i], 5);
                voxels.Add(GetNextVoxelCoord(cInt, dir));
            }
            return voxels.ToArray();
        }

        //Exclusive by default (i.e. if line passes through vertex/edge it ony includes two voxels that are actually passed through)
        private static int[] GetNextVoxelCoord(double[] cInt, double[] direction)
        {
            var searchDirs = new List<int>();
            for (var i = 0; i < 3; i++)
                if (Math.Abs(direction[i]) > 0.001) searchDirs.Add(i);

            var searchSigns = new[] { 0, 0, 0 };
            foreach (var dir in searchDirs)
                searchSigns[dir] = Math.Sign(direction[dir]);

            var voxel = new int[3];
            for (var i = 0; i < 3; i++)
                if (Math.Sign(direction[i]) == -1)
                    voxel[i] = (int)Math.Ceiling(cInt[i] - 1);
                else voxel[i] = (int)Math.Floor(cInt[i]);

            return voxel;
        }

        //firstVoxel needs to be in voxel coordinates and represent the center of the voxel (i.e. {0.5, 0.5, 0.5})
        private double[] FindIntersectionDistances(double[] firstVoxel, double[] direction, double tLimit)
        {
            var intersections = new ConcurrentBag<double>();
            var searchDirs = new List<int>();

            for (var i = 0; i < 3; i++)
                if (Math.Abs(direction[i]) > 0.001) searchDirs.Add(i);

            var searchSigns = new[] { 0, 0, 0 };
            var firstInt = new[] { 0, 0, 0 };

            foreach (var dir in searchDirs)
            {
                searchSigns[dir] = Math.Sign(direction[dir]);
                firstInt[dir] = (int)(firstVoxel[dir] + 0.5 * searchSigns[dir]);
            }

            foreach (var dir in searchDirs)
            //Parallel.ForEach(searchDirs, dir =>
            {
                var c = firstVoxel[dir];
                var d = direction[dir];
                //var toValue = searchSigns[dir] == -1 ? 0 : voxelsPerDimension[lastLevel][dir];
                var toValue = searchSigns[dir] == -1 ? VoxelsPerSide[dir] - Math.Ceiling(tLimit) : Math.Ceiling(tLimit);
                var toInt = Math.Max(toValue, firstInt[dir]) + (searchSigns[dir] == -1 ? 1 : 0);
                var fromInt = Math.Min(toValue, firstInt[dir]);
                for (var i = fromInt; i < toInt; i++)
                {
                    var t = (i - c) / d;
                    if (t <= tLimit) intersections.Add(t);
                }
            } //);

            var sortedIntersections = new SortedSet<double>(intersections).ToArray();
            return sortedIntersections;
        }

        #endregion
    }
}
