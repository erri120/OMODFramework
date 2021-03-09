﻿using System;
using JetBrains.Annotations;

namespace OMODFramework
{
    [PublicAPI]
    public enum OMODScriptType : byte
    {
        /// <summary>
        /// Classic OBMMScript
        /// </summary>
        OBMMScript,
        
        /// <summary>
        /// Python using IronPython (not supported)
        /// </summary>
        [Obsolete("Not supported, use C# or OBMMScript instead.")]
        Python,
        
        /// <summary>
        /// C#
        /// </summary>
        CSharp,
        
        /// <summary>
        /// Visual Basic (not supported)
        /// </summary>
        [Obsolete("Not supported, use C# or OBMMScript instead.")]
        VisualBasic
    }
}
