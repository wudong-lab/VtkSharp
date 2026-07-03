using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using VtkSharp.Wpf;

namespace VtkSharp.ExampleBrowser.ExtraExamples;

[Example("WPF VTK D3DImage Lifecycle", "WPF",
    Description = "Manual lifecycle stress checks for the WPF OpenGL/D3DImage VTK viewport.",
    SourceFiles = new[] { "ExtraExamples/WpfOpenGLD3DImageLifecycle/WpfOpenGLD3DImageLifecycle.cs" })]
internal sealed class WpfOpenGLD3DImageLifecycle : IExample
{
    public void Run()
    {
        var dispatcher = Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("WPF application dispatcher is not available.");

        dispatcher.Invoke(() =>
        {
            var window = new WpfOpenGLD3DImageLifecycleWindow
            {
                Owner = Application.Current.MainWindow
            };
            window.Show();
        });
    }

    private sealed class WpfOpenGLD3DImageLifecycleWindow : Window
    {
        private readonly Grid _viewportHost = new();
        private readonly TextBlock _statusText = new();
        private readonly TabControl _tabControl = new();
        private readonly DispatcherTimer _hideTimer;
        private readonly DispatcherTimer _resizeTimer;

        private VtkOpenGlD3DImageRenderControl? _viewport;
        private vtkConeSource? _cone;
        private vtkPolyDataMapper? _mapper;
        private vtkActor? _actor;
        private VtkObserverHandle? _timerObserver;
        private int _animationTimerId;
        private int _initializedCount;
        private int _renderFailureCount;
        private int _resizeStep;
        private bool _isViewportLoaded = true;
        private bool _resizeStressEnabled;
        private bool _cacheOnUnload = true;

        public WpfOpenGLD3DImageLifecycleWindow()
        {
            this.Title = "VtkSharp WPF OpenGL D3DImage Lifecycle";
            this.Width = 980;
            this.Height = 700;
            this.MinWidth = 620;
            this.MinHeight = 420;

            this._hideTimer = new DispatcherTimer(DispatcherPriority.Normal, this.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(700)
            };
            this._hideTimer.Tick += this.OnHideTimerTick;

            this._resizeTimer = new DispatcherTimer(DispatcherPriority.Normal, this.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(180)
            };
            this._resizeTimer.Tick += this.OnResizeTimerTick;

            var root = new DockPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(10, 12, 16))
            };

            var toolbar = new WrapPanel
            {
                Margin = new Thickness(10)
            };
            DockPanel.SetDock(toolbar, Dock.Top);
            root.Children.Add(toolbar);

            this.AddButton(toolbar, "Unload/Load", this.ToggleViewportLoaded);
            this.AddButton(toolbar, "Recreate", this.RecreateViewport);
            this.AddButton(toolbar, "Hide/Show", this.HideThenShow);
            this.AddButton(toolbar, "Timer", this.ToggleAnimationTimer);
            this.AddButton(toolbar, "Resize Stress", this.ToggleResizeStress);
            this.AddButton(toolbar, "Switch Tab", this.SwitchTab);

            var cacheCheckBox = new CheckBox
            {
                Content = "Cache VTK on Unload",
                IsChecked = this._cacheOnUnload,
                Margin = new Thickness(8, 4, 8, 8),
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            cacheCheckBox.Checked += (_, _) => this.SetCacheOnUnload(true);
            cacheCheckBox.Unchecked += (_, _) => this.SetCacheOnUnload(false);
            toolbar.Children.Add(cacheCheckBox);

            this._statusText.Margin = new Thickness(12, 0, 12, 10);
            this._statusText.Foreground = Brushes.White;
            DockPanel.SetDock(this._statusText, Dock.Bottom);
            root.Children.Add(this._statusText);

            this._viewportHost.Background = Brushes.Black;
            this._tabControl.Margin = new Thickness(10, 0, 10, 10);
            this._tabControl.Items.Add(new TabItem
            {
                Header = "VTK",
                Content = this._viewportHost
            });
            this._tabControl.Items.Add(new TabItem
            {
                Header = "Other",
                Content = new TextBlock
                {
                    Text = "Switch back to the VTK tab and check whether initialized count changes.",
                    Margin = new Thickness(16),
                    Foreground = Brushes.White
                }
            });
            root.Children.Add(this._tabControl);

            this.Content = root;
            this.Closed += this.OnClosed;

            this.CreateViewport();
            this.UpdateStatus("Ready");
        }

        private void AddButton(Panel panel, string text, Action action)
        {
            var button = new Button
            {
                Content = text,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 6, 12, 6)
            };
            button.Click += (_, _) => action();
            panel.Children.Add(button);
        }

        private void CreateViewport()
        {
            this.DisposeViewportContent();

            this._viewport = new VtkOpenGlD3DImageRenderControl();
            this._viewport.DisposeOnUnload = !this._cacheOnUnload;
            this._viewport.VtkRenderInitialized += this.OnVtkRenderInitialized;
            this._viewport.VtkRenderFailed += this.OnVtkRenderFailed;

            if (this._isViewportLoaded)
            {
                this._viewportHost.Children.Clear();
                this._viewportHost.Children.Add(this._viewport);
            }
        }

        private void ToggleViewportLoaded()
        {
            if (this._viewport is null) return;

            if (this._isViewportLoaded)
            {
                this._viewportHost.Children.Clear();
                this._isViewportLoaded = false;
                this.UpdateStatus("Viewport unloaded from visual tree");
                return;
            }

            this._viewportHost.Children.Add(this._viewport);
            this._isViewportLoaded = true;
            this.UpdateStatus("Viewport loaded into visual tree");
        }

        private void RecreateViewport()
        {
            this._viewportHost.Children.Clear();
            this._isViewportLoaded = true;
            this.CreateViewport();
            this.UpdateStatus("Viewport recreated");
        }

        private void HideThenShow()
        {
            this.Hide();
            this._hideTimer.Start();
        }

        private void OnHideTimerTick(object? sender, EventArgs e)
        {
            this._hideTimer.Stop();
            this.Show();
            this.Activate();
            this.UpdateStatus("Window hidden and shown");
        }

        private void ToggleAnimationTimer()
        {
            if (this._viewport?.RenderWindowInteractor is null)
            {
                this.UpdateStatus("Interactor not initialized");
                return;
            }

            if (this._animationTimerId != 0)
            {
                this.StopAnimationTimer();
                this.UpdateStatus("VTK repeating timer stopped");
                return;
            }

            this._timerObserver = this._viewport.RenderWindowInteractor.AddTimerEventObserver(this.OnTimer);
            this._animationTimerId = this._viewport.RenderWindowInteractor.CreateRepeatingTimer(33);
            this.UpdateStatus("VTK repeating timer started");
        }

        private void ToggleResizeStress()
        {
            this._resizeStressEnabled = !this._resizeStressEnabled;
            if (this._resizeStressEnabled)
            {
                this._resizeTimer.Start();
                this.UpdateStatus("Resize stress started");
            }
            else
            {
                this._resizeTimer.Stop();
                this.UpdateStatus("Resize stress stopped");
            }
        }

        private void OnResizeTimerTick(object? sender, EventArgs e)
        {
            this._resizeStep++;
            var width = 860 + (this._resizeStep % 5) * 28;
            var height = 560 + (this._resizeStep % 4) * 24;
            this.Width = width;
            this.Height = height;
        }

        private void SwitchTab()
        {
            this._tabControl.SelectedIndex = this._tabControl.SelectedIndex == 0 ? 1 : 0;
            this.UpdateStatus("Tab switched");
        }

        private void SetCacheOnUnload(bool cacheOnUnload)
        {
            this._cacheOnUnload = cacheOnUnload;
            if (this._viewport is not null)
            {
                this._viewport.DisposeOnUnload = !cacheOnUnload;
            }

            this.UpdateStatus(cacheOnUnload ? "Unload cache enabled" : "Unload disposal enabled");
        }

        private void OnVtkRenderInitialized(object? sender, VtkRenderInitializedEventArgs e)
        {
            this._initializedCount++;

            this._cone = vtkConeSource.New();
            this._cone.SetHeight(3.0);
            this._cone.SetRadius(1.0);
            this._cone.SetResolution(48);

            this._mapper = vtkPolyDataMapper.New();
            this._mapper.SetInputConnection(this._cone.GetOutputPort());

            this._actor = vtkActor.New();
            this._actor.SetMapper(this._mapper);
            this._actor.GetProperty().SetColor(0.25, 0.72, 1.0);

            e.Renderer.SetBackground(0.08, 0.1, 0.13);
            e.Renderer.AddActor(this._actor);
            e.Renderer.ResetCamera();

            if (sender is VtkOpenGlD3DImageRenderControl control)
            {
                control.Render();
            }

            this.UpdateStatus("VTK initialized");
        }

        private void OnTimer(VtkTimerEventArgs e)
        {
            if (e.TimerId != this._animationTimerId || this._actor is null || this._viewport is null) return;

            this._actor.RotateY(0.8);
            this._viewport.RequestRender();
        }

        private void OnVtkRenderFailed(object? sender, VtkRenderFailedEventArgs e)
        {
            this._renderFailureCount++;
            this.UpdateStatus($"Render failed: {e.Message}");
        }

        private void StopAnimationTimer()
        {
            if (this._animationTimerId != 0)
            {
                this._viewport?.RenderWindowInteractor?.DestroyTimer(this._animationTimerId);
                this._animationTimerId = 0;
            }

            this._timerObserver?.Dispose();
            this._timerObserver = null;
        }

        private void DisposeViewportContent()
        {
            this.StopAnimationTimer();

            if (this._viewport is not null)
            {
                this._viewport.VtkRenderInitialized -= this.OnVtkRenderInitialized;
                this._viewport.VtkRenderFailed -= this.OnVtkRenderFailed;
                this._viewport.Dispose();
                this._viewport = null;
            }

            this._actor?.Dispose();
            this._mapper?.Dispose();
            this._cone?.Dispose();
            this._actor = null;
            this._mapper = null;
            this._cone = null;
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            this._hideTimer.Stop();
            this._resizeTimer.Stop();
            this.DisposeViewportContent();
        }

        private void UpdateStatus(string action)
        {
            this._statusText.Text =
                $"{action} | initialized={this._initializedCount} | renderFailures={this._renderFailureCount} | loaded={this._isViewportLoaded} | cache={this._cacheOnUnload} | timer={(this._animationTimerId != 0 ? "on" : "off")}";
        }
    }
}
