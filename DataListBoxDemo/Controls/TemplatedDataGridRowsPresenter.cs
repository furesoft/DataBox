﻿using System;
using Avalonia;
using Avalonia.Controls;

namespace DataListBoxDemo.Controls
{
    public class TemplatedDataGridRowsPresenter : VirtualizingStackPanel
    {
        private TemplatedDataGridCellsPresenter? GetCellsPresenter(IControl? control)
        {
            if (control is ListBoxItem)
            {
                return control.LogicalChildren[0] as TemplatedDataGridCellsPresenter;
            }
            return control as TemplatedDataGridCellsPresenter;
        }

        private double UpdateActualWidths(Avalonia.Controls.Controls children, TemplatedDataGrid root, bool measureStarAsAuto)
        {
            var accumulatedWidth = 0.0;
            var actualWidths = new double[root.Columns.Count];

            for (var c = 0; c < root.Columns.Count; c++)
            {
                var column = root.Columns[c];
                actualWidths[c] = double.IsNaN(column.ActualWidth) ? 0.0 : column.ActualWidth;
            }

            for (var c = 0; c < root.Columns.Count; c++)
            {
                var column = root.Columns[c];
                var type = column.Width.GridUnitType;
                var value = column.Width.Value;

                if (measureStarAsAuto && type is GridUnitType.Star)
                {
                    type = GridUnitType.Auto;
                }
  
                switch (type)
                {
                    case GridUnitType.Pixel:
                    {
                        var actualWidth = actualWidths[c];

                        foreach (var child in children)
                        {
                            var cellPresenter = GetCellsPresenter(child);
                            if (cellPresenter is { })
                            {
                                // var cells = cellPresenter.Children;
                                // var cell = cells[c];
                                // var width = DataCell.GetItemWidth(cell);
                                var width = value;
                                actualWidth = width;
                            }
                        }

                        actualWidth = Math.Max(column.MinWidth, actualWidth);
                        actualWidth = Math.Min(column.MaxWidth, actualWidth);
                        
                        actualWidths[c] = actualWidth;
                        accumulatedWidth += actualWidths[c];

                        break;
                    }
                    case GridUnitType.Auto:
                    {
                        var actualWidth = actualWidths[c];
  
                        foreach (var child in children)
                        {
                            var cellPresenter = GetCellsPresenter(child);
                            if (cellPresenter is { })
                            {
                                var cells = cellPresenter.Children;
                                var cell = cells[c];
                                var width = TemplatedDataGridCell.GetItemWidth(cell);
                                actualWidth = Math.Max(actualWidth, width);
                            }
                        }

                        actualWidth = Math.Max(column.MinWidth, actualWidth);
                        actualWidth = Math.Min(column.MaxWidth, actualWidth);

                        actualWidths[c] = actualWidth;
                        accumulatedWidth += actualWidths[c];

                        break;
                    }
                    case GridUnitType.Star:
                    {
                        break;
                    }
                }
            }

            var totalWidthForStars = Math.Max(0.0, root.AvailableWidth - accumulatedWidth);
            var totalStarValue = 0.0;

            for (var c = 0; c < root.Columns.Count; c++)
            {
                var column = root.Columns[c];
                var type = column.Width.GridUnitType;

                if (measureStarAsAuto && type is GridUnitType.Star)
                {
                    type = GridUnitType.Auto;
                }

                if (type == GridUnitType.Star)
                {
                    totalStarValue += column.Width.Value;
                }
            }

            for (var c = 0; c < root.Columns.Count; c++)
            {
                var column = root.Columns[c];
                var type = column.Width.GridUnitType;
                var value = column.Width.Value;

                if (measureStarAsAuto && type is GridUnitType.Star)
                {
                    type = GridUnitType.Auto;
                }

                switch (type)
                {
                    case GridUnitType.Star:
                    {
                        var actualWidth = (value / totalStarValue) * totalWidthForStars;

                        actualWidth = Math.Max(column.MinWidth, actualWidth);
                        actualWidth = Math.Min(column.MaxWidth, actualWidth);

                        totalWidthForStars -= actualWidth;
                        totalStarValue -= value;

                        actualWidths[c] = actualWidth;
                        accumulatedWidth += actualWidths[c];

                        break;
                    }
                }
            }
            
            for (var c = 0; c < root.Columns.Count; c++)
            {
                var column = root.Columns[c];
                column.ActualWidth = actualWidths[c];
            }

            return accumulatedWidth;
        }

        private void MeasureCells(Avalonia.Controls.Controls children)
        {
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];
                var cellPresenter = GetCellsPresenter(child);
                if (cellPresenter is { })
                {
                    cellPresenter.MeasureCells();
                }
            }
        }

        private void InvalidateMeasureChildren(Avalonia.Controls.Controls children)
        {
            InvalidateMeasure();
            /*
            foreach (var child in children)
            {
                child.InvalidateMeasure();

                var cellPresenter = GetCellsPresenter(child);
                if (cellPresenter is { })
                {
                    cellPresenter.InvalidateMeasure();

                    var cells = cellPresenter.Children;
                    foreach (var cell in cells)
                    {
                        cell.InvalidateMeasure();
                    }
                }
            }
            */
        }

        private void InvalidateArrangeChildren(Avalonia.Controls.Controls children)
        {
            foreach (var child in children)
            {
                child.InvalidateArrange();

                var cellPresenter = GetCellsPresenter(child);
                if (cellPresenter is { })
                {
                    cellPresenter.InvalidateArrange();

                    var cells = cellPresenter.Children;
                    foreach (var cell in cells)
                    {
                        cell.InvalidateArrange();
                    }
                }
            }
        }

        private Size MeasureRows(Size availableSize, TemplatedDataGrid root)
        {
            var children = Children;

            var measureStarAsAuto = double.IsPositiveInfinity(availableSize.Width);

            root.AvailableWidth = availableSize.Width;
            root.AvailableHeight = availableSize.Height;

            // TODO: Measure children only when column ActualWidth changes.
            MeasureCells(children);

            var accumulatedWidth = UpdateActualWidths(children, root, measureStarAsAuto);
            var panelSize = availableSize.WithWidth(accumulatedWidth);

            // TODO: InvalidateMeasure children only when column ActualWidth changes.
            InvalidateMeasureChildren(children);

            panelSize = base.MeasureOverride(panelSize);
            panelSize = panelSize.WithWidth(accumulatedWidth);

            return panelSize;
        }

        private Size ArrangeRows(Size finalSize, TemplatedDataGrid root)
        {
            var children = Children;

            root.AvailableWidth = finalSize.Width;
            root.AvailableHeight = finalSize.Height;

            root.AccumulatedWidth = UpdateActualWidths(children, root, false);
            var panelSize = finalSize.WithWidth(root.AccumulatedWidth);

            // TODO: InvalidateArrange children only when column ActualWidth changes.
            InvalidateArrangeChildren(children);

            panelSize = base.ArrangeOverride(panelSize);
            panelSize = panelSize.WithWidth(root.AccumulatedWidth);

            return panelSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var root = TemplatedDataGridProperties.GetRoot(this);
            if (root is null)
            {
                return availableSize;
            }

            return MeasureRows(availableSize, root);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var root = TemplatedDataGridProperties.GetRoot(this);
            if (root is null)
            {
                return finalSize;
            }

            return ArrangeRows(finalSize, root);
        }
    }
}