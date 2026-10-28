# 实现与验证计划

目标：交付独立截图器及源码。架构和边界见 design.md。技术：C#、WinForms、Windows OCR、GDI 屏幕捕获；不引入微信运行库。

- [x] 实现 SelectionMath（归一化、限制、移动、缩放）、ImageStitcher（精确重叠、重复帧、低置信度），使用实际位图检查关键边界。
- [x] 实现 DesktopCapture、Annotation、CaptureForm，自绘工具条、放大镜与独立渲染导出。
- [x] 实现 HostContext、LauncherForm、PinForm、LongCaptureForm、OcrService，以及托盘、单实例、热键和冲突提示。
- [x] 编译并运行 24 项自动检查，执行启动、选区、矩形标注、撤销/重做、取消保存保留选区，以及 Ctrl+A 录入/保存/重启保留/跨应用触发的 UI 回放；边界见 verification.md。
- [x] 发布自包含 EXE，编写中文 README 与功能差异，保留 REA 证据和分析能力限制。
- [ ] 补齐保存落盘、剪贴板粘贴、贴图、全部标注与实际长截图的端到端验收。
- [ ] 完成微信原版各功能的逐项行为对照，建立像素差异基准，核对混合 DPI/HDR/多屏边界。
- [ ] 恢复截图函数调用链与依赖闭包，验证是否存在可独立调用接口；现有证据不足以支持直接抽取 DLL。

当前交付为独立重建可用版。严格的全功能、像素级 1:1 目标仍未完成，以上未完成项不随构建成功自动视为完成。
