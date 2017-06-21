# Mesh-Voxelization

This project converts a mesh into voxels in Unity. The idea is to ray trace the mesh and find where each ray intersects a triangle. These positions can then be used to make a 3D array of voxels.


The ray tracing is accelerated by using a AABB tree to group the mesh triangles. The AABB tree should be much faster for large meshes but the overhead might not be worth it for smaller meshes.


The original code for the AABB tree can be found in the core section of [this](https://github.com/mmacklin/sandbox) collection of code from Miles Macklins [blog](http://blog.mmacklin.com/).


For the demo scene the voxels are converted back into a mesh by adding quads at the edge of each voxel.

See [home page](https://www.digital-dust.com/single-post/2017/04/17/Mesh-voxelization-in-Unity) for more information.

The mesh before voxelization.

![Before voxelization](https://static.wixstatic.com/media/1e04d5_cb4d56c0ad57454b938fc883779888e4~mv2.jpg/v1/fill/w_550,h_550,al_c,q_80,usm_0.66_1.00_0.01/1e04d5_cb4d56c0ad57454b938fc883779888e4~mv2.jpg)

The mesh after voxelization.

![After voxelization](https://static.wixstatic.com/media/1e04d5_f8c8cebbe5c54ffe98b2bc1eb6ae2c82~mv2.jpg/v1/fill/w_550,h_550,al_c,q_80,usm_0.66_1.00_0.01/1e04d5_f8c8cebbe5c54ffe98b2bc1eb6ae2c82~mv2.jpg)
