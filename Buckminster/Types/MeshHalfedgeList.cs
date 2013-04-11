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
        private Mesh m_mesh;
        /// <summary>
        /// Creates a halfedge list that is aware of its parent mesh
        /// </summary>
        /// <param name="mesh"></param>
        public MeshHalfedgeList(Mesh mesh)
            : base()
        {
            m_mesh = mesh;
        }
        /// <summary>
        /// Convenience constructor, for use outside of the mesh class
        /// </summary>
        public MeshHalfedgeList()
            : base()
        {
            m_mesh = null;
        }
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
        /// <returns>true on success, false if changing the vertex of this halfedge would cause it to duplicate an existing halfedge</returns>
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
        /// <summary>
        /// Searches for and pairs opposing halfedges
        /// </summary>
        /// <param name="items">a list of halfedges</param>
        /// <returns>the number of halfedges for which a pair was found</returns>
        //public int MatchPairs(IEnumerable<Halfedge> items)
        public int MatchPairs()
        {
            int count = 0;
            foreach (Halfedge edge in this)
            {
                String rname = edge.Prev.Vertex.Name + edge.Vertex.Name;
                if (Contains(rname))
                {
                    edge.Pair = this[rname];
                    count++;
                }
            }
            return count;
        }
        public List<Halfedge> GetUnique()
        {
            MeshHalfedgeList marker = new MeshHalfedgeList();
            List<Halfedge> unique = new List<Halfedge>();
            
            foreach (Halfedge halfedge in this)
            {
                // do not add halfedge to unique list if its pair has already been added
                if (marker.Contains(halfedge.Name)) { continue; }

                // otherwise, add it to the unique list and 'mark' its pair
                unique.Add(halfedge);

                if (halfedge.Pair != null)
                {
                    marker.Add(this[halfedge.Pair.Name]);
                }
            }
            return unique;
        }
        public void Flip()
        {
            int n = Count;
            var vertices = new Vertex[n];
            var edges = new Halfedge[n];
            for (int i = 0; i < n; i++)
            {
                vertices[i] = this[i].Prev.Vertex;
                edges[i] = this[i];
            }
            Clear();
            for (int i = 0; i < n; i++)
            {
                var edge = edges[i];
                var temp = edge.Next;
                edge.Next = edge.Prev;
                edge.Prev = temp;
                edge.Vertex = vertices[i];
            }
            foreach (Halfedge edge in edges)
            {
                Add(edge);
            }
        }
    }
}
