using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Buckminster.Types.Collections
{
    public class MeshFaceList : KeyedCollection<String, Face>
    {
        private Mesh m_mesh;
        public MeshFaceList(Mesh mesh) : base()
        {
            m_mesh = mesh;
        }
        protected override string GetKeyForItem(Face face)
        {
            return face.Name;
        }
        /// <summary>
        /// Reduce an n-gon mesh face to triangles.
        /// </summary>
        /// <param name="index">index of the mesh face to be triangulated</param>
        /// <param name="quads">if true, quad-faces will not be touched</param>
        /// <returns>the number of new tri-faces created</returns>
        ///
        public int Triangulate(int index, bool quads)
        {
            // fan method
            Face face = this[index];
            Vertex start = face.Halfedge.Vertex;
            List<Vertex> end = face.GetVertices();
            if (end.Count < 4 && !quads || end.Count <= 4 && quads) { return 0; }
            int count = 0;
            for (int i = 2; i < end.Count-1; i++)
            {
                Face f_new;
                Halfedge he_new, he_new_pair;
                face.Split(start, end[i], out f_new, out he_new, out he_new_pair);
                m_mesh.Faces.Add(f_new);
                m_mesh.Halfedges.Add(he_new);
                m_mesh.Halfedges.Add(he_new_pair);
                count++;
            }
            return count;
        }
        public int Triangulate(int index)
        {
            return this.Triangulate(index, false);
        }
    }
}
