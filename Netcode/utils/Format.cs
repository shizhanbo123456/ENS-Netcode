using System;
using System.Collections.Generic;
using System.Text;

public static class Format
{
    public const char BoundaryStart = '{';
    public const char BoundaryEnd = '}';
    public const char ListSeparator = '/';
    public const char DictionaryPair = ':';
    public const char DictionarySeparator = ',';

    public static string DictionaryToString<Tkey,Tvalue>(Dictionary<Tkey,Tvalue>dict,
        char pair=DictionaryPair,char separator=DictionarySeparator,bool addboundary=true,
        Func<Tkey,string> keyconverter=null,Func<Tvalue,string>valueconverter=null,bool wrapAll=true)
    {
        StringBuilder sb = new StringBuilder();
        if (keyconverter == null) keyconverter = t => t.ToString();
        if(valueconverter == null) valueconverter = t => t.ToString();

        if(wrapAll)sb.Append(BoundaryStart);
        if (addboundary)
        {
            bool add = false;
            foreach (var i in dict)
            {
                if(add)sb.Append(separator);
                else add=true;
                sb.Append(BoundaryStart + keyconverter.Invoke(i.Key) + BoundaryEnd + pair + BoundaryStart + valueconverter.Invoke(i.Value) + BoundaryEnd);
            }
        }
        else
        {
            bool add = false;
            foreach (var i in dict)
            {
                if (add) sb.Append(separator);
                else add = true;
                sb.Append(i.Key.ToString() + pair + i.Value.ToString());
            }
        }
        if (wrapAll)sb.Append(BoundaryEnd);
        return sb.ToString();
    }
    public static Dictionary<Tkey, Tvalue> StringToDictionary<Tkey, Tvalue>(string data, Func<string, Tkey> keyconverter, Func<string, Tvalue> valueconverter, char pair = DictionaryPair, char separator = DictionarySeparator, bool removeboudary = true,bool wrapAll=true)
    {
        Dictionary<Tkey, Tvalue> r = new Dictionary<Tkey, Tvalue>();
        if (wrapAll&&data[0] == BoundaryStart && data[data.Length - 1] == BoundaryEnd)
        {
            data=data.Substring(1,data.Length-2);
        }
        else throw new Exception("字典数据不符合规范："+data);
        var s = SplitWithBoundaries(data, separator: separator, removeBoundary: false);
        foreach (var i in s)
        {
            var list = SplitWithBoundaries(i, pair,removeBoundary:removeboudary);
            r.Add(keyconverter.Invoke(list[0]), valueconverter.Invoke(list[1]));
        }
        return r;
    }

    public static string ListToString<T>(IEnumerable<T> list, Func<T, string> converter = null, char c = ListSeparator)
    {
        if (converter == null) converter = T => T.ToString();
        StringBuilder sb= new StringBuilder();
        bool isFirst = true;
        foreach(var i in list)
        {
            if (isFirst) isFirst = false;
            else sb.Append(c);
            sb.Append(converter(i));
        }
        return sb.ToString();
    }
    public static List<T> StringToList<T>(string a,Func<string,T>converter, char c = ListSeparator)
    {
        string[] s = a.Split(new char[] { c }, StringSplitOptions.RemoveEmptyEntries);
        List<T> list = new List<T>();
        foreach (var i in s) list.Add(converter.Invoke(i));
        return list;
    }
    public static string ArrayToString<T>(T[] list, char c = ListSeparator)
    {
        StringBuilder sb = new StringBuilder();
        bool isFirst = true;
        foreach (var i in list)
        {
            if (isFirst) isFirst = false;
            else sb.Append(c);
            sb.Append(i.ToString());
        }
        return sb.ToString();
    }
    public static T[] StringToArray<T>(string a, Func<string, T> converter, char c = ListSeparator)
    {
        string[] s = a.Split(new char[] { c }, StringSplitOptions.RemoveEmptyEntries);
        var list = new T[s.Length];
        for (int index = 0; index < s.Length; index++)
        {
            string i = s[index];
            list[index] = converter.Invoke(i);
        }
        return list;
    }


    public static List<string> SplitWithBoundaries(string s, char separator, char boundaryStart = BoundaryStart, char boundaryEnd = BoundaryEnd,bool removeBoundary=true)
    {
        List<string> result = new List<string>();
        int currentStart = 0;
        int boundaryDepth = 0; // 用于处理嵌套边界的情况

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];

            // 处理边界开始符
            if (c == boundaryStart)
            {
                boundaryDepth++;
            }
            // 处理边界结束符
            else if (c == boundaryEnd)
            {
                if (boundaryDepth > 0)
                {
                    boundaryDepth--;
                }
            }
            // 当遇到分隔符且不在边界内时进行分割
            else if (c == separator && boundaryDepth == 0)
            {
                // 提取当前片段并处理边界
                if (i > currentStart)
                {
                    string segment = s.Substring(currentStart, i - currentStart);
                    if(removeBoundary)
                        segment = (segment.Length >= 2 && segment[0] == boundaryStart && 
                        segment[segment.Length - 1] == boundaryEnd) ? 
                        segment.Substring(1, segment.Length - 2) : segment;
                    result.Add(segment);
                }

                currentStart = i + 1; // 移动到下一个片段的起始位置
            }
        }

        // 添加最后一个片段
        if (currentStart <= s.Length - 1)
        {
            string lastSegment = s.Substring(currentStart);
            if(removeBoundary)
                lastSegment = (lastSegment.Length >= 2 && lastSegment[0] == boundaryStart &&
                lastSegment[lastSegment.Length - 1] == boundaryEnd) ?
                lastSegment.Substring(1, lastSegment.Length - 2) : lastSegment;
            result.Add(lastSegment);
        }

        return result;
    }
}