using System;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Unity反序列化检测器 - 编辑器拓展窗口
/// </summary>
public class DeserializeDetectorWindow : EditorWindow
{
    // 各类型字节输入框内容（序列化存储，窗口重启不丢失）
    private string _boolBytesInput;
    private string _charBytesInput;
    private string _shortBytesInput;
    private string _intBytesInput;
    private string _longBytesInput;
    private string _floatBytesInput;
    private string _doubleBytesInput;
    private string _stringBytesInput;

    // 滚动视图位置
    private Vector2 _scrollPos;

    [MenuItem("Ens/反序列化检测器", false, 100)]
    public static void OpenDetectorWindow()
    {
        // 打开/聚焦窗口
        DeserializeDetectorWindow window = GetWindow<DeserializeDetectorWindow>("反序列化检测器");
        window.minSize = new Vector2(350, 600); // 设置窗口最小尺寸
        window.Show();
    }

    private void OnGUI()
    {
        // 标题样式
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 10, 20)
        };
        GUILayout.Label("Unity 通用类型反序列化检测器", titleStyle);

        // 滚动视图包裹所有内容（适配多类型、窗口缩放）
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        {
            // 分隔线
            EditorGUILayout.Separator();

            // 逐个绘制类型检测栏目
            DrawTypeDetectItem("布尔类型 (Bool)", ref _boolBytesInput, DeserializeBool);
            DrawTypeDetectItem("字符类型 (Char)", ref _charBytesInput, DeserializeChar);
            DrawTypeDetectItem("短整型 (Short)", ref _shortBytesInput, DeserializeShort);
            DrawTypeDetectItem("整型 (Int)", ref _intBytesInput, DeserializeInt);
            DrawTypeDetectItem("长整型 (Long)", ref _longBytesInput, DeserializeLong);
            DrawTypeDetectItem("浮点型 (Float)", ref _floatBytesInput, DeserializeFloat);
            DrawTypeDetectItem("双精度浮点 (Double)", ref _doubleBytesInput, DeserializeDouble);
            DrawTypeDetectItem("UTF8字符串 (String)", ref _stringBytesInput, DeserializeString);
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制单个类型的检测栏目（通用模板）
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <param name="inputStr">输入的字节字符串</param>
    /// <param name="deserializeFunc">反序列化执行方法</param>
    private void DrawTypeDetectItem(string typeName, ref string inputStr, Func<byte[], string> deserializeFunc)
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        {
            // 1. 左侧：类型名称 + 字节输入框（占70%宽度）
            GUILayout.Label(typeName, GUILayout.Width(120));
            inputStr = EditorGUILayout.TextField(inputStr, GUILayout.ExpandWidth(true), GUILayout.MinWidth(150));

            // 2. 右侧：结果展示区（占30%宽度）
            string result = "未检测";
            Color originColor = GUI.color;

            // 解析字节数组
            if (TryParseBytesInput(inputStr, out byte[] dataBytes))
            {
                try
                {
                    // 调用对应类型的反序列化方法
                    result = deserializeFunc.Invoke(dataBytes);
                    GUI.color = Color.green; // 成功：绿色
                }
                catch (Exception e)
                {
                    result = $"失败：{e.Message}";
                    GUI.color = Color.red; // 失败：红色
                }
            }
            else if (!string.IsNullOrEmpty(inputStr))
            {
                result = "字节格式错误（请输入空格分隔的0-255数字）";
                GUI.color = Color.red;
            }

            // 绘制结果
            GUILayout.Label(result, GUILayout.Width(150), GUILayout.ExpandWidth(false));
            GUI.color = originColor; // 恢复默认颜色
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
    }

    /// <summary>
    /// 解析输入的字节字符串 → 字节数组（核心工具方法）
    /// 支持格式：空格分隔的十进制数字，如 "236 192 167 0 1"
    /// </summary>
    private bool TryParseBytesInput(string input, out byte[] result)
    {
        result = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(input)) return false;

        try
        {
            string[] strArr = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            byte[] bytes = new byte[strArr.Length];
            for (int i = 0; i < strArr.Length; i++)
            {
                int val = int.Parse(strArr[i]);
                if (val < 0 || val > 255) return false;
                bytes[i] = (byte)val;
            }
            result = bytes;
            return true;
        }
        catch
        {
            return false;
        }
    }

    #region 各类型反序列化执行方法（对接序列化器API）
    private string DeserializeBool(byte[] data)
    {
        int index = 0;
        bool val = BoolSerializer.Deserialize(data, ref index, data.Length);
        return $"{val} | 索引偏移：{index}";
    }

    private string DeserializeChar(byte[] data)
    {
        int index = 0;
        char val = CharSerializer.Deserialize(data, ref index, data.Length);
        return $"{val} | 索引偏移：{index}";
    }

    private string DeserializeShort(byte[] data)
    {
        int index = 0;
        short val = ShortSerializer.Deserialize(data, ref index, data.Length);
        return $"{val} | 索引偏移：{index}";
    }

    private string DeserializeInt(byte[] data)
    {
        int index = 0;
        int val = IntSerializer.Deserialize(data, ref index, data.Length);
        return $"{val} | 索引偏移：{index}";
    }

    private string DeserializeLong(byte[] data)
    {
        int index = 0;
        long val = LongSerializer.Deserialize(data, ref index, data.Length);
        return $"{val} | 索引偏移：{index}";
    }

    private string DeserializeFloat(byte[] data)
    {
        int index = 0;
        float val = FloatSerializer.Deserialize(data, ref index, data.Length);
        return $"{val:F4} | 索引偏移：{index}";
    }

    private string DeserializeDouble(byte[] data)
    {
        int index = 0;
        double val = DoubleSerializer.Deserialize(data, ref index, data.Length);
        return $"{val:F6} | 索引偏移：{index}";
    }

    private string DeserializeString(byte[] data)
    {
        int index = 0;
        string val = StringSerializer.Deserialize(data, ref index, data.Length);
        return $"\"{val}\" | 索引偏移：{index}";
    }
    #endregion
}