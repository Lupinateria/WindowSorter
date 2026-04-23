using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using WindowSorter.Core;
using Brushes = System.Windows.Media.Brushes;

namespace WindowSorter.View;

public class DwmThumbnailView : FrameworkElement {
    private IntPtr _hThumbnail = IntPtr.Zero;
    private IntPtr _destHandle = IntPtr.Zero;
    private NativeMethods.RECT _lastRect;
    private bool _lastVisible = false;

    // サムネイルを表示するかフラグ
    private static bool _isThumbnailReady = false;
    public static bool IsThumbnailReady {
        get => _isThumbnailReady;
        set {
            if (_isThumbnailReady != value) {
                _isThumbnailReady = value;
                IsThumbnailReadyChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }
    public static event EventHandler? IsThumbnailReadyChanged;

    // 表示するhwnd
    public static readonly DependencyProperty SourceHandleProperty =
        DependencyProperty.Register(nameof(SourceHandle), typeof(IntPtr), typeof(DwmThumbnailView),
            new PropertyMetadata(IntPtr.Zero, OnSourceHandleChanged));
    public IntPtr SourceHandle {
        get => (IntPtr)GetValue(SourceHandleProperty);
        set => SetValue(SourceHandleProperty, value);
    }

    // クリッピングするXAML要素
    public static readonly DependencyProperty ClippingElementProperty =
    DependencyProperty.Register(nameof(ClippingElement), typeof(FrameworkElement), typeof(DwmThumbnailView),
        new PropertyMetadata(null));

    public FrameworkElement ClippingElement {
        get => (FrameworkElement)GetValue(ClippingElementProperty);
        set => SetValue(ClippingElementProperty, value);
    }

    public DwmThumbnailView() {
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
        this.IsVisibleChanged += (s, e) => UpdateThumbnailStatus();
        IsThumbnailReadyChanged += OnIsThumbnailReadyChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        var window = Window.GetWindow(this);
        if (window != null) {
            _destHandle = new WindowInteropHelper(window).Handle;
            UpdateThumbnailStatus();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        IsThumbnailReadyChanged -= OnIsThumbnailReadyChanged;
        StopRendering();
        UnregisterThumbnail();
    }

    private void OnIsThumbnailReadyChanged(object? sender, EventArgs e) {
        UpdateThumbnailStatus();
    }

    private static void OnSourceHandleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is DwmThumbnailView view) {
            view.UpdateThumbnailStatus();
        }
    }

    private void UpdateThumbnailStatus() {
        bool shouldBeActive = IsThumbnailReady && this.IsVisible && SourceHandle != IntPtr.Zero && _destHandle != IntPtr.Zero;

        if (shouldBeActive) {
            if (_hThumbnail == IntPtr.Zero) {
                RegisterThumbnail();
            }
            StartRendering();
        } else {
            StopRendering();
            UnregisterThumbnail();
        }
    }

    private void StartRendering() {
        CompositionTarget.Rendering -= OnRendering;
        CompositionTarget.Rendering += OnRendering;
    }

    private void StopRendering() {
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e) {
        UpdateThumbnailPosition();
    }

    private void RegisterThumbnail() {
        if (_destHandle == IntPtr.Zero || SourceHandle == IntPtr.Zero || _hThumbnail != IntPtr.Zero) return;

        int hr = NativeMethods.DwmRegisterThumbnail(_destHandle, SourceHandle, out _hThumbnail);
        if (hr != 0) {
            Debug.WriteLine($"DwmRegisterThumbnail failed. HR: {hr:X}");
            return;
        }
        _lastRect = default;
        _lastVisible = false;
        UpdateThumbnailPosition();
    }

    private void UnregisterThumbnail() {
        if (_hThumbnail != IntPtr.Zero) {
            NativeMethods.DwmUnregisterThumbnail(_hThumbnail);
            _hThumbnail = IntPtr.Zero;
        }
    }

    private void UpdateThumbnailPosition() {
        if (_hThumbnail == IntPtr.Zero) return;

        bool currentVisible = IsThumbnailReady && this.IsVisible && this.ActualWidth > 0 && this.ActualHeight > 0;

        try {
            var window = Window.GetWindow(this);
            if (window == null) return;

            var transform = this.TransformToAncestor(window);
            var fullRect = transform.TransformBounds(new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            var rect = fullRect;

            var clippingElement = ClippingElement;
            if (clippingElement != null) {
                var clipRect = clippingElement.TransformToAncestor(window).TransformBounds(new Rect(0, 0, clippingElement.ActualWidth, clippingElement.ActualHeight));

                var intersected = Rect.Intersect(clipRect, fullRect);

                if (intersected.IsEmpty) {
                    currentVisible = false;
                } else {
                    rect = intersected;
                }
            }

            var win32Rect = new NativeMethods.RECT(
                (int)rect.Left,
                (int)rect.Top,
                (int)rect.Right,
                (int)rect.Bottom);

            // 状態や座標が変わっていないなら API を呼ばない
            if (currentVisible == _lastVisible &&
                win32Rect.Left == _lastRect.Left &&
                win32Rect.Top == _lastRect.Top &&
                win32Rect.Right == _lastRect.Right &&
                win32Rect.Bottom == _lastRect.Bottom) {
                return;
            }

            NativeMethods.DWM_THUMBNAIL_PROPERTIES props = new NativeMethods.DWM_THUMBNAIL_PROPERTIES {
                dwFlags = NativeMethods.DWM_TNP_VISIBLE | NativeMethods.DWM_TNP_RECTDESTINATION | NativeMethods.DWM_TNP_OPACITY,
                fVisible = currentVisible ? 1 : 0,
                rcDestination = win32Rect,
                opacity = 255,
                fSourceClientAreaOnly = 0
            };

            if (NativeMethods.DwmQueryThumbnailSourceSize(_hThumbnail, out NativeMethods.SIZE size) == 0) {
                double leftRatio = Math.Max(0, (rect.Left - fullRect.Left) / fullRect.Width);
                double topRatio = Math.Max(0, (rect.Top - fullRect.Top) / fullRect.Height);
                double rightRatio = Math.Max(0, (fullRect.Right - rect.Right) / fullRect.Width);
                double bottomRatio = Math.Max(0, (fullRect.Bottom - rect.Bottom) / fullRect.Height);

                props.dwFlags |= NativeMethods.DWM_TNP_RECTSOURCE;
                props.rcSource = new NativeMethods.RECT(
                    (int)(size.cx * leftRatio),
                    (int)(size.cy * topRatio),
                    (int)(size.cx * (1.0 - rightRatio)),
                    (int)(size.cy * (1.0 - bottomRatio))
                );
            }

            NativeMethods.DwmUpdateThumbnailProperties(_hThumbnail, ref props);

            _lastRect = win32Rect;
            _lastVisible = currentVisible;
        } catch {

        }
    }

    protected override void OnRender(DrawingContext drawingContext) {
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
    }
}