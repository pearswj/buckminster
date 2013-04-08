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
    }
}
