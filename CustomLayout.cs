using Microsoft.Maui.Layouts;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace LayoutIssue;
record Field(Label Lead, View Value, Button Assist = null) {
    internal Size Size = Size.Zero;
}
class CustomLayout : VerticalStackLayout {
    readonly List<Field> Fields = new();
    public void Add(params Field[] fields) {
        foreach (var field in fields) {
            Fields.Add(field);
            Children.Add(field.Lead);
            Children.Add(field.Value);
            if (field.Assist != null)
                Children.Add(field.Assist);
        }
    }
    protected override ILayoutManager CreateLayoutManager() =>
        new Manager(this);
    public double MinColumnWidth { get; init; } = 300;
    public double MaxLeadWidth { get; init; } = 120;
    class Manager : VerticalStackLayoutManager {
        public Manager(CustomLayout layout) : base(layout) { }
        public new CustomLayout Layout => (CustomLayout)base.Layout;
        IReadOnlyList<Field> Fields => Layout.Fields;
        int columnCount;
        double columnWidth, leadWidth, leadHeight;
        Size assistSize;
        public override Size Measure(double widthConstraint, double heightConstraint) {
            if (Fields.Count < 1) return new Size(0, 0);
            var padding = Layout.Padding;
            heightConstraint -= padding.VerticalThickness;
            leadWidth = 0; leadHeight = 0; assistSize = new();
            foreach (var field in Fields)
                if (field.Assist is Button assist) { 
                    Size size = assist.Measure(widthConstraint, heightConstraint);
                    assistSize = new Size(
                        Math.Max(assistSize.Width, size.Width),
                        Math.Max(assistSize.Height, size.Height));
                }
            foreach (var field in Fields) {
                Size leadMeasure =
                    field.Lead.Measure(double.PositiveInfinity, double.PositiveInfinity);
                leadWidth = Math.Min(Layout.MaxLeadWidth,
                    Math.Max(leadWidth, leadMeasure.Width + assistSize.Width));
                leadHeight = Math.Max(leadHeight, leadMeasure.Height);
            }
            leadHeight = Math.Max(leadHeight, assistSize.Height);
            columnCount = Math.Max(1,
                Math.Min(Fields.Count, (int)(widthConstraint / Layout.MinColumnWidth)));
            double availableWidth = widthConstraint - padding.HorizontalThickness -
                (columnCount - 1) * Layout.Spacing;
            columnWidth = availableWidth / columnCount;
            double valueWidth = columnWidth - leadWidth - Layout.Spacing;
            double maxHeight = Math.Min(heightConstraint, 30 * 7);
            foreach (var field in Fields) {
                double width = columnWidth;
                width -= leadWidth + Layout.Spacing;
                field.Size = field.Value.Measure(width, maxHeight);
            }
            double totalHeight = (Fields.Count - columnCount) * Layout.Spacing +
                Fields.Sum(f => f.Size.Height);
            if (columnCount > 1) {
                double averageHeight = totalHeight / columnCount;
                double GetColumnHeight(int startColumn, int firstField) {
                    if (startColumn == columnCount - 1)
                        return Fields.Skip(firstField).Sum(f => f.Size.Height)
                            + (Fields.Count - firstField - 1) * Layout.Spacing;
                    double columnHeight = Fields[firstField].Size.Height;
                    int f = firstField;
                    while (columnHeight < averageHeight
                        && f < Fields.Count - (columnCount - startColumn))
                        columnHeight += Layout.Spacing + Fields[++f].Size.Height;
                    double downstreamColumnHeight =
                        GetColumnHeight(startColumn + 1, f + 1);
                    if (f > firstField) {
                        double alternateDownstreamColumnHeight =
                            GetColumnHeight(startColumn + 1, f);
                        return Math.Min(
                            Math.Max(downstreamColumnHeight, columnHeight),
                            Math.Max(alternateDownstreamColumnHeight,
                                columnHeight - Fields[f].Size.Height));
                    }
                    return Math.Max(columnHeight, downstreamColumnHeight);
                }
                totalHeight = GetColumnHeight(0, 0);
            }
            totalHeight += 2 * padding.VerticalThickness;
            var finalHeight = ResolveConstraints(
                heightConstraint, Stack.Height, totalHeight,
                Stack.MinimumHeight, Stack.MaximumHeight);
            return new Size(widthConstraint, finalHeight);
        }
        public override Size ArrangeChildren(Rect bounds) {
            var padding = Stack.Padding;
            double top = padding.Top + bounds.Top;
            double left = padding.Left + bounds.Left;
            double bottom = bounds.Bottom - padding.Bottom;
            double currentX = left;
            double currentY = top;
            double maxColumnHeight = 0;
            int fieldsInColumn = 0;
            int currentColumn = 0;
            for (int f = 0; f < Fields.Count; f++) {
                var field = Fields[f];
                if (fieldsInColumn > 0 &&
                    (currentY + field.Size.Height > bottom ||
                     f == Fields.Count - (columnCount - currentColumn - 1))) {
                    currentY = top;
                    currentX += Layout.Spacing + columnWidth;
                    fieldsInColumn = 0;
                    currentColumn++;
                }
                fieldsInColumn++;
                var (width, height) = (leadWidth, field.Size.Height);
                double x = currentX;
                Arrange(field.Lead, x, currentY, width, height);
                double assistX = x + leadWidth + Layout.Spacing;
                double assistY = currentY;
                x += width + Layout.Spacing;
                width = columnWidth - width - Layout.Spacing;
                Arrange(field.Value, x, currentY, width, height);
                assistX -= 2 * assistSize.Width;
                if (field.Assist is Button assist)
                    Arrange(assist,
                        assistX += assistSize.Width, assistY,
                        assistSize.Width, assistSize.Height);
                currentY += height + Layout.Spacing;
                if (currentY - top > maxColumnHeight)
                    maxColumnHeight = currentY - top;
                static void Arrange(IView view,
                    double x, double y, double w, double h) =>
                    view.Arrange(new Rect(x, y, w, h));
            }
            Size actual = new Size(bounds.Right - bounds.Left,
                padding.Top + maxColumnHeight + padding.Bottom).
                AdjustForFill(bounds, Stack);
            return actual;
        }
    }
}