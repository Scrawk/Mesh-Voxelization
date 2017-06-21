using System;
using System.Collections.Generic;

using UnityEngine;

namespace MeshVoxelizerProject
{

    public class MeshVoxelizer
    {

        public int Count { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int Depth { get; private set; }

        public int[,,] Voxels { get; private set; }

        public List<Box3> Bounds { get; private set; }

        public MeshVoxelizer(int width, int height, int depth)
        {

            Width = width;
            Height = height;
            Depth = depth;
            Bounds = new List<Box3>();
            Voxels = new int[width, height, depth];
        }
        public void Voxelize(IList<Vector3> vertices, IList<int> indices, Box3 bounds)
        {
            Array.Clear(Voxels, 0, Voxels.Length);

            // build an aabb tree of the mesh
            MeshRayTracer tree = new MeshRayTracer(vertices, indices);
            Bounds = tree.GetBounds();

            // parity count method, single pass
            Vector3 extents = bounds.Size;
	        Vector3 delta = new Vector3(extents.x/Width, extents.y/Height, extents.z/Depth);
	        Vector3 offset = new Vector3(0.5f/Width, 0.5f/Height, 0.5f/Depth);

            float eps = 1e-7f * extents.z;

            for (int x = 0; x < Width; ++x)
	        {
		        for (int y = 0; y < Height; ++y)
		        {
			        bool inside = false;
			        Vector3 rayDir = new Vector3(0.0f, 0.0f, 1.0f);

                     // z-coord starts somewhat outside bounds 
			        Vector3 rayStart = bounds.Min + new Vector3(x*delta.x + offset.x, y*delta.y + offset.y, -0.0f*extents.z);

			        while(true)
			        {
                        MeshRay ray = tree.TraceRay(rayStart, rayDir);

                        if (ray.hit)
				        {
                            // calculate cell in which intersection occurred
                            float zpos = rayStart.z + ray.distance * rayDir.z;
                            float zhit = (zpos - bounds.Min.z) / delta.z;

                            int z = (int)((rayStart.z - bounds.Min.z) / delta.z);
                            int zend = (int)Math.Min(zhit, Depth - 1);

                            if (inside)
                            {
                                for (int k = z; k <= zend; ++k)
                                {
                                    Voxels[x, y, k] = 1;
                                    Count++;
                                }
                            }

                            inside = !inside;
					        rayStart += rayDir*(ray.distance + eps);
                        }
				        else
					        break;
			        }
		        }
	        }
	
            //end
        }

    }

}
