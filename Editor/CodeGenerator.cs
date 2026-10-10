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
    private static readonly string genDir = "Assets/ENS-Netcode/Gen";

    //每个类的Rpc方法信息：方法声明 + 生成的Invoke调用名（按fullName合并partial多文件声明）
    private class ClassRpcInfo
    {
        public List<MethodDeclarationSyntax> Methods = new();
        public List<string> InvokeNames = new();
        public HashSet<string> ParamKeys = new();
        public Dictionary<string, int> OverloadCounter = new();
    }
    private static Dictionary<string, ClassRpcInfo> rpcInfoByClass;

    [UnityEditor.MenuItem("Ens/GenerateCode")]
    public static void GenCode()
    {
        UnityEngine.Debug.Log($"[ENS] GenerateCode start. Source={sourceDir}, Output={genDir}");
        int generatedCount = 0;
        Directory.CreateDirectory(genDir);
        CleanGeneratedFiles(genDir);

        // 收集所有类的信息并构建继承关系
        var allClasses = new List<ClassDeclarationSyntax>();
        var classFullNames = new Dictionary<ClassDeclarationSyntax, string>();
        rpcInfoByClass = new Dictionary<string, ClassRpcInfo>();

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

                //收集该类的Rpc方法；同一类的partial多文件声明合并，Invoke调用名按fullName维度统一编号
                var rpcMethods = cls.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.AttributeLists.Any(a => a.Attributes.Any(IsRpcAttribute)))
                    .Where(m => !m.AttributeLists.Any(a => a.ToString().Contains("GeneratedCode")))
                    .ToList();

                if (!rpcMethods.Any()) continue;
                if (!rpcInfoByClass.TryGetValue(fullName, out var info))
                {
                    info = new ClassRpcInfo();
                    rpcInfoByClass[fullName] = info;
                }
                foreach (var m in rpcMethods)
                {
                    info.Methods.Add(m);
                    info.InvokeNames.Add(NextInvokeName(info, m.Identifier.Text));
                    info.ParamKeys.Add(GetParameterTypeKey(m.ParameterList));
                }
            }
        }

        // 构建继承哈希表（包含直接和间接继承EnsBehaviour的类）
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

        //fullName -> 类声明（继承链回溯用），同名取首个声明
        var classByName = new Dictionary<string, ClassDeclarationSyntax>();
        foreach (var kvp in classFullNames)
        {
            if (!classByName.ContainsKey(kvp.Value)) classByName.Add(kvp.Value, kvp.Key);
        }

        // 处理符合条件的类
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains("Generated")) continue;
            generatedCount += ProcessFile(file, genDir, inheritedFromTestBehaviour, allClasses, classByName, classFullNames);
        }

        UnityEditor.AssetDatabase.Refresh();
        UnityEngine.Debug.Log($"[ENS] GenerateCode finished. GeneratedFiles={generatedCount}, Output={genDir}");
    }

    // 获取基类的完整名称
    private static string GetBaseTypeFullName(TypeSyntax baseType, Dictionary<ClassDeclarationSyntax, string> classFullNames, List<ClassDeclarationSyntax> allClasses)
    {
        var baseTypeName = baseType.ToString();
        // 检查是否是当前代码库中的类
        var matchedClass = allClasses.FirstOrDefault(c => c.Identifier.Text == baseTypeName);
        return matchedClass != null ? classFullNames[matchedClass] : baseTypeName;
    }

    //同名重载的Invoke调用名计数（按类维度，与类内声明顺序一致）
    private static string NextInvokeName(ClassRpcInfo info, string methodName)
    {
        if (!info.OverloadCounter.TryGetValue(methodName, out int count))
        {
            count = 0;
        }
        info.OverloadCounter[methodName] = count + 1;
        return $"{methodName}{count}";
    }

    //判断特性是否为Rpc标记：兼容[Rpc]、[RpcAttribute]及带命名空间前缀的写法
    private static bool IsRpcAttribute(AttributeSyntax attr)
    {
        string name = attr.Name.ToString();
        int dot = name.LastIndexOf('.');
        if (dot >= 0) name = name.Substring(dot + 1);
        if (name.EndsWith("Attribute") && name.Length > "Attribute".Length)
        {
            name = name.Substring(0, name.Length - "Attribute".Length);
        }
        return name == "Rpc";
    }

    static int ProcessFile(string sourcePath, string genDir, HashSet<string> targetBaseClasses, List<ClassDeclarationSyntax> allClasses, Dictionary<string, ClassDeclarationSyntax> classByName, Dictionary<ClassDeclarationSyntax, string> classFullNames)
    {
        int generatedCount = 0;
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
            string fullName = string.IsNullOrEmpty(currentNamespace) ? cls.Identifier.Text : $"{currentNamespace}.{cls.Identifier.Text}";
            if (GenerateCodeForClass(cls, genDir, root, fullName, allClasses, classByName, classFullNames)) generatedCount++;
        }
        return generatedCount;
    }

    //构建fullName的继承链（根→自身），只含工程内扫描到的类；到EnsBehaviour或外部基类为止
    private static List<string> BuildChain(string fullName, List<ClassDeclarationSyntax> allClasses, Dictionary<string, ClassDeclarationSyntax> classByName, Dictionary<ClassDeclarationSyntax, string> classFullNames)
    {
        var chain = new List<string>();
        var visited = new HashSet<string>();
        var current = fullName;
        while (current != null && visited.Add(current))
        {
            chain.Add(current);
            if (!classByName.TryGetValue(current, out var cls)) break;
            string baseName = null;
            if (cls.BaseList != null)
            {
                foreach (var t in cls.BaseList.Types)
                {
                    var name = GetBaseTypeFullName(t.Type, classFullNames, allClasses);
                    if (name == nameof(EnsBehaviour)) { baseName = null; break; } //到链顶
                    if (classByName.ContainsKey(name)) { baseName = name; break; }
                }
            }
            current = baseName;
        }
        chain.Reverse(); //根→自身
        return chain;
    }

    static bool GenerateCodeForClass(ClassDeclarationSyntax cls, string genDir, CompilationUnitSyntax root, string fullName, List<ClassDeclarationSyntax> allClasses, Dictionary<string, ClassDeclarationSyntax> classByName, Dictionary<ClassDeclarationSyntax, string> classFullNames)
    {
        string className = cls.Identifier.Text;
        string @namespace = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";

        //构建继承链并统一分配id：根→自身依次编号，保证同一继承链内id唯一，且基类自身生成、子类收录两个视图完全一致
        var chain = BuildChain(fullName, allClasses, classByName, classFullNames);
        var recorderEntries = new List<string>();
        var ownMethodIds = new Dictionary<MethodDeclarationSyntax, byte>();
        var ownInvokeNames = new Dictionary<MethodDeclarationSyntax, string>();
        var ownMethods = new List<MethodDeclarationSyntax>();
        var ancestorParamKeys = new HashSet<string>();
        byte nextId = 0;
        foreach (var member in chain)
        {
            if (!rpcInfoByClass.TryGetValue(member, out var info)) continue;
            bool isSelf = member == fullName;
            for (int i = 0; i < info.Methods.Count; i++)
            {
                if (nextId > 255)
                {
                    UnityEngine.Debug.LogError($"[{className}] 继承链上Rpc方法总数超过255，无法分配id，跳过生成");
                    return false;
                }
                recorderEntries.Add($"            {{ {nextId}, (p, b, s) => p.{info.InvokeNames[i]}(b, s) }},");
                if (isSelf)
                {
                    ownMethodIds[info.Methods[i]] = nextId;
                    ownInvokeNames[info.Methods[i]] = info.InvokeNames[i];
                    ownMethods.Add(info.Methods[i]);
                }
                else
                {
                    ancestorParamKeys.UnionWith(info.ParamKeys);
                }
                nextId++;
            }
        }

        //本类无Rpc方法时不生成文件：无同名FuncRecorder遮蔽，调度由基类的InvokeFunc承担
        if (!ownMethods.Any()) return false;

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

        // 1. FuncRecorder：收录链上所有方法（含基类），id为链内唯一编号
        //（基类生成的FuncRecorder是private，不参与继承隐藏，无需new）
        codeBuilder.AppendLine($"    private static Dictionary<byte, Action<{className}, byte[], Segment>> FuncRecorder = new()");
        codeBuilder.AppendLine("    {");
        foreach (var entry in recorderEntries)
        {
            codeBuilder.AppendLine(entry);
        }
        codeBuilder.AppendLine("    };");
        codeBuilder.AppendLine();

        // 2. 为不同参数类型的方法生成对应的映射和RpcInvoke方法（仅本类方法，基类方法用基类生成的CallFuncRpc）
        var groupedMethods = ownMethods.GroupBy(m => GetParameterTypeKey(m.ParameterList));
        foreach (var group in groupedMethods)
        {
            string paramKey = group.Key;
            var method = group.First();
            var parameters = method.ParameterList.Parameters;
            var paramTypes = parameters.Select(p => p.Type.ToString()).ToList();
            var paramNames = parameters.Select((p, i) => $"param{i + 1}").ToList();

            // 与基类生成的同签名public方法CallFuncRpc需要用new隐藏，避免CS0108警告
            //（map字段基类里是private，不参与隐藏，无需new）
            bool hideBase = ancestorParamKeys.Contains(paramKey);
            //0参数版本始终与基类的无参CallFuncRpc同签名，保持原有的new
            string methodNew = hideBase || !parameters.Any() ? "new " : "";

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

            codeBuilder.AppendLine($"    public {methodNew}void CallFuncRpc({actionType} func, SendTo sendto, Delivery delivery{parametersDeclaration})");
            codeBuilder.AppendLine("    {");
            codeBuilder.AppendLine($"        if (map_{paramKey} == null) map_{paramKey} = new()");
            codeBuilder.AppendLine("        {");
            foreach (var m in group)
            {
                codeBuilder.AppendLine($"            {{ {m.Identifier.Text}, {ownMethodIds[m]} }},");
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

        // 3. 生成方法反序列化调用（protected供子类FuncRecorder引用；Segment作参数传递，避免跨类静态字段）
        foreach (var method in ownMethods)
        {
            string methodName = method.Identifier.Text;
            var parameters = method.ParameterList.Parameters;
            var paramTypes = parameters.Select(p => p.Type.ToString()).ToList();
            var paramNames = parameters.Select((p, i) => $"param{i + 1}").ToList();
            string invokeName = ownInvokeNames[method];

            codeBuilder.AppendLine($"    protected void {invokeName}(byte[] bytes, Segment s)");
            codeBuilder.AppendLine("    {");
            if (parameters.Count != 0)
            {
                codeBuilder.AppendLine("        int indexStart = s.StartIndex+1;");
                codeBuilder.AppendLine("        int invalidIndex = s.StartIndex+s.Length;");
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
        codeBuilder.AppendLine("    public override bool InvokeFunc(byte[] bytes,Segment s)");
        codeBuilder.AppendLine("    {");
        codeBuilder.AppendLine("        byte funcId = bytes[s.StartIndex];");
        codeBuilder.AppendLine("        if (FuncRecorder.TryGetValue(funcId, out var action))");
        codeBuilder.AppendLine("        {");
        codeBuilder.AppendLine("            action.Invoke(this, bytes, s);");
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
        UnityEngine.Debug.Log($"[ENS] 生成代码: {genFilePath}");
        return true;
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
