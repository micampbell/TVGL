﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Math;
using STPLoader;
using STPLoader.Implementation.Model;
using STPLoader.Implementation.Model.Entity;

namespace TVGL.IOFunctions.Step
{
    class SurfaceConvertable : IConvertable
    {
        private readonly Surface _surface;
        private readonly StpFile _model;

        public SurfaceConvertable(Surface surface, StpFile model)
        {
            _surface = surface;
            _model = model;
            Init();
        }

        private void Init()
        {
            var child = Convertable(_surface, _model);
            Points = child.Points;
            Indices = child.Indices;
        }

        private static IConvertable Convertable(Surface surface, StpFile model)
        {
            if (surface.GetType() == typeof (CylindricalSurface))
            {
                return new CylindricalSurfaceConvertable(surface, model);
            }
            else if (surface.GetType() == typeof(ConicalSurface))
            {
                return new ConicalSurfaceConvertable(surface, model);
            }
            else if (surface.GetType() == typeof(Plane))
            {
                return new PlaneConvertable(surface, model);
            }
            else if (surface.GetType() == typeof(ToroidalSurface))
            {
                return new ToroidalSurfaceConvertable(surface, model);
            }
            throw new Exception("No convertable found!");
        }

        public IList<Vector3> Points { get; private set; }
        public IList<int> Indices { get; private set; }
    }

    internal class ToroidalSurfaceConvertable : IConvertable
    {
        private readonly Surface _surface;
        private readonly StpFile _model;

        public ToroidalSurfaceConvertable(Surface surface, StpFile model)
        {
            _surface = surface;
            _model = model;
            Init();
        }

        private void Init()
        {
            Points = new List<Vector3>();
            Indices = new List<int>();
            
        }

        public IList<Vector3> Points { get; private set; }
        public IList<int> Indices { get; private set; }
    }
    
    internal class ConicalSurfaceConvertable : IConvertable
    {
        private readonly Surface _surface;
        private readonly StpFile _model;

        public ConicalSurfaceConvertable(Surface surface, StpFile model)
        {
            _surface = surface;
            _model = model;
            Init();
        }

        private void Init()
        {
            Points = new List<Vector3>();
            Indices = new List<int>();
        }

        public IList<Vector3> Points { get; private set; }
        public IList<int> Indices { get; private set; }
    }
}
