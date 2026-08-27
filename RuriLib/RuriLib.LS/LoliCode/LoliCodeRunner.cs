using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Media;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using RuriLib.Models;

namespace RuriLib.LS.LoliCode;

/// <summary>
/// Entry point for LoliCode execution. Parses, compiles and runs a LoliCode script
/// against a BotData using Roslyn C# scripting.
/// </summary>
public static class LoliCodeRunner
{
    /// <summary>Returns true if the given script text is in LoliCode format.</summary>
    public static bool IsLoliCode(string script) => LoliCodeParser.IsLoliCode(script);

    /// <summary>
    /// Runs a LoliCode script. Logs any compilation/runtime errors to data.LogBuffer.
    /// </summary>
    public static void Run(string loliCode, BotData data)
    {
        // Step 1 — Parse
        List<LoliCodeSegment> segments;
        try
        {
            segments = LoliCodeParser.Parse(loliCode);
        }
        catch (Exception ex)
        {
            data.Log(new LogEntry($"[LoliCode] Parse error: {ex.Message}", Colors.Tomato));
            return;
        }

        // Step 2 — Compile to C#
        string csharpScript;
        try
        {
            csharpScript = LoliCodeCompiler.Compile(segments);
        }
        catch (Exception ex)
        {
            data.Log(new LogEntry($"[LoliCode] Compile error: {ex.Message}", Colors.Tomato));
            return;
        }

        // Step 3 — Execute with Roslyn
        try
        {
            ExecuteRoslyn(csharpScript, data);
        }
        catch (Exception ex)
        {
            if (ex is Microsoft.CodeAnalysis.Scripting.CompilationErrorException cee)
            {
                var scriptLines = csharpScript.Split('\n');
                var errors = cee.Diagnostics
                    .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                    .ToList();

                // Dump the full compiled script to a temp file so the error can be diagnosed.
                try
                {
                    string dumpPath = System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(), "SilverBullet_lolicode_debug.cs");
                    System.IO.File.WriteAllText(dumpPath, csharpScript);
                    data.Log(new LogEntry($"[LoliCode] Compiled script dumped to: {dumpPath}", Colors.Orange));
                }
                catch { /* non-critical */ }

                var sb = new StringBuilder();
                sb.AppendLine($"[LoliCode] {errors.Count} compilation error{(errors.Count == 1 ? "" : "s")}:");

                foreach (var diag in errors)
                {
                    var span    = diag.Location.GetLineSpan();
                    int lineIdx = span.StartLinePosition.Line;

                    sb.AppendLine();
                    sb.AppendLine($"  ► {diag.Id}  (line {lineIdx + 1} of compiled script)");
                    sb.AppendLine($"    {diag.GetMessage()}");

                    if (lineIdx >= 0 && lineIdx < scriptLines.Length)
                    {
                        sb.AppendLine();
                        int from = Math.Max(0, lineIdx - 15);
                        int to   = Math.Min(scriptLines.Length - 1, lineIdx + 5);
                        for (int i = from; i <= to; i++)
                        {
                            string marker = (i == lineIdx) ? ">>>" : "   ";
                            sb.AppendLine($"    {i + 1,4}  {marker}  {scriptLines[i].TrimEnd()}");
                        }
                    }
                }

                data.Log(new LogEntry(sb.ToString().TrimEnd(), Colors.Tomato));
            }
            else
            {
                string typeName = ex.GetType().Name;
                data.Log(new LogEntry(
                    $"[LoliCode] Runtime error ({typeName}):\n  {ex.Message}",
                    Colors.Tomato));
            }
        }
    }

    /// <summary>
    /// Like Run() but returns the ScriptState so that subsequent inline C# blocks can
    /// call RunContinuation() — variables declared in this block remain in scope.
    /// Throws on parse/compile/runtime errors (caller must catch).
    /// </summary>
    public static (Microsoft.CodeAnalysis.Scripting.ScriptState<object> state, LoliCodeData loliData) RunFresh(string loliCode, BotData data)
    {
        var segments     = LoliCodeParser.Parse(loliCode);
        string csScript  = LoliCodeCompiler.Compile(segments);
        var loliData     = new LoliCodeData(data);
        var globals      = new LoliCodeGlobals { data = loliData, input = loliData };
        var state        = CSharpScript
            .RunAsync(csScript, _cachedOptions.Value, globals, typeof(LoliCodeGlobals))
            .GetAwaiter().GetResult();
        return (state, loliData);
    }

    /// <summary>
    /// Continues a previous ScriptState with new code. All variables from prior blocks
    /// remain in scope. The Preamble is stripped before continuation to avoid re-declaration.
    /// Throws on parse/compile/runtime errors (caller must catch).
    /// </summary>
    public static Microsoft.CodeAnalysis.Scripting.ScriptState<object> RunContinuation(
        string loliCode,
        Microsoft.CodeAnalysis.Scripting.ScriptState<object> prevState)
    {
        var segments    = LoliCodeParser.Parse(loliCode);
        string csScript = LoliCodeCompiler.Compile(segments);
        // Strip the preamble — __rv / log / print / LOG / input are already in scope.
        string body     = csScript.Replace(LoliCodeCompiler.Preamble, "");
        return prevState.ContinueWithAsync<object>(body).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Same as Run but also returns the generated C# script string (useful for the debugger tab).
    /// </summary>
    public static string GetCompiledScript(string loliCode)
    {
        var segments = LoliCodeParser.Parse(loliCode);
        return LoliCodeCompiler.Compile(segments);
    }

    /// <summary>
    /// Warms up the Roslyn scripting engine on a background thread so the first real
    /// LoliCode execution does not pay the 3-5 second JIT cold-start penalty.
    /// Call this once at application startup.
    /// </summary>
    public static void WarmUp()
    {
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                CSharpScript
                    .RunAsync("1+1;", _cachedOptions.Value)
                    .GetAwaiter()
                    .GetResult();
            }
            catch { /* warm-up errors are intentionally swallowed */ }
        });
    }

    // ─── Roslyn execution ─────────────────────────────────────────────────────

    private static readonly Lazy<Microsoft.CodeAnalysis.Scripting.ScriptOptions> _cachedOptions =
        new Lazy<Microsoft.CodeAnalysis.Scripting.ScriptOptions>(() =>
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a =>
                {
                    try { return !a.IsDynamic && !string.IsNullOrEmpty(a.Location); }
                    catch { return false; }
                })
                .Concat(new Assembly[]
                {
                    // Force-include these assemblies even if not yet loaded in the AppDomain.
                    // Required for OB2 configs that use X509Certificate2, EnvelopedCms, GZipStream, etc.
                    typeof(X509Certificate2).Assembly,
                    typeof(EnvelopedCms).Assembly,
                    typeof(GZipStream).Assembly,
                })
                .Distinct()
                .ToArray();

            return Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
                .WithReferences(assemblies)
                .WithImports(
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Net.Http",
                    "System.Text",
                    "System.Text.RegularExpressions",
                    "System.Threading",
                    "System.Threading.Tasks",
                    "System.Security.Cryptography",
                    "System.Security.Cryptography.X509Certificates",
                    "System.Security.Cryptography.Pkcs",
                    "System.Numerics",
                    "System.IO",
                    "System.IO.Compression",
                    "System.Data",
                    "Newtonsoft.Json",
                    "Newtonsoft.Json.Linq",
                    "RuriLib",
                    "RuriLib.Models",
                    "RuriLib.LS",
                    "RuriLib.LS.LoliCode"
                );
        });

    private static void ExecuteRoslyn(string script, BotData data)
    {
        var loliData = new LoliCodeData(data);
        var globals = new LoliCodeGlobals { data = loliData, input = loliData };

        CSharpScript
            .RunAsync(script, _cachedOptions.Value, globals, typeof(LoliCodeGlobals))
            .GetAwaiter()
            .GetResult();
    }
}
