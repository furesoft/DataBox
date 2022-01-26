﻿using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;

namespace DataBox.Primitives.Layout;

internal static class DataBoxRowsLayout
{
    private static DataBoxCellsPresenter? GetCellsPresenter(IControl? control)
    {
        if (control is DataBoxRow row)
        {
            return row.CellsPresenter;
        }
        return control as DataBoxCellsPresenter;
    }

    private static double SetColumnsActualWidth(AvaloniaList<IControl> rows, DataBox root, bool measureStarAsAuto)
    {
        var accumulatedWidth = 0.0;
        var actualWidths = new double[root.Columns.Count];

        for (var c = 0; c < root.Columns.Count; c++)
        {
            var column = root.Columns[c];
            actualWidths[c] = double.IsNaN(column.MeasureWidth) ? 0.0 : column.MeasureWidth;
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
                    var actualWidth = value;

                    actualWidth = Math.Max(column.MinWidth, actualWidth);
                    actualWidth = Math.Min(column.MaxWidth, actualWidth);
                        
                    actualWidths[c] = actualWidth;
                    accumulatedWidth += actualWidths[c];

                    break;
                }
                case GridUnitType.Auto:
                {
                    var actualWidth = actualWidths[c];
  
                    foreach (var row in rows)
                    {
                        var cellPresenter = GetCellsPresenter(row);
                        if (cellPresenter is { })
                        {
                            var cells = cellPresenter.Children;
                            if (cells.Count > c && cells[c] is DataBoxCell cell)
                            {
                                var width = cell.MeasuredWidth;
                                actualWidth = Math.Max(actualWidth, width);
                            }
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
                    var actualWidth = 0.0;
  
                    foreach (var row in rows)
                    {
                        var cellPresenter = GetCellsPresenter(row);
                        if (cellPresenter is { })
                        {
                            var cells = cellPresenter.Children;
                            if (cells.Count > c && cells[c] is DataBoxCell cell)
                            {
                                var width = cell.MeasuredWidth;
                                actualWidth = Math.Max(actualWidth, width);
                            }
                        }
                    }

                    actualWidths[c] = actualWidth;

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

                    if (actualWidths[c] > actualWidth)
                    {
                        actualWidth = actualWidths[c];
                        actualWidth = Math.Max(column.MinWidth, actualWidth);
                        actualWidth = Math.Min(column.MaxWidth, actualWidth);
                    }

                    actualWidths[c] = actualWidth;
                    accumulatedWidth += actualWidths[c];

                    break;
                }
            }
        }
            
        for (var c = 0; c < root.Columns.Count; c++)
        {
            var column = root.Columns[c];
            column.MeasureWidth = actualWidths[c];
        }

        return accumulatedWidth;
    }

    private static double AdjustAccumulatedWidth(double accumulatedWidth, double availableWidth)
    {
        if (double.IsPositiveInfinity(availableWidth))
        {
            return accumulatedWidth;
        }
        return accumulatedWidth < availableWidth ? availableWidth : accumulatedWidth;
    }

    private static void MeasureCells(AvaloniaList<IControl> rows)
    {
        for (int r = 0, rowsCount = rows.Count; r < rowsCount; ++r)
        {
            var row = rows[r];
            var cellPresenter = GetCellsPresenter(row);
            if (cellPresenter is null)
            {
                continue;
            }

            var cells = cellPresenter.Children;

            for (int c = 0, cellsCount = cells.Count; c < cellsCount; ++c)
            {
                if (cells[c] is not DataBoxCell cell)
                {
                    continue;
                }

                // TODO: Optimize measure performance.Do not measure twice cells. Should be done only once in DataBoxCellsPresenter.MeasureOverride().
                // cell.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                cell.MeasuredWidth = cell.DesiredSize.Width;
            }
        }
    }

    public static Size Measure(Size availableSize, DataBox root, Func<Size, Size> measureOverride, Action invalidateMeasure, AvaloniaList<IControl> rows)
    {
        var availableSizeWidth = availableSize.Width;
        var measureStarAsAuto = double.IsPositiveInfinity(availableSize.Width);

        root.AvailableWidth = availableSize.Width;
        root.AvailableHeight = availableSize.Height;

        MeasureCells(rows);

        var accumulatedWidth = SetColumnsActualWidth(rows, root, measureStarAsAuto);
        var panelSize = availableSize.WithWidth(accumulatedWidth);

        // TODO: Optimize measure performance.
        invalidateMeasure();

        panelSize = measureOverride(panelSize);
        panelSize = panelSize.WithWidth(AdjustAccumulatedWidth(accumulatedWidth, availableSizeWidth));

        return panelSize;
    }

    public static Size Arrange(Size finalSize, DataBox root, Func<Size, Size> arrangeOverride, AvaloniaList<IControl> rows)
    {
        var finalSizeWidth = finalSize.Width;

        root.AvailableWidth = finalSize.Width;
        root.AvailableHeight = finalSize.Height;
        root.AccumulatedWidth = SetColumnsActualWidth(rows, root, false);
        var panelSize = finalSize.WithWidth(root.AccumulatedWidth);

        panelSize = arrangeOverride(panelSize);
        panelSize = panelSize.WithWidth(AdjustAccumulatedWidth(root.AccumulatedWidth, finalSizeWidth));

        return panelSize;
    }
}
