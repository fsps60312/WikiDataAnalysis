using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace WikiDataAnalysis
{
    static class Utils
    {
        public static void Swap<T>(ref T a,ref T b)
        {
            T c = a;
            a = b;
            b = c;
        }
        static int counter = 0;
        public static void Resize<T>(this List<T>s,int size,T v)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine($"Resize({size},{v}) #{++counter}...");
                while (s.Count < size)
                {
                    s.Add(v);
                }
                if (s.Count > size) s.RemoveRange(size, s.Count - size);
            }
            finally { Trace.Write("Done"); Trace.Unindent(); }
        }
        public static class DynamicCompile
        {
            public static MethodInfo GetMethod(
                string code,
                string nameSpaceName/*DynamicCodeGenerate*/,
                string className/*HelloWorld*/,
                string methodName/*OutPut*/,
                params string[] referencedAssemblies)
            {
                try
                {
                    Trace.Indent();
                    Trace.WriteLine("Compiling...");
                    // 1.CSharpCodePrivoder
                    CSharpCodeProvider objCSharpCodePrivoder = new CSharpCodeProvider();

                    //// 2.ICodeComplier
                    //ICodeCompiler objICodeCompiler = objCSharpCodePrivoder.CreateCompiler();

                    // 3.CompilerParameters
                    CompilerParameters objCompilerParameters = new CompilerParameters();
                    //objCompilerParameters.ReferencedAssemblies.Add("System.dll");
                    foreach (var r in referencedAssemblies) objCompilerParameters.ReferencedAssemblies.Add($"{r}.dll");
                    objCompilerParameters.GenerateExecutable = false;
                    objCompilerParameters.GenerateInMemory = true;

                    // 4.CompilerResults
                    CompilerResults cr = objCSharpCodePrivoder.CompileAssemblyFromSource(objCompilerParameters, code);
                    Trace.Write("OK");
                    if (cr.Errors.HasErrors)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Compile Error:");
                        foreach (CompilerError err in cr.Errors)
                        {
                            sb.AppendLine(err.ErrorText);
                        }
                        throw new Exception(sb.ToString());
                    }
                    else
                    {
                        // 通過反射，呼叫HelloWorld的例項
                        Assembly objAssembly = cr.CompiledAssembly;
                        var objType = objAssembly.GetType($"{nameSpaceName}.{className}", true, false);
                        Trace.WriteLine($"objType: {objType}");
                        Trace.Assert(objType != null);
                        var objMethod= objType.GetMethod(methodName);
                        Trace.Write($"\tmethod: {objMethod}");
                        Trace.Assert(objMethod != null);
                        return objMethod;
                    }
                }
                finally { Trace.Unindent(); }
            }
            static string GenerateCode()
            {
                return
                    "using System;" +
                    "namespace DynamicCodeGenerate" +
                    "{" +
                    "    public class HelloWorld" +
                    "    {" +
                    "        public string OutPut()" +
                    "        {" +
                    "             return \"Hello world!\";" +
                    "        }" +
                    "    }" +
                    "}";
            }
        }
    }
}
