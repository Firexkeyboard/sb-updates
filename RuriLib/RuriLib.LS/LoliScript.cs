using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;
using IronPython.Compiler;
using IronPython.Hosting;
using IronPython.Runtime;
using Jint;
using Jint.Native;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Scripting.Hosting;
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;
using RuriLib.Functions.Conditions;
using RuriLib.LS.LoliCode;
using RuriLib.Models;

namespace RuriLib.LS;

public class LoliScript
{
	private int i;

	private string[] lines = new string[0];

	// Globals object passed to Roslyn so scripts can use `data` directly
	// (e.g. data.Data.Username, data.Variables, data.ResponseSource).
	// _vars holds CVar values so the script text stays constant across bots → Roslyn compilation cache hits.
	public sealed class CSharpScriptGlobals
	{
		public BotData data;
		public Dictionary<string, object> _vars = new Dictionary<string, object>(StringComparer.Ordinal);
	}

	private static readonly Lazy<Microsoft.CodeAnalysis.Scripting.ScriptOptions> _csScriptOptions =
		new Lazy<Microsoft.CodeAnalysis.Scripting.ScriptOptions>(() =>
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies()
				.Where(a => { try { return !a.IsDynamic && !string.IsNullOrEmpty(a.Location); } catch { return false; } })
				.Concat(new Assembly[]
				{
					typeof(X509Certificate2).Assembly,
					typeof(EnvelopedCms).Assembly,
					typeof(GZipStream).Assembly,
				})
				.Distinct();
			return Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
				.WithReferences(assemblies)
				.WithImports("System", "System.Net.Http", "System.Text",
							"System.Text.RegularExpressions", "System.Collections.Generic",
							"System.Linq", "Newtonsoft.Json", "System.Threading.Tasks", "System.IO",
							"System.IO.Compression", "System.Security.Cryptography",
							"System.Security.Cryptography.X509Certificates",
							"System.Security.Cryptography.Pkcs");
		});

	// Per-thread IronPython engine: each bot thread gets its own engine + runtime so scripts
	// run in PARALLEL with no lock. A single shared engine + global lock was forcing all bots
	// to execute IronPython sequentially (1 at a time regardless of bot count).
	[System.ThreadStatic]
	private static ScriptEngine _ironPythonEnginePerThread;
	// _ironPythonLock kept only for the old shared engine path (no longer used; can be removed later)
	private static readonly object _ironPythonLock = new object();

	private string otherScript = "";

	private ScriptingLanguage language;

	private string jsFilePath;

	private string jsEngine;

	// TRY/CATCH state
	private bool inTryBlock = false;
	private bool tryErrorOccurred = false;
	private string tryErrorMessage = "";

	// Inline PYTHON / IRONPYTHON block state
	private bool inPythonBlock = false;
	private bool inIronPythonBlock = false;
	private readonly List<string> pythonBuf = new List<string>();

	// FOREACH state: clave = índice de línea del FOREACH en el array lines[]
	private readonly Dictionary<int, List<string>> foreachLists = new Dictionary<int, List<string>>();
	private readonly Dictionary<int, int> foreachCounters = new Dictionary<int, int>();

	// Persistent Roslyn ScriptState: variables declared in one inline C# block survive
	// across interleaved REQUEST/PARSE/FUNCTION lines and into the next C# block.
	// Without this, each { } block is a fresh Roslyn execution and all C# locals vanish.
	private Microsoft.CodeAnalysis.Scripting.ScriptState<object> _csState = null;
	private LoliCodeData _csLoliData = null;
	private readonly HashSet<string> _csDeclaredVars = new HashSet<string>(StringComparer.Ordinal);

	public string Script { get; set; }

	private string[] CompressedLines
	{
		get
		{
			int num = 0;
			bool flag = false;
			List<string> list = Script.Split(new string[2]
			{
				Environment.NewLine,
				"\n"
			}, StringSplitOptions.None).ToList();
			while (num < list.Count - 1)
			{
				// KEYCHECK continuation: KEYCHAIN and KEY lines may appear non-indented.
				// Join them into the KEYCHECK line regardless of leading whitespace.
				if (!flag && BlockParser.IsBlock(list[num]) &&
				    BlockParser.GetBlockType(list[num]).Equals("KEYCHECK", StringComparison.OrdinalIgnoreCase))
				{
					string nextT = list[num + 1].TrimStart();
					if (nextT.StartsWith("KEYCHAIN ", StringComparison.OrdinalIgnoreCase) ||
					    nextT.StartsWith("KEY ",      StringComparison.OrdinalIgnoreCase) ||
					    nextT.StartsWith("! KEYCHAIN ", StringComparison.OrdinalIgnoreCase) ||
					    nextT.StartsWith("! KEY ",      StringComparison.OrdinalIgnoreCase) ||
					    list[num + 1].StartsWith(" ") || list[num + 1].StartsWith("\t"))
					{
						list[num] = list[num] + " " + nextT;
						list.RemoveAt(num + 1);
						continue;
					}
				}
				if (!flag && BlockParser.IsBlock(list[num]) && (list[num + 1].StartsWith(" ") || list[num + 1].StartsWith("\t")))
				{
					List<string> list2 = list;
					int index = num;
					list2[index] = list2[index] + " " + list[num + 1].Trim();
					list.RemoveAt(num + 1);
					continue;
				}
				if (!flag && BlockParser.IsBlock(list[num]) && (list[num + 1].StartsWith("! ") || list[num + 1].StartsWith("!\t")))
				{
					List<string> list2 = list;
					int index = num;
					list2[index] = list2[index] + " " + list[num + 1].Substring(1).Trim();
					list.RemoveAt(num + 1);
					continue;
				}
				if (list[num].StartsWith("BEGIN SCRIPT"))
				{
					flag = true;
				}
				else if (list[num].StartsWith("END SCRIPT"))
				{
					flag = false;
				}
				num++;
			}
			return list.ToArray();
		}
	}

	public string CurrentLine { get; set; } = "";

	public int Line { get; set; }

	public string NextBlock
	{
		get
		{
			for (int i = this.i; i < lines.Count(); i++)
			{
				string input = lines[i];
				if (!IsEmptyOrCommentOrDisabled(input) && BlockParser.IsBlock(input))
				{
					string text = "";
					if (lines[i].StartsWith("#"))
					{
						text = LineParser.ParseLabel(ref input);
					}
					string text2 = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false, proceed: false);
					if (text != string.Empty)
					{
						return text2 + " (" + text + ")";
					}
					return text2;
				}
			}
			return "";
		}
	}

	public string CurrentBlock { get; set; } = "";

	public bool CanProceed
	{
		get
		{
			for (int j = i; j < lines.Length; j++)
				if (!IsEmptyOrCommentOrDisabled(lines[j])) return true;
			return false;
		}
	}

	public LoliScript()
	{
		Script = "";
	}

	public LoliScript(string script)
	{
		Script = script;
	}

	public List<BlockBase> ToBlocks()
	{
		// LoliCode mode: parse segments and convert each to a visual Stacker block
		if (LoliCodeParser.IsLoliCode(Script))
		{
			var segments = LoliCodeParser.Parse(Script);
			return LoliCodeSerializer.SegmentsToBlocks(segments);
		}

		List<BlockBase> list = new List<BlockBase>();
		string[] compressedLines = CompressedLines;
		List<string> list2 = new List<string>();
		bool flag = false;
		foreach (string item in compressedLines.Where((string c) => !string.IsNullOrEmpty(c.Trim())))
		{
			if (!flag && BlockParser.IsBlock(item))
			{
				if (list2.Count > 0)
				{
					BlockLSCode blockLSCode = new BlockLSCode();
					list.Add(blockLSCode.FromLS(list2));
					list2.Clear();
				}
				try
				{
					list.Add(BlockParser.Parse(item));
				}
				catch (Exception ex)
				{
					string input = item;
					input = BlockBase.TruncatePretty(input, 50);
					throw new Exception("Exception while parsing block " + input + "\nReason: " + ex.Message);
				}
			}
			else
			{
				list2.Add(item);
				if (item.StartsWith("BEGIN SCRIPT"))
				{
					flag = true;
				}
				else if (item.StartsWith("END SCRIPT"))
				{
					flag = false;
				}
			}
		}
		if (list2.Count > 0)
		{
			BlockLSCode blockLSCode2 = new BlockLSCode();
			list.Add(blockLSCode2.FromLS(list2));
			list2.Clear();
		}
		return list;
	}

	public void FromBlocks(List<BlockBase> blocks)
	{
		// Use LoliCode serialization only if the stored script was explicitly LoliCode
		if (LoliCodeParser.IsLoliCode(Script))
		{
			Script = LoliCodeSerializer.BlocksToLoliCode(blocks);
			return;
		}

		var _sb = new System.Text.StringBuilder();
		foreach (BlockBase block in blocks)
			_sb.Append(block.ToLS()).Append(Environment.NewLine).Append(Environment.NewLine);
		Script = _sb.ToString();
	}

	public void Reset()
	{
		i = 0;
		otherScript = "";
		language = ScriptingLanguage.JavaScript;
		lines = Regex.Split(Script, "\r\n|\r|\n");
		inTryBlock = false;
		tryErrorOccurred = false;
		tryErrorMessage = "";
		foreachLists.Clear();
		foreachCounters.Clear();
		inPythonBlock = false;
		inIronPythonBlock = false;
		pythonBuf.Clear();
		_csState = null;
		_csLoliData = null;
		_csDeclaredVars.Clear();
	}

	public void TakeStep(BotData data)
	{
		data.LogBuffer.Clear();
		if (this.i == 0) { _csState = null; _csLoliData = null; _csDeclaredVars.Clear(); }

		// LoliCode mode: run the entire script on the first call, then signal done
		if (this.i == 0 && LoliCodeParser.IsLoliCode(Script))
		{
			LoliCodeRunner.Run(Script, data);
			this.i = lines.Count();
			return;
		}

		// Mixed mode: LoliScript script that contains raw C# code blocks.
		// Pure-LoliScript parsers can't handle C# → convert to LoliCode and run at once.
		// Detection: any BlockLSCode whose Script is itself "LoliCode-like" (i.e. not all comments)
		// means it contains real C# code that LoliScript would choke on.
		if (this.i == 0)
		{
			var mixedBlocks = ToBlocks();
			bool hasCSharpBlocks = mixedBlocks.Any(b =>
				b is BlockLSCode lsc &&
				lsc.Script != null &&
				lsc.Script.Split('\n').Any(l => l.TrimStart().StartsWith("BLOCK:", StringComparison.Ordinal)));
			if (hasCSharpBlocks)
			{
				string loliCode = LoliCodeSerializer.BlocksToLoliCode(mixedBlocks);
				LoliCodeRunner.Run(loliCode, data);
				data.Flush(); // expose any errors from Run() before returning
				this.i = lines.Count();
				return;
			}
		}

		if (inPythonBlock || inIronPythonBlock)
		{
			while (this.i < lines.Count() && IsEmptyOrCommentOrDisabled(lines[this.i]))
				this.i++;
			if (this.i >= lines.Count())
			{
				inPythonBlock = false; inIronPythonBlock = false; pythonBuf.Clear();
				return;
			}
			string pyRaw     = lines[this.i];
			string pyTrimmed = pyRaw.Trim();
			bool isEndPy  = pyTrimmed.Equals("ENDPYTHON",    StringComparison.OrdinalIgnoreCase)
			             || pyTrimmed.StartsWith("ENDPYTHON ",    StringComparison.OrdinalIgnoreCase);
			bool isEndIPy = pyTrimmed.Equals("ENDIRONPYTHON", StringComparison.OrdinalIgnoreCase)
			             || pyTrimmed.StartsWith("ENDIRONPYTHON ", StringComparison.OrdinalIgnoreCase);
			if (isEndPy || isEndIPy)
			{
				string rest = isEndIPy
					? pyTrimmed.Substring("ENDIRONPYTHON".Length).Trim()
					: pyTrimmed.Substring("ENDPYTHON".Length).Trim();
				// Use RunInlinePython so the 'data' dict preamble is injected (data["SOURCE"] etc.)
				try { RunInlinePython(string.Join(Environment.NewLine, pythonBuf), rest, inIronPythonBlock, data); }
				catch (Exception ex) { data.LogBuffer.Add(new LogEntry("Python block error: " + ex.Message, Colors.Tomato)); }
				pythonBuf.Clear();
				inPythonBlock    = false;
				inIronPythonBlock = false;
			}
			else
			{
				pythonBuf.Add(pyRaw);
			}
			this.i++;
			return;
		}

		if ((data.Status == BotStatus.CUSTOM && !data.ConfigSettings.ContinueOnCustom) || (data.Status != 0 && data.Status != BotStatus.SUCCESS && data.Status != BotStatus.CUSTOM))
		{
			this.i = lines.Count();
			return;
		}
		while (true)
		{
			CurrentLine = lines[this.i];
			Line = this.i;
			if (!IsEmptyOrCommentOrDisabled(CurrentLine))
			{
				break;
			}
			this.i++;
		}
		int i = 0;
		// Only concatenate continuation lines for block commands (REQUEST, PARSE, etc.)
		// NOT for flow control (IF, WHILE, ELSE...) which would consume their body statements.
		// CompressedLines already does this correctly; TakeStep was missing this guard.
		// This also fixes SCRIPT blocks: without the guard, BEGIN SCRIPT consumed END SCRIPT,
		// so RunScript was never called in step-by-step mode.
		if (BlockParser.IsBlock(CurrentLine))
		{
			bool isKeycheck = BlockParser.GetBlockType(CurrentLine)
				.Equals("KEYCHECK", StringComparison.OrdinalIgnoreCase);
			for (i = 0; this.i + 1 + i < lines.Count(); i++)
			{
				string text  = lines[this.i + 1 + i];
				string textT = text.TrimStart();
				// KEYCHECK: also absorb non-indented KEYCHAIN/KEY lines
				if (isKeycheck &&
					(textT.StartsWith("KEYCHAIN ", StringComparison.OrdinalIgnoreCase) ||
					 textT.StartsWith("KEY ", StringComparison.OrdinalIgnoreCase) ||
					 textT.StartsWith("! KEYCHAIN ", StringComparison.OrdinalIgnoreCase) ||
					 textT.StartsWith("! KEY ", StringComparison.OrdinalIgnoreCase)))
				{
					CurrentLine += " " + textT;
					continue;
				}
				if (!text.StartsWith(" ") && !text.StartsWith("\t"))
				{
					break;
				}
				CurrentLine = CurrentLine + " " + text.Trim();
			}
		}
		try
		{
			if (BlockParser.IsBlock(CurrentLine))
			{
				BlockBase blockBase = null;
				try
				{
					blockBase = BlockParser.Parse(CurrentLine);
					CurrentBlock = blockBase.Label;
					if (!blockBase.Disabled)
					{
						blockBase.Process(data);
					}
				}
				catch (Exception ex)
				{
					if (inTryBlock)
					{
						data.LogBuffer.Add(new LogEntry("TRY caught: " + ex.Message, Colors.Orange));
						tryErrorOccurred = true;
						tryErrorMessage = ex.Message;
						// Saltar al CATCH (o ENDTRY si no hay CATCH).
						// Restamos 1 porque this.i += 1+i al final sumará 1, quedando en CATCH.
						try { this.i = ScanFor(lines, this.i, downwards: true, new string[] { "CATCH" }, new string[] { "TRY" }, new string[] { "ENDTRY" }) - 1; }
						catch { try { this.i = ScanFor(lines, this.i, downwards: true, new string[] { "ENDTRY" }, new string[] { "TRY" }, new string[] { "ENDTRY" }) - 1; } catch { } }
						i = 0;
					}
					else
					{
						data.LogBuffer.Add(new LogEntry("ERROR: " + ex.Message, Colors.Tomato));
#pragma warning disable CS0612
						if (blockBase != null && (blockBase.GetType() == typeof(BlockRequest) || blockBase.GetType() == typeof(BlockBypassCF) || blockBase.GetType() == typeof(BlockImageCaptcha) || blockBase.GetType() == typeof(BlockRecaptcha)))
#pragma warning restore CS0612
						{
							data.Status = BotStatus.ERROR;
							throw new BlockProcessingException(ex.Message);
						}
					}
				}
			}
			else if (CommandParser.IsCommand(CurrentLine))
			{
				try
				{
					CommandParser.Parse(CurrentLine, data)?.Invoke();
				}
				catch (Exception ex2)
				{
					data.LogBuffer.Add(new LogEntry("ERROR: " + ex2.Message, Colors.Tomato));
					data.Status = BotStatus.ERROR;
				}
			}
			else
			{
				string input = CurrentLine;
				string text2 = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false).ToUpper();
				if (text2 != null)
				{
					switch (text2.Length)
					{
					case 4:
						switch (text2[0])
						{
						case 'E':
							if (text2 == "ELSE")
							{
								this.i = ScanFor(lines, this.i, downwards: true, new string[1] { "ENDIF" }, new string[] { "IF" }, new string[] { "ENDIF" });
								data.LogBuffer.Add(new LogEntry($"Jumping to line {this.i + 1}", Colors.White));
							}
							break;
						case 'J':
							if (text2 == "JUMP")
							{
								string text3 = "";
								try
								{
									text3 = LineParser.ParseToken(ref input, TokenType.Label, essential: true);
									this.i = ScanFor(lines, -1, downwards: true, new string[1] { text3 ?? "" }) - 1;
									data.LogBuffer.Add(new LogEntry($"Jumping to line {this.i + 2}", Colors.White));
								}
								catch
								{
									throw new Exception("No block with label " + text3 + " was found");
								}
							}
							break;
						}
						break;
					case 5:
						switch (text2[0])
						{
						case 'E':
							if (text2 == "ENDIF")
							{
							}
							break;
						case 'W':
							if (text2 == "WHILE" && !ParseCheckCondition(ref input, data))
							{
								this.i = ScanFor(lines, this.i, downwards: true, new string[1] { "ENDWHILE" }, new string[] { "WHILE" }, new string[] { "ENDWHILE" });
								data.LogBuffer.Add(new LogEntry($"Jumping to line {this.i + 1}", Colors.White));
							}
							break;
						case 'C':
							if (text2 == "CATCH")
							{
								if (!tryErrorOccurred)
								{
									// No hubo error en el TRY → saltar a ENDTRY.
									// IMPORTANTE: resetear el estado TRY aquí porque ENDTRY se va a saltar
									// (this.i apunta a ENDTRY y luego +=1 pasa de largo, así que su handler no corre).
									this.i = ScanFor(lines, this.i, downwards: true, new string[] { "ENDTRY" }, new string[] { "TRY" }, new string[] { "ENDTRY" });
									inTryBlock = false;
									tryErrorOccurred = false;
									tryErrorMessage = "";
									data.LogBuffer.Add(new LogEntry($"TRY sin error, saltando a ENDTRY en línea {this.i + 1}", Colors.White));
								}
								else
								{
									// Error capturado → ejecutar body del CATCH, guardar mensaje en variable ERROR
									data.Variables.Set(new CVar("ERROR", tryErrorMessage));
									inTryBlock = false;
									data.LogBuffer.Add(new LogEntry($"CATCH: {tryErrorMessage}", Colors.Orange));
								}
							}
							break;
						case 'B':
						{
							if (!(text2 == "BEGIN") || !(LineParser.ParseToken(ref input, TokenType.Parameter, essential: true).ToUpper() == "SCRIPT"))
							{
								break;
							}
							language = (ScriptingLanguage)LineParser.ParseEnum(ref input, "LANGUAGE", typeof(ScriptingLanguage));
							if (LineParser.Lookahead(ref input) == TokenType.Parameter)
							{
								try
								{
									jsEngine = LineParser.ParseToken(ref input, TokenType.Parameter, essential: false);
								}
								catch
								{
									jsEngine = string.Empty;
								}
							}
							try
							{
								jsFilePath = LineParser.ParseLiteral(ref input, "PATH");
							}
							catch
							{
								jsFilePath = string.Empty;
							}
							int num = 0;
							try
							{
								num = ScanFor(lines, this.i, downwards: true, new string[1] { "END" }) - 1;
							}
							catch
							{
								throw new Exception("No 'END SCRIPT' specified");
							}
							otherScript = string.Join(Environment.NewLine, lines.Skip(this.i + 1).Take(num - this.i));
							this.i = num;
							data.LogBuffer.Add(new LogEntry($"Jumping to line {this.i + 2}", Colors.White));
							break;
						}
						}
						break;
					case 2:
						if (text2 == "IF" && !ParseCheckCondition(ref input, data))
						{
							this.i = ScanFor(lines, this.i, downwards: true, new string[2] { "ENDIF", "ELSE" }, new string[] { "IF" }, new string[] { "ENDIF" });
							data.LogBuffer.Add(new LogEntry($"Jumping to line {this.i + 1}", Colors.White));
						}
						break;
					case 8:
						if (text2 == "ENDWHILE")
						{
							this.i = ScanFor(lines, this.i, downwards: false, new string[1] { "WHILE" }, new string[] { "ENDWHILE" }, new string[] { "WHILE" }) - 1;
							data.LogBuffer.Add(new LogEntry($"Jumping to line {this.i + 1}", Colors.White));
						}
						break;
					case 3:
					{
						if (text2 == "TRY")
						{
							inTryBlock = true;
							tryErrorOccurred = false;
							tryErrorMessage = "";
							data.LogBuffer.Add(new LogEntry("Entrando en bloque TRY", Colors.White));
							break;
						}
						if (!(text2 == "END") || !(LineParser.ParseToken(ref input, TokenType.Parameter, essential: true).ToUpper() == "SCRIPT"))
						{
							break;
						}
						LineParser.EnsureIdentifier(ref input, "->");
						LineParser.EnsureIdentifier(ref input, "VARS");
						// Read the first literal, then consume any additional space-separated literals.
						// LoliScript format: END SCRIPT -> VARS "Var1" "Var2" "Var3"
						string outputs = LineParser.ParseLiteral(ref input, "OUTPUTS");
						while (!string.IsNullOrWhiteSpace(input))
						{
							try { outputs += "," + LineParser.ParseLiteral(ref input, "EXTRA_OUTPUT"); }
							catch { break; }
						}
						try
						{
							if (language == ScriptingLanguage.IronPython || language == ScriptingLanguage.Python)
							{
								// Route through RunInlinePython so the 'data' dict preamble is injected
								// and execution bypasses TakeStep/LogBuffer.Clear() stale-offset issues.
								RunInlinePython(otherScript, outputs, language == ScriptingLanguage.IronPython, data);
							}
							else if (otherScript != string.Empty || jsFilePath != string.Empty)
							{
								RunScript(otherScript, language, outputs, data, jsFilePath);
							}
						}
						catch (Exception ex3)
						{
							data.LogBuffer.Add(new LogEntry("The script failed to be executed: " + ex3.Message, Colors.Tomato));
						}
						break;
					}
					case 6:
						if (text2 == "ENDTRY")
						{
							inTryBlock = false;
							tryErrorOccurred = false;
							tryErrorMessage = "";
							data.LogBuffer.Add(new LogEntry("Saliendo de bloque TRY/CATCH", Colors.White));
						}
						else if (text2 == "PYTHON")
						{
							inPythonBlock = true;
							pythonBuf.Clear();
							data.LogBuffer.Add(new LogEntry("<--- Entering inline PYTHON block --->", Colors.Orange));
						}
						break;
					case 7:
						if (text2 == "FOREACH")
						{
							string outVar = LineParser.ParseLiteral(ref input, "VARIABLE");
							LineParser.EnsureIdentifier(ref input, "IN");
							string listRef = LineParser.ParseLiteral(ref input, "LIST");
							// Aceptar "<varName>" o "varName"
							string listVarName = listRef.TrimStart('<').TrimEnd('>');

							if (!foreachLists.ContainsKey(this.i))
							{
								// Primera vez en este FOREACH: copiar la lista al estado interno del iterador
								List<string> source = data.Variables.GetList(listVarName) ?? new List<string>();
								foreachLists[this.i] = new List<string>(source);
								foreachCounters[this.i] = 0;
							}

							int idx = foreachCounters[this.i];
							List<string> itemList = foreachLists[this.i];

							if (idx >= itemList.Count)
							{
								// Lista agotada: saltar justo al ENDFOREACH; el += 1+0 final pasa a la siguiente línea
								foreachLists.Remove(this.i);
								foreachCounters.Remove(this.i);
								this.i = ScanFor(lines, this.i, downwards: true, new string[] { "ENDFOREACH" }, new string[] { "FOREACH" }, new string[] { "ENDFOREACH" });
								data.LogBuffer.Add(new LogEntry($"FOREACH terminado, saltando más allá de ENDFOREACH", Colors.White));
							}
							else
							{
								// Asignar elemento actual y avanzar contador
								data.Variables.Set(new CVar(outVar, itemList[idx]));
								foreachCounters[this.i]++;
								data.LogBuffer.Add(new LogEntry($"FOREACH {outVar} = \"{itemList[idx]}\" ({idx + 1}/{itemList.Count})", Colors.White));
							}
						}
						break;
					case 10:
						if (text2 == "IRONPYTHON")
						{
							inIronPythonBlock = true;
							pythonBuf.Clear();
							data.LogBuffer.Add(new LogEntry("<--- Entering inline IRONPYTHON block --->", Colors.Orange));
						}
						else if (text2 == "ENDFOREACH")
						{
							// Volver al FOREACH correspondiente: ScanFor devuelve su índice, restamos 1
							// para que += 1+0 al final aterrice exactamente en la línea FOREACH
							this.i = ScanFor(lines, this.i, downwards: false, new string[] { "FOREACH" }, new string[] { "ENDFOREACH" }, new string[] { "FOREACH" }) - 1;
							data.LogBuffer.Add(new LogEntry("ENDFOREACH: volviendo al FOREACH", Colors.White));
						}
						break;
					}
				}
				// ─── Inline C# code: collect the full multi-line block and run it once ─────────
				// Any line that reaches here (past all LoliScript block/command/keyword checks) is
				// raw C# code. Accumulate lines until brace-depth returns to 0 AND the look-ahead
				// is a recognised LoliScript construct, then execute the whole chunk in one Roslyn
				// call so variable scope is intact across multi-line using/for/anonymous-type blocks.
				// Guard: skip for LoliScript keywords (BEGIN SCRIPT, END SCRIPT, IF, WHILE, etc.)
				// that were already handled by the switch above. Running Roslyn on "BEGIN SCRIPT NodeJS"
				// would waste 3-5 s on Roslyn cold-start and always fail with a compilation error.
				if (!IsLoliScriptLine(CurrentLine))
				{
					var _csLines = new List<string> { CurrentLine };
					int _csSkip = 0;
					int _depth  = NetBracesInLine(CurrentLine);
					string _lastNonEmptyCs = CurrentLine;
					while (this.i + 1 + i + _csSkip < lines.Length)
					{
						string _nxt = lines[this.i + 1 + i + _csSkip];
						// At brace depth ≤ 0, stop on empty/comment lines or any LoliScript construct
						if (_depth <= 0 && (IsEmptyOrCommentOrDisabled(_nxt) || IsLoliScriptLine(_nxt)))
							break;
						_csLines.Add(_nxt);
						// OB2 `try { {` pattern: bare `{` line immediately after a `try {` line.
						// Don't count its depth so the try-catch block correctly closes at depth 0.
						bool _isOb2TryDoubleBrace = _nxt.Trim() == "{" &&
							_lastNonEmptyCs.TrimStart().StartsWith("try", System.StringComparison.Ordinal) &&
							_lastNonEmptyCs.TrimEnd().EndsWith("{");
						if (!_isOb2TryDoubleBrace)
							_depth += NetBracesInLine(_nxt);
						if (!string.IsNullOrWhiteSpace(_nxt)) _lastNonEmptyCs = _nxt;
						_csSkip++;
					}
					i += _csSkip;
					if (_csLines.Any(l => !string.IsNullOrWhiteSpace(l)))
					{
						// Inject bot CVars as C# locals — but ONLY for vars not already in scope
						// from a previous block (tracked in _csDeclaredVars). This avoids
						// "already declared" errors in Roslyn ScriptState continuations.
						var _varPreamble = new System.Text.StringBuilder();
						var _reserved = new System.Collections.Generic.HashSet<string>(
							System.StringComparer.Ordinal)
							{ "__rv", "log", "print", "LOG", "input", "data" };
						foreach (var _cv in data.Variables.All)
						{
							if (_cv.Hidden) continue;
							string _vn = _cv.Name;
							if (string.IsNullOrEmpty(_vn)) continue;
							if (!System.Text.RegularExpressions.Regex.IsMatch(
									_vn, @"^[A-Za-z_][A-Za-z0-9_]*$")) continue;
							if (_reserved.Contains(_vn)) continue;
							if (_csDeclaredVars.Contains(_vn)) continue; // already in C# scope
							if (_cv.Type == CVar.VarType.List)
								_varPreamble.AppendLine($"var {_vn} = data.GetListVar(\"{_vn}\");");
							else
								_varPreamble.AppendLine($"string {_vn} = data.GetVar(\"{_vn}\");");
						}
						// Translate LoliScript status directives so they work inside inline C# blocks
						for (int _li = 0; _li < _csLines.Count; _li++)
						{
							string _t = _csLines[_li].TrimStart();
							if (_t.StartsWith("SET STATUS ", StringComparison.OrdinalIgnoreCase))
							{
								string _st = _t.Substring("SET STATUS ".Length).Trim().TrimEnd(';').ToUpper();
								string _indent = _csLines[_li].Substring(0, _csLines[_li].Length - _t.Length);
								_csLines[_li] = _indent + $"data.STATUS = \"{_st}\";";
							}
						}
						string _userCode = string.Join("\n", _csLines);
						_userCode = LsFixBraceAfterTry(_userCode); // OB2: fix `try { {` → `try {`
						string _fullCs = _varPreamble.Length > 0
							? _varPreamble.ToString() + _userCode
							: _userCode;

						try
						{
							if (_csState == null)
							{
								// First C# block: fresh Roslyn execution, captures ScriptState.
								var _fr = LoliCodeRunner.RunFresh(_fullCs, data);
								_csState    = _fr.state;
								_csLoliData = _fr.loliData;
							}
							else
							{
								// Subsequent blocks: bypass the LoliCode pipeline (Parse→Compile→strip preamble)
								// and pass the raw C# directly to Roslyn. The continuation already has all
								// preamble helpers (__rv, log, data, etc.) in scope from RunFresh, and running
								// LoliCodeParser/Compiler on plain C# blocks causes spurious CS1002 errors.
								_csState = _csState.ContinueWithAsync<object>(_fullCs)
									.GetAwaiter().GetResult();
							}

							// Flush string/numeric ScriptState variables back to bot CVars so that
							// <varName> templates in REQUEST headers/body resolve correctly.
							if (_csState != null)
							{
								foreach (var _sv in _csState.Variables)
								{
									if (_sv.Value == null) continue;
									string _sn = _sv.Name;
									if (_reserved.Contains(_sn)) continue;
									try
									{
										if      (_sv.Value is string _ss)
											data.Variables.Set(new CVar(_sn, _ss));
										else if (_sv.Value is int    _si)
											data.Variables.Set(new CVar(_sn, _si.ToString()));
										else if (_sv.Value is long   _sl)
											data.Variables.Set(new CVar(_sn, _sl.ToString()));
										else if (_sv.Value is bool   _sb2)
											data.Variables.Set(new CVar(_sn, _sb2.ToString().ToLower()));
										else if (_sv.Value is double _sd)
											data.Variables.Set(new CVar(_sn, _sd.ToString(System.Globalization.CultureInfo.InvariantCulture)));
										else if (_sv.Value is byte[] _sba)
											data.Variables.Set(new CVar(_sn, CVar.VarType.Single, (object)_sba));
										// Func<>, Action<>, BigInteger etc. are skipped silently.
									}
									catch { /* ignore non-serialisable types */ }
								}

								// Update declared-var registry for preamble de-duplication.
								_csDeclaredVars.Clear();
								foreach (var _sv in _csState.Variables)
									_csDeclaredVars.Add(_sv.Name);
							}
						}
						catch (Microsoft.CodeAnalysis.Scripting.CompilationErrorException _cee)
						{
							var _errs = _cee.Diagnostics
								.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
								.ToList();

							// ── CS0103 auto-retry ─────────────────────────────────────────────────
							// If ALL errors are "identifier not declared" (CS0103) and we can find
							// the missing variables in the bot's CVars, re-inject them and retry once.
							// This recovers from cases where a var was declared inside a { } scope
							// in a previous block and didn't make it into _csDeclaredVars.
							var _missingVars = new System.Collections.Generic.List<string>();
							foreach (var _ed in _errs)
							{
								if (_ed.Id != "CS0103") continue;
								var _mx = System.Text.RegularExpressions.Regex.Match(
									_ed.GetMessage(), @"The name '(\w+)' does not exist");
								if (_mx.Success) _missingVars.Add(_mx.Groups[1].Value);
							}

							bool _recovered = false;
							if (_missingVars.Count > 0 && _missingVars.Count == _errs.Count)
							{
								var _retryPreamble = new System.Text.StringBuilder();
								foreach (var _mv in _missingVars.Distinct(System.StringComparer.Ordinal))
								{
									if (_csDeclaredVars.Contains(_mv)) continue;
									var _foundCv = data.Variables.All.FirstOrDefault(cv => cv.Name == _mv);
									if (_foundCv == null) continue;
									if (_foundCv.Type == CVar.VarType.List)
										_retryPreamble.AppendLine($"var {_mv} = data.GetListVar(\"{_mv}\");");
									else
										_retryPreamble.AppendLine($"string {_mv} = data.GetVar(\"{_mv}\");");
								}
								if (_retryPreamble.Length > 0)
								{
									string _retryCode = _retryPreamble.ToString() + _fullCs;
									try
									{
										if (_csState == null)
										{
											var _fr2 = LoliCodeRunner.RunFresh(_retryCode, data);
											_csState    = _fr2.state;
											_csLoliData = _fr2.loliData;
										}
										else
										{
											_csState = _csState.ContinueWithAsync<object>(_retryCode)
												.GetAwaiter().GetResult();
										}
										// Flush recovered state
										if (_csState != null)
										{
											var _res2 = new System.Collections.Generic.HashSet<string>(
												System.StringComparer.Ordinal)
												{ "__rv", "log", "print", "LOG", "input", "data" };
											foreach (var _sv2 in _csState.Variables)
											{
												if (_sv2.Value == null || _res2.Contains(_sv2.Name)) continue;
												try
												{
													if      (_sv2.Value is string _ss2) data.Variables.Set(new CVar(_sv2.Name, _ss2));
													else if (_sv2.Value is int    _si2) data.Variables.Set(new CVar(_sv2.Name, _si2.ToString()));
													else if (_sv2.Value is long   _sl2) data.Variables.Set(new CVar(_sv2.Name, _sl2.ToString()));
													else if (_sv2.Value is bool   _sb3) data.Variables.Set(new CVar(_sv2.Name, _sb3.ToString().ToLower()));
													else if (_sv2.Value is double _sd2) data.Variables.Set(new CVar(_sv2.Name, _sd2.ToString(System.Globalization.CultureInfo.InvariantCulture)));
													else if (_sv2.Value is byte[] _sba2) data.Variables.Set(new CVar(_sv2.Name, CVar.VarType.Single, (object)_sba2));
												}
												catch { }
											}
											_csDeclaredVars.Clear();
											foreach (var _sv2 in _csState.Variables)
												_csDeclaredVars.Add(_sv2.Name);
										}
										data.Log(new LogEntry(
											$"[LS C#] Auto-recovered: injected missing var(s) [{string.Join(", ", _missingVars.Distinct())}] from CVars",
											Colors.Yellow));
										_recovered = true;
									}
									catch { /* retry also failed — fall through to error display */ }
								}
							}

							if (!_recovered)
							{
								// ── Improved error display ────────────────────────────────────────────
								// Count preamble lines so user sees line numbers relative to their code,
								// not the injected preamble.
								int _preLines = _varPreamble.Length > 0
									? _varPreamble.ToString().Split('\n').Length - 1
									: 0;
								// First meaningful line of the block as context label
								string _blockCtx = _csLines
									.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("//"))
									?.Trim() ?? _csLines.FirstOrDefault()?.Trim() ?? "?";
								if (_blockCtx.Length > 72) _blockCtx = _blockCtx.Substring(0, 69) + "...";

								var _esb = new System.Text.StringBuilder();
								_esb.AppendLine($"[LS C#] {_errs.Count} error(s) in block: {_blockCtx}");
								var _codeLines = _fullCs.Split('\n');
								foreach (var _ed in _errs)
								{
									var _sp  = _ed.Location.GetLineSpan();
									int _ln  = _sp.StartLinePosition.Line;
									int _uln = _ln - _preLines + 1; // 1-based user code line
									string _lineRef = _uln >= 1 ? $"line ~{_uln}" : "injected preamble";
									_esb.AppendLine($"  ► {_ed.Id} ({_lineRef}): {_ed.GetMessage()}");
									if (_ln >= 0 && _ln < _codeLines.Length)
										_esb.AppendLine($"    >>> {_codeLines[_ln].TrimEnd()}");
								}
								data.Log(new LogEntry(_esb.ToString().TrimEnd(), Colors.Tomato));
							}
						}
						catch (Exception _cex)
						{
							string _blockCtx2 = _csLines
								.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("//"))
								?.Trim() ?? "?";
							if (_blockCtx2.Length > 70) _blockCtx2 = _blockCtx2.Substring(0, 67) + "...";
							data.Log(new LogEntry(
								$"[LS C#] Runtime error in block ({_blockCtx2}): {_cex.GetType().Name}: {_cex.Message}",
								Colors.Tomato));
						}

						data.Flush();
						Thread.Sleep(1);
					}
				}
			}
		}
		catch (BlockProcessingException)
		{
			throw;
		}
		catch (Exception ex5)
		{
			// Increment i before rethrowing so a bad line never causes an infinite loop.
			this.i += 1;
			throw new Exception($"Parsing Exception on line {this.i}: {ex5.Message}");
		}
		this.i += 1 + i;
	}

	/// <summary>
	/// Called from LoliCode (Roslyn) scripts to run an inline PYTHON/IRONPYTHON block.
	/// Wraps the code in a BEGIN SCRIPT … END SCRIPT mini-LoliScript and runs it.
	/// Injects a 'data' dict so Python code can use data["SOURCE"], data["RESPONSECODE"],
	/// data["ADDRESS"] and any user CVar by name — e.g. data["myVar"].
	/// </summary>
	public static void RunInlinePython(string code, string outputs, bool ironPython, BotData data)
	{
		string outputsStr = string.IsNullOrWhiteSpace(outputs) ? "" : outputs;

		// Build a Python dict literal so scripts can use data["SOURCE"], data["USERNAME"], etc.
		var pySb = new System.Text.StringBuilder("data = {");
		pySb.Append(PyLitStr("SOURCE")).Append(": ").Append(PyLitStr(data.ResponseSource ?? "")).Append(", ");
		pySb.Append(PyLitStr("RESPONSECODE")).Append(": ").Append(PyLitStr(data.ResponseCode ?? "")).Append(", ");
		pySb.Append(PyLitStr("ADDRESS")).Append(": ").Append(PyLitStr(data.Address ?? "")).Append(", ");
		foreach (var cv in data.Variables.All)
		{
			if (cv.Type == CVar.VarType.Single)
				pySb.Append(PyLitStr(cv.Name)).Append(": ").Append(PyLitStr(cv.Value?.ToString() ?? "")).Append(", ");
			else if (cv.Type == CVar.VarType.List && cv.Value is System.Collections.Generic.List<string> cvList)
			{
				pySb.Append(PyLitStr(cv.Name)).Append(": [");
				foreach (var listItem in cvList) pySb.Append(PyLitStr(listItem)).Append(", ");
				pySb.Append("], ");
			}
		}
		pySb.Append("}\n");
		string preamble = pySb.ToString();

		// Strip OB2 C-style metadata directives (// _INPUTS:, // _OUTPUTS:) — invalid Python syntax.
		{
			var stripped = new System.Collections.Generic.List<string>();
			foreach (string ln in code.Split('\n'))
			{
				string t = ln.TrimStart();
				if (t.StartsWith("// _INPUTS:",  StringComparison.OrdinalIgnoreCase) ||
				    t.StartsWith("// _OUTPUTS:", StringComparison.OrdinalIgnoreCase) ||
				    t.StartsWith("//_INPUTS:",   StringComparison.OrdinalIgnoreCase) ||
				    t.StartsWith("//_OUTPUTS:",  StringComparison.OrdinalIgnoreCase))
					stripped.Add("# " + t.Substring(2));
				else
					stripped.Add(ln);
			}
			code = string.Join("\n", stripped);
		}

		// IronPython: inject .NET-backed hashlib/base64 polyfills and register them in
		// sys.modules so that a subsequent "import hashlib" in user code finds the polyfill
		// rather than failing with "No module named 'hashlib'".
		string ipyPolyfill = ironPython ? @"
try:
    import hashlib as _hl_test
    _hl_test.md5(b'')
except:
    import clr
    import sys as _sys_poly
    from System.Security.Cryptography import MD5CryptoServiceProvider, SHA1CryptoServiceProvider, SHA256CryptoServiceProvider, SHA512CryptoServiceProvider
    class _HashObj:
        def __init__(self, prov, data=None):
            self._p = prov
            self._d = bytearray(data) if data else bytearray()
        def update(self, d):
            self._d.extend(bytearray(d))
        def digest(self):
            from System import Array, Byte
            net_bytes = Array[Byte](list(self._d))
            result = self._p.ComputeHash(net_bytes)
            return bytes([int(b) for b in result])
        def hexdigest(self):
            return ''.join('{:02x}'.format(b) for b in self.digest())
    class _HashlibPolyfill:
        def md5(self, data=None): return _HashObj(MD5CryptoServiceProvider(), data)
        def sha1(self, data=None): return _HashObj(SHA1CryptoServiceProvider(), data)
        def sha256(self, data=None): return _HashObj(SHA256CryptoServiceProvider(), data)
        def sha512(self, data=None): return _HashObj(SHA512CryptoServiceProvider(), data)
        def new(self, name, data=None):
            n = name.lower().replace('-','')
            if n == 'md5': return self.md5(data)
            if n == 'sha1': return self.sha1(data)
            if n == 'sha256': return self.sha256(data)
            if n == 'sha512': return self.sha512(data)
            raise ValueError('Unknown hash: ' + name)
    hashlib = _HashlibPolyfill()
    _sys_poly.modules['hashlib'] = hashlib
try:
    import base64 as _b64_test
    _b64_test.b64encode(b'')
except:
    import clr
    import sys as _sys_poly2
    from System import Convert as _Convert
    class _Base64Polyfill:
        @staticmethod
        def _to_net(s):
            from System import Array, Byte
            return Array[Byte](list(bytearray(s)))
        def b64encode(self, s): return _Convert.ToBase64String(self._to_net(s)).encode('ascii')
        def b64decode(self, s):
            if isinstance(s, bytes): s = s.decode('ascii')
            return bytes([b for b in _Convert.FromBase64String(s)])
        def urlsafe_b64encode(self, s): return self.b64encode(s).replace(b'+', b'-').replace(b'/', b'_')
        def urlsafe_b64decode(self, s):
            if isinstance(s, bytes): s = s.decode('ascii')
            return self.b64decode(s.replace('-', '+').replace('_', '/'))
    base64 = _Base64Polyfill()
    _sys_poly2.modules['base64'] = base64
" : "";

		string fullCode = preamble + ipyPolyfill + "\n" + code;

		// Parse output variable names
		var outVarList = new System.Collections.Generic.List<string>();
		if (!string.IsNullOrWhiteSpace(outputsStr))
		{
			try { outVarList = outputsStr.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList(); }
			catch { }
		}

		var now = DateTime.Now;

		if (ironPython)
		{
			// Per-thread engine: no lock, all bots run IronPython in parallel.
			// Old code: one shared engine + global lock → bots serialize (1 at a time regardless of bot count).
			try
			{
				if (_ironPythonEnginePerThread == null)
				{
					_ironPythonEnginePerThread = Python.CreateEngine();
					((PythonCompilerOptions)_ironPythonEnginePerThread.GetCompilerOptions()).Module &= ~ModuleOptions.Optimized;
					foreach (var asm in new[]
					{
						typeof(System.Security.Cryptography.Aes).Assembly,
						typeof(System.Security.Cryptography.SHA256).Assembly,
						typeof(System.Net.Http.HttpClient).Assembly,
						typeof(System.Convert).Assembly,
						typeof(System.Text.Encoding).Assembly,
						typeof(System.Text.RegularExpressions.Regex).Assembly,
					}.Distinct())
					{ try { _ironPythonEnginePerThread.Runtime.LoadAssembly(asm); } catch { } }
				}

				ScriptEngine engine = _ironPythonEnginePerThread;
				using var msIpy = new MemoryStream();
				engine.Runtime.IO.SetOutput(msIpy, Encoding.UTF8);
				engine.Runtime.IO.SetErrorOutput(msIpy, Encoding.UTF8);

				ScriptScope scriptScope = engine.CreateScope();
				ScriptSource scriptSource = engine.CreateScriptSourceFromString(fullCode);

				foreach (CVar item in data.Variables.All)
					try { scriptScope.SetVariable(item.Name, item.Value); } catch { }
				foreach (CVar slice in data.Data.GetVariables(false))
					try { scriptScope.SetVariable(slice.Name, (string)slice.Value); } catch { }

				scriptSource.Execute(scriptScope);

				data.Log(new LogEntry("DEBUG LOG: " + Encoding.UTF8.GetString(msIpy.ToArray()), Colors.White));
				data.Log(new LogEntry($"Parsing {outVarList.Count} variables", Colors.White));
				foreach (string item2 in outVarList)
				{
					try
					{
						dynamic variable = scriptScope.GetVariable(item2);
						bool isPyList = variable is System.Collections.IList && !(variable is string);
						if (variable is string[] || isPyList)
						{
							var lst = new System.Collections.Generic.List<string>();
							foreach (var elem in (System.Collections.IEnumerable)variable) lst.Add(elem?.ToString() ?? "");
							data.Variables.Set(new CVar(item2, CVar.VarType.List, lst));
						}
						else
						{
							string sval = "";
							try { sval = System.Convert.ToString(variable) ?? ""; } catch { sval = variable?.ToString() ?? ""; }
							data.Variables.Set(new CVar(item2, CVar.VarType.Single, sval));
						}
						data.Log(new LogEntry($"SET VARIABLE {item2} WITH VALUE {variable}", Colors.Yellow));
					}
					catch (Exception __ipyEx) { data.Log(new LogEntry($"COULD NOT FIND VARIABLE {item2}: {__ipyEx.Message}", Colors.Tomato)); }
				}
			}
			catch (Exception ex) { data.Log(new LogEntry("[ERROR] INFO: " + ex.Message, Colors.White)); }
			data.Log(new LogEntry($"Execution completed in {(DateTime.Now - now).TotalSeconds} seconds", Colors.GreenYellow));
		}
		else
		{
			// External CPython — build script with data dict preamble so data["VAR"] works.
			try
			{
				string pythonScript = ExternalScriptRunner.BuildPythonScript(data.Variables.All, fullCode, outVarList);
				string stdout = ExternalScriptRunner.RunScript("python", "", pythonScript, ".py");
				string stderr = ExternalScriptRunner.LastStderr;
				if (!string.IsNullOrEmpty(stderr))
					data.Log(new LogEntry("STDERR: " + stderr, Colors.Tomato));
				data.Log(new LogEntry("STDOUT: " + stdout, Colors.White));
				var outputMap = ExternalScriptRunner.ParseOutput(stdout);
				data.Log(new LogEntry($"Parsing {outVarList.Count} variables", Colors.White));
				foreach (string outVar in outVarList)
				{
					if (outputMap.TryGetValue(outVar, out var val))
					{
						if (val is Newtonsoft.Json.Linq.JArray arr)
							data.Variables.Set(new CVar(outVar, arr.Select(t => t.ToString()).ToList()));
						else
							data.Variables.Set(new CVar(outVar, val?.ToString() ?? ""));
						data.Log(new LogEntry($"SET VARIABLE {outVar} WITH VALUE {val}", Colors.Yellow));
					}
					else
						data.Log(new LogEntry("COULD NOT FIND VARIABLE " + outVar, Colors.Tomato));
				}
			}
			catch (Exception ex) { data.Log(new LogEntry("[ERROR] INFO: " + ex.Message, Colors.White)); }
			data.Log(new LogEntry($"Execution completed in {(DateTime.Now - now).TotalSeconds} seconds", Colors.GreenYellow));
		}
	}

	private static string PyLitStr(string s) =>
		"\"" + (s ?? "")
			.Replace("\\", "\\\\")
			.Replace("\"", "\\\"")
			.Replace("\n", "\\n")
			.Replace("\r", "\\r")
			.Replace("\t", "\\t") + "\"";

	/// <summary>
	/// Called from LoliCode (Roslyn) scripts generated by BLOCK:Script parser.
	/// Routes execution to the correct language engine based on the interpreter name.
	/// Supported interpreter names (case-insensitive):
	///   IronPython → IronPython engine  (uses RunInlinePython data-dict preamble)
	///   Python     → External CPython   (uses RunInlinePython data-dict preamble)
	///   Jint       → Jint (JavaScript) engine
	///   NodeJS     → External Node.js
	/// </summary>
	public static void RunInlineScript(string code, string inputs, string outputs, string interpreter, BotData data)
	{
		string langName = (interpreter ?? "").Trim().ToLowerInvariant() switch {
			"ironpython"                    => "IronPython",
			"python"                        => "Python",
			"jint" or "javascript"          => "JavaScript",
			"nodejs" or "node.js"           => "NodeJS",
			"typescript"                    => "TypeScript",
			"lua"                           => "Lua",
			"csharp" or "c#"               => "CSharp",
			"php"                           => "PHP",
			"ruby"                          => "Ruby",
			"go"                            => "Go",
			"java"                          => "Java",
			"cplusplus" or "c++"            => "CPlusPlus",
			"rust"                          => "Rust",
			_                               => "IronPython"
		};

		// IronPython and Python use the existing RunInlinePython which injects a data dict preamble.
		if (langName == "IronPython" || langName == "Python")
		{
			RunInlinePython(code, outputs, langName == "IronPython", data);
			return;
		}

		string outputsStr = string.IsNullOrWhiteSpace(outputs) ? "" : outputs;
		var ls = new LoliScript($"BEGIN SCRIPT {langName}\n{code}\nEND SCRIPT -> VARS \"{outputsStr}\"");
		ls.Reset();
		while (ls.CanProceed) ls.TakeStep(data);
	}

	private static bool IsEmptyOrCommentOrDisabled(string line)
	{
		try
		{
			return line.Trim() == string.Empty || line.StartsWith("##") || line.StartsWith("!");
		}
		catch
		{
			return true;
		}
	}

	/// <summary>
	/// OB2 compat: removes the extra bare `{` that OB2 emits after `try {`, so that
	/// `try { \n {` becomes `try {`. The matching `}` that closed the inner scope now
	/// closes the try body instead — which is valid C# and fixes CS1513.
	/// </summary>
	private static string LsFixBraceAfterTry(string code)
	{
		if (string.IsNullOrEmpty(code) || !code.Contains("try")) return code;
		return System.Text.RegularExpressions.Regex.Replace(
			code,
			@"(try\s*\{[ \t]*)(\r?\n[ \t]*)\{([ \t]*\r?\n)",
			"$1$2$3",
			System.Text.RegularExpressions.RegexOptions.None);
	}

	/// <summary>Net count of { minus } in a line, skipping chars inside string literals.</summary>
	private static int NetBracesInLine(string line)
	{
		int depth = 0;
		bool inStr = false;
		bool verbatim = false;
		char strQ = '"';
		for (int k = 0; k < line.Length; k++)
		{
			char c = line[k];
			if (inStr)
			{
				if (verbatim)
				{
					// In verbatim strings @"...", "" is the escape sequence for a literal quote
					if (c == '"' && k + 1 < line.Length && line[k + 1] == '"') k++;
					else if (c == '"') inStr = false;
				}
				else
				{
					if (c == '\\') k++;
					else if (c == strQ) inStr = false;
				}
			}
			else if (c == '@' && k + 1 < line.Length && line[k + 1] == '"')
			{
				inStr = true; verbatim = true; strQ = '"'; k++;
			}
			else if (c == '"' || c == '\'') { inStr = true; verbatim = false; strQ = c; }
			else if (c == '{') depth++;
			else if (c == '}') depth--;
		}
		return depth;
	}

	/// <summary>
	/// Returns true when a line is a recognised LoliScript construct (block, command,
	/// flow keyword, or empty). Used to stop inline C# block collection in TakeStep.
	/// </summary>
	private static bool IsLoliScriptLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line)) return true;
		string t = line.TrimStart();
		if (BlockParser.IsBlock(t)) return true;
		if (CommandParser.IsCommand(t)) return true;
		int sp = t.IndexOfAny(new[] { ' ', '\t' });
		string tok = sp >= 0 ? t.Substring(0, sp) : t;
		switch (tok)
		{
			case "IF":   case "ELSE":   case "ENDIF":   case "END":
			case "WHILE": case "ENDWHILE": case "FOREACH": case "ENDFOREACH":
			case "JUMP": case "TRY":   case "CATCH":   case "ENDTRY":
			case "PYTHON":     case "ENDPYTHON":
			case "IRONPYTHON": case "ENDIRONPYTHON":
			case "BEGIN":
				return true;
		}
		if (t.StartsWith("LABEL:", StringComparison.OrdinalIgnoreCase)) return true;
		return false;
	}

	public static int ScanFor(string[] lines, int current, bool downwards, string[] options,
		string[] openerTokens = null, string[] closerTokens = null)
	{
		int num = (downwards ? (current + 1) : (current - 1));
		int depth = 0;
		bool inScript = false; // skip lines inside BEGIN SCRIPT…END SCRIPT so embedded 'if' etc. aren't misread as LoliScript tokens
		bool flag = false;
		while (num >= 0 && num < lines.Count())
		{
			try
			{
				string trimmed = lines[num].TrimStart();

				// Track script block boundaries so inner language keywords are ignored.
				if (downwards)
				{
					if (!inScript && trimmed.StartsWith("BEGIN SCRIPT", StringComparison.OrdinalIgnoreCase))
					{ inScript = true; goto Next; }
					if (inScript)
					{
						if (trimmed.StartsWith("END SCRIPT", StringComparison.OrdinalIgnoreCase))
							inScript = false;
						goto Next;
					}
				}
				else
				{
					if (!inScript && trimmed.StartsWith("END SCRIPT", StringComparison.OrdinalIgnoreCase))
					{ inScript = true; goto Next; }
					if (inScript)
					{
						if (trimmed.StartsWith("BEGIN SCRIPT", StringComparison.OrdinalIgnoreCase))
							inScript = false;
						goto Next;
					}
				}

				{
					string token = LineParser.ParseToken(ref lines[num], TokenType.Parameter, essential: false, proceed: false);
					string upper = token.ToUpper();
					if (openerTokens != null && openerTokens.Any(o => upper == o.ToUpper()))
					{
						depth++;
					}
					else if (depth > 0 && closerTokens != null && closerTokens.Any(c => upper == c.ToUpper()))
					{
						depth--;
					}
					else if (depth == 0 && options.Any(o => upper == o.ToUpper()))
					{
						flag = true;
						break;
					}
				}
			}
			catch
			{
			}
			Next:
			num = ((!downwards) ? (num - 1) : (num + 1));
		}
		if (flag)
		{
			return num;
		}
		throw new Exception("Not found");
	}

	public static bool ParseCheckCondition(ref string cfLine, BotData data)
	{
		string left = LineParser.ParseLiteral(ref cfLine, "STRING");
		Comparer comparer = (Comparer)LineParser.ParseEnum(ref cfLine, "Comparer", typeof(Comparer));
		string right = "";
		if (comparer != Comparer.Exists && comparer != Comparer.DoesNotExist)
		{
			right = LineParser.ParseLiteral(ref cfLine, "STRING");
		}
		return Condition.ReplaceAndVerify(left, comparer, right, data);
	}

	private void RunScript(string script, ScriptingLanguage language, string outputs, BotData data, string jsFilePath = "")
	{
		StringWriter sw = new StringWriter();
		jsFilePath = BlockBase.ReplaceValues(jsFilePath, data);
		if (jsFilePath != string.Empty && File.Exists(jsFilePath))
		{
			script += File.ReadAllText(jsFilePath);
		}
		// Strip OB2 metadata directives that use C-style // comments — invalid in Python/IronPython.
		// // _INPUTS:VAR1,VAR2 and // _OUTPUTS:VAR1 are OB2 annotations; variables are already
		// injected by the preamble / scriptScope.SetVariable, so these lines are safe to remove.
		if (language == ScriptingLanguage.IronPython || language == ScriptingLanguage.Python)
		{
			var stripped = new System.Collections.Generic.List<string>();
			foreach (string ln in script.Split('\n'))
			{
				string t = ln.TrimStart();
				if (t.StartsWith("// _INPUTS:", StringComparison.OrdinalIgnoreCase) ||
				    t.StartsWith("// _OUTPUTS:", StringComparison.OrdinalIgnoreCase) ||
				    t.StartsWith("//_INPUTS:", StringComparison.OrdinalIgnoreCase) ||
				    t.StartsWith("//_OUTPUTS:", StringComparison.OrdinalIgnoreCase))
					stripped.Add("# " + t.Substring(2)); // convert to Python comment
				else
					stripped.Add(ln);
			}
			script = string.Join("\n", stripped);
		}
		List<string> outVarList = new List<string>();
		if (outputs != string.Empty)
		{
			try
			{
				outVarList = (from x in outputs.Split(',')
					select x.Trim()).ToList();
			}
			catch
			{
			}
		}
		DateTime now = DateTime.Now;
		try
		{
			switch (language)
			{
			// ── Embedded: JavaScript (Jint) ──────────────────────────────────
			case ScriptingLanguage.JavaScript:
				InvokeJint(script);
				break;

			// ── Embedded: TypeScript → type-strip → Jint ────────────────────
			case ScriptingLanguage.TypeScript:
				InvokeJint(StripTypeScriptTypes(script));
				break;

			// ── Embedded: IronPython ─────────────────────────────────────────
			case ScriptingLanguage.IronPython:
			{
				// Per-thread engine — no global lock, all bots run in parallel.
				if (_ironPythonEnginePerThread == null)
				{
					_ironPythonEnginePerThread = Python.CreateEngine();
					((PythonCompilerOptions)_ironPythonEnginePerThread.GetCompilerOptions()).Module &= ~ModuleOptions.Optimized;
					foreach (var asm in new[]
					{
						typeof(System.Security.Cryptography.Aes).Assembly,
						typeof(System.Security.Cryptography.SHA256).Assembly,
						typeof(System.Security.Cryptography.HMACSHA256).Assembly,
						typeof(System.Net.Http.HttpClient).Assembly,
						typeof(System.Convert).Assembly,
						typeof(System.Uri).Assembly,
						typeof(System.Text.Encoding).Assembly,
						typeof(System.Text.RegularExpressions.Regex).Assembly,
					}.Distinct())
					{ try { _ironPythonEnginePerThread.Runtime.LoadAssembly(asm); } catch { } }
				}
				ScriptEngine engine = _ironPythonEnginePerThread;
				using var msIpy = new MemoryStream();
				engine.Runtime.IO.SetOutput(msIpy, Encoding.UTF8);
				engine.Runtime.IO.SetErrorOutput(msIpy, Encoding.UTF8);

				ScriptScope scriptScope = engine.CreateScope();
				ScriptSource scriptSource = engine.CreateScriptSourceFromString(script);

				foreach (CVar item in data.Variables.All)
				{
					try { scriptScope.SetVariable(item.Name, item.Value); }
					catch { }
				}
				foreach (CVar slice in data.Data.GetVariables(false))
				{
					try { scriptScope.SetVariable(slice.Name, (string)slice.Value); }
					catch { }
				}

				dynamic val = scriptSource.Execute(scriptScope);
				data.Log(new LogEntry("DEBUG LOG: " + Encoding.UTF8.GetString(msIpy.ToArray()), Colors.White));
				data.Log(new LogEntry($"Parsing {outVarList.Count} variables", Colors.White));
				foreach (string item2 in outVarList)
				{
					try
					{
						dynamic variable = scriptScope.GetVariable(item2);
						bool isPyList = variable is System.Collections.IList && !(variable is string);
						if (variable is string[] || (isPyList && !(variable is string)))
						{
							var lst = new System.Collections.Generic.List<string>();
							foreach (var elem in (System.Collections.IEnumerable)variable) lst.Add(elem?.ToString() ?? string.Empty);
							data.Variables.Set(new CVar(item2, CVar.VarType.List, lst));
						}
						else
						{
							string sval = "";
							try { sval = System.Convert.ToString(variable) ?? ""; } catch { sval = variable?.ToString() ?? ""; }
							data.Variables.Set(new CVar(item2, CVar.VarType.Single, sval));
						}
						data.Log(new LogEntry($"SET VARIABLE {item2} WITH VALUE {variable}", Colors.Yellow));
					}
					catch (Exception __ipyEx) { data.Log(new LogEntry($"COULD NOT FIND VARIABLE {item2}: {__ipyEx.Message}", Colors.Tomato)); }
				}
				if (val != null) data.Log(new LogEntry($"Completion value: {(object)val}", Colors.White));
				break;
			}

			// ── Embedded: C# (Roslyn) ────────────────────────────────────────
			case ScriptingLanguage.CSharp:
				RunCSharpScript();
				break;

			// ── Embedded: Lua (MoonSharp) ────────────────────────────────────
			case ScriptingLanguage.Lua:
				RunLuaScript();
				break;

			// ── External: CPython ────────────────────────────────────────────
			case ScriptingLanguage.Python:
				RunExternal("python", "", ExternalScriptRunner.BuildPythonScript(data.Variables.All, script, outVarList), ".py");
				break;

			// ── External: Node.js ────────────────────────────────────────────
			case ScriptingLanguage.NodeJS:
				RunExternal("node", "", ExternalScriptRunner.BuildNodeScript(data.Variables.All, script, outVarList), ".js");
				break;

			// ── External: PHP ────────────────────────────────────────────────
			case ScriptingLanguage.PHP:
				RunExternal("php", "", ExternalScriptRunner.BuildPhpScript(data.Variables.All, script, outVarList), ".php");
				break;

			// ── External: Ruby ───────────────────────────────────────────────
			case ScriptingLanguage.Ruby:
				RunExternal("ruby", "", ExternalScriptRunner.BuildRubyScript(data.Variables.All, script, outVarList), ".rb");
				break;

			// ── External: Go ─────────────────────────────────────────────────
			case ScriptingLanguage.Go:
				RunExternal("go", "run", ExternalScriptRunner.BuildGoScript(data.Variables.All, script, outVarList), ".go");
				break;

			// ── External: Java (jshell) ──────────────────────────────────────
			case ScriptingLanguage.Java:
				RunExternal("jshell", "--execution local", ExternalScriptRunner.BuildJavaScript(data.Variables.All, script, outVarList), ".jsh");
				break;

			// ── Compiled: C++ (g++) ──────────────────────────────────────────
			case ScriptingLanguage.CPlusPlus:
				RunCompiled(ExternalScriptRunner.BuildCppScript(data.Variables.All, script, outVarList), ".cpp");
				break;

			// ── Compiled: Rust (rustc) ───────────────────────────────────────
			case ScriptingLanguage.Rust:
				RunCompiled(ExternalScriptRunner.BuildRustScript(data.Variables.All, script, outVarList), ".rs");
				break;
			}
			data.Log(new LogEntry($"Execution completed in {(DateTime.Now - now).TotalSeconds} seconds", Colors.GreenYellow));
		}
		catch (Exception ex)
		{
			data.Log(new LogEntry("[ERROR] INFO: " + ex.Message, Colors.White));
		}

		// ── LOCAL HELPERS ────────────────────────────────────────────────────

		void InvokeJint(string jintScript)
		{
			Engine engine2 = new Engine()
				.SetValue("log", new Action<object>(o => sw.WriteLine(o)))
				.SetValue("request", new Func<string, string, string, string>((url, method, body) =>
				{
					using var http = new HttpClient();
					HttpResponseMessage resp = string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
						? http.PostAsync(url, new StringContent(body ?? "", Encoding.UTF8)).GetAwaiter().GetResult()
						: http.GetAsync(url).GetAwaiter().GetResult();
					return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				}));
			foreach (CVar item3 in data.Variables.All)
			{
				try
				{
					if (item3.Type != CVar.VarType.List)
						engine2.SetValue(item3.Name, item3.Value.ToString());
					else
						engine2.SetValue(item3.Name, (item3.Value as List<string>).ToArray());
				}
				catch { }
			}
			engine2.Execute(jintScript);
			data.Log(new LogEntry("DEBUG LOG: " + sw.ToString(), Colors.White));
			data.Log(new LogEntry($"Parsing {outVarList.Count} variables", Colors.White));
			foreach (string item4 in outVarList)
			{
				try
				{
					JsValue value = engine2.Global.GetProperty(item4).Value;
					if (value.IsUndefined() || value.IsNull())
					{
						data.Log(new LogEntry("COULD NOT FIND VARIABLE " + item4, Colors.Tomato));
					}
					else if (value.IsArray())
					{
						var list = new List<string>();
						try
						{
							var obj = value.AsObject();
							uint len = (uint)obj.GetProperty("length").Value.AsNumber();
							for (uint idx = 0; idx < len; idx++)
							{
								try
								{
									var pd = obj.GetProperty(idx.ToString());
									var el = pd?.Value;
									list.Add((el == null || el.IsNull() || el.IsUndefined()) ? "" : el.ToString());
								}
								catch { list.Add(""); }
							}
						}
						catch { }
						data.Variables.Set(new CVar(item4, CVar.VarType.List, list));
						data.Log(new LogEntry("SET VARIABLE " + item4 + " WITH VALUE [array]", Colors.Yellow));
					}
					else
					{
						data.Variables.Set(new CVar(item4, CVar.VarType.Single, value.ToString()));
						data.Log(new LogEntry("SET VARIABLE " + item4 + " WITH VALUE " + value.ToString(), Colors.Yellow));
					}
				}
				catch { data.Log(new LogEntry("COULD NOT FIND VARIABLE " + item4, Colors.Tomato)); }
			}
			if (engine2.GetCompletionValue() != null)
				data.Log(new LogEntry($"Completion value: {engine2.GetCompletionValue()}", Colors.White));
		}

		void RunCSharpScript()
		{
			var options = _csScriptOptions.Value;

			// Hoist any leading `using` directives out of the user script so they
			// appear before the CVar preamble — Roslyn CS1529 requires all using
			// directives to precede other statements/declarations.
			var scriptAllLines = script.Split('\n');
			var hoistedUsings = new List<string>();
			int bodyStart = 0;
			for (int si = 0; si < scriptAllLines.Length; si++)
			{
				string tl = scriptAllLines[si].TrimStart();
				if (tl.StartsWith("using ") || string.IsNullOrWhiteSpace(tl) || tl.StartsWith("//"))
				{
					if (tl.StartsWith("using ")) hoistedUsings.Add(scriptAllLines[si]);
					bodyStart = si + 1;
				}
				else break;
			}
			string scriptBody = string.Join("\n", scriptAllLines.Skip(bodyStart));

			var code = new StringBuilder();
			// Hoisted using directives must precede all other statements
			foreach (string u in hoistedUsings)
				code.AppendLine(u);
			code.AppendLine("var http = new HttpClient();");

			// Build globals — values go into _vars so the SCRIPT TEXT is constant across bots.
			// Constant text → Roslyn compilation cache hits → only the first bot pays compile cost.
			var globals = new CSharpScriptGlobals { data = data };
			var declaredVars = new HashSet<string>(StringComparer.Ordinal);

			foreach (CVar v in data.Variables.All)
			{
				string n = ExternalScriptRunner.SafeName(v.Name);
				if (!declaredVars.Add(n)) continue;
				if (v.Type == CVar.VarType.Single)
				{
					globals._vars[n] = v.Value?.ToString() ?? "";
					code.AppendLine($"var {n} = _vars.ContainsKey(\"{n}\") ? (string)_vars[\"{n}\"] : \"\";");
				}
				else if (v.Type == CVar.VarType.List && v.Value is List<string> lst)
				{
					globals._vars[n] = lst;
					code.AppendLine($"var {n} = _vars.ContainsKey(\"{n}\") ? (List<string>)_vars[\"{n}\"] : new List<string>();");
				}
			}
			foreach (CVar slice in data.Data.GetVariables(false))
			{
				string n = ExternalScriptRunner.SafeName(slice.Name);
				if (!declaredVars.Add(n)) continue;
				globals._vars[n] = slice.Value?.ToString() ?? "";
				code.AppendLine($"var {n} = _vars.ContainsKey(\"{n}\") ? (string)_vars[\"{n}\"] : \"\";");
			}
			code.AppendLine(scriptBody);

			var state = CSharpScript.RunAsync(code.ToString(), options, globals, typeof(CSharpScriptGlobals)).GetAwaiter().GetResult();
			data.Log(new LogEntry($"Parsing {outVarList.Count} variables", Colors.White));
			foreach (string outVar in outVarList)
			{
				try
				{
					string n = ExternalScriptRunner.SafeName(outVar);
					var sv = state.Variables.LastOrDefault(v => v.Name == n);
					if (sv != null)
					{
						if (sv.Value is List<string> strListVal)
						{
							data.Variables.Set(new CVar(outVar, strListVal));
							data.Log(new LogEntry($"SET VARIABLE {outVar} WITH VALUE [list, {strListVal.Count} items]", Colors.Yellow));
						}
						else if (sv.Value is System.Collections.IEnumerable enumVal && !(sv.Value is string))
						{
							var lst = new List<string>();
							foreach (var e in enumVal) lst.Add(e?.ToString() ?? "");
							data.Variables.Set(new CVar(outVar, lst));
							data.Log(new LogEntry($"SET VARIABLE {outVar} WITH VALUE [list, {lst.Count} items]", Colors.Yellow));
						}
						else
						{
							data.Variables.Set(new CVar(outVar, sv.Value?.ToString() ?? ""));
							data.Log(new LogEntry($"SET VARIABLE {outVar} WITH VALUE {sv.Value}", Colors.Yellow));
						}
					}
					else
						data.Log(new LogEntry("COULD NOT FIND VARIABLE " + outVar, Colors.Tomato));
				}
				catch { data.Log(new LogEntry("COULD NOT FIND VARIABLE " + outVar, Colors.Tomato)); }
			}
		}

		void RunLuaScript()
		{
			var lua = new MoonSharp.Interpreter.Script(CoreModules.Preset_HardSandbox | CoreModules.String | CoreModules.Table | CoreModules.Math | CoreModules.Metatables);
			foreach (CVar v in data.Variables.All)
			{
				string n = ExternalScriptRunner.SafeName(v.Name);
				if (v.Type == CVar.VarType.Single)
					lua.Globals.Set(n, DynValue.NewString(v.Value?.ToString() ?? ""));
				else if (v.Type == CVar.VarType.List && v.Value is List<string> lst)
				{
					var tbl = new Table(lua);
					for (int ti = 0; ti < lst.Count; ti++) tbl.Set(ti + 1, DynValue.NewString(lst[ti]));
					lua.Globals.Set(n, DynValue.NewTable(tbl));
				}
			}
			lua.Globals.Set("log", DynValue.FromObject(lua,
				(Action<string>)(msg => data.Log(new LogEntry(msg, Colors.White)))));
			lua.Globals.Set("request", DynValue.FromObject(lua,
				new Func<string, string, string, string>((url, method, body) =>
				{
					using var http = new HttpClient();
					HttpResponseMessage resp = string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
						? http.PostAsync(url, new StringContent(body ?? "", Encoding.UTF8)).GetAwaiter().GetResult()
						: http.GetAsync(url).GetAwaiter().GetResult();
					return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				})));
			lua.DoString(script);
			data.Log(new LogEntry($"Parsing {outVarList.Count} variables", Colors.White));
			foreach (string outVar in outVarList)
			{
				try
				{
					string n = ExternalScriptRunner.SafeName(outVar);
					DynValue val = lua.Globals.Get(n);
					if (val != null && val.Type != DataType.Nil)
					{
						if (val.Type == DataType.Table)
						{
							var list = new List<string>();
							foreach (var pair in val.Table.Pairs)
								list.Add(pair.Value.Type == DataType.String ? pair.Value.String : pair.Value.ToString());
							data.Variables.Set(new CVar(outVar, list));
						}
						else
							data.Variables.Set(new CVar(outVar, val.CastToString() ?? ""));
						data.Log(new LogEntry($"SET VARIABLE {outVar} WITH VALUE {val}", Colors.Yellow));
					}
					else
						data.Log(new LogEntry("COULD NOT FIND VARIABLE " + outVar, Colors.Tomato));
				}
				catch { data.Log(new LogEntry("COULD NOT FIND VARIABLE " + outVar, Colors.Tomato)); }
			}
		}

		void RunExternal(string command, string cmdArgs, string scriptContent, string extension)
		{
			string stdout = ExternalScriptRunner.RunScript(command, cmdArgs, scriptContent, extension);
			if (!string.IsNullOrEmpty(ExternalScriptRunner.LastStderr))
				data.Log(new LogEntry("STDERR: " + ExternalScriptRunner.LastStderr, Colors.Tomato));
			data.Log(new LogEntry("STDOUT: " + stdout, Colors.White));
			var outputMap = ExternalScriptRunner.ParseOutput(stdout);
			data.Log(new LogEntry($"Parsing {outVarList.Count} variables", Colors.White));
			foreach (string outVar in outVarList)
			{
				if (outputMap.TryGetValue(outVar, out var val))
				{
					if (val is JArray arr)
						data.Variables.Set(new CVar(outVar, arr.Select(t => t.ToString()).ToList()));
					else
						data.Variables.Set(new CVar(outVar, val?.ToString() ?? ""));
					data.Log(new LogEntry($"SET VARIABLE {outVar} WITH VALUE {val}", Colors.Yellow));
				}
				else
					data.Log(new LogEntry("COULD NOT FIND VARIABLE " + outVar, Colors.Tomato));
			}
		}

		void RunCompiled(string scriptContent, string extension)
		{
			string compiler     = extension == ".cpp" ? "g++"   : "rustc";
			string compilerArgs = extension == ".cpp" ? "-o {EXE} {SRC} -std=c++17" : "-o {EXE} {SRC}";
			string stdout = ExternalScriptRunner.RunCompiledScript(compiler, compilerArgs, scriptContent, extension);
			if (!string.IsNullOrEmpty(ExternalScriptRunner.LastStderr))
				data.Log(new LogEntry("STDERR: " + ExternalScriptRunner.LastStderr, Colors.Tomato));
			var outputMap = ExternalScriptRunner.ParseOutput(stdout);
			foreach (string outVar in outVarList)
			{
				if (outputMap.TryGetValue(outVar, out var val))
				{
					data.Variables.Set(new CVar(outVar, val?.ToString() ?? ""));
					data.Log(new LogEntry($"SET VARIABLE {outVar} WITH VALUE {val}", Colors.Yellow));
				}
				else
					data.Log(new LogEntry("COULD NOT FIND VARIABLE " + outVar, Colors.Tomato));
			}
		}
	}

	// Removes TypeScript-specific syntax so the result can run through Jint.
	private static string StripTypeScriptTypes(string ts)
	{
		// Remove interface declarations
		ts = Regex.Replace(ts, @"\binterface\s+\w+\s*(\s+extends[^{]*)?\{[^}]*\}", "", RegexOptions.Singleline);
		// Remove type aliases
		ts = Regex.Replace(ts, @"\btype\s+\w+\s*(<[^>]*>)?\s*=\s*[^;]+;", "");
		// Remove 'as Type' casts
		ts = Regex.Replace(ts, @"\s+as\s+[\w<>\[\]|&?,\s.(){}]+(?=[\s;,)}\]])", "");
		// Remove variable/param type annotations ': TypeName' — only when preceded by an identifier
		// (variable/param name), not by an expression (which would be a ternary colon).
		// The negative lookbehind (?<!\s) ensures we don't strip ternary `: value` branches.
		ts = Regex.Replace(ts, @"(?<=\w):\s*[\w<>\[\]|&?.,()\s{}]+(?=\s*[=,;)\n{])", "");
		// Remove generic type params from calls: func<Type>(...)
		ts = Regex.Replace(ts, @"<[A-Z]\w*(?:<[^>]*>)?>\s*(?=\()", "");
		// Remove OOP access modifiers
		ts = Regex.Replace(ts, @"\b(public|private|protected|readonly|abstract|override)\s+", "");
		return ts;
	}
}
