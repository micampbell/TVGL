﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using STPLoader.Implementation.Parser;

namespace TVGL.IOFunctions.Step
{
    /// <summary>
    /// 
    /// </summary>
    public class ClosedShell : Entity
    {
        /// <summary>
        /// 
        /// </summary>
        public string Info;
        /// <summary>
        /// 
        /// </summary>
        public IList<long> PointIds;
        
        public override void Init()
        {
            Info = ParseHelper.ParseString(Data[0]);
            PointIds = ParseHelper.ParseList<string>(Data[1]).Select(ParseHelper.ParseId).ToList();
        }

        public override string ToString()
        {
            return String.Format("<ClosedShell({0}, {1})", Info, PointIds);
        }
    }

}
