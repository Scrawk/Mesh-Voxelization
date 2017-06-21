using UnityEngine;
using System;
using System.Collections.Generic;

namespace MeshVoxelizerProject
{

    public static class DrawLines
    {

        private static IList<Vector4> m_corners = new Vector4[8];

        private static Material m_lineMaterial;
        private static Material LineMaterial
        {
            get
            {
                if(m_lineMaterial == null)
                    m_lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                return m_lineMaterial;
            }
        }

        private static IList<int> m_cube = new int[]
        {
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        };

        public static void DrawBounds(Camera camera, Color color, Box3 bounds, Matrix4x4 localToWorld)
        {
            if (camera == null) return;

            bounds.GetCorners(m_corners);

            for (int i = 0; i < 8; i++)
                m_corners[i] = localToWorld * m_corners[i];

            DrawVerticesAsLines(camera, color, m_corners, m_cube);
        }

        private static void DrawVerticesAsLines(Camera camera, Color color, IList<Vector4> vertices, IList<int> indices)
        {
            if (camera == null || vertices == null || indices == null) return;

            GL.PushMatrix();

            GL.LoadIdentity();
            GL.MultMatrix(camera.worldToCameraMatrix);
            GL.LoadProjectionMatrix(camera.projectionMatrix);

            LineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(color);

            int vertexCount = vertices.Count;

            for (int i = 0; i < indices.Count / 2; i++)
            {
                int i0 = indices[i * 2 + 0];
                int i1 = indices[i * 2 + 1];

                if (i0 < 0 || i0 >= vertexCount) continue;
                if (i1 < 0 || i1 >= vertexCount) continue;

                GL.Vertex(vertices[i0]);
                GL.Vertex(vertices[i1]);
            }

            GL.End();

            GL.PopMatrix();
        }

    }

}