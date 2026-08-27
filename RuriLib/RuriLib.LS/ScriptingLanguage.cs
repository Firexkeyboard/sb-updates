namespace RuriLib.LS;

public enum ScriptingLanguage
{
    // Embedded engines (always available, no external runtime needed)
    JavaScript,   // Jint engine
    IronPython,   // IronPython 3 (.NET Python implementation)
    CSharp,       // Roslyn C# scripting
    TypeScript,   // TypeScript via type-stripping + Jint
    Lua,          // MoonSharp Lua engine

    // External process engines (require the runtime to be installed and in PATH)
    Python,       // CPython (python / python3)
    NodeJS,       // Node.js (node)
    PHP,          // PHP interpreter (php)
    Ruby,         // Ruby interpreter (ruby)
    Go,           // Go compiler (go run)
    Java,         // Java jshell (jshell)
    CPlusPlus,    // C++ compiler (g++ / clang++)
    Rust          // Rust compiler (rustc)
}
