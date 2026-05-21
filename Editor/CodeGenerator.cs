using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class RpcCodeGenerator
{
    private static readonly string sourceDir = "Assets/Scripts";
    private static readonly string genDir = "Assets/Scripts/EnsNetcode/Gen";

    [UnityEditor.MenuItem("Ens/GenerateCode")]
    public static void GenCode()
    {
        Directory.CreateDirectory(genDir);
        CleanGeneratedFiles(genDir);

        // 收集所有类的信息并构建继承关系
        var allClasses = new List<ClassDeclarationSyntax>();
        var classFullNames = new Dictionary<ClassDeclarationSyntax, string>();

        // 首先收集所有类及其完整名称
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains("Generated")) continue;
            var code = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot() as CompilationUnitSyntax;

            var classes = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                .ToList();

            foreach (var cls in classes)
            {
                allClasses.Add(cls);
                string @namespace = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";
                string fullName = string.IsNullOrEmpty(@namespace) ? cls.Identifier.Text : $"{@namespace}.{cls.Identifier.Text}";
                classFullNames[cls] = fullName;
            }
        }

        // 构建继承哈希表（包含直接和间接继承TestBehaviour的类）
        var inheritedFromTestBehaviour = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(EnsBehaviour) // 初始加入目标基类
        };

        bool hasNewAdded;
        do
        {
            hasNewAdded = false;
            foreach (var cls in allClasses)
            {
                var fullName = classFullNames[cls];
                if (inheritedFromTestBehaviour.Contains(fullName))
                    continue;

                // 检查类的所有基类是否在哈希表中
                if (cls.BaseList?.Types.Any(t =>
                    inheritedFromTestBehaviour.Contains(GetBaseTypeFullName(t.Type, classFullNames, allClasses))) ?? false)
                {
                    inheritedFromTestBehaviour.Add(fullName);
                    hasNewAdded = true;
                }
            }
        } while (hasNewAdded); // 循环直到没有新类加入

        // 处理符合条件的类
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains("Generated")) continue;
            ProcessFile(file, genDir, inheritedFromTestBehaviour);
        }

        UnityEditor.AssetDatabase.Refresh();
    }

    // 获取基类的完整名称
    private static string GetBaseTypeFullName(TypeSyntax baseType, Dictionary<ClassDeclarationSyntax, string> classFullNames, List<ClassDeclarationSyntax> allClasses)
    {
        var baseTypeName = baseType.ToString();
        // 检查是否是当前代码库中的类
        var matchedClass = allClasses.FirstOrDefault(c => c.Identifier.Text == baseTypeName);
        return matchedClass != null ? classFullNames[matchedClass] : baseTypeName;
    }

    static void ProcessFile(string sourcePath, string genDir, HashSet<string> targetBaseClasses)
    {
        string code = File.ReadAllText(sourcePath);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot() as CompilationUnitSyntax;

        string currentNamespace = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";

        // 筛选继承自目标基类（直接或间接）且是partial的类
        var targetClasses = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c =>
            {
                string className = c.Identifier.Text;
                string fullName = string.IsNullOrEmpty(currentNamespace) ? className : $"{currentNamespace}.{className}";
                return targetBaseClasses.Contains(fullName);
            })
            .Where(c =>
            {
                var b = c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
                if (!b&& c.Identifier.Text!=nameof(EnsBehaviour)) //EnsBehaviour作为基类，本身不要求partial
                    UnityEngine.Debug.LogWarning($"类 {c.Identifier.Text} 没有声明为partial，跳过生成代码。");
                return b;
            })
            .ToList();

        foreach (var cls in targetClasses)
        {
            GenerateCodeForClass(cls, sourcePath, genDir, root);
        }
    }

    static void GenerateCodeForClass(ClassDeclarationSyntax cls, string sourcePath, string genDir, CompilationUnitSyntax root)
    {
        string className = cls.Identifier.Text;
        string @namespace = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";

        // 获取带有Rpc属性的方法（排除已生成的代码）
        var rpcMethods = cls.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists.Any(a => a.Attributes.Any(attr => attr.Name.ToString() == "Rpc")))
            .Where(m => !m.AttributeLists.Any(a => a.ToString().Contains("GeneratedCode")))
            .ToList();

        if (!rpcMethods.Any()) return;

        // 为每个方法分配唯一ID
        var methodIdMap = rpcMethods.Select((m, i) => new { Method = m, Id = (byte)i }).ToDictionary(x => x.Method, x => x.Id);

        // 生成代码
        var codeBuilder = new StringBuilder();
        HashSet<string> requiredUsings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "using System;",
            "using System.Collections.Generic;",
            "using UnityEngine;"
        };

        // 添加原文件的using指令
        foreach (var usingDirective in root.Usings)
        {
            string usingStr = usingDirective.ToString().TrimEnd('\r', '\n');
            requiredUsings.Add(usingStr);
        }

        // 写入using指令
        foreach (var usingStr in requiredUsings)
        {
            codeBuilder.AppendLine(usingStr);
        }
        codeBuilder.AppendLine();

        if (!string.IsNullOrEmpty(@namespace))
        {
            codeBuilder.AppendLine($"namespace {@namespace}");
            codeBuilder.AppendLine("{");
        }
        string baseClassDeclaration = GetOriginalBaseClassDeclaration(cls);
        codeBuilder.AppendLine($"public partial class {className} : {baseClassDeclaration}");
        codeBuilder.AppendLine("{");

        // 1. 生成FuncRecorder字典
        Dictionary<string, int> OverloadingCounter = new();

        codeBuilder.AppendLine($"    private static Dictionary<byte, Action<{className}, byte[]>> FuncRecorder = new()");
        codeBuilder.AppendLine("    {");
        foreach (var method in rpcMethods)
        {
            byte id = methodIdMap[method];
            string methodIdentifier = method.Identifier.Text;
            int count;
            if (OverloadingCounter.ContainsKey(methodIdentifier))
            {
                OverloadingCounter[methodIdentifier] += 1;
                count = OverloadingCounter[methodIdentifier];
            }
            else
            {
                OverloadingCounter.Add(methodIdentifier, 0);
                count = 0;
            }
            codeBuilder.AppendLine($"        {{ {id}, (p, b) => p.Invoke_{methodIdentifier}{count}(b) }},");
        }
        codeBuilder.AppendLine("    };");
        codeBuilder.AppendLine();

        // 2. 为不同参数类型的方法生成对应的映射和RpcInvoke方法
        var groupedMethods = rpcMethods.GroupBy(m => GetParameterTypeKey(m.ParameterList));
        foreach (var group in groupedMethods)
        {
            string paramKey = group.Key;
            var method = group.First();
            var parameters = method.ParameterList.Parameters;
            var paramTypes = parameters.Select(p => p.Type.ToString()).ToList();
            var paramNames = parameters.Select((p, i) => $"param{i + 1}").ToList();

            // 生成映射字典
            string actionType = parameters.Any()
                ? $"Action<{string.Join(", ", paramTypes)}>"
                : "Action";
            codeBuilder.AppendLine($"    private Dictionary<{actionType}, byte> map_{paramKey};");
            codeBuilder.AppendLine();

            // 生成RpcInvoke方法
            bool parametersIsNotNull = parameters.Any();
            string parametersDeclaration = parametersIsNotNull
                ? $", {string.Join(", ", parameters.Select(p => $"{p.Type} {paramNames[parameters.IndexOf(p)]}"))}"
                : string.Empty;

            codeBuilder.AppendLine($"    public {(parametersIsNotNull ? string.Empty : "new ")}void CallFuncRpc({actionType} func, SendTo sendto, Delivery delivery{parametersDeclaration})");
            codeBuilder.AppendLine("    {");
            codeBuilder.AppendLine($"        if (map_{paramKey} == null) map_{paramKey} = new()");
            codeBuilder.AppendLine("        {");
            foreach (var m in group)
            {
                codeBuilder.AppendLine($"            {{ {m.Identifier.Text}, {methodIdMap[m]} }},");
            }
            codeBuilder.AppendLine("        };");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine($"        if (!map_{paramKey}.ContainsKey(func)) throw new Exception(\"目标函数未注册\");");
            codeBuilder.AppendLine();

            //写入方法id
            codeBuilder.AppendLine("        EnsTemporaryBuffer.length=1;");
            codeBuilder.AppendLine($"        EnsTemporaryBuffer.bytes[0] = map_{paramKey}[func];");
            codeBuilder.AppendLine();

            // 序列化参数
            for (int i = 0; i < parameters.Count; i++)
            {
                string type = paramTypes[i];
                string serializer = $"{char.ToUpperInvariant(type[0])}{type.Substring(1)}Serializer";
                codeBuilder.AppendLine($"        {serializer}.Serialize({paramNames[i]}, EnsTemporaryBuffer.bytes, ref EnsTemporaryBuffer.length);");
            }

            codeBuilder.AppendLine("        Send(delivery, sendto);");
            codeBuilder.AppendLine("    }");
            codeBuilder.AppendLine();
        }
        OverloadingCounter.Clear();

        // 3. 生成方法反序列化调用
        foreach (var method in rpcMethods)
        {
            string methodName = method.Identifier.Text;
            var parameters = method.ParameterList.Parameters;
            var paramTypes = parameters.Select(p => p.Type.ToString()).ToList();
            var paramNames = parameters.Select((p, i) => $"param{i + 1}").ToList();

            int count;
            if (OverloadingCounter.ContainsKey(methodName))
            {
                OverloadingCounter[methodName] += 1;
                count = OverloadingCounter[methodName];
            }
            else
            {
                OverloadingCounter.Add(methodName, 0);
                count = 0;
            }
            codeBuilder.AppendLine($"    private void Invoke_{methodName}{count}(byte[] bytes)");
            codeBuilder.AppendLine("    {");
            if (parameters.Count != 0)
            {
                codeBuilder.AppendLine("        int indexStart = RpcInvokeSegment.StartIndex+1;");
                codeBuilder.AppendLine("        int invalidIndex = RpcInvokeSegment.StartIndex+RpcInvokeSegment.Length;");
            }

            // 反序列化参数
            for (int i = 0; i < parameters.Count; i++)
            {
                string type = paramTypes[i];
                string serializer = $"{char.ToUpperInvariant(type[0])}{type.Substring(1)}Serializer";
                codeBuilder.AppendLine($"        {type} {paramNames[i]} = {serializer}.Deserialize(bytes, ref indexStart,invalidIndex);");
            }

            // 调用原方法
            codeBuilder.AppendLine($"        {methodName}({string.Join(", ", paramNames)});");
            codeBuilder.AppendLine("    }");
            codeBuilder.AppendLine();
        }

        // 4. 生成InvokeFunc方法
        codeBuilder.AppendLine("    private static Segment RpcInvokeSegment;");
        codeBuilder.AppendLine("    public override bool InvokeFunc(byte[] bytes,Segment s)");
        codeBuilder.AppendLine("    {");
        codeBuilder.AppendLine("        RpcInvokeSegment=s;");
        codeBuilder.AppendLine("        byte funcId = bytes[s.StartIndex];");
        codeBuilder.AppendLine("        if (FuncRecorder.TryGetValue(funcId, out var action))");
        codeBuilder.AppendLine("        {");
        codeBuilder.AppendLine("            action.Invoke(this, bytes);");
        codeBuilder.AppendLine("            return true;");
        codeBuilder.AppendLine("        }");
        codeBuilder.AppendLine("        else return false;");
        codeBuilder.AppendLine("    }");

        codeBuilder.AppendLine("}");

        if (!string.IsNullOrEmpty(@namespace))
        {
            codeBuilder.AppendLine("}");
        }

        // 写入生成的文件
        string genFilePath = Path.Combine(genDir, $"{className}.Generated.cs");
        File.WriteAllText(genFilePath, codeBuilder.ToString());
        Console.WriteLine($"生成代码: {genFilePath}");
    }
    private static string GetOriginalBaseClassDeclaration(ClassDeclarationSyntax cls)
    {
        if (cls.BaseList == null || !cls.BaseList.Types.Any())
        {
            return string.Empty; // 无基类则返回空，不生成继承语法
        }
        // 原样拼接基类声明（保留泛型、多重继承等完整语法）
        string baseTypes = string.Join(", ", cls.BaseList.Types.Select(t => t.ToString().Trim()));
        return baseTypes;
    }
    // 根据参数列表获取参数类型标识（用于分组）
    static string GetParameterTypeKey(ParameterListSyntax paramList)
    {
        if (!paramList.Parameters.Any())
            return "void";

        return string.Join("_", paramList.Parameters.Select(p => p.Type.ToString().ToLower()));
    }

    [UnityEditor.MenuItem("Ens/CleanGeneratedCode")]
    public static void CleanGeneratedCode()
    {
        CleanGeneratedFiles(genDir);
        UnityEditor.AssetDatabase.Refresh();
    }

    public static void CleanGeneratedFiles(string genDir)
    {
        if (Directory.Exists(genDir))
        {
            foreach (var file in Directory.EnumerateFiles(genDir, "*.Generated.cs"))
            {
                try
                {
                    File.Delete(file);
                    string metaFile = file + ".meta";
                    if(File.Exists(metaFile))
                        File.Delete(metaFile);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"删除文件失败: {file}, 错误: {ex.Message}");
                }
            }
            UnityEditor.AssetDatabase.Refresh();
        }
    }
}