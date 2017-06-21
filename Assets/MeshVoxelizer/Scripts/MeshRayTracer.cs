using System;
using System.Collections.Generic;

using UnityEngine;

namespace MeshVoxelizerProject
{

    public struct MeshRay
    {
        public float u, v, w;
        public bool hit;
        public float distance;
        public float faceSign;
        public int faceIndex;
    }

    public class MeshRayTracer
    {

        public int MaxDepth { get; private set; }

        public int LeafNodes { get; private set; }

        public float AvgFacesPerLeaf { get { return Faces.Length / (float)LeafNodes; } }

        private IList<Vector3> Vertices { get; set; }

		private IList<int> Indices { get; set; }

        private int NumFaces { get; set; }

        private int FreeNode { get; set; }

        private int InnerNodes { get; set; }

        private List<AABBNode> Nodes { get; set; }

        private int[] Faces { get; set; }

        private List<Box3> FaceBounds { get; set; }

        private int CurrentDepth { get; set; }

        public MeshRayTracer(IList<Vector3> vertices, IList<int> indices) 
        {
            Vertices = vertices;
            Indices = indices;
            NumFaces = indices.Count / 3;

            Nodes = new List<AABBNode>((int)(NumFaces * 1.5));
            Faces = new int[NumFaces];
            FaceBounds = new List<Box3>();

            Build();
        }

        public List<Box3> GetBounds(int level = -1)
        {
            List<Box3> bounds = new List<Box3>();

            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].Level == level || level == -1)
                    bounds.Add(Nodes[i].Bounds);
            }

            return bounds;
        }

        private void Build()
        {

            MaxDepth = 0;
            InnerNodes = 0;
            LeafNodes = 0;
            CurrentDepth = 0;
            FaceBounds.Clear();

            for (int i = 0; i < NumFaces; i++)
            {
                Box3 top = CalculateFaceBounds(i);

                Faces[i] = i;
                FaceBounds.Add(top);
            }

            CurrentDepth = 0;
	        FreeNode = 1;
            BuildRecursive(0, 0, NumFaces);

        }

        public MeshRay TraceRay(Vector3 start, Vector3 dir)
        {

            MeshRay ray = new MeshRay();
            ray.distance = float.PositiveInfinity;

            TraceRecursive(0, start, dir, ref ray);

            ray.hit = ray.distance != float.PositiveInfinity;

            return ray;
        }

        private void TraceRecursive(int nodeIndex, Vector3 start, Vector3 dir, ref MeshRay ray)
        {
	        AABBNode node = Nodes[nodeIndex];

            if (node.Faces == null)
            {
                // find closest node
                AABBNode leftChild = Nodes[node.Children+0];
                AABBNode rightChild = Nodes[node.Children+1];

                float[] dist = new float[]{float.PositiveInfinity, float.PositiveInfinity};

                IntersectRayAABB(start, dir, leftChild.Bounds.Min, leftChild.Bounds.Max, out dist[0]);
                IntersectRayAABB(start, dir, rightChild.Bounds.Min, rightChild.Bounds.Max, out dist[1]);
        
                int closest = 0;
                int furthest = 1;
		
                if (dist[1] < dist[0])
                {
                    closest = 1;
                    furthest = 0;
                }		

                if (dist[closest] < ray.distance)
                    TraceRecursive(node.Children + closest, start, dir, ref ray);

                if (dist[furthest] < ray.distance)
                    TraceRecursive(node.Children + furthest, start, dir, ref ray);
            }
            else
            {
                float t, u, v, w, s;

                for (int i=0; i < node.Faces.Length; ++i)
                {
                    int indexStart = node.Faces[i]*3;

                    Vector3 a = Vertices[Indices[indexStart + 0]];
                    Vector3 b = Vertices[Indices[indexStart + 1]];
                    Vector3 c = Vertices[Indices[indexStart + 2]];

                    if (IntersectRayTriTwoSided(start, dir, a, b, c, out t, out u, out v, out w, out s))
                    {
                        if (t < ray.distance)
                        {
                            ray.distance = t;
					        ray.u = u;
					        ray.v = v;
					        ray.w = w;
                            ray.faceSign = s;
                            ray.faceIndex = node.Faces[i];
                        }
                    }
                }
            }
        }

        public MeshRay TraceRaySlow(Vector3 start, Vector3 dir)
        {    
            float minT, minU, minV, minW, minS;
	        minT = minU = minV = minW = minS = float.PositiveInfinity;

            float t, u, v, w, s;
            bool hit = false;
	        int minIndex = 0;

            for (int i = 0; i < NumFaces; ++i)
            {
                Vector3 a = Vertices[Indices[i*3+0]];
                Vector3 b = Vertices[Indices[i*3+1]];
                Vector3 c = Vertices[Indices[i * 3 + 2]];

                if (IntersectRayTriTwoSided(start, dir, a, b, c, out t, out u, out v, out w, out s))
                {
                    if (t < minT)
                    {
                        minT = t;
				        minU = u;
				        minV = v;
				        minW = w;
				        minS = s;
				        minIndex = i;
                        hit = true;
                    }
                }
            }

            MeshRay ray = new MeshRay();

            ray.distance = minT;
            ray.u = minU;
            ray.v = minV;
            ray.w = minW;
            ray.faceSign = minS;
            ray.faceIndex = minIndex;
            ray.hit = hit;

            return ray;
        }

        private void BuildRecursive(int nodeIndex, int start, int numFaces)
        {
            int MaxFacesPerLeaf = 6;

            // a reference to the current node, need to be careful here as this reference may become invalid if array is resized
            AABBNode n = GetNode(nodeIndex);
            
            // track max tree depth
            ++CurrentDepth;
            MaxDepth = Math.Max(MaxDepth, CurrentDepth);

            int[] faces = GetFaces(start, numFaces);

            Vector3 min, max;
            CalculateFaceBounds(faces, out min, out max);

            n.Bounds = new Box3(min, max);
            n.Level = CurrentDepth - 1;

            // calculate bounds of faces and add node  
            if (numFaces <= MaxFacesPerLeaf)
            {
                n.Faces = faces;
                ++LeafNodes;
            }
            else
            {
                ++InnerNodes;

                // face counts for each branch
                //const uint32_t leftCount = PartitionMedian(n, faces, numFaces);
                int leftCount = PartitionSAH(faces);
                int rightCount = numFaces - leftCount;

                // alloc 2 nodes
                Nodes[nodeIndex].Children = FreeNode;

                // allocate two nodes
                FreeNode += 2;

                // split faces in half and build each side recursively
                BuildRecursive(GetNode(nodeIndex).Children + 0, start, leftCount);
                BuildRecursive(GetNode(nodeIndex).Children + 1, start + leftCount, rightCount);
            }

            --CurrentDepth;
        }

        // partion faces based on the surface area heuristic
        private int PartitionSAH(int[] faces)
        {
            int numFaces = faces.Length;
            int bestAxis = 0;
	        int bestIndex = 0;
	        float bestCost = float.PositiveInfinity;

            FaceSorter predicate = new FaceSorter();
            predicate.Vertices = Vertices;
            predicate.Indices = Indices;

            // two passes over data to calculate upper and lower bounds
            float[] cumulativeLower = new float[numFaces];
            float[] cumulativeUpper = new float[numFaces];

            for (int a = 0; a < 3; ++a)	
	        {
		        // sort faces by centroids
                predicate.Axis = a;
                Array.Sort(faces, predicate);

                Box3 lower = new Box3(float.PositiveInfinity, float.NegativeInfinity);
		        Box3 upper = new Box3(float.PositiveInfinity, float.NegativeInfinity);

		        for (int i = 0; i < numFaces; ++i)
		        {
                    lower.Min = Min(lower.Min, FaceBounds[faces[i]].Min);
                    lower.Max = Max(lower.Max, FaceBounds[faces[i]].Max);

                    upper.Min = Min(upper.Min, FaceBounds[faces[numFaces - i - 1]].Min);
                    upper.Max = Max(upper.Max, FaceBounds[faces[numFaces - i - 1]].Max);

                    cumulativeLower[i] = lower.SurfaceArea;
                    cumulativeUpper[numFaces - i - 1] = upper.SurfaceArea;
                }

                float invTotalSA = 1.0f / cumulativeUpper[0];

		        // test all split positions
		        for (int i = 0; i < numFaces-1; ++i)
		        {
			        float pBelow = cumulativeLower[i] * invTotalSA;
			        float pAbove = cumulativeUpper[i] * invTotalSA;

                    float cost = 0.125f + (pBelow * i + pAbove * (numFaces - i));
                    if (cost <= bestCost)
			        {
				        bestCost = cost;
				        bestIndex = i;
				        bestAxis = a;
			        }
		        }
	        }

            // re-sort by best axis
            predicate.Axis = bestAxis;
            Array.Sort(faces, predicate);

            return bestIndex+1;
        }

        private void CalculateFaceBounds(int[] faces, out Vector3 outMin, out Vector3 outMax)
        {
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            // calculate face bounds
            for (int i = 0; i < faces.Length; ++i)
            {
                Vector3 a = Vertices[Indices[faces[i] * 3 + 0]];
                Vector3 b = Vertices[Indices[faces[i] * 3 + 1]];
                Vector3 c = Vertices[Indices[faces[i] * 3 + 2]];

                min = Min(a, min);
                max = Max(a, max);

                min = Min(b, min);
                max = Max(b, max);

                min = Min(c, min);
                max = Max(c, max);
            }

            outMin = min;
            outMax = max;
        }

        private Box3 CalculateFaceBounds(int i)
        {
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            Vector3 a = Vertices[Indices[i + 0]];
            Vector3 b = Vertices[Indices[i + 1]];
            Vector3 c = Vertices[Indices[i + 2]];

            min = Min(a, min);
            max = Max(a, max);

            min = Min(b, min);
            max = Max(b, max);

            min = Min(c, min);
            max = Max(c, max);

            return new Box3(min, max);
        }

        private Vector3 Min(Vector3 a, Vector3 b)
        {
            a.x = Math.Min(a.x, b.x);
            a.y = Math.Min(a.y, b.y);
            a.z = Math.Min(a.z, b.z);

            return a;
        }

        private Vector3 Max(Vector3 a, Vector3 b)
        {
            a.x = Math.Max(a.x, b.x);
            a.y = Math.Max(a.y, b.y);
            a.z = Math.Max(a.z, b.z);

            return a;
        }

        private AABBNode GetNode(int index)
        {
            if (index >= Nodes.Count)
            {
                int diff = index - Nodes.Count + 1;
                for(int i = 0; i < diff; i++)
                    Nodes.Add(new AABBNode());
            }

            return Nodes[index];
        }

        private int[] GetFaces(int start, int num)
        {
            int[] faces = new int[num];

            for (int i = 0; i < num; i++)
                faces[i] = Faces[i + start];

            return faces;
        }

        private bool IntersectRayTriTwoSided(Vector3 p, Vector3 dir, Vector3 a, Vector3 b, Vector3 c, out float t, out float u, out float v, out float w, out float sign)
        {
            // Moller and Trumbore's method
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 n = Vector3.Cross(ab, ac);

            float d = Vector3.Dot(dir * -1.0f, n);
            float ood = 1.0f / d; 
            Vector3 ap = p - a;

            t = u = v = w = sign = 0.0f;

            t = Vector3.Dot(ap, n) * ood;
            if (t < 0.0f)
                return false;

            Vector3 e = Vector3.Cross(dir * -1.0f, ap);

            v = Vector3.Dot(ac, e) * ood;
            if (v < 0.0 || v > 1.0)
                return false;

            w = -Vector3.Dot(ab, e) * ood;
            if (w < 0.0 || v + w > 1.0)
                return false;

            u = 1.0f - v - w;
	        sign = d;

            return true;
        }

        private bool IntersectRayAABB(Vector3 start, Vector3 dir, Vector3 min, Vector3 max, out float t)
        {
	        //calculate candidate plane on each axis
	        float tx = -1.0f, ty = -1.0f, tz = -1.0f;
	        bool inside = true;
            t = 0;
			
	        if (start.x < min.x)
	        {
		        if (dir.x != 0.0)
			        tx = (min.x-start.x)/dir.x;
		        inside = false;
	        }
	        else if (start.x > max.x)
	        {
		        if (dir.x != 0.0)
			        tx = (max.x-start.x)/dir.x;
		        inside = false;
	        }

	        if (start.y < min.y)
	        {
		        if (dir.y != 0.0)
			        ty = (min.y-start.y)/dir.y;
		        inside = false;
	        }
	        else if (start.y > max.y)
	        {
		        if (dir.y != 0.0)
			        ty = (max.y-start.y)/dir.y;
		        inside = false;
	        }

	        if (start.z < min.z)
	        {
		        if (dir.z != 0.0)
			        tz = (min.z-start.z)/dir.z;
		        inside = false;
	        }
	        else if (start.z > max.z)
	        {
		        if (dir.z != 0.0)
			        tz = (max.z-start.z)/dir.z;
		        inside = false;
	        }

	        //if point inside all planes
	        if (inside)
            {
                t = 0.0f;
		        return true;
            }

	        //we now have t values for each of possible intersection planes
	        //find the maximum to get the intersection point
	        float tmax = tx;
	        int taxis = 0;

	        if (ty > tmax)
	        {
		        tmax = ty;
		        taxis = 1;
	        }
	        if (tz > tmax)
	        {
		        tmax = tz;
		        taxis = 2;
	        }

	        if (tmax < 0.0f)
		        return false;

	        //check that the intersection point lies on the plane we picked
	        //we don't test the axis of closest intersection for precision reasons

	        //no eps for now
	        float eps = 0.0f;

	        Vector3 hit = start + dir*tmax;

	        if ((hit.x < min.x-eps || hit.x > max.x+eps) && taxis != 0)
		        return false;
	        if ((hit.y < min.y-eps || hit.y > max.y+eps) && taxis != 1)
		        return false;
	        if ((hit.z < min.z-eps || hit.z > max.z+eps) && taxis != 2)
		        return false;

	        //output results
	        t = tmax;
	        return true;
        }

        private class AABBNode
        {
            public Box3 Bounds { get; internal set; }

            public bool IsLeaf { get { return Faces == null; } }

            public int Level { get; internal set; }

            internal int[] Faces { get; set; }

            internal int Children { get; set; }
        };

        private class FaceSorter : IComparer<int>
        {
            internal IList<Vector3> Vertices;
            internal IList<int> Indices;
            internal int Axis;

            public int Compare(int i0, int i1)
            {
                float a = GetCentroid(i0);
                float b = GetCentroid(i1);

                return a.CompareTo(b);
            }

            private float GetCentroid(int face)
            {
                Vector3 a = Vertices[Indices[face * 3 + 0]];
                Vector3 b = Vertices[Indices[face * 3 + 1]];
                Vector3 c = Vertices[Indices[face * 3 + 2]];

                return (a[Axis] + b[Axis] + c[Axis]) / 3.0f;
            }
        }

    }

}