using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MoreWidgets {
    public class Table2DWidget<TRow, TColumn> : TableWidget<TRow> {

        public Func<TRow, string> rowLabel;
        public Func<TColumn, string> columnLabel;
        public Action<Rect, TRow, TColumn> doCell;
        public Func<TRow, TColumn, float> cellMinWidth;
        public Func<TRow, TColumn, float> cellMaxWidth;
        public Func<TRow, TRow, TColumn, int> rowComparer;
        public Func<TColumn, TColumn, TRow, int> columnComparer;
        public new Action<TColumn> onHeaderClick;
        public new Action<TColumn> onColumnClick;
        public new Action<Rect, TColumn> onColumnHover;
        public Action<TRow> onRowLabelClick;
        public new bool highlightColumn = false;

        private readonly List<TColumn> columnItems = new List<TColumn>();
        private readonly List<TColumn> columnsSorted = new List < TColumn >();

        private bool sortingCol;
        private TRow sortColBy;
        private bool sortColReverse;

        public Table2DWidget() {
            firstColumnLocked = true;
        }

        public void SetRowItems(IEnumerable<TRow> rows)
            => SetItems(rows);

        public void SetColumnItems(IEnumerable<TColumn> columns) {
            columnItems.Clear();
            columnItems.AddRange(columns);
            colsDirty = true;
            sortDirty = true;
        }

        public override void OnGUI(Rect rect) {
            if (base.onHeaderClick == null && onHeaderClick != null) {
                base.onHeaderClick = col => onHeaderClick?.Invoke(columnItems[col]);
            }
            if (base.onHeaderClick == null && onHeaderClick != null) {
                base.onHeaderClick = col => onHeaderClick?.Invoke(columnItems[col]);
            }
            if (base.onColumnHover == null && (highlightColumn || onColumnHover != null)) {
                base.onColumnHover = OnColumnHover;
            }
            base.OnGUI(rect);
        }

        protected override void Sort() {
            bool doSort = sortingCol && columnComparer != null;

            if (colsDirty || !doSort) {
                columnsSorted.Clear();
                columnsSorted.AddRange(columnItems);
            }

            if (doSort) {
                Sort(columnsSorted, (a, b) => columnComparer(a, b, sortColBy), sortColReverse);
            }

            if (colsDirty || doSort) {
                UpdateColumns();
            }

            base.Sort();
        }

        private void UpdateColumns() {
            ClearColumns();
            AddColumn("", rowLabel, onClick: OnRowLabelClick);
            foreach (var col in columnsSorted) {
                AddColumn(new ItemColumn(this, col));
            }
        }

        private void OnColumnHover(Rect rect, int col) {
            if (col == 0) return;
            onColumnHover?.Invoke(rect, columnItems[col - 1]);
            if (highlightColumn) {
                Widgets.DrawHighlight(rect);
            }
        }

        private void OnRowLabelClick(TRow row) {
            if (columnComparer != null && Event.current.button == 0) {
                HeaderClicked(row, ref sortingCol, ref sortColBy, ref sortColReverse);
            } else {
                onRowLabelClick?.Invoke(row);
            }
        }

        private class ItemColumn : Column {
            private readonly Table2DWidget<TRow, TColumn> table;
            public readonly TColumn col;

            public ItemColumn(Table2DWidget<TRow, TColumn> table, TColumn col) {
                this.table = table;
                this.col = col;
            }

            protected override string Name
                => table.columnLabel(col);

            public override Comparison<TRow> Comparer
                => (table.rowComparer == null) ? (Comparison<TRow>) null : CompareFunc;

            public override bool Equals(object obj)
                => obj is ItemColumn ic && ReferenceEquals(col, ic.col) && ReferenceEquals(table, ic.table);

            public override int GetHashCode()
                => col.GetHashCode() ^ table.GetHashCode();

            public override float MinWidth(List<TRow> list)
                => Mathf.Max(base.MinWidth(list), list.Max(MinCellWidth));

            public override float MaxWidth(List<TRow> list)
                => Mathf.Min(base.MaxWidth(list), list.Max(MaxCellWidth));

            public override void DoCell(Rect rect, TRow row, int _)
                => table.doCell?.Invoke(rect, row, col);

            private int CompareFunc(TRow x, TRow y)
                => table.rowComparer(x, y, col);

            private float MinCellWidth(TRow row)
                => table.cellMinWidth?.Invoke(row, col) ?? 0;

            private float MaxCellWidth(TRow row)
                => table.cellMaxWidth?.Invoke(row, col) ?? float.MaxValue;
        }
    }
}
