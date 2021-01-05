# Mesh-Voxelization

This project converts a mesh into voxels in Unity. The idea is to ray trace the mesh and find where each ray intersects a triangle. These positions can then be used to make a 3D array of voxels.


The ray tracing is accelerated by using a AABB tree to group the mesh triangles. The AABB tree should be much faster for large meshes but the overhead might not be worth it for smaller meshes.


The original code for the AABB tree can be found in the core section of [this](https://github.com/mmacklin/sandbox) collection of code from Miles Macklins [blog](http://blog.mmacklin.com/).


For the demo scene the voxels are converted back into a mesh by adding quads at the edge of each voxel.

The mesh before voxelization.

![Before voxelization](./Media/BeforeVoxelization.jfif)

The mesh after voxelization.

![After voxelization](./Media/AfterVoxelization.jfif)
