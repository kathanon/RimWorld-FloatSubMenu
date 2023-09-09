using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace MoreWidgets {
    public class TableWidget<T> {
        protected const float Margin = 4f;

        protected readonly List<Column> columns = new List<Column>();
        protected readonly List<float> columnWidths = new List<float>();
        protected readonly List<T> items = new List<T>();
        protected readonly List<T> sorted = new List<T>();

        public Action<Rect, T> onRowHover;
        public Action<T> onRowClick;
        public Action<Rect, int> onColumnHover;
        public Action<int> onColumnClick;
        public Action<int> onHeaderClick;
        public bool highlightRow = true;
        public bool highlightColumn = false;
        public bool highlightHeader = true;
        public bool sameWidthColumns = false;
        public bool firstColumnLocked = false;

        private bool sorting;
        private Column sortBy;
        private bool sortReverse;
        protected bool sortDirty = true;
        protected bool itemsDirty = true;
        protected bool colsDirty = true;
        protected Vector2 scroll;
        private Vector2 lastSize;
        private float headerHeight = 30f;
        private float cellHeight = 24f;
        private float totalWidth;

        public int RowCount
            => sorted.Count;

        public int ColumnCount
            => columns.Count;

        public IEnumerable<T> Rows
            => sorted;

        public IEnumerable<Column> Columns
            => columns;

        public Vector2 ViewSize
            => new Vector2(totalWidth, items.Count * (cellHeight + Margin) - Margin);

        public void Resort()
            => sortDirty = true;

        public void RecaculateWidths()
            => colsDirty = true;

        public virtual void OnGUI(Rect rect) {
            if (sortDirty) {
                Sort();
            }
            if (colsDirty || rect.size != lastSize) {
                // TODO: Calculate: header height, cell height, more?
                UpdateColumnWidths(rect);

                var scrollSize = ViewSize - rect.size;
                scrollSize.y += headerHeight + Margin;
                scroll.x = Mathf.Max(Mathf.Min(scroll.x, scrollSize.x), 0f);
                scroll.y = Mathf.Max(Mathf.Min(scroll.y, scrollSize.y), 0f);

                lastSize = rect.size;
            }

            Widgets.BeginGroup(rect);
            rect = rect.AtZero();
            DoTable(rect);

            Widgets.EndGroup();
        }

        private void DoTable(Rect rect) {
            int mouseColumn = -1;
            if (highlightColumn || onColumnHover != null || onColumnClick != null) {
                Rect col = rect;
                col.x -= scroll.x;
                for (int i = 0, n = columns.Count; i < n; i++) {
                    col.width = columnWidths[i];
                    if (Mouse.IsOver(col)) {
                        if (highlightColumn) {
                            Widgets.DrawHighlightIfMouseover(col);
                        }
                        onColumnHover?.Invoke(col, i);
                        mouseColumn = i;
                    }
                    col.StepX(Margin);
                }
            }

            Rect row = rect.TopPartPixels(headerHeight);
            Widgets.BeginGroup(row);
            var header = row.AtZero();
            header.x = -scroll.x;
            header.width = totalWidth;
            DoHeaders(header);
            Widgets.EndGroup();
            rect.yMin += headerHeight + Margin;

            float totalHeight = items.Count * (cellHeight + Margin) - Margin;
            Rect view = new Rect(default, ViewSize);
            Widgets.BeginScrollView(rect, ref scroll, view);
            Rect visible = new Rect(scroll, rect.size);

            row = view.TopPartPixels(cellHeight);
            foreach (var item in sorted) {
                if (visible.Overlaps(row)) {
                    bool hover = Mouse.IsOver(row);
                    if (highlightRow && hover) {
                        Widgets.DrawHighlight(row);
                    }

                    if (onRowHover != null && hover) {
                        onRowHover(row, item);
                    }

                    DoCells(row, item, visible);

                    if (onRowClick != null && Widgets.ButtonInvisible(row, false)) {
                        onRowClick(item);
                    }
                }

                row.StepY(Margin);
            }

            if (mouseColumn >= 0 && Event.current.type == EventType.MouseDown) {
                onColumnClick?.Invoke(mouseColumn);
            }

            Widgets.EndScrollView();
        }

        private void UpdateColumnWidths(Rect rect) {
            columnWidths.Clear();
            columnWidths.AddRange(columns.Select(x => x.MinWidth(sorted)));
            if (sameWidthColumns && columns.Count > 0) {
                float width = columnWidths.Max();
                for (int i = 0, n = columnWidths.Count; i < n; i++) {
                    columnWidths[i] = width;
                }
            }
            totalWidth = rect.width - 18f;
            float margins = (columnWidths.Count - 1) * Margin;
            float minWidth = columnWidths.Sum() + margins;
            if (minWidth > totalWidth) {
                totalWidth = minWidth;
            } else {
                int n = columns.Count;
                // TODO: Design better and configurable fill strategy
                float add = Mathf.Min((totalWidth - minWidth) / n, 16f);
                for (int i = 0; i < n; i++) {
                    columnWidths[i] = Mathf.Min(columnWidths[i] + add, columns[i].MaxWidth(sorted));
                }
                totalWidth = columnWidths.Sum() + margins;
            }
            colsDirty = false;
        }

        private void DoHeaders(Rect rect) {
            for (int i = 0, n = columns.Count; i < n; i++) {
                var col = columns[i];
                rect.width = columnWidths[i];

                HeaderButton(rect, i, onHeaderClick, col.Comparer != null);
                // TODO: Sorting indicator?

                Widgets.BeginGroup(rect);
                col.DoHeader(rect.AtZero());
                Widgets.EndGroup();

                rect.StepX(Margin);
            }
        }

        private void DoCells(Rect rect, T item, Rect visible) {
            for (int i = 0, n = columns.Count; i < n; i++) {
                var col = columns[i];
                rect.width = columnWidths[i];
                if (visible.Overlaps(rect)) {
                    Widgets.BeginGroup(rect);
                    col.DoCell(rect.AtZero(), item, i);
                    Widgets.EndGroup();
                }
                rect.StepX(Margin);
            }
        }

        public void AddColumn(Column c) {
            columns.Add(c);
            colsDirty = true;
        }

        public Column AddColumn(string name,
                                Action<Rect, T> doCell,
                                Func<T, float> minWidth = null,
                                Func<T, float> maxWidth = null,
                                Comparison<T> comparer = null) {
            var c = new SimpleColumn(name, doCell, minWidth, maxWidth, comparer);
            AddColumn(c);
            return c;
        }

        public Column AddColumn(string name,
                                Action<Rect, T> doCell,
                                float minWidth,
                                float maxWidth = -1f,
                                Comparison<T> comparer = null)
            => AddColumn(name,
                         doCell,
                         _ => minWidth,
                         (maxWidth >= minWidth) ? _ => maxWidth : (Func<T, float>) null,
                         comparer);

        public Column AddColumn(string name,
                                Func<T, string> cellText,
                                Func<T, string> tooltip = null,
                                Action<T> onClick = null) {
            var c = new StringColumn(name, cellText, tooltip, onClick);
            AddColumn(c);
            return c;
        }

        public void SetColumn(int i, Column c) {
            columns[i] = c;
            colsDirty = true;
        }

        public void RemoveColumn(Column c) {
            columns.Remove(c);
            colsDirty = true;
        }

        public void RemoveColumn(int i) {
            columns.RemoveAt(i);
            colsDirty = true;
        }

        public void ClearColumns() {
            columns.Clear();
            colsDirty = true;
        }

        public void SetItems(IEnumerable<T> items) {
            this.items.Clear();
            this.items.AddRange(items);
            itemsDirty = true;
            sortDirty = true;
        }

        protected void HeaderButton(Rect rect,
                                    int cur,
                                    Action<int> onClick,
                                    bool hasSort) {
            if (highlightHeader) {
                Widgets.DrawHighlightIfMouseover(rect);
            }
            if (Widgets.ButtonInvisible(rect)) {
                if (hasSort && Event.current.button == 0) {
                    HeaderClicked(columns[cur], ref sorting, ref sortBy, ref sortReverse);
                } else {
                    onClick?.Invoke(cur);
                }
            }
        }

        protected void HeaderClicked<S>(S cur, ref bool sorting, ref S sortBy, ref bool sortReverse) {
            if (sorting && cur.Equals(sortBy)) {
                if (sortReverse) {
                    sorting = false;
                    sortBy = default;
                } else {
                    sortReverse = true;
                }
            } else {
                sorting = true;
                sortBy = cur;
                sortReverse = false;
            }
            sortDirty = true;
        }

        protected virtual void Sort() {
            if (sorting && (!columns.Contains(sortBy) || sortBy?.Comparer == null)) {
                sorting = false;
                sortReverse = false;
                sortBy = null;
            }
            if (itemsDirty || !sorting) {
                sorted.Clear();
                sorted.AddRange(items);
                itemsDirty = false;
            }
            if (sorting) {
                Sort(sorted, sortBy.Comparer, sortReverse);
            }
            sortDirty = false;
        }

        protected static void Sort<S>(List<S> list, Comparison<S> cmp, bool reverse) {
            if (reverse) {
                var normal = cmp;
                cmp = (x, y) => -normal(x, y);
            }
            list.Sort(cmp);
        }

        public abstract class Column {
            protected virtual string Name
                => null;

            public virtual Comparison<T> Comparer
                => null;

            public override string ToString()
                => Name;

            public virtual float MinWidth(List<T> list)
                => Text.CalcSize(Name).x + 4f;

            public virtual float MaxWidth(List<T> list)
                => float.MaxValue;

            public virtual void DoHeader(Rect rect) {
                Text.Anchor = TextAnchor.LowerCenter;
                Widgets.Label(rect, Name);
                Text.Anchor = TextAnchor.UpperLeft;
            }

            public abstract void DoCell(Rect rect, T item, int row);

            protected float Max(List<T> list, Func<T, float> f)
                => list.Any() ? list.Max(f) : 0f;

            protected float Min(List<T> list, Func<T, float> f)
                => list.Any() ? list.Min(f) : float.MaxValue;
        }

        protected class SimpleColumn : Column {
            private readonly string name;
            private readonly Action<Rect, T> doCell;
            private readonly Func<T, float> minWidth;
            private readonly Func<T, float> maxWidth;
            private readonly Comparison<T> comparer;

            public SimpleColumn(string name,
                                Action<Rect, T> doCell,
                                Func<T, float> minWidth = null,
                                Func<T, float> maxWidth = null,
                                Comparison<T> comparer = null) {
                this.name = name;
                this.doCell = doCell;
                this.minWidth = minWidth ?? (_ => 0f);
                this.maxWidth = maxWidth ?? (_ => float.MaxValue);
                this.comparer = comparer;
            }

            protected override string Name
                => name;

            public override Comparison<T> Comparer
                => comparer;

            public override float MinWidth(List<T> list)
                => Mathf.Max(base.MinWidth(list), Max(list, minWidth));

            public override float MaxWidth(List<T> list)
                => Min(list, maxWidth);

            public override void DoCell(Rect rect, T item, int row)
                => doCell(rect, item);
        }

        protected class StringColumn : Column {
            private static int nextID = 265498191;

            private readonly int ID;
            private readonly string name;
            private readonly Func<T, string> cellText;
            private readonly Func<T, string> tooltip;
            private readonly Action<T> onClick;
            private readonly Comparison<T> comparer;

            public StringColumn(string name, Func<T, string> cellText, Func<T, string> tooltip = null, Action<T> onClick = null) {
                this.name = name;
                this.cellText = cellText;
                this.tooltip = tooltip;
                this.onClick = onClick;
                comparer = ComparerMethod;
                ID = nextID++;
            }

            protected override string Name
                => name;

            public override Comparison<T> Comparer
                => comparer;

            public override float MinWidth(List<T> list)
                => Mathf.Max(base.MinWidth(list), Max(list, x => Text.CalcSize(cellText(x)).x));

            public override void DoCell(Rect rect, T item, int row) {
                string text = cellText(item);
                Text.Anchor = (Text.CalcSize(text).x > rect.width) ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
                Widgets.Label(rect, text);
                Text.Anchor = TextAnchor.UpperLeft;

                if (tooltip != null && tooltip(item) != null) {
                    TooltipHandler.TipRegion(rect, () => tooltip(item), ID & (row * 1000));
                }

                if (onClick != null && Widgets.ButtonInvisible(rect)) {
                    onClick(item);
                }
            }

            private int ComparerMethod(T x, T y)
                => cellText(x).CompareTo(cellText(y));
        }
    }
}