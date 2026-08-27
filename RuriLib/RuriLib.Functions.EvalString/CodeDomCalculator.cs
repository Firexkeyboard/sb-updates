using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace RuriLib.Functions.EvalString;

public class CodeDomCalculator
{
	private ArrayList _mathMembers = new ArrayList();

	private Hashtable _mathMembersMap = new Hashtable();

	private StringBuilder _source = new StringBuilder();

	public string Eval { get; set; }

	public CodeDomCalculator()
	{
		GetMathMemberNames();
	}

	public CodeDomCalculator(string eval)
		: this()
	{
		Eval = eval;
	}

#pragma warning disable CS0618 // CSharpCodeProvider.CreateCompiler() is obsolete but no replacement exists for runtime expression eval
	private ICodeCompiler CreateCompiler()
	{
		return new CSharpCodeProvider().CreateCompiler();
	}
#pragma warning restore CS0618

	private CompilerParameters CreateCompilerParameters()
	{
		return new CompilerParameters
		{
			CompilerOptions = "/target:library /optimize",
			GenerateExecutable = false,
			GenerateInMemory = true,
			IncludeDebugInformation = false,
			ReferencedAssemblies = { "mscorlib.dll", "System.dll", "System.Windows.Forms.dll" }
		};
	}

	private void WriteLine(string txt, params object[] args)
	{
		Console.WriteLine(string.Format(txt, args) + "\r\n");
	}

	private CompilerResults CompileCode(ICodeCompiler compiler, CompilerParameters parms, string source)
	{
		CompilerResults compilerResults = compiler.CompileAssemblyFromSource(parms, source);
		if (compilerResults.Errors.Count > 0)
		{
			foreach (CompilerError error in compilerResults.Errors)
			{
				WriteLine("Compile Error:" + error.ErrorText);
			}
			return null;
		}
		return compilerResults;
	}

	private string RefineEvaluationString(string eval)
	{
		Regex regex = new Regex("[a-zA-Z_]+");
		ArrayList replacelist = new ArrayList();
		regex.Matches(eval).Cast<Match>().ToList()
			.ForEach(delegate(Match m)
			{
				bool flag = _mathMembersMap[m.Value.ToUpper()] != null;
				if (!replacelist.Contains(m.Value) && flag)
				{
					eval = eval.Replace(m.Value, "Math." + _mathMembersMap[m.Value.ToUpper()]);
				}
				replacelist.Add(m.Value);
			});
		return eval;
	}

	private CompilerResults CompileAssembly()
	{
		ICodeCompiler compiler = CreateCompiler();
		CompilerParameters parms = CreateCompilerParameters();
		return CompileCode(compiler, parms, _source.ToString());
	}

	public object Calculate()
	{
		string expression = RefineEvaluationString(Eval);
		BuildClass(expression);
		CompilerResults compilerResults = CompileAssembly();
		if (compilerResults != null && compilerResults.CompiledAssembly != null)
		{
			return RunCode(compilerResults);
		}
		return string.Empty;
	}

	private void GetMathMemberNames()
	{
		Assembly assembly = Assembly.GetAssembly(typeof(Math));
		try
		{
			if (!(assembly != null))
			{
				return;
			}
			Type[] types = assembly.GetModules(getResourceModules: false).First().GetTypes();
			foreach (Type type in types)
			{
				if (type.Name == "Math")
				{
					MemberInfo[] members = type.GetMembers();
					foreach (MemberInfo memberInfo in members)
					{
						_mathMembers.Add(memberInfo.Name);
						_mathMembersMap[memberInfo.Name.ToUpper()] = memberInfo.Name;
					}
				}
			}
		}
		catch (Exception arg)
		{
			Console.WriteLine("Error:  An exception occurred while executing the script", arg);
		}
	}

	private object RunCode(CompilerResults results)
	{
		Assembly compiledAssembly = results.CompiledAssembly;
		try
		{
			if (compiledAssembly != null)
			{
				object obj = compiledAssembly.CreateInstance("ExpressionEvaluator.Calculator");
				Type[] types = compiledAssembly.GetModules(getResourceModules: false).First().GetTypes();
				for (int i = 0; i < types.Length; i++)
				{
					MethodInfo methodInfo = types[i].GetMethods().FirstOrDefault((MethodInfo m) => m.Name == "Calculate");
					if (!(methodInfo == null))
					{
						return methodInfo.Invoke(obj, null);
					}
				}
			}
			return string.Empty;
		}
		catch (Exception arg)
		{
			Console.WriteLine("Error:  An exception occurred while executing the script", arg);
			return string.Empty;
		}
	}

	private CodeMemberField FieldVariable(string fieldName, string typeName, MemberAttributes accessLevel)
	{
		return new CodeMemberField(typeName, fieldName)
		{
			Attributes = accessLevel
		};
	}

	private CodeMemberField FieldVariable(string fieldName, Type type, MemberAttributes accessLevel)
	{
		return new CodeMemberField(type, fieldName)
		{
			Attributes = accessLevel
		};
	}

	private CodeMemberProperty MakeProperty(string propertyName, string internalName, Type type)
	{
		return new CodeMemberProperty
		{
			Name = propertyName,
			Comments = 
			{
				new CodeCommentStatement($"The {propertyName} property is the returned result")
			},
			Attributes = MemberAttributes.Public,
			Type = new CodeTypeReference(type),
			HasGet = true,
			GetStatements = { (CodeStatement)new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), internalName)) },
			HasSet = true,
			SetStatements = { (CodeStatement)new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), internalName), new CodePropertySetValueReferenceExpression()) }
		};
	}

	private void BuildClass(string expression)
	{
		_source = new StringBuilder();
		StringWriter stringWriter = new StringWriter(_source);
		ICodeGenerator codeGenerator = new CSharpCodeProvider().CreateGenerator(stringWriter);
		CodeGeneratorOptions o = new CodeGeneratorOptions();
		CodeNamespace codeNamespace = new CodeNamespace("ExpressionEvaluator")
		{
			Imports = 
			{
				new CodeNamespaceImport("System"),
				new CodeNamespaceImport("System.Windows.Forms")
			}
		};
		CodeTypeDeclaration codeTypeDeclaration = new CodeTypeDeclaration
		{
			IsClass = true,
			Name = "Calculator",
			Attributes = MemberAttributes.Public,
			Members = { (CodeTypeMember)FieldVariable("answer", typeof(double), MemberAttributes.Private) }
		};
		CodeConstructor value = new CodeConstructor
		{
			Attributes = MemberAttributes.Public,
			Comments = 
			{
				new CodeCommentStatement("Default Constructor for class", docComment: true)
			},
			Statements = { (CodeStatement)new CodeSnippetStatement("//TODO: implement default constructor") }
		};
		codeTypeDeclaration.Members.Add(value);
		codeTypeDeclaration.Members.Add(MakeProperty("Answer", "answer", typeof(double)));
		CodeMemberMethod value2 = new CodeMemberMethod
		{
			Name = "Calculate",
			ReturnType = new CodeTypeReference(typeof(double)),
			Comments = 
			{
				new CodeCommentStatement("Calculate an expression", docComment: true)
			},
			Attributes = MemberAttributes.Public,
			Statements = 
			{
				(CodeStatement)new CodeAssignStatement(new CodeSnippetExpression("Answer"), new CodeSnippetExpression(expression)),
				(CodeStatement)new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "Answer"))
			}
		};
		codeTypeDeclaration.Members.Add(value2);
		codeNamespace.Types.Add(codeTypeDeclaration);
		codeGenerator.GenerateCodeFromNamespace(codeNamespace, stringWriter, o);
		stringWriter.Flush();
		stringWriter.Close();
	}
}
