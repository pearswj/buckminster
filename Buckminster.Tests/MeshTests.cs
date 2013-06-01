using System;
using NUnit.Framework;

using Buckminster;
using Rhino.Geometry;
using Mesh = Buckminster.Types.Mesh;

namespace Buckminster.Tests
{
    [TestFixture]
    public class MeshTests
    {
        [Test]
        public void CanDual()
        {
            // 1. set up dummy mesh
            // 2. apply dual
            // 3. compare number of vertex/face elements
            // OR
            // 1. set up dummy mesh
            // 2. dual twice
            // 3. remove boundary faces from original mesh and compare
        }

        
        [Test]
        public void CanCreateFromRhinoMesh()
        {
            // 1. get/create rhino mesh

            Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
            mesh.Vertices.Add(0.0, 0.0, 1.0); //0
            mesh.Vertices.Add(1.0, 0.0, 1.0); //1
            mesh.Vertices.Add(2.0, 0.0, 1.0); //2
            mesh.Vertices.Add(3.0, 0.0, 0.0); //3
            mesh.Vertices.Add(0.0, 1.0, 1.0); //4
            mesh.Vertices.Add(1.0, 1.0, 2.0); //5
            mesh.Vertices.Add(2.0, 1.0, 1.0); //6
            mesh.Vertices.Add(3.0, 1.0, 0.0); //7
            mesh.Vertices.Add(0.0, 2.0, 1.0); //8
            mesh.Vertices.Add(1.0, 2.0, 1.0); //9
            mesh.Vertices.Add(2.0, 2.0, 1.0); //10
            mesh.Vertices.Add(3.0, 2.0, 1.0); //11

            mesh.Faces.AddFace(0, 1, 5, 4);
            mesh.Faces.AddFace(1, 2, 6, 5);
            mesh.Faces.AddFace(2, 3, 7, 6);
            mesh.Faces.AddFace(4, 5, 9, 8);
            mesh.Faces.AddFace(5, 6, 10, 9);
            mesh.Faces.AddFace(6, 7, 11, 10);
            mesh.Normals.ComputeNormals();
            mesh.Compact();

            // 2. call BmMesh(Mesh) constructor

            Mesh polymesh = new Mesh(mesh);

            // 3. compare number of vertex/face elements
            // 4. compare number of halfedges against no. required for no. faces
        }
    }
}
