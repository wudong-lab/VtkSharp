# LabelContours 移植说明

原例：[LabelContours](https://examples.vtk.org/site/Cxx/Visualization/LabelContours/)。
源码：[LabelContours.cxx](https://gitlab.kitware.com/vtk/vtk-examples/-/blob/master/src/Cxx/Visualization/LabelContours.cxx)。

## 范围与差异

- 迁移无参数路径：10×10 平面、种子 1、随机标量范围 [-100, 100]、5 个等值面值，经过 `vtkStripper` 拼接后为每条折线随机选点标注。
- 保留点序列中间点的替代写法；它不是按弧长计算的几何中点。`Debug.WriteLine` 替代控制台输出。
- ExampleBrowser 的 `IExample.Run()` 不接受参数，本次不接入原例的 `.vtp` 文件、短折线过滤和命令行等值线配置分支，不新增文件选择 UI。
- 标签使用 `{:6.2f}`，符合本地 VTK 的 `std::format` 规范；本地实现也兼容原例 `%6.2f`。
- 标签仍为 `vtkActor2D`，不执行三维遮挡筛选。交互旋转后背面标签仍可见，属于原例限制。
- `ISmokeExample` 的截图与交互模式共用场景。截图模式保存 600×600 PNG 后释放对象，不进入 `Start()`。

## 绑定规划

需求、规划及审核候选位于忽略目录 `artifacts/label-contours/`：`requests.json`、`plan.json`、`candidate.yml`。
审核 `diff-whitelist --summary` 后通过 `merge-candidate` 合并：3 个新增类型、19 个新增方法、0 冲突。`vtkStripper` 仅请求类型；现有基类提供管线方法。未修改生成器或手工编辑生成代码。

## 互操作依据

核查版本为 VTK 9.7，源码提交 `23f0a095621e91bbdbeace8451e22b950c8e5f46`，
仓库 [Kitware/VTK](https://gitlab.kitware.com/vtk/vtk/-/tree/23f0a095621e91bbdbeace8451e22b950c8e5f46)。
本地源码目录 `D:/Code/VTK/VtkGitSource`，安装目录 `D:/Code/VTK/VtkGitBuild/install`。

| 签名 | 契约及决定 | 源码依据 |
| --- | --- | --- |
| `vtkCellArray::NewIterator()` | 返回一个新引用，`ownership: owned`，`using` 负责释放。 | `Common/DataModel/vtkCellArray.cxx` 调用 `vtkCellArrayIterator::New()` 后直接返回；迭代器通过智能指针持有 cell array。 |
| `vtkCellArrayIterator::GetCurrentCell()` | `ownership: borrowed`，借用迭代器的 `TempCell`，下次读取可能覆盖内容；在当前迭代内使用完毕。 | `Common/DataModel/vtkCellArrayIterator.h` 返回 `vtkNew<vtkIdList> TempCell`，不增加引用。 |
| `vtkPoints::GetPoint(vtkIdType,double[3])` | 写入调用者提供的 3 个 double，配置 `direction: out`，固定长度由数组签名给出，映射为 `Span<double>`。 | `Common/Core/vtkPoints.h` 明确说明复制三个分量，并调用 `Data->GetTuple(id,x)`。 |
| `vtkPointSet::GetPoints()` | `ownership: borrowed`，数据集存活且不替换点集期间使用。 | `Common/DataModel/vtkPointSet.h` 直接返回 `this->Points`。 |
| `vtkPolyData::GetLines()` | `ownership: borrowed`，本例在滤波器输出存活且不更新期间使用。 | `Common/DataModel/vtkPolyData.cxx` 返回 `this->Lines` 或共享空容器，无新引用。 |
| `vtkDataSetAttributes::GetScalars()` | `ownership: borrowed`，不能在属性数组替换或数据集释放后继续访问。 | `Common/DataModel/vtkDataSetAttributes.cxx` 经 `GetAttribute(SCALARS)` 返回 `Data[index]`，无新引用；无标量时可返回空指针，本例显式设置标量。 |

所核查文件 SHA256：

```text
Common/Core/vtkPoints.h
A8469E9160BD574FD650460A12133DE7202068DFCE585A6666697651246EDF62
Common/DataModel/vtkCellArray.cxx
46D993F2D58F5AF3E208776D22D2BBA1A30AC5489E2B68740E3EC7090E6A37DE
Common/DataModel/vtkCellArrayIterator.h
E22A5156FBC0C1ED71B18A90306FE6FED51D0E6C56E5B221859B75DC9851A3D4
Common/DataModel/vtkPointSet.h
4B04DF6B8CA3725B7389ECCD24D940971B11B1FDC4230774D29BBDE60A7E873F
Common/DataModel/vtkPolyData.cxx
2A914D48CBF33EB438F93DCA7687E2111F34BE7F894207DFF0CD12E4D14341CF
Common/DataModel/vtkDataSetAttributes.cxx
CAFC1775B7CFAA6B945AAE024BFF7F8D5BBC6F16D9EBB8F5DA715073B0AE3A54
```

VTK 升级、上述文件或签名变化、类型映射或生命周期策略变化时重新复核。截图数据持有关系复用 [已有依据](../../../../../../docs/interop/window-image.md)。

## 验证

```powershell
$env:VTK_ROOT = 'D:/Code/VTK/VtkGitBuild/install'
.\tools\verify-workflow.ps1 -VtkDir "$env:VTK_ROOT/lib/cmake/vtk-9.7" -Regenerate -Example Visualization/LabelContours
```

2026-08-31 的统一报告位于 `artifacts/label-contours/verification/verification.json`：

- 生成器测试 233/233、托管测试 39/39 通过；Release native 与 ExampleBrowser 构建通过，各阶段未报告警告。
- `Visualization/LabelContours` 截图验收通过，PNG 为 600×600；检查确认彩色标量面、黑色折线和金色两位小数标签正常。随机标签局部重叠，沿用原例行为。
- 首次生成一致性检查因撤回无关配置排序时生成进程已读取旧排序，发现 `vtkColorSeries` 和 `vtkWindow` 的 4 个生成文件仅有顺序差异。随后重新增量生成，消除这两类与本任务无关的改动；单独重跑最后的检查，不重复已通过且功能未变的测试。
- 全量生成还刷新了旧版本增量缓存，已撤回无关模块的缓存变更；保留新增接口所属模块的生成清单。
- 尝试桌面验收时，Computer Use 窗口捕获两次报 `SetIsBorderRequired: No such interface supported (0x80004002)`，故未验证旋转、缩放和手动关闭。已清理此次启动的浏览器进程。长期重复创建/释放和内存泄漏未验证。

生成一致性复查通过（退出码 0，`Generated output is up to date.`），日志为 `artifacts/label-contours/generated-recheck.stdout.log` 和 `generated-recheck.stderr.log`。原统一报告保留首次失败记录，未改写历史结果。`git diff --check` 通过。
