using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var root=args.Length>0?args[0]:"Assets";
var files=Directory.GetFiles(root,"*.cs",SearchOption.AllDirectories);
int errors=0;
foreach(var file in files)
{
    var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(file),new CSharpParseOptions(LanguageVersion.CSharp9,preprocessorSymbols:new[]{"UNITY_EDITOR"}),file);
    foreach(var diagnostic in tree.GetDiagnostics().Where(d=>d.Severity==DiagnosticSeverity.Error)){Console.Error.WriteLine(diagnostic);errors++;}
}
Console.WriteLine($"C# 9 syntax only: {files.Length} files, {errors} errors (not a Unity compilation).");
return errors>0?1:0;
