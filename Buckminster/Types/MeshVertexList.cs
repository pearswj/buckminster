using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Buckminster
{
    /// <summary>
    /// 
    /// </summary>
    public class MeshVertexList : KeyedCollection<String, Vertex>
    {
        protected override string GetKeyForItem(Vertex vertex)
        {
            return vertex.Name;
        }
    }
}
