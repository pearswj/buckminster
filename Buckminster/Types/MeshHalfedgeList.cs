using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Buckminster.Types.Collections
{
    /// <summary>
    /// 
    /// </summary>
    public class MeshHalfedgeList : KeyedCollection<String, Halfedge>
    {
        protected override string GetKeyForItem(Halfedge edge)
        {
            return edge.Name;
        }

        #region add methods
        public void Add(Vertex vertex)
        {
            this.Add(new Halfedge(vertex));
        }
        public void Add(Vertex vertex, Halfedge next, Halfedge prev, Face face)
        {
            this.Add(new Halfedge(vertex, next, prev, face));
        }
        public void Add(Vertex vertex, Halfedge next, Halfedge prev, Face face, Halfedge pair)
        {
            this.Add(new Halfedge(vertex, next, prev, face, pair));
        }
        #endregion
    }
}
