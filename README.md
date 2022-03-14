# URasterizer: 基于Unity平台的光栅化渲染器
本项目是在Unity上实现一套软件光栅化渲染，利用Unity的基础设施来读取模型、贴图，控制GameObject的变换，以及设置相机参数等，再通过软件光栅化来将场景渲染到一张贴图上。
URasterizer有两种模式：CPU模式完全使用C#实现软件光栅化，GPU Driven模式完全使用Compute shader实现整个光栅化流水线。GPU Driven模式的性能和直接使用Unity渲染相当。

# 支持特性(部分特性仅CPU模式支持)
* Wireframe mode
* Frustum culling
* Clipping
* Backfarce culling
* Depth Test
* 可视化Depth Buffer
* MSAA
* Blinn Phong着色
* 可视化顶点色
* 可视化法线
* 自定义几何图元
* 自定义顶点色
* 纹理采样模式 Nearest & Bilinear

# 项目截图
![CPU BlinnPhong](https://gitcode.net/n5/urasterizer/-/blob/master/URasterizer/Readme/cpu_blinnphong.png)
