using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OpenBullet;

public class App : Application
{
    private bool _contentLoaded;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
        {
            OnUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
        };
        base.Dispatcher.UnhandledException += delegate(object s, DispatcherUnhandledExceptionEventArgs e)
        {
            OnUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
        };
        Application.Current.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        TaskScheduler.UnobservedTaskException += delegate(object s, UnobservedTaskExceptionEventArgs e)
        {
            OnUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        };
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        OnUnhandledException(e.Exception, "Dispatcher_UnhandledException");
    }

    public void OnUnhandledException(Exception ex, string @event)
    {
        File.AppendAllText(SB.logFile, $"[FATAL][{@event}] UHANDLED EXCEPTION{Environment.NewLine}{ex}");
    }

    public Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        AssemblyName requested = new AssemblyName(args.Name);
        string appDir = Path.GetDirectoryName(typeof(App).Assembly.Location)
                        ?? AppDomain.CurrentDomain.BaseDirectory;

        // For any assembly not found through normal probing, try loading it directly
        // from the application directory using LoadFrom (bypasses strict version/PKT matching).
        // This handles legacy CLR 2.0 assemblies (e.g. WPFToolkit 3.5) embedded in BAML.
        string localPath = Path.Combine(appDir, requested.Name + ".dll");
        if (File.Exists(localPath))
        {
            try { return Assembly.LoadFrom(localPath); }
            catch { }
        }

        return null;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    public void InitializeComponent()
    {
        if (!_contentLoaded)
        {
            _contentLoaded = true;
            base.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
            Uri resourceLocator = new Uri("/SilverBullet;component/app.xaml", UriKind.Relative);
            Application.LoadComponent(this, resourceLocator);
        }
    }

    [STAThread]
    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    public static void Main()
    {
        // Bug 5 fix: pre-warm the ThreadPool so LoliCode's blocking Roslyn calls don't starve each other.
        // With the default minimum (ProcessorCount), the pool injects only 1 thread/500ms when exhausted.
        // Pre-setting 200 workers means up to 200 bots can run LoliCode concurrently without queuing delays.
        ThreadPool.GetMinThreads(out int workerThreads, out int ioThreads);
        ThreadPool.SetMinThreads(Math.Max(workerThreads, 200), ioThreads);

        // .NET 8 does not automatically set CWD to the exe directory; all relative paths depend on this.
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        App app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
