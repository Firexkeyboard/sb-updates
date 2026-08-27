using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RuriLib.Models;

namespace RuriLib.LS;

public static class ExternalScriptRunner
{
    // Runs a script file with the given interpreter and returns stdout.
    public static string RunScript(string command, string commandArgs, string scriptContent,
        string extension, int timeoutMs = 30000)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sb_script_{Guid.NewGuid():N}{extension}");
        try
        {
            File.WriteAllText(tempFile, scriptContent, Encoding.UTF8);

            string args = string.IsNullOrEmpty(commandArgs)
                ? $"\"{tempFile}\""
                : $"{commandArgs} \"{tempFile}\"";

            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi)
                ?? throw new Exception($"Could not start process: {command}");

            var stdoutTask = Task.Run(() => proc.StandardOutput.ReadToEnd());
            var stderrTask = Task.Run(() => proc.StandardError.ReadToEnd());
            bool exited = proc.WaitForExit(timeoutMs);
            if (!exited) { try { proc.Kill(entireProcessTree: true); } catch { } }
            string stdout = stdoutTask.GetAwaiter().GetResult();
            string stderr = stderrTask.GetAwaiter().GetResult();

            if (!string.IsNullOrEmpty(stderr))
                _lastStderr = stderr;

            return stdout;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new Exception(
                $"Failed to run '{command}'. Make sure it is installed and in PATH.\nDetail: {ex.Message}");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // Run a compile-then-execute language (C++, Rust).
    public static string RunCompiledScript(string compiler, string compilerArgs,
        string scriptContent, string sourceExt, int timeoutMs = 30000)
    {
        string tempSrc = Path.Combine(Path.GetTempPath(), $"sb_src_{Guid.NewGuid():N}{sourceExt}");
        string tempExe = Path.ChangeExtension(tempSrc, ".exe");
        try
        {
            File.WriteAllText(tempSrc, scriptContent, Encoding.UTF8);

            // Compilation step
            string compileArgs = compilerArgs
                .Replace("{SRC}", $"\"{tempSrc}\"")
                .Replace("{EXE}", $"\"{tempExe}\"");

            var psiCompile = new ProcessStartInfo
            {
                FileName = compiler,
                Arguments = compileArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var compileProc = Process.Start(psiCompile)
                ?? throw new Exception($"Could not start compiler: {compiler}"))
            {
                var cOutTask = Task.Run(() => compileProc.StandardOutput.ReadToEnd());
                var cErrTask = Task.Run(() => compileProc.StandardError.ReadToEnd());
                bool compileExited = compileProc.WaitForExit(timeoutMs);
                if (!compileExited) { try { compileProc.Kill(entireProcessTree: true); } catch { } }
                _ = cOutTask.GetAwaiter().GetResult();
                string compileErr = cErrTask.GetAwaiter().GetResult();
                if (compileProc.ExitCode != 0)
                    throw new Exception($"Compilation failed:\n{compileErr}");
            }

            // Execution step
            var psiRun = new ProcessStartInfo
            {
                FileName = tempExe,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var runProc = Process.Start(psiRun)
                ?? throw new Exception("Could not start compiled executable");
            var runOutTask = Task.Run(() => runProc.StandardOutput.ReadToEnd());
            var runErrTask = Task.Run(() => runProc.StandardError.ReadToEnd());
            bool runExited = runProc.WaitForExit(timeoutMs);
            if (!runExited) { try { runProc.Kill(entireProcessTree: true); } catch { } }
            string stdout = runOutTask.GetAwaiter().GetResult();
            _lastStderr = runErrTask.GetAwaiter().GetResult();
            return stdout;
        }
        finally
        {
            try { File.Delete(tempSrc); } catch { }
            try { File.Delete(tempExe); } catch { }
        }
    }

    // Parses "__OUTPUT__:{...}" from the process stdout.
    public static Dictionary<string, object> ParseOutput(string stdout)
    {
        var match = Regex.Match(stdout, @"__OUTPUT__:(\{.*\})", RegexOptions.Singleline);
        if (match.Success)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(match.Groups[1].Value)
                    ?? new Dictionary<string, object>();
            }
            catch { }
        }
        return new Dictionary<string, object>();
    }

    public static string LastStderr => _lastStderr ?? "";
    [System.ThreadStatic]
    private static string _lastStderr;

    // ──────────────────────── PYTHON ────────────────────────
    public static string BuildPythonScript(IEnumerable<CVar> variables, string userScript, IEnumerable<string> outputVars)
    {
        var sb = new StringBuilder();
        sb.AppendLine("import json as _json");
        sb.AppendLine("import urllib.request as _urllib");
        sb.AppendLine();
        sb.AppendLine("def request(url, method='GET', body='', headers=None):");
        sb.AppendLine("    data = body.encode() if body else None");
        sb.AppendLine("    req = _urllib.Request(url, data=data, headers=headers or {}, method=method)");
        sb.AppendLine("    try:");
        sb.AppendLine("        with _urllib.urlopen(req) as r: return r.read().decode('utf-8', errors='replace')");
        sb.AppendLine("    except Exception as _e: return str(_e)");
        sb.AppendLine();
        foreach (var v in variables)
        {
            string name = SafeName(v.Name);
            if (v.Type == CVar.VarType.Single)
                sb.AppendLine($"{name} = {JsonConvert.SerializeObject(v.Value?.ToString() ?? "")}");
            else if (v.Type == CVar.VarType.List && v.Value is List<string> lst)
                sb.AppendLine($"{name} = {JsonConvert.SerializeObject(lst)}");
        }
        // Pre-declare output variables with default empty values
        var outList = outputVars.ToList();
        foreach (var outVar in outList)
        {
            string name = SafeName(outVar);
            sb.AppendLine($"if '{name}' not in dir(): {name} = ''");
        }
        sb.AppendLine();
        sb.AppendLine("# === USER SCRIPT ===");
        foreach (string _ln in userScript.Split('\n'))
        {
            string _t = _ln.TrimStart();
            if (_t.StartsWith("// _INPUTS:", StringComparison.OrdinalIgnoreCase) ||
                _t.StartsWith("// _OUTPUTS:", StringComparison.OrdinalIgnoreCase) ||
                _t.StartsWith("//_INPUTS:", StringComparison.OrdinalIgnoreCase) ||
                _t.StartsWith("//_OUTPUTS:", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine("# " + _t.Substring(2));
            else
                sb.AppendLine(_ln);
        }
        sb.AppendLine("# === END SCRIPT ===");
        sb.AppendLine();
        sb.AppendLine("_out = {}");
        foreach (var outVar in outList)
        {
            string name = SafeName(outVar);
            sb.AppendLine($"try:");
            sb.AppendLine($"    _v = {name}");
            sb.AppendLine($"    _out['{outVar}'] = list(_v) if isinstance(_v, (list, tuple)) else str(_v)");
            sb.AppendLine($"except Exception: _out['{outVar}'] = ''");
        }
        sb.AppendLine("print('__OUTPUT__:' + _json.dumps(_out))");
        return sb.ToString();
    }

    // ──────────────────────── NODE.JS ────────────────────────
    public static string BuildNodeScript(IEnumerable<CVar> variables, string userScript, IEnumerable<string> outputVars)
    {
        var sb = new StringBuilder();
        sb.AppendLine("'use strict';");
        sb.AppendLine("const https = require('https'), http = require('http');");
        sb.AppendLine();
        sb.AppendLine("function request(url, method, body, headers) {");
        sb.AppendLine("    return new Promise((resolve, reject) => {");
        sb.AppendLine("        try {");
        sb.AppendLine("            const u = new URL(url);");
        sb.AppendLine("            const lib = u.protocol === 'https:' ? https : http;");
        sb.AppendLine("            const port = u.port || (u.protocol === 'https:' ? 443 : 80);");
        sb.AppendLine("            const opts = { hostname: u.hostname, port, path: u.pathname + (u.search || ''), method: method || 'GET', headers: headers || {} };");
        sb.AppendLine("            const req = lib.request(opts, res => {");
        sb.AppendLine("                let d = ''; res.on('data', c => d += c); res.on('end', () => resolve(d)); res.on('error', reject);");
        sb.AppendLine("            });");
        sb.AppendLine("            req.on('error', reject);");
        sb.AppendLine("            if (body) req.write(body);");
        sb.AppendLine("            req.end();");
        sb.AppendLine("        } catch(e) { reject(e); }");
        sb.AppendLine("    });");
        sb.AppendLine("}");
        sb.AppendLine();
        foreach (var v in variables)
        {
            string name = SafeJsName(v.Name);
            if (v.Type == CVar.VarType.Single)
                sb.AppendLine($"let {name} = {JsonConvert.SerializeObject(v.Value?.ToString() ?? "")};");
            else if (v.Type == CVar.VarType.List && v.Value is List<string> lst)
                sb.AppendLine($"let {name} = {JsonConvert.SerializeObject(lst)};");
        }
        var outList = outputVars.ToList();
        // Only pre-declare output vars that were NOT already injected as `let` above.
        // In 'use strict', mixing let + var for the same name is a SyntaxError.
        var injectedNames = new System.Collections.Generic.HashSet<string>(
            variables.Select(v => SafeJsName(v.Name)), StringComparer.Ordinal);
        foreach (var outVar in outList)
        {
            string jsName = SafeJsName(outVar);
            if (!injectedNames.Contains(jsName))
                sb.AppendLine($"if (typeof {jsName} === 'undefined') var {jsName} = '';");
        }
        sb.AppendLine();
        // _out is declared before try so it survives the catch and is always printed.
        // Output collection runs INSIDE try so block-scoped user variables (const/let) are visible.
        sb.AppendLine("(async () => {");
        sb.AppendLine("const _out = {};");
        sb.AppendLine("try {");
        sb.AppendLine("// === USER SCRIPT ===");
        sb.AppendLine(userScript);
        sb.AppendLine("// === END SCRIPT ===");
        foreach (var outVar in outList)
        {
            string name = SafeJsName(outVar);
            sb.AppendLine($"try {{ _out['{outVar}'] = typeof {name} !== 'undefined' ? {name} : ''; }} catch(e) {{}}");
        }
        sb.AppendLine("} catch(e) { process.stderr.write('SCRIPT ERROR: ' + e.message + '\\n'); }");
        sb.AppendLine("process.stdout.write('__OUTPUT__:' + JSON.stringify(_out) + '\\n');");
        sb.AppendLine("})();");
        return sb.ToString();
    }

    // ──────────────────────── PHP ────────────────────────
    public static string BuildPhpScript(IEnumerable<CVar> variables, string userScript, IEnumerable<string> outputVars)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?php");
        sb.AppendLine("function request($url, $method = 'GET', $body = '', $headers = []) {");
        sb.AppendLine("    if (!function_exists('curl_init')) {");
        sb.AppendLine("        $ctx = stream_context_create(['http' => ['method' => strtoupper($method), 'content' => $body, 'header' => implode(\"\\r\\n\", $headers)]]);");
        sb.AppendLine("        return file_get_contents($url, false, $ctx) ?: '';");
        sb.AppendLine("    }");
        sb.AppendLine("    $ch = curl_init($url);");
        sb.AppendLine("    curl_setopt_array($ch, [CURLOPT_RETURNTRANSFER => true, CURLOPT_CUSTOMREQUEST => strtoupper($method), CURLOPT_SSL_VERIFYPEER => false]);");
        sb.AppendLine("    if ($body) curl_setopt($ch, CURLOPT_POSTFIELDS, $body);");
        sb.AppendLine("    if ($headers) curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);");
        sb.AppendLine("    $r = curl_exec($ch); curl_close($ch); return $r ?: '';");
        sb.AppendLine("}");
        sb.AppendLine();
        foreach (var v in variables)
        {
            string name = SafeName(v.Name);
            if (v.Type == CVar.VarType.Single)
                sb.AppendLine($"${name} = {JsonConvert.SerializeObject(v.Value?.ToString() ?? "")};");
            else if (v.Type == CVar.VarType.List && v.Value is List<string> lst)
                sb.AppendLine($"${name} = {JsonConvert.SerializeObject(lst)};");
        }
        var outList = outputVars.ToList();
        foreach (var outVar in outList)
            sb.AppendLine($"${SafeName(outVar)} = isset(${SafeName(outVar)}) ? ${SafeName(outVar)} : '';");
        sb.AppendLine();
        sb.AppendLine("// === USER SCRIPT ===");
        sb.AppendLine(userScript);
        sb.AppendLine("// === END SCRIPT ===");
        sb.AppendLine();
        sb.AppendLine("$_out = [];");
        foreach (var outVar in outList)
        {
            string name = SafeName(outVar);
            sb.AppendLine($"$_out['{outVar}'] = isset(${name}) ? (is_array(${name}) ? ${name} : strval(${name})) : '';");
        }
        sb.AppendLine("echo '__OUTPUT__:' . json_encode($_out);");
        return sb.ToString();
    }

    // ──────────────────────── RUBY ────────────────────────
    public static string BuildRubyScript(IEnumerable<CVar> variables, string userScript, IEnumerable<string> outputVars)
    {
        var sb = new StringBuilder();
        sb.AppendLine("require 'json'");
        sb.AppendLine("require 'net/http'");
        sb.AppendLine("require 'uri'");
        sb.AppendLine("require 'openssl'");
        sb.AppendLine();
        sb.AppendLine("def request(url, method = 'GET', body = nil, headers = {})");
        sb.AppendLine("  begin");
        sb.AppendLine("    uri = URI.parse(url)");
        sb.AppendLine("    http = Net::HTTP.new(uri.host, uri.port)");
        sb.AppendLine("    http.use_ssl = (uri.scheme == 'https')");
        sb.AppendLine("    http.verify_mode = OpenSSL::SSL::VERIFY_NONE");
        sb.AppendLine("    req_map = { 'GET' => Net::HTTP::Get, 'POST' => Net::HTTP::Post, 'PUT' => Net::HTTP::Put, 'DELETE' => Net::HTTP::Delete, 'PATCH' => Net::HTTP::Patch }");
        sb.AppendLine("    req = (req_map[method.upcase] || Net::HTTP::Get).new(uri.request_uri)");
        sb.AppendLine("    headers.each { |k, v| req[k] = v }");
        sb.AppendLine("    req.body = body if body");
        sb.AppendLine("    http.request(req).body");
        sb.AppendLine("  rescue => e");
        sb.AppendLine("    e.message");
        sb.AppendLine("  end");
        sb.AppendLine("end");
        sb.AppendLine();
        foreach (var v in variables)
        {
            string name = SafeName(v.Name);
            if (v.Type == CVar.VarType.Single)
                sb.AppendLine($"{name} = {JsonConvert.SerializeObject(v.Value?.ToString() ?? "")}");
            else if (v.Type == CVar.VarType.List && v.Value is List<string> lst)
                sb.AppendLine($"{name} = {JsonConvert.SerializeObject(lst)}");
        }
        var outList = outputVars.ToList();
        foreach (var outVar in outList)
            sb.AppendLine($"defined?({SafeName(outVar)}) or {SafeName(outVar)} = ''");
        sb.AppendLine();
        sb.AppendLine("# === USER SCRIPT ===");
        sb.AppendLine(userScript);
        sb.AppendLine("# === END SCRIPT ===");
        sb.AppendLine();
        sb.AppendLine("_out = {}");
        foreach (var outVar in outList)
        {
            string name = SafeName(outVar);
            sb.AppendLine($"begin; _out['{outVar}'] = {name}.is_a?(Array) ? {name} : {name}.to_s; rescue NameError; _out['{outVar}'] = ''; end");
        }
        sb.AppendLine("$stdout.print('__OUTPUT__:' + _out.to_json)");
        return sb.ToString();
    }

    // ──────────────────────── GO ────────────────────────
    public static string BuildGoScript(IEnumerable<CVar> variables, string userScript, IEnumerable<string> outputVars)
    {
        var pkgVars = new StringBuilder();
        foreach (var v in variables)
        {
            string name = SafeGoName(v.Name);
            if (v.Type == CVar.VarType.Single)
                pkgVars.AppendLine($"var {name} = {JsonConvert.SerializeObject(v.Value?.ToString() ?? "")}");
            else if (v.Type == CVar.VarType.List && v.Value is List<string> lst)
            {
                string items = string.Join(", ", lst.Select(x => JsonConvert.SerializeObject(x)));
                pkgVars.AppendLine($"var {name} = []string{{{items}}}");
            }
        }

        var outList = outputVars.ToList();
        var defaultDecls = new StringBuilder();
        foreach (var outVar in outList)
            defaultDecls.AppendLine($"\tvar {SafeGoName(outVar)} = \"\"");

        var outCode = new StringBuilder();
        outCode.AppendLine("\t_output := map[string]interface{}{}");
        foreach (var outVar in outList)
            outCode.AppendLine($"\t_output[\"{outVar}\"] = {SafeGoName(outVar)}");
        outCode.AppendLine("\t_b, _ := json.Marshal(_output)");
        outCode.AppendLine("\tfmt.Println(\"__OUTPUT__:\" + string(_b))");

        return $@"package main

import (
	""crypto/tls""
	""encoding/json""
	""fmt""
	""io""
	""net/http""
	""strings""
)

{pkgVars}
func request(url, method, body string, headers map[string]string) string {{
	tr := &http.Transport{{TLSClientConfig: &tls.Config{{InsecureSkipVerify: true}}}}
	client := &http.Client{{Transport: tr}}
	var bodyReader io.Reader
	if body != """" {{
		bodyReader = strings.NewReader(body)
	}}
	req, err := http.NewRequest(method, url, bodyReader)
	if err != nil {{
		return err.Error()
	}}
	for k, v := range headers {{
		req.Header.Set(k, v)
	}}
	resp, err := client.Do(req)
	if err != nil {{
		return err.Error()
	}}
	defer resp.Body.Close()
	b, _ := io.ReadAll(resp.Body)
	return string(b)
}}

func main() {{
{defaultDecls}
	// === USER SCRIPT ===
{IndentLines(userScript, "\t")}
	// === END SCRIPT ===
{outCode}}}
";
    }

    // ──────────────────────── JAVA (jshell) ────────────────────────
    public static string BuildJavaScript(IEnumerable<CVar> variables, string userScript, IEnumerable<string> outputVars)
    {
        var sb = new StringBuilder();
        sb.AppendLine("import java.net.*;");
        sb.AppendLine("import java.net.http.*;");
        sb.AppendLine("import java.util.*;");
        sb.AppendLine("import java.io.*;");
        sb.AppendLine();
        sb.AppendLine("String request(String url, String method, String body) {");
        sb.AppendLine("    try {");
        sb.AppendLine("        var client = HttpClient.newBuilder().version(HttpClient.Version.HTTP_1_1).build();");
        sb.AppendLine("        var pub = (body != null && !body.isEmpty())");
        sb.AppendLine("            ? HttpRequest.BodyPublishers.ofString(body)");
        sb.AppendLine("            : HttpRequest.BodyPublishers.noBody();");
        sb.AppendLine("        var req = HttpRequest.newBuilder().uri(URI.create(url)).method(method != null ? method : \"GET\", pub).build();");
        sb.AppendLine("        return client.send(req, HttpResponse.BodyHandlers.ofString()).body();");
        sb.AppendLine("    } catch(Exception e) { return e.getMessage(); }");
        sb.AppendLine("}");
        sb.AppendLine();
        var outList = outputVars.ToList();
        foreach (var v in variables)
        {
            string name = SafeJavaName(v.Name);
            if (v.Type == CVar.VarType.Single)
                sb.AppendLine($"String {name} = {JsonConvert.SerializeObject(v.Value?.ToString() ?? "")};");
            else if (v.Type == CVar.VarType.List && v.Value is List<string> lst)
            {
                string items = string.Join(", ", lst.Select(x => JsonConvert.SerializeObject(x)));
                sb.AppendLine($"List<String> {name} = new ArrayList<>(java.util.Arrays.asList({items}));");
            }
        }
        foreach (var outVar in outList)
        {
            string name = SafeJavaName(outVar);
            sb.AppendLine($"String {name} = \"\";");
        }
        sb.AppendLine();
        sb.AppendLine("// === USER SCRIPT ===");
        sb.AppendLine(userScript);
        sb.AppendLine("// === END SCRIPT ===");
        sb.AppendLine();
        var parts = outList.Select(v =>
        {
            string n = SafeJavaName(v);
            return $"\"\\\"{v}\\\":\\\"\" + {n}.replace(\"\\\\\", \"\\\\\\\\\").replace(\"\\\"\", \"\\\\\\\"\").replace(\"\\n\", \"\\\\n\").replace(\"\\r\", \"\\\\r\").replace(\"\\t\", \"\\\\t\") + \"\\\"\"";
        }).ToList();
        string joined = parts.Count > 0 ? string.Join(" + \",\" + ", parts) : "\"\"";
        sb.AppendLine($"System.out.println(\"__OUTPUT__:{{\" + {joined} + \"}}\");");
        sb.AppendLine("/exit");
        return sb.ToString();
    }

    // ──────────────────────── C++ ────────────────────────
    public static string BuildCppScript(IEnumerable<CVar> variables, string userScript, IEnumerable<string> outputVars)
    {
        var varDecls = new StringBuilder();
        foreach (var v in variables)
        {
            string name = SafeName(v.Name);
            if (v.Type == CVar.VarType.Single)
                varDecls.AppendLine($"    std::string {name} = {JsonConvert.SerializeObject(v.Value?.ToString() ?? "")};");
        }
        var outList = outputVars.ToList();
        foreach (var outVar in outList)
            varDecls.AppendLine($"    std::string {SafeName(outVar)} = \"\";");

        var outCode = new StringBuilder();
        outCode.AppendLine("    std::string _json_out = \"{\";");
        bool first = true;
        foreach (var outVar in outList)
        {
            string name = SafeName(outVar);
            if (!first) outCode.AppendLine("    _json_out += \",\";");
            first = false;
            outCode.AppendLine($"    _json_out += \"\\\"\" + std::string(\"{outVar}\") + \"\\\":\\\"\";");
            outCode.AppendLine($"    for(char _c : {name}) {{");
            outCode.AppendLine("        if(_c == '\"') _json_out += \"\\\\\\\"\";");
            outCode.AppendLine("        else if(_c == '\\\\') _json_out += \"\\\\\\\\\";");
            outCode.AppendLine("        else if(_c == '\\n') _json_out += \"\\\\n\";");
            outCode.AppendLine("        else if(_c == '\\r') _json_out += \"\\\\r\";");
            outCode.AppendLine("        else if(_c == '\\t') _json_out += \"\\\\t\";");
            outCode.AppendLine("        else _json_out += _c;");
            outCode.AppendLine("    }");
            outCode.AppendLine("    _json_out += \"\\\"\";");
        }
        outCode.AppendLine("    _json_out += \"}\";");
        outCode.AppendLine("    std::cout << \"__OUTPUT__:\" << _json_out << std::endl;");

        return $@"#include <iostream>
#include <string>
#include <vector>
#include <sstream>
#include <map>

// HTTP request helper (requires libcurl: compile with -lcurl)
std::string request(const std::string& url, const std::string& method = ""GET"", const std::string& body = """") {{
    return """"; // Link with libcurl for real HTTP support
}}

int main() {{
{varDecls}
    // === USER SCRIPT ===
{IndentLines(userScript, "    ")}
    // === END SCRIPT ===
{outCode}
    return 0;
}}
";
    }

    // ──────────────────────── RUST ────────────────────────
    public static string BuildRustScript(IEnumerable<CVar> variables, string userScript, IEnumerable<string> outputVars)
    {
        var varDecls = new StringBuilder();
        foreach (var v in variables)
        {
            string name = SafeName(v.Name);
            if (v.Type == CVar.VarType.Single)
            {
                string escaped = (v.Value?.ToString() ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
                varDecls.AppendLine($"    let {name} = \"{escaped}\".to_string();");
            }
        }
        var outList = outputVars.ToList();
        foreach (var outVar in outList)
            varDecls.AppendLine($"    let mut {SafeName(outVar)} = String::new();");

        var outCode = new StringBuilder();
        outCode.AppendLine("    let _pairs: Vec<String> = vec![");
        foreach (var outVar in outList)
        {
            string name = SafeName(outVar);
            outCode.AppendLine($"        format!(\"\\\"{{}}\\\":\\\"{{}}\\\"\", \"{outVar}\", {name}.replace('\\\\', \"\\\\\\\\\").replace('\"', \"\\\\\\\"\").replace('\\n', \"\\\\n\").replace('\\r', \"\\\\r\").replace('\\t', \"\\\\t\")),");
        }
        outCode.AppendLine("    ];");
        outCode.AppendLine("    println!(\"__OUTPUT__:{{{}}}\", _pairs.join(\",\"));");

        return $@"fn request(url: &str, _method: &str, _body: &str) -> String {{
    // Add reqwest = ""0.12"" to Cargo.toml for real HTTP support
    String::new()
}}

fn main() {{
{varDecls}
    // === USER SCRIPT ===
{IndentLines(userScript, "    ")}
    // === END SCRIPT ===
{outCode}}}
";
    }

    // ──────────────────────── HELPERS ────────────────────────
    private static string IndentLines(string code, string indent)
    {
        if (string.IsNullOrEmpty(code)) return "";
        return string.Join(Environment.NewLine,
            code.Split('\n').Select(l => indent + l.TrimEnd('\r')));
    }

    public static string SafeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "_var";
        string s = Regex.Replace(name, @"[^\w]", "_");
        return char.IsDigit(s[0]) ? "_" + s : s;
    }

    public static string SafeJsName(string name) => SafeName(name);
    public static string SafeJavaName(string name) => SafeName(name);

    public static string SafeGoName(string name)
    {
        string s = SafeName(name);
        return char.IsUpper(s[0]) ? char.ToLower(s[0]) + s.Substring(1) : s;
    }
}
