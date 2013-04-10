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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="he">halfedge to change</param>
        /// <param name="new_vertex">new vertex</param>
        /// <returns>true on success, otherwise false</returns>
        public Boolean SetVertex(Halfedge he, Vertex new_vertex)
        {
            try
            {
                ChangeItemKey(he, new_vertex.Name + he.Prev.Vertex.Name);
                ChangeItemKey(he.Next, he.Next.Vertex.Name + new_vertex.Name);
            }
            catch (ArgumentException)
            {
                return false;
            }
            he.Vertex = new_vertex;
            return true;
        }
    }
}
