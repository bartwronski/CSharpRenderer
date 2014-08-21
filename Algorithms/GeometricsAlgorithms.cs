using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;
using SlimDX;
using System.IO;
using System.Diagnostics;

namespace CSharpRenderer
{
    static class GeometricsAlgorithms
    {
        public static Vector2 GetAxisProjectionLimits(Vector3[] points, Vector3 axis)
        {
            Vector2 limits = new Vector2(Single.MaxValue, Single.MinValue);

            foreach (Vector3 vec in points)
            {
                float projection = Vector3.Dot(vec, axis);
                limits.X = Math.Min(projection, limits.X);
                limits.Y = Math.Max(projection, limits.Y);
            }

            return limits;
        }
    };

    public struct Tetrahedron
    {
        public Vector3[] m_Vertices;

        public Vector3[] m_BarycentricMatrix;
        public Vector4[] m_TetPlanes;

        public BoundingBox m_BoundingBox;

        // optional
        public int[] m_VerticesIndices;

        public Tetrahedron(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 vertex4)
        {
            m_Vertices = new Vector3[4];
            m_BarycentricMatrix = new Vector3[3];
            m_VerticesIndices = new int[4];

            m_Vertices[0] = vertex1;
            m_Vertices[1] = vertex2;
            m_Vertices[2] = vertex3;
            m_Vertices[3] = vertex4;

            // Note, we need only 3x3 matrix, but I use Matrix class for clarity / convenience
            Matrix m = new Matrix();
            m[0, 0] = vertex1.X - vertex4.X;
            m[1, 0] = vertex2.X - vertex4.X;
            m[2, 0] = vertex3.X - vertex4.X;

            m[0, 1] = vertex1.Y - vertex4.Y;
            m[1, 1] = vertex2.Y - vertex4.Y;
            m[2, 1] = vertex3.Y - vertex4.Y;

            m[0, 2] = vertex1.Z - vertex4.Z;
            m[1, 2] = vertex2.Z - vertex4.Z;
            m[2, 2] = vertex3.Z - vertex4.Z;

            double det = (double)m[0, 0] * ((double)m[1, 1] * (double)m[2, 2] - (double)m[2, 1] * (double)m[1, 2]) -
                         (double)m[0, 1] * ((double)m[1, 0] * (double)m[2, 2] - (double)m[1, 2] * (double)m[2, 0]) +
                         (double)m[0, 2] * ((double)m[1, 0] * (double)m[2, 1] - (double)m[1, 1] * (double)m[2, 0]);

            double invdet = 1.0 / det;

            Matrix minv = new Matrix(); // inverse of matrix m
            minv[0, 0] = (float)(((double)m[1, 1] * (double)m[2, 2] - (double)m[2, 1] * (double)m[1, 2]) * invdet);
            minv[0, 1] = (float)(((double)m[0, 2] * (double)m[2, 1] - (double)m[0, 1] * (double)m[2, 2]) * invdet);
            minv[0, 2] = (float)(((double)m[0, 1] * (double)m[1, 2] - (double)m[0, 2] * (double)m[1, 1]) * invdet);
            minv[1, 0] = (float)(((double)m[1, 2] * (double)m[2, 0] - (double)m[1, 0] * (double)m[2, 2]) * invdet);
            minv[1, 1] = (float)(((double)m[0, 0] * (double)m[2, 2] - (double)m[0, 2] * (double)m[2, 0]) * invdet);
            minv[1, 2] = (float)(((double)m[1, 0] * (double)m[0, 2] - (double)m[0, 0] * (double)m[1, 2]) * invdet);
            minv[2, 0] = (float)(((double)m[1, 0] * (double)m[2, 1] - (double)m[2, 0] * (double)m[1, 1]) * invdet);
            minv[2, 1] = (float)(((double)m[2, 0] * (double)m[0, 1] - (double)m[0, 0] * (double)m[2, 1]) * invdet);
            minv[2, 2] = (float)(((double)m[0, 0] * (double)m[1, 1] - (double)m[1, 0] * (double)m[0, 1]) * invdet);

            m_BarycentricMatrix[0] = new Vector3(minv[0, 0], minv[1, 0], minv[2, 0]);
            m_BarycentricMatrix[1] = new Vector3(minv[0, 1], minv[1, 1], minv[2, 1]);
            m_BarycentricMatrix[2] = new Vector3(minv[0, 2], minv[1, 2], minv[2, 2]);

            m_BoundingBox = BoundingBox.FromPoints(m_Vertices);


            m_TetPlanes = new Vector4[4];

            /// Initialize tetrahedron planes
            Vector3 planeVector;
            float planeDistance;

            planeVector = Vector3.Normalize(Vector3.Cross(m_Vertices[1] - m_Vertices[0], m_Vertices[2] - m_Vertices[0]));
            planeDistance = -Vector3.Dot(planeVector, m_Vertices[0]);
            m_TetPlanes[0] = new Vector4(planeVector, planeDistance);

            planeVector = Vector3.Normalize(Vector3.Cross(m_Vertices[3] - m_Vertices[0], m_Vertices[2] - m_Vertices[0]));
            planeDistance = -Vector3.Dot(planeVector, m_Vertices[0]);
            m_TetPlanes[1] = new Vector4(planeVector, planeDistance);

            planeVector = Vector3.Normalize(Vector3.Cross(m_Vertices[1] - m_Vertices[0], m_Vertices[3] - m_Vertices[0]));
            planeDistance = -Vector3.Dot(planeVector, m_Vertices[0]);
            m_TetPlanes[2] = new Vector4(planeVector, planeDistance);

            planeVector = Vector3.Normalize(Vector3.Cross(m_Vertices[2] - m_Vertices[1], m_Vertices[3] - m_Vertices[1]));
            planeDistance = -Vector3.Dot(planeVector, m_Vertices[1]);
            m_TetPlanes[3] = new Vector4(planeVector, planeDistance);

        }

        public void CalculateBarycentricCoordinates(Vector3 point, out float b0, out float b1, out float b2, out float b3)
        {
            Vector3 referencePoint = point - m_Vertices[3];
            b0 = Vector3.Dot(referencePoint, m_BarycentricMatrix[0]);
            b1 = Vector3.Dot(referencePoint, m_BarycentricMatrix[1]);
            b2 = Vector3.Dot(referencePoint, m_BarycentricMatrix[2]);
            b3 = 1.0f - b0 - b1 - b2;
        }

        public bool IsVertexInside(Vector3 point)
        {
            float b0, b1, b2, b3;
            CalculateBarycentricCoordinates(point, out b0, out b1, out b2, out b3);
            if (b0 < 0 || b1 < 0 || b2 < 0 || b3 < 0)
            {
                return false;
            }

            return true;
        }

        public static Vector4 FindTwoTetrahedronsSplittingPlane(Tetrahedron first, Tetrahedron second)
        {
            List<Vector3> potentialSplitPlanes = new List<Vector3>();

            potentialSplitPlanes.Add(new Vector3(first.m_TetPlanes[0].X, first.m_TetPlanes[0].Y, first.m_TetPlanes[0].Z));
            potentialSplitPlanes.Add(new Vector3(first.m_TetPlanes[1].X, first.m_TetPlanes[1].Y, first.m_TetPlanes[1].Z));
            potentialSplitPlanes.Add(new Vector3(first.m_TetPlanes[2].X, first.m_TetPlanes[2].Y, first.m_TetPlanes[2].Z));
            potentialSplitPlanes.Add(new Vector3(first.m_TetPlanes[3].X, first.m_TetPlanes[3].Y, first.m_TetPlanes[3].Z));

            potentialSplitPlanes.Add(new Vector3(second.m_TetPlanes[0].X, second.m_TetPlanes[0].Y, second.m_TetPlanes[0].Z));
            potentialSplitPlanes.Add(new Vector3(second.m_TetPlanes[1].X, second.m_TetPlanes[1].Y, second.m_TetPlanes[1].Z));
            potentialSplitPlanes.Add(new Vector3(second.m_TetPlanes[2].X, second.m_TetPlanes[2].Y, second.m_TetPlanes[2].Z));
            potentialSplitPlanes.Add(new Vector3(second.m_TetPlanes[3].X, second.m_TetPlanes[3].Y, second.m_TetPlanes[3].Z));

            Vector3[] tetEdgeVectorsFirst = new Vector3[6];
            Vector3[] tetEdgeVectorsSecond = new Vector3[6];

            tetEdgeVectorsFirst[0] = first.m_Vertices[1] - first.m_Vertices[0];
            tetEdgeVectorsFirst[1] = first.m_Vertices[2] - first.m_Vertices[0];
            tetEdgeVectorsFirst[2] = first.m_Vertices[3] - first.m_Vertices[0];

            tetEdgeVectorsFirst[3] = first.m_Vertices[2] - first.m_Vertices[1];
            tetEdgeVectorsFirst[4] = first.m_Vertices[3] - first.m_Vertices[2];
            tetEdgeVectorsFirst[5] = first.m_Vertices[1] - first.m_Vertices[3];

            tetEdgeVectorsSecond[0] = second.m_Vertices[1] - second.m_Vertices[0];
            tetEdgeVectorsSecond[1] = second.m_Vertices[2] - second.m_Vertices[0];
            tetEdgeVectorsSecond[2] = second.m_Vertices[3] - second.m_Vertices[0];

            tetEdgeVectorsSecond[3] = second.m_Vertices[2] - second.m_Vertices[1];
            tetEdgeVectorsSecond[4] = second.m_Vertices[3] - second.m_Vertices[2];
            tetEdgeVectorsSecond[5] = second.m_Vertices[1] - second.m_Vertices[3];

            foreach (var tetEdgeFirst in tetEdgeVectorsFirst)
            {
                foreach (var tetEdgeSecond in tetEdgeVectorsSecond)
                {
                    // Normal of plane constructed by those 2 edges
                    var planeNormal = Vector3.Cross(tetEdgeFirst, tetEdgeSecond);

                    if (planeNormal.LengthSquared() > 0.001f)
                    {
                        planeNormal.Normalize();
                        potentialSplitPlanes.Add(planeNormal);
                    }
                }
            }

            const float epsilon = 0.001f;// fixed 1mm

            foreach (var splittingPlaneNormal in potentialSplitPlanes)
            {
                Vector2 tetOneLimits = GeometricsAlgorithms.GetAxisProjectionLimits(first.m_Vertices, splittingPlaneNormal);
                Vector2 tetTwoLimits = GeometricsAlgorithms.GetAxisProjectionLimits(second.m_Vertices, splittingPlaneNormal);

                if ((tetOneLimits.X + epsilon) > tetTwoLimits.Y)
                {
                    return new Vector4(splittingPlaneNormal, -(tetOneLimits.X + tetTwoLimits.Y) / 2.0f);
                }
                if (tetOneLimits.Y < (tetTwoLimits.X + epsilon))
                {
                    return new Vector4(splittingPlaneNormal, -(tetOneLimits.Y + tetTwoLimits.X) / 2.0f);
                }
            }

            throw new Exception("no splitting plane found");
        }

        public bool DoesBoxOverlap(BoundingBox box)
        {
            /// We are using separating axis theorem
            /// We have to test 4 planes from tetrahedron, 3 planes from BB (trivial) and 6 * 3 planes from cross product of edges
            /// 
            /// First early out on box planes - trivial world space bounding box
            /// 

            if (!BoundingBox.Intersects(m_BoundingBox, box))
                return false;

            Vector3[] corners = box.GetCorners();

            Vector4 baryCentricBoundsMin = new Vector4(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);
            Vector4 baryCentricBoundsMax = new Vector4(float.MinValue, float.MinValue, float.MinValue, float.MinValue);

            /// Second check tetrahedron planes, easy due to barycentric matrices
            foreach (var vert in corners)
            {
                float b0, b1, b2, b3;
                CalculateBarycentricCoordinates(vert, out b0, out b1, out b2, out b3);

                baryCentricBoundsMin = Vector4.Minimize(baryCentricBoundsMin, new Vector4(b0, b1, b2, b3));
                baryCentricBoundsMax = Vector4.Maximize(baryCentricBoundsMax, new Vector4(b0, b1, b2, b3));
            }

            if (baryCentricBoundsMin.X > 1.0f || baryCentricBoundsMin.Y > 1.0f || baryCentricBoundsMin.Z > 1.0f || baryCentricBoundsMin.W > 1.0f)
                return false;

            if (baryCentricBoundsMax.X < 0.0f || baryCentricBoundsMax.Y < 0.0f || baryCentricBoundsMax.Z < 0.0f || baryCentricBoundsMax.W < 0.0f)
                return false;

            // Finally, more difficult part - planes between the edges of both
            Vector3[] boxVectors = { new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1) };
            Vector3[] tetEdgeVectors = new Vector3[6];
            // Note: not necessary to normalize, we are interested in axis limits overlap
            // And those vectors cannot be 0 (degenerated tetrahedron)

            tetEdgeVectors[0] = m_Vertices[1] - m_Vertices[0];
            tetEdgeVectors[1] = m_Vertices[2] - m_Vertices[0];
            tetEdgeVectors[2] = m_Vertices[3] - m_Vertices[0];

            tetEdgeVectors[3] = m_Vertices[2] - m_Vertices[1];
            tetEdgeVectors[4] = m_Vertices[3] - m_Vertices[2];
            tetEdgeVectors[5] = m_Vertices[1] - m_Vertices[3];

            foreach (var tetEdge in tetEdgeVectors)
            {
                foreach (var boxEdge in boxVectors)
                {
                    // Normal of plane constructed by those 2 edges
                    var planeNormal = Vector3.Cross(tetEdge, boxEdge);

                    if (planeNormal.LengthSquared() > 0.0001f)
                    {
                        Vector2 tetLimits = GeometricsAlgorithms.GetAxisProjectionLimits(m_Vertices, planeNormal);
                        Vector2 boxLimits = GeometricsAlgorithms.GetAxisProjectionLimits(corners, planeNormal);

                        if (tetLimits.X > boxLimits.Y || tetLimits.Y < boxLimits.X)
                        {
                            return false;
                        }
                    }
                }
            }


            return true;
        }
    };

}