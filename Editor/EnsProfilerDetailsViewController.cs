using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using TreeView = UnityEditor.IMGUI.Controls.TreeView;   // UnityEngine.UIElements 里也有 TreeView，必须消歧义

// 模块下方详情面板：可搜索、可按列排序的表格，列出每个远程调用函数在选中帧里的调用次数。
// 面板只接触字符串（函数名来自编辑器会话状态，数值按名字去问 Profiler），不引用任何业务代码。
public class EnsProfilerDetailsViewController : ProfilerModuleViewController
{
    const string k_NameListKey = "EnsProfiler.InvokeNames";
    const string k_CategoryName = "ENS";
    const string k_GlobalNamespaceText = "(全局)";

    const int k_ColumnNamespace = 0;
    const int k_ColumnClass = 1;
    const int k_ColumnFunction = 2;
    const int k_ColumnCount = 3;

    const int k_MaxVisibleRows = 14;
    const float k_RowHeight = 18f;

    sealed class Row
    {
        public string Namespace;
        public string Class;
        public string Function;
        public string CountText;
        public long Count;
    }

    sealed class RpcTable : TreeView
    {
        readonly List<Row> m_Source;
        GUIStyle m_RightAlignedLabel;

        public RpcTable(TreeViewState state, MultiColumnHeader header, List<Row> source) : base(state, header)
        {
            m_Source = source;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(0, -1, "Root");
            var items = new List<TreeViewItem>(m_Source.Count);
            for (int i = 0; i < m_Source.Count; i++)
                items.Add(new TreeViewItem(i + 1, 0, m_Source[i].Function));
            SetupParentsAndChildrenFromDepths(root, items);
            return root;
        }

        protected override void RowGUI(TreeView.RowGUIArgs args)
        {
            int index = args.item.id - 1;
            if (index < 0 || index >= m_Source.Count) return;

            Row row = m_Source[index];
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                int column = args.GetColumn(i);
                Rect cell = args.GetCellRect(i);
                EditorGUI.LabelField(cell, CellText(row, column), column == k_ColumnCount ? RightAlignedLabel() : EditorStyles.label);
            }
        }

        GUIStyle RightAlignedLabel()
        {
            if (m_RightAlignedLabel == null)
                m_RightAlignedLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            return m_RightAlignedLabel;
        }
    }

    readonly List<Row> m_Rows = new List<Row>();
    readonly List<Row> m_VisibleRows = new List<Row>();

    string m_LoadedNameList = "";
    long m_LoadedFrameIndex = long.MinValue;
    bool m_HasFrameData;

    string m_SearchText = "";
    MultiColumnHeader m_Header;
    RpcTable m_Table;
    bool m_TableDirty = true;
    int m_LastSortedColumn = int.MinValue;
    bool m_LastSortedAscending;

    public EnsProfilerDetailsViewController(ProfilerWindow profilerWindow) : base(profilerWindow)
    {
    }

    protected override VisualElement CreateView()
    {
        m_Header = new MultiColumnHeader(new MultiColumnHeaderState(new[]
        {
            MakeColumn("命名空间", 200f),
            MakeColumn("类", 190f),
            MakeColumn("函数", 160f),
            MakeColumn("调用次数", 80f),
        }));
        m_Header.canSort = true;
        m_Header.SetSorting(k_ColumnCount, false);

        m_Table = new RpcTable(new TreeViewState(), m_Header, m_VisibleRows);

        var container = new IMGUIContainer();
        container.onGUIHandler = DrawPanel;
        return container;
    }

    static MultiColumnHeaderState.Column MakeColumn(string title, float width)
    {
        return new MultiColumnHeaderState.Column
        {
            headerContent = new GUIContent(title),
            width = width,
            minWidth = 50f,
            maxWidth = 1200f,
            autoResize = true,
            canSort = true,
            allowToggleVisibility = true,
        };
    }

    static string CellText(Row row, int column)
    {
        switch (column)
        {
            case k_ColumnNamespace: return row.Namespace;
            case k_ColumnClass: return row.Class;
            case k_ColumnFunction: return row.Function;
            case k_ColumnCount: return row.CountText;
            default: return string.Empty;
        }
    }

    bool RefreshIfNeeded()
    {
        long frameIndex = ProfilerWindow.selectedFrameIndex;
        string nameList = SessionState.GetString(k_NameListKey, string.Empty);
        if (frameIndex == m_LoadedFrameIndex && nameList == m_LoadedNameList) return false;

        m_LoadedFrameIndex = frameIndex;
        m_LoadedNameList = nameList;

        m_Rows.Clear();
        m_HasFrameData = false;

        int frameIndexInt32 = Convert.ToInt32(frameIndex);
        var seen = new HashSet<string>();
        foreach (string line in nameList.Split('\n'))
        {
            string name = line.Trim();
            if (name.Length == 0 || !seen.Add(name)) continue;

            string value = UnityEditorInternal.ProfilerDriver.GetFormattedCounterValue(frameIndexInt32, k_CategoryName, name);
            if (!string.IsNullOrEmpty(value)) m_HasFrameData = true;

            long count;
            TryParseCount(value, out count);

            var row = new Row
            {
                Count = count,
                CountText = string.IsNullOrEmpty(value) ? "-" : value,
            };
            SplitCounterName(name, out row.Namespace, out row.Class, out row.Function);
            m_Rows.Add(row);
        }

        return true;
    }

    // 计数器名是 "命名空间.类名 :方法名"（无命名空间的类没有点号前缀）
    static void SplitCounterName(string name, out string nameSpace, out string className, out string function)
    {
        int colon = name.IndexOf(':');
        string owner = (colon < 0 ? name : name.Substring(0, colon)).Trim();
        function = colon < 0 ? string.Empty : name.Substring(colon + 1).Trim();

        int dot = owner.LastIndexOf('.');
        if (dot < 0)
        {
            nameSpace = k_GlobalNamespaceText;
            className = owner;
        }
        else
        {
            nameSpace = owner.Substring(0, dot);
            className = owner.Substring(dot + 1);
        }
    }

    static bool TryParseCount(string text, out long count)
    {
        count = 0;
        if (string.IsNullOrEmpty(text)) return false;
        return long.TryParse(text.Replace(",", "").Replace(" ", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
    }

    // 表头的点击排序由 MultiColumnHeader 自己维护，这里靠比对排序状态触发重排（不依赖它的排序事件）
    bool SortStateChanged()
    {
        int column = m_Header.state.sortedColumnIndex;
        bool ascending = m_Header.IsSortedAscending(column);
        if (column == m_LastSortedColumn && ascending == m_LastSortedAscending) return false;
        m_LastSortedColumn = column;
        m_LastSortedAscending = ascending;
        return true;
    }

    void ApplyFilterAndSort()
    {
        m_VisibleRows.Clear();

        string search = m_SearchText.Trim();
        for (int i = 0; i < m_Rows.Count; i++)
        {
            if (search.Length == 0 || Matches(m_Rows[i], search))
                m_VisibleRows.Add(m_Rows[i]);
        }

        m_VisibleRows.Sort(CompareRows);
    }

    static bool Matches(Row row, string search)
    {
        return row.Namespace.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || row.Class.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || row.Function.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    int CompareRows(Row a, Row b)
    {
        int result;
        switch (m_Header.state.sortedColumnIndex)
        {
            case k_ColumnNamespace: result = string.Compare(a.Namespace, b.Namespace, StringComparison.OrdinalIgnoreCase); break;
            case k_ColumnClass: result = string.Compare(a.Class, b.Class, StringComparison.OrdinalIgnoreCase); break;
            case k_ColumnFunction: result = string.Compare(a.Function, b.Function, StringComparison.OrdinalIgnoreCase); break;
            default: result = a.Count.CompareTo(b.Count); break;
        }
        if (result == 0) result = string.Compare(a.Function, b.Function, StringComparison.OrdinalIgnoreCase);
        return m_Header.IsSortedAscending(m_Header.state.sortedColumnIndex) ? result : -result;
    }

    void DrawPanel()
    {
        if (RefreshIfNeeded()) m_TableDirty = true;

        if (m_Rows.Count == 0)
        {
            EditorGUILayout.HelpBox("无远程函数调用记录", MessageType.Info);
            return;
        }

        if (!m_HasFrameData)
        {
            EditorGUILayout.HelpBox("当前选中帧里没有这些计数器的数据，换一帧试试。", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("搜索", GUILayout.Width(34f));
        string search = EditorGUILayout.TextField(m_SearchText);
        EditorGUILayout.EndHorizontal();
        if (search != m_SearchText)
        {
            m_SearchText = search;
            m_TableDirty = true;
        }

        if (SortStateChanged()) m_TableDirty = true;

        if (m_TableDirty)
        {
            ApplyFilterAndSort();
            m_Table.Reload();
            m_TableDirty = false;
        }

        if (m_VisibleRows.Count == 0)
        {
            EditorGUILayout.HelpBox("没有匹配的函数。", MessageType.Info);
            return;
        }

        float height = m_Header.height + Mathf.Min(m_VisibleRows.Count, k_MaxVisibleRows) * k_RowHeight + 4f;
        Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(height));
        m_Table.OnGUI(rect);
    }
}
