﻿using System;
using System.Linq;
using STPLoader.Implementation.Parser;

namespace TVGL.IOFunctions.Step
{
    /// <summary>
    /// 
    /// </summary>
    public class Plane : Surface
    {
        /// <summary>
        /// 
        /// </summary>
        public string Info;
        /// <summary>
        /// 
        /// </summary>
        public long AxisId;

        public override void Init()
        {
            Info = ParseHelper.Parse<string>(Data[0]);
            AxisId = ParseHelper.ParseId(Data[1]);
        }

        public override string ToString()
        {
            return String.Format("<Plane({0}, {1})", Info, AxisId);
        }
    }

}
