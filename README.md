# PathLib.Sharp

## 概述

PathLib.Sharp 是一个C#库，它模仿了Python pathlib模块的功能和API设计。它提供了一种面向对象的方式来处理文件系统路径，比传统的字符串操作更加直观和安全。

## 主要特性

### 1. 路径构造和表示

- 支持多种路径构造方式
- 自动处理不同平台的路径分隔符
- 支持路径连接操作符（`/`）
- 隐式字符串转换

### 2. 路径属性访问

- `Parts` - 路径组件数组
- `Name` - 文件名（包含扩展名）
- `Stem` - 文件主名（不含扩展名）
- `Suffix` - 文件扩展名
- `Suffixes` - 所有扩展名（支持多重扩展名如`.tar.gz`）
- `Parent` - 父目录
- `Parents` - 所有祖先目录
- `Drive` - 驱动器字母（Windows）
- `Root` - 根路径
- `Anchor` - 驱动器+根路径

### 3. 路径操作方法

- `JoinPath()` - 路径连接
- `WithName()` - 修改文件名
- `WithStem()` - 修改文件主名
- `WithSuffix()` - 修改扩展名
- `Absolute()` - 获取绝对路径
- `Resolve()` - 解析路径（处理符号链接）
- `IsAbsolute` - 判断是否为绝对路径

### 4. 文件系统查询

- `Exists` - 路径是否存在
- `IsFile` - 是否为文件
- `IsDirectory` - 是否为目录
- `IsSymlink` - 是否为符号链接
- `Stat()` - 获取文件系统信息

### 5. 文件操作

- `Open()` - 打开文件流
- `ReadText()` / `WriteText()` - 文本文件读写
- `ReadBytes()` / `WriteBytes()` - 二进制文件读写
- `Touch()` - 创建空文件或更新修改时间

### 6. 目录操作

- `IterateDirectory()` - 遍历目录内容
- `Glob()` - 模式匹配文件搜索
- `RecursiveGlob()` - 递归模式匹配
- `MakeDirectory()` - 创建目录
- `RemoveDirectory()` - 删除空目录

### 7. 文件系统操作

- `Rename()` - 重命名/移动
- `Replace()` - 替换（强制覆盖）
- `Unlink()` - 删除文件
- `SymlinkTo()` - 创建符号链接

### 8. 静态方法

- `SharpPath.CurrentDirectory` - 当前工作目录
- `SharpPath.Home` - 用户主目录

## 使用示例

### 基础路径操作

```csharp
using PathLib;

// 创建路径
var path = new SharpPath("documents", "projects", "myfile.txt");
Console.WriteLine(path); // documents\projects\myfile.txt (Windows)

// 路径属性
Console.WriteLine($"名称: {path.Name}");           // myfile.txt
Console.WriteLine($"主名: {path.Stem}");           // myfile
Console.WriteLine($"扩展名: {path.Suffix}");       // .txt
Console.WriteLine($"父目录: {path.Parent}");       // documents\projects
```

### 路径连接

```csharp
var basePath = new SharpPath("C:", "Users");
var fullPath = basePath / "username" / "Documents" / "file.txt";
// C:\Users\username\Documents\file.txt
```

### 路径修改

```csharp
var originalPath = new SharpPath("document.pdf");
var withNewName = originalPath.WithName("report.pdf");      // report.pdf
var withNewStem = originalPath.WithStem("backup");          // backup.pdf  
var withNewExt = originalPath.WithSuffix(".txt");           // document.txt
```

### 文件操作

```csharp
var textFile = new SharpPath("example.txt");

// 写入文件
textFile.WriteText("Hello, PathLib.Sharp!");

// 读取文件
string content = textFile.ReadText();

// 检查文件
if (textFile.Exists && textFile.IsFile)
{
    Console.WriteLine($"文件大小: {textFile.Stat()?.Length} 字节");
}
```

### 目录操作

```csharp
var directory = new SharpPath("my_folder");

// 创建目录
directory.MakeDirectory();

// 遍历目录
foreach (var item in directory.IterateDirectory())
{
    if (item.IsFile)
        Console.WriteLine($"文件: {item.Name}");
    else if (item.IsDirectory)
        Console.WriteLine($"目录: {item.Name}");
}

// 模式匹配
var txtFiles = directory.Glob("*.txt");
```

### 高级功能

```csharp
// 递归搜索所有Python文件
var projectDir = new SharpPath("my_project");
var pythonFiles = projectDir.Glob("**/*.py");

// 创建符号链接
var target = new SharpPath("original_file.txt");
var link = new SharpPath("link_to_file.txt");
link.SymlinkTo(target);

// 安全文件删除
var tempFile = new SharpPath("temp.log");
tempFile.Unlink(missingOk: true); // 不存在时不会抛出异常
```

## 平台兼容性

- ✅ Windows (.NET 8.0+)
- ✅ Linux (.NET 8.0+)
- ✅ macOS (.NET 8.0+)

## 与Python pathlib的对比

| Python pathlib | PathLib.Sharp | 功能说明 |
|----------------|---------------|----------|
| `Path('a', 'b')` | `new SharpPath("a", "b")` | 路径构造 |
| `path / 'subdir'` | `path / "subdir"` | 路径连接 |
| `path.name` | `path.Name` | 文件名 |
| `path.stem` | `path.Stem` | 文件主名 |
| `path.suffix` | `path.Suffix` | 扩展名 |
| `path.parent` | `path.Parent` | 父目录 |
| `path.exists()` | `path.Exists` | 存在检查 |
| `path.is_file()` | `path.IsFile` | 文件检查 |
| `path.read_text()` | `path.ReadText()` | 读取文本 |
| `path.glob('*.txt')` | `path.Glob("*.txt")` | 模式匹配 |

## 安装和构建

```bash
# 克隆项目
git clone [repository-url]
cd PathLib.Sharp

# 构建项目
dotnet build

# 运行测试
dotnet test

# 打包NuGet包
dotnet pack
```

## 测试覆盖率

本项目包含61个测试用例，覆盖以下方面：

- ✅ 构造函数测试（4个测试）
- ✅ 属性访问测试（11个测试）  
- ✅ 操作符重载测试（4个测试）
- ✅ 路径操作测试（6个测试）
- ✅ 文件系统查询测试（4个测试）
- ✅ 文件操作测试（6个测试）
- ✅ 目录操作测试（6个测试）
- ✅ 文件系统操作测试（5个测试）
- ✅ 静态方法测试（2个测试）
- ✅ 相等性和比较测试（5个测试）
- ✅ 通配符匹配测试（1个测试）
- ✅ 错误处理和边界情况测试（7个测试）

## 许可证

[MIT License](LICENSE)

## 贡献

欢迎贡献代码！请提交Pull Request或创建Issue来报告bug或提出功能建议。

## 注意事项

- 路径操作遵循当前操作系统的约定
- Windows上的路径比较不区分大小写，Unix/Linux上区分大小写
- 符号链接操作需要适当的系统权限
- 某些操作可能因为文件系统权限而失败，请适当处理异常
