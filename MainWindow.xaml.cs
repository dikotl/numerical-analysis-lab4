using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Lab4;

public partial class MainWindow : Window
{
    private static readonly SolidColorBrush _splineColorBrush = new((Color)ColorConverter.ConvertFromString("#FF8C00"));
    private static readonly SolidColorBrush _polynomialColorBrush = new((Color)ColorConverter.ConvertFromString("#1E90FF"));
    private static readonly SolidColorBrush _differenceColorBrush = new((Color)ColorConverter.ConvertFromString("#DC143C"));

    private List<Point> _points = [new(1, 1), new(3, 14), new(5, 8), new(6, 12), new(9, 10)];

    // Dragging stuff.
    private bool _isDragging = false;
    private int _draggedPointIndex = -1;

    // Screen scaling, pixels per 1 unit.
    private const double SCALE = 20.0;
    private const double POINT_RADIUS = 6.0;
    private const double GRAPH_DISTANCE_BETWEEN_POINTS = 0.05;

    private readonly Polyline _spline = new()
    {
        Stroke = _splineColorBrush,
        StrokeThickness = 2.5
    };

    private readonly Polyline _polynomial = new()
    {
        Stroke = _polynomialColorBrush,
        StrokeThickness = 2
    };

    private readonly Polyline _difference = new()
    {
        Stroke = _differenceColorBrush,
        StrokeThickness = 2,
        StrokeDashArray = [4, 3],
    };

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) => Redraw();
        SizeChanged += (_, _) => Redraw();
    }

    #region Event handlers

    private void Canvas_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {
        Point mousePosition = e.GetPosition(DrawCanvas);
        Point mousePositionInUnits = ScreenSpaceToUnits(mousePosition);

        int pointIndex = GetPointIndexAt(mousePosition);

        if (pointIndex != -1)
        {
            _isDragging = true;
            _draggedPointIndex = pointIndex;
            DrawCanvas.CaptureMouse();
        }
        else
        {
            _points.Add(mousePositionInUnits);
            Redraw();
        }
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging && _draggedPointIndex != -1)
        {
            _points[_draggedPointIndex] = ScreenSpaceToUnits(e.GetPosition(DrawCanvas));
            Redraw();
        }
    }

    private void Canvas_MouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            _draggedPointIndex = -1;
            DrawCanvas.ReleaseMouseCapture();
        }
    }

    private void Canvas_MouseRightButtonDown(object? sender, MouseButtonEventArgs e)
    {
        var index = GetPointIndexAt(e.GetPosition(DrawCanvas));

        if (index != -1 && _points.Count > 2)
        {
            _points.RemoveAt(index);
            Redraw();
        }
    }

    private void IsClosedSpline_CheckedChanged(object? sender, RoutedEventArgs e)
    {
        Redraw();
    }

    private int GetPointIndexAt(Point screenPos)
    {
        return _points.FindIndex(point =>
        {
            var pScreen = UnitsToScreenSpace(point);
            var dx = pScreen.X - screenPos.X;
            var dy = pScreen.Y - screenPos.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= POINT_RADIUS * 2;
        });
    }

    #endregion

    #region Draw

    private void Redraw()
    {
        if (DrawCanvas.ActualWidth == 0) return;

        DrawCanvas.Children.Clear();

        Point? activePoint = _draggedPointIndex != -1 ? _points[_draggedPointIndex] : null;

        // Sort points by X axis so we can draw spline correctly.
        _points = _points.OrderBy(p => p.X).ToList();

        // If there is 2 points with almost identical X value slightly offset one of them.
        for (int i = 1; i < _points.Count; i++)
        {
            if (_points[i].X - _points[i - 1].X < 0.01)
            {
                _points[i] = new Point(_points[i - 1].X + 0.01, _points[i].Y);
            }
        }

        // Set correct point index if they was reordered.
        if (activePoint is Point active)
        {
            _draggedPointIndex = _points.IndexOf(active);
        }

        // In closed splines first and last points must have same Y value.
        if (IsClosedSpline?.IsChecked is true && _points.Count > 1)
        {
            _points[^1] = new Point(_points[^1].X, _points[0].Y);
        }

        DrawAxes();
        if (_points.Count >= 2) DrawGraph();
        DrawPoints();
    }

    private void DrawAxes()
    {
        var center = UnitsToScreenSpace(new(0, 0));
        var xAxisLine = new Line()
        {
            X1 = 0,
            Y1 = center.Y,
            X2 = DrawCanvas.ActualWidth,
            Y2 = center.Y,
            Stroke = Brushes.LightGray,
            StrokeThickness = 1
        };
        var yAxisLine = new Line()
        {
            X1 = center.X,
            Y1 = 0,
            X2 = center.X,
            Y2 = DrawCanvas.ActualHeight,
            Stroke = Brushes.LightGray,
            StrokeThickness = 1
        };
        DrawCanvas.Children.Add(xAxisLine);
        DrawCanvas.Children.Add(yAxisLine);
    }

    private void DrawPoints()
    {
        for (int i = 0; i < _points.Count; i++)
        {
            var fillBrush = (IsClosedSpline?.IsChecked is true && i == _points.Count - 1)
                ? Brushes.Red
                : Brushes.White;
            var pointPosition = UnitsToScreenSpace(_points[i]);
            var ellipse = new Ellipse
            {
                Width = POINT_RADIUS * 2,
                Height = POINT_RADIUS * 2,
                Fill = fillBrush,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            Canvas.SetLeft(ellipse, pointPosition.X - POINT_RADIUS);
            Canvas.SetTop(ellipse, pointPosition.Y - POINT_RADIUS);
            DrawCanvas.Children.Add(ellipse);
        }
    }

    private Point UnitsToScreenSpace(Point mathPoint)
    {
        double x = DrawCanvas.ActualWidth / 2;
        double y = DrawCanvas.ActualHeight / 2;
        return new Point(x + mathPoint.X * SCALE, y - mathPoint.Y * SCALE);
    }

    private Point ScreenSpaceToUnits(Point screenPoint)
    {
        double x = DrawCanvas.ActualWidth / 2;
        double y = DrawCanvas.ActualHeight / 2;
        return new Point((screenPoint.X - x) / SCALE, -(screenPoint.Y - y) / SCALE);
    }

    #endregion

    #region Math

    private void DrawGraph()
    {
        const double OVERDRAW = 2.0;

        var P = Spline.Generate(_points, IsClosedSpline?.IsChecked is true);
        if (P is null) return;

        _polynomial.Points.Clear();
        _spline.Points.Clear();
        _difference.Points.Clear();

        var (a, b, c, d) = P;
        var minX = _points.First().X - OVERDRAW;
        var maxX = _points.Last().X + OVERDRAW;
        var currentSegmentIndex = 0;
        var n = _points.Count - 1;

        for (var x = minX; x <= maxX; x += GRAPH_DISTANCE_BETWEEN_POINTS)
        {
            // Increment segment index if X is outside of the current segment.
            while (currentSegmentIndex < n - 1 && x > _points[currentSegmentIndex + 1].X)
            {
                currentSegmentIndex++;
            }

            var polyY = LagrangeInterpolation(x);
            var dx = x - _points[currentSegmentIndex].X;
            var splineY = a[currentSegmentIndex]
                        + b[currentSegmentIndex] * dx
                        + c[currentSegmentIndex] * dx * dx
                        + d[currentSegmentIndex] * dx * dx * dx;

            _spline.Points.Add(UnitsToScreenSpace(new Point(x, splineY)));
            _polynomial.Points.Add(UnitsToScreenSpace(new Point(x, polyY)));
            _difference.Points.Add(UnitsToScreenSpace(new Point(x, polyY - splineY)));
        }

        DrawCanvas.Children.Add(_spline);
        DrawCanvas.Children.Add(_polynomial);
        DrawCanvas.Children.Add(_difference);
    }

    private double LagrangeInterpolation(double x)
    {
        var sum = 0.0;

        for (int i = 0; i < _points.Count; i++)
        {
            var p = 1.0;

            for (int j = 0; j < _points.Count; j++)
            {
                if (i != j)
                {
                    // 2 points can't be placed at the same X.
                    p *= (x - _points[j].X) / (_points[i].X - _points[j].X);
                }
            }

            sum += _points[i].Y * p;
        }

        return sum;
    }

    #endregion

    #region Formulas export

    private void BtnCopyFormulas_Click(object? sender, RoutedEventArgs e)
    {
        var formulas = ExportFormulas();

        if (string.IsNullOrEmpty(formulas))
        {
            MessageBox.Show(
                "Unable to export formulas due to internal error.",
                "Internal error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }
        else
        {
            Clipboard.SetText(formulas);
            MessageBox.Show(
                "Formulas was copied to clipboard!",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }

    private string ExportFormulas()
    {
        static string Float(double x) => x.ToString("0.####", CultureInfo.InvariantCulture);

        var points = _points.OrderBy(p => p.X).ToList();
        var C = Spline.Generate(points);
        if (C is null) return "";
        var (a, b, c, d) = C;

        // Force using '.' instead of ',' for floats.
        var cultureInfo = CultureInfo.InvariantCulture;
        var buffer = new StringBuilder();

        // Write points.
        foreach (var point in points)
        {
            buffer.AppendLine($"({Float(point.X)}, {Float(point.Y)})");
        }

        // Write spline formula.
        buffer.Append(@"S(x) = \{");
        for (int i = 0; i < points.Count - 1; i++)
        {
            var term = $"{Float(a[i])} + " +
                $"{Float(b[i])}(x - ({Float(points[i].X)})) + " +
                $"{Float(c[i])}(x - ({Float(points[i].X)}))^2 + " +
                $"{Float(d[i])}(x - ({Float(points[i].X)}))^3";
            buffer.Append(
                (i == points.Count - 2)
                ? term // last
                : $"x <= {Float(points[i + 1].X)}: {term}, "
            );
        }
        buffer.AppendLine(@"\}");

        // Write polynomial formula.
        buffer.Append("P(x) = ");
        for (int i = 0; i < points.Count; i++)
        {
            buffer.Append(Float(points[i].Y));

            for (int j = 0; j < points.Count; j++)
            {
                if (i != j)
                {
                    buffer.Append(
                        $"((x - ({Float(points[j].X)}))/" +
                       $"({Float(points[i].X)} - ({Float(points[j].X)})))"
                    );
                }
            }

            if (i < points.Count - 1)
            {
                buffer.Append(" + ");
            }
        }

        return buffer.ToString();
    }

    #endregion
}

record Spline(double[] A, double[] B, double[] C, double[] D)
{
    private const double EPS = 1e-8;

    public static Spline? Generate(IList<Point> points, bool isClosed = false)
    {
        if (points.Count < 2)
        {
            // Impossible to generate spline for 0 or 1 points.
            return null;
        }

        var n = points.Count - 1;
        var a = new double[n + 1];
        var b = new double[n];
        var d = new double[n];
        var h = new double[n];
        double[] c;

        for (int i = 0; i <= n; i++)
        {
            a[i] = points[i].Y;
        }
        for (int i = 0; i < n; i++)
        {
            h[i] = points[i + 1].X - points[i].X;

            // If all points are at Y=0, replace with epsilon.
            if (Math.Abs(h[i]) < EPS) h[i] = EPS;
        }

        if (isClosed)
        {
            c = new double[n + 1];
            var A = new double[n, n];
            var B = new double[n];

            // Closed spline requires first & last points to be equal.
            a[n] = a[0];

            for (int i = 0; i < n; i++)
            {
                int prev = (i - 1 + n) % n;
                int next = (i + 1) % n;

                A[i, prev] += h[prev];
                A[i, next] += h[i];
                A[i, i] += 2.0 * (h[prev] + h[i]);

                var d0 = (a[i] - a[prev]) / h[prev];
                var d1 = (a[next] - a[i]) / h[i];
                B[i] = 6.0 * (d1 - d0);
            }

            var cReduced = LupSolve(A, B);

            for (int i = 0; i < n; i++)
            {
                c[i] = cReduced[i];
            }
            c[n] = c[0];
        }
        else
        {
            var A = new double[n + 1, n + 1];
            var B = new double[n + 1];

            A[0, 0] = 1.0;
            A[n, n] = 1.0;

            B[0] = 0.0;
            B[n] = 0.0;

            for (int i = 1; i < n; i++)
            {
                A[i, i - 1] = h[i - 1];
                A[i, i + 1] = h[i];
                A[i, i] = 2.0 * (h[i - 1] + h[i]);

                B[i] = 3.0 * ((a[i + 1] - a[i]) / h[i] - (a[i] - a[i - 1]) / h[i - 1]);
            }

            try
            {
                c = LupSolve(A, B);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        for (int i = 0; i < n; i++)
        {
            b[i] = (a[i + 1] - a[i]) / h[i] - h[i] * (c[i + 1] + 2.0 * c[i]) / 3.0;
            d[i] = (c[i + 1] - c[i]) / (3.0 * h[i]);
        }

        return new(a, b, c, d);
    }

    public static double[] LupSolve(double[,] A, double[] b)
    {
        var n = b.Length;
        var (LU, P) = LupDecomposition(A);
        var x = new double[n];
        var y = new double[n];

        // Forward substitution: Solve Ly = Pb
        for (int i = 0; i < n; i++)
        {
            double sum = 0;
            for (int k = 0; k < i; k++)
            {
                sum += LU[i, k] * y[k];
            }
            // Apply permutation from P. L has ones on the diagonal.
            y[i] = b[P[i]] - sum;
        }

        // Backward substitution: Solve Ux = y
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = 0;
            for (int k = i + 1; k < n; k++)
            {
                sum += LU[i, k] * x[k];
            }
            // Divide by diagonal of U.
            x[i] = (y[i] - sum) / LU[i, i];
        }

        return x;
    }

    private static (double[,] LU, int[] P) LupDecomposition(double[,] A)
    {
        int N = A.GetLength(0);
        double[,] LU = (double[,])A.Clone();
        int[] P = new int[N];

        for (int i = 0; i < N; i++)
        {
            P[i] = i;
        }

        for (int i = 0; i < N; i++)
        {
            FindPivot(LU, P, i, N);

            for (int j = i + 1; j < N; j++)
            {
                LU[j, i] /= LU[i, i];
                for (int k = i + 1; k < N; k++)
                {
                    LU[j, k] -= LU[j, i] * LU[i, k];
                }
            }
        }

        return (LU, P);
    }

    private static void FindPivot(double[,] LU, int[] P, int i, int N)
    {
        double maxValue = 0;
        int pivot = i;

        for (int k = i; k < N; k++)
        {
            double val = Math.Abs(LU[k, i]);
            if (val > maxValue)
            {
                maxValue = val;
                pivot = k;
            }
        }

        if (maxValue == 0)
        {
            throw new ArgumentException("Matrix is singular (invalid input).");
        }

        // Swap P values
        (P[i], P[pivot]) = (P[pivot], P[i]);

        // Swap LU rows
        for (int j = 0; j < N; j++)
        {
            (LU[i, j], LU[pivot, j]) = (LU[pivot, j], LU[i, j]);
        }
    }
}
