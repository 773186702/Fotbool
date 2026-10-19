using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fotbool.Verification
{
    /// <summary>
    /// Parses every C# file under a Unity project's Assets folder with the real Roslyn parser and
    /// reports syntax errors. It also dumps the declared namespace/type/member surface as JSON so a
    /// separate contract check can verify the API other files depend on.
    /// This tool never references UnityEngine: it validates syntax and declarations, not engine semantics.
    /// </summary>
    internal static class Program
    {
        private const int ExitUsage = 2;
        private static readonly string[] IgnoredDirectoryNames =
            { "Library", "Temp", "obj", "bin", "Logs", "UserSettings", ".git" };

        /// <summary>
        /// Contracts are hand written in camelCase, so the dump uses the same policy and both files can be
        /// diffed by eye. The top level members of the anonymous dump object are declared camelCase already.
        /// </summary>
        private static readonly JsonSerializerOptions DumpJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static int Main(string[] args)
        {
            string assetsRoot = null;
            string dumpPath = null;
            string reportPath = null;
            string projectRoot = null;
            string contractsPath = null;
            bool integrityOnly = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--assets": assetsRoot = NextValue(args, ref i); break;
                    case "--dump": dumpPath = NextValue(args, ref i); break;
                    case "--report": reportPath = NextValue(args, ref i); break;
                    case "--project": projectRoot = NextValue(args, ref i); break;
                    case "--contracts": contractsPath = NextValue(args, ref i); break;
                    case "--integrity-only": integrityOnly = true; break;
                    case "--help":
                    case "-h":
                        PrintUsage();
                        return 0;
                    default:
                        Console.Error.WriteLine("Unknown argument: " + args[i]);
                        PrintUsage();
                        return ExitUsage;
                }
            }

            if (string.IsNullOrWhiteSpace(projectRoot))
                projectRoot = Directory.GetCurrentDirectory();
            projectRoot = Path.GetFullPath(projectRoot);

            if (string.IsNullOrWhiteSpace(assetsRoot))
            {
                assetsRoot = Path.Combine(projectRoot, "Assets");
                if (!integrityOnly) Console.WriteLine("No --assets given; defaulting to " + assetsRoot);
            }

            if (!Directory.Exists(assetsRoot))
            {
                Console.Error.WriteLine("Assets folder not found: " + assetsRoot);
                return ExitUsage;
            }

            assetsRoot = Path.GetFullPath(assetsRoot);
            List<string> files = EnumerateSources(assetsRoot);
            if (files.Count == 0 && !integrityOnly)
            {
                Console.Error.WriteLine("No C# files found under " + assetsRoot);
                return ExitUsage;
            }

            var report = new StringBuilder();
            report.AppendLine("Fotbool C# syntax verification (Roslyn " + typeof(CSharpSyntaxTree).Assembly.GetName().Version + ")");
            report.AppendLine("Assets root: " + assetsRoot);
            report.AppendLine("Files: " + files.Count.ToString(CultureInfo.InvariantCulture));

            int errorCount = 0;
            int warningCount = 0;
            int lineCount = 0;
            var diagnostics = new List<string>();
            var fileRecords = new List<FileRecord>();
            var types = new Dictionary<string, TypeRecord>(StringComparer.Ordinal);
            var duplicateTypes = new List<string>();

            if (!integrityOnly)
            {
                foreach (string file in files)
                {
                    string text = File.ReadAllText(file, Encoding.UTF8);
                    string relative = RelativePath(assetsRoot, file);
                    int lines = CountLines(text);
                    lineCount += lines;

                    SyntaxTree tree = CSharpSyntaxTree.ParseText(
                        SourceText.From(text, Encoding.UTF8),
                        new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse),
                        file);

                    foreach (Diagnostic diagnostic in tree.GetDiagnostics())
                    {
                        if (diagnostic.Severity == DiagnosticSeverity.Error)
                        {
                            errorCount++;
                            diagnostics.Add("ERROR " + relative + " " + diagnostic.Id + ": " + diagnostic.GetMessage(CultureInfo.InvariantCulture));
                        }
                        else if (diagnostic.Severity == DiagnosticSeverity.Warning)
                        {
                            warningCount++;
                            diagnostics.Add("WARN  " + relative + " " + diagnostic.Id + ": " + diagnostic.GetMessage(CultureInfo.InvariantCulture));
                        }
                    }

                    fileRecords.Add(new FileRecord { Path = relative, Lines = lines, Sha256 = Sha256(text) });
                    CollectTypes(tree, relative, types, duplicateTypes);
                }

                errorCount += AppendSummary(report, lineCount, types.Count, errorCount, warningCount, diagnostics, duplicateTypes);
            }

            if (!integrityOnly || !string.IsNullOrWhiteSpace(contractsPath)) WriteDumpForContracts(dumpPath, projectRoot, assetsRoot, fileRecords, lineCount, types);
            if (!string.IsNullOrWhiteSpace(contractsPath)) errorCount += ContractCheck.Run(contractsPath, DumpPathFor(dumpPath, projectRoot), report);
            errorCount += UnityIntegrity.Run(projectRoot, report);

            string summary = report.ToString();
            Console.WriteLine(summary);
            if (!string.IsNullOrWhiteSpace(reportPath)) WriteText(reportPath, summary);

            Console.WriteLine(errorCount == 0 ? "VERIFICATION PASSED" : "VERIFICATION FAILED (" + errorCount + " problem(s))");
            return errorCount == 0 ? 0 : 1;
        }

        /// <summary>Contracts read the API dump, so make sure it exists on disk before they run.</summary>
        private static void WriteDumpForContracts(string dumpPath, string projectRoot, string assetsRoot,
            List<FileRecord> fileRecords, int lineCount, Dictionary<string, TypeRecord> types)
        {
            if (fileRecords.Count == 0) return;
            WriteDump(DumpPathFor(dumpPath, projectRoot), assetsRoot, fileRecords, lineCount, types);
        }

        private static string DumpPathFor(string dumpPath, string projectRoot)
        {
            return string.IsNullOrWhiteSpace(dumpPath)
                ? Path.Combine(projectRoot, "Tools", "Verification", "artifacts", "api-dump.json")
                : dumpPath;
        }

        private static int AppendSummary(StringBuilder report, int lineCount, int typeCount, int errorCount,
            int warningCount, List<string> diagnostics, List<string> duplicateTypes)
        {
            report.AppendLine("Lines: " + lineCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("Declared types: " + typeCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("Syntax errors: " + errorCount.ToString(CultureInfo.InvariantCulture) +
                              "  Warnings: " + warningCount.ToString(CultureInfo.InvariantCulture));
            foreach (string diagnostic in diagnostics) report.AppendLine("  " + diagnostic);
            foreach (string duplicate in duplicateTypes)
                report.AppendLine("  ERROR duplicate type declaration: " + duplicate);

            int total = errorCount + duplicateTypes.Count;
            report.AppendLine(total == 0 ? "[PASS] all files parse as valid C#" : "[FAIL] syntax errors found");
            return duplicateTypes.Count;
        }

        private static void WriteDump(string dumpPath, string assetsRoot, List<FileRecord> fileRecords, int lineCount,
            Dictionary<string, TypeRecord> types)
        {
            WriteText(dumpPath, JsonSerializer.Serialize(new
            {
                generatedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                assetsRoot,
                fileCount = fileRecords.Count,
                lineCount,
                files = fileRecords,
                types = types.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToDictionary(pair => pair.Key, pair => pair.Value)
            }, DumpJsonOptions));
            Console.WriteLine("API dump written to " + Path.GetFullPath(dumpPath));
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: FotboolSyntaxCheck [--project <root>] [--assets <Assets folder>] [--contracts <contracts.json>]");
            Console.WriteLine("                          [--dump <api.json>] [--report <report.txt>] [--integrity-only]");
            Console.WriteLine("  --project        Unity project root used for Unity-side integrity checks (default: cwd)");
            Console.WriteLine("  --assets         Unity Assets folder scanned recursively (default: <project>\\Assets)");
            Console.WriteLine("  --contracts      JSON file listing the public API other files depend on");
            Console.WriteLine("  --dump           write the declared namespace/type/member surface as JSON");
            Console.WriteLine("  --report         write the human-readable report to a file");
            Console.WriteLine("  --integrity-only skip the Roslyn syntax pass and only verify Unity asset integrity + contracts");
        }

        private static string NextValue(string[] args, ref int index)
        {
            if (index + 1 >= args.Length) throw new ArgumentException("Missing value after " + args[index]);
            index++;
            return args[index];
        }

        private static List<string> EnumerateSources(string root)
        {
            var results = new List<string>();
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                string directory = pending.Pop();
                foreach (string sub in Directory.GetDirectories(directory))
                {
                    string name = Path.GetFileName(sub);
                    if (IgnoredDirectoryNames.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                    if (name.StartsWith(".")) continue;
                    pending.Push(sub);
                }
                foreach (string file in Directory.GetFiles(directory, "*.cs"))
                {
                    if (Path.GetFileName(file).StartsWith(".")) continue;
                    results.Add(file);
                }
            }
            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        private static void CollectTypes(SyntaxTree tree, string relativePath, Dictionary<string, TypeRecord> types,
            List<string> duplicateTypes)
        {
            SyntaxNode root = tree.GetRoot();
            foreach (BaseTypeDeclarationSyntax declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                string name = declaration.Identifier.ValueText;
                string namespaceName = NamespaceOf(declaration);
                string fullName = string.IsNullOrEmpty(namespaceName) ? name : namespaceName + "." + name;
                int lineStart = LineOf(declaration);

                if (types.TryGetValue(fullName, out TypeRecord existing))
                {
                    if (!string.Equals(existing.File, relativePath, StringComparison.OrdinalIgnoreCase) || existing.LineStart != lineStart)
                        duplicateTypes.Add(fullName + " (" + relativePath + ":" + lineStart.ToString(CultureInfo.InvariantCulture) + ")");
                    continue;
                }

                var record = new TypeRecord
                {
                    Kind = KindOf(declaration),
                    Namespace = namespaceName,
                    Name = name,
                    File = relativePath,
                    LineStart = lineStart,
                    Abstract = declaration.Modifiers.Any(SyntaxKind.AbstractKeyword),
                    Sealed = declaration.Modifiers.Any(SyntaxKind.SealedKeyword),
                    Static = declaration.Modifiers.Any(SyntaxKind.StaticKeyword),
                    BaseTypes = BaseTypesOf(declaration),
                    Members = new Dictionary<string, MemberRecord>(StringComparer.Ordinal)
                };
                AddMembers(declaration, record.Members);
                types[fullName] = record;
            }
        }

        private static void AddMembers(BaseTypeDeclarationSyntax declaration, Dictionary<string, MemberRecord> members)
        {
            if (declaration is EnumDeclarationSyntax enumDeclaration)
            {
                foreach (EnumMemberDeclarationSyntax enumMember in enumDeclaration.Members)
                    Record(members, enumMember.Identifier.ValueText, "enumMember", "public", 0, null);
                return;
            }

            if (!(declaration is TypeDeclarationSyntax typeDeclaration)) return;
            foreach (MemberDeclarationSyntax member in typeDeclaration.Members)
            {
                switch (member)
                {
                    case MethodDeclarationSyntax method:
                        Record(members, method.Identifier.ValueText, "method", ModifiersOf(method.Modifiers),
                            method.ParameterList.Parameters.Count, method.ReturnType.ToString());
                        break;
                    case ConstructorDeclarationSyntax constructor:
                        Record(members, ".ctor", "constructor", ModifiersOf(constructor.Modifiers),
                            constructor.ParameterList.Parameters.Count, null);
                        break;
                    case PropertyDeclarationSyntax property:
                        Record(members, property.Identifier.ValueText, "property", ModifiersOf(property.Modifiers),
                            0, property.Type.ToString());
                        break;
                    case EventFieldDeclarationSyntax eventField:
                        foreach (VariableDeclaratorSyntax variable in eventField.Declaration.Variables)
                            Record(members, variable.Identifier.ValueText, "event", ModifiersOf(eventField.Modifiers),
                                0, eventField.Declaration.Type.ToString());
                        break;
                    case FieldDeclarationSyntax field:
                        foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                            Record(members, variable.Identifier.ValueText, "field", ModifiersOf(field.Modifiers),
                                0, field.Declaration.Type.ToString());
                        break;
                    case DelegateDeclarationSyntax delegateDeclaration:
                        Record(members, delegateDeclaration.Identifier.ValueText, "delegate", ModifiersOf(delegateDeclaration.Modifiers),
                            delegateDeclaration.ParameterList.Parameters.Count, delegateDeclaration.ReturnType.ToString());
                        break;
                }
            }
        }

        private static void Record(Dictionary<string, MemberRecord> members, string name, string kind,
            string visibility, int parameterCount, string typeName)
        {
            if (members.TryGetValue(name, out MemberRecord existing))
            {
                existing.Overloads++;
                if (existing.Visibility == "private" && visibility != "private") existing.Visibility = visibility;
                return;
            }
            members[name] = new MemberRecord
            {
                Kind = kind,
                Visibility = visibility,
                Parameters = parameterCount,
                Type = typeName ?? string.Empty,
                Overloads = 1
            };
        }

        private static string ModifiersOf(SyntaxTokenList modifiers)
        {
            if (modifiers.Any(SyntaxKind.PublicKeyword)) return "public";
            if (modifiers.Any(SyntaxKind.ProtectedKeyword) && modifiers.Any(SyntaxKind.InternalKeyword)) return "protected internal";
            if (modifiers.Any(SyntaxKind.ProtectedKeyword)) return "protected";
            if (modifiers.Any(SyntaxKind.InternalKeyword)) return "internal";
            return "private";
        }

        private static string NamespaceOf(SyntaxNode node)
        {
            var names = new List<string>();
            for (SyntaxNode current = node.Parent; current != null; current = current.Parent)
                if (current is BaseNamespaceDeclarationSyntax namespaceDeclaration)
                    names.Insert(0, namespaceDeclaration.Name.ToString());
            return string.Join(".", names);
        }

        private static string KindOf(BaseTypeDeclarationSyntax declaration)
        {
            if (declaration is EnumDeclarationSyntax) return "enum";
            if (declaration is InterfaceDeclarationSyntax) return "interface";
            if (declaration is StructDeclarationSyntax) return "struct";
            if (declaration is RecordDeclarationSyntax) return "record";
            if (declaration is ClassDeclarationSyntax)
                return declaration.Modifiers.Any(SyntaxKind.StaticKeyword) ? "static class" : "class";
            return "unknown";
        }

        private static List<string> BaseTypesOf(BaseTypeDeclarationSyntax declaration)
        {
            var bases = new List<string>();
            BaseListSyntax baseList = null;
            switch (declaration)
            {
                case ClassDeclarationSyntax classDeclaration: baseList = classDeclaration.BaseList; break;
                case StructDeclarationSyntax structDeclaration: baseList = structDeclaration.BaseList; break;
                case InterfaceDeclarationSyntax interfaceDeclaration: baseList = interfaceDeclaration.BaseList; break;
                case RecordDeclarationSyntax recordDeclaration: baseList = recordDeclaration.BaseList; break;
            }
            if (baseList != null)
                foreach (BaseTypeSyntax type in baseList.Types) bases.Add(type.Type.ToString());
            return bases;
        }

        private static int LineOf(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        private static int CountLines(string text)
        {
            int lines = 1;
            foreach (char character in text) if (character == '\n') lines++;
            return lines;
        }

        private static string Sha256(string text)
        {
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
                return Convert.ToHexString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
        }

        private static string RelativePath(string root, string file)
        {
            string normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(file);
            return full.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
                ? full.Substring(normalizedRoot.Length).Replace(Path.DirectorySeparatorChar, '/')
                : full.Replace(Path.DirectorySeparatorChar, '/');
        }

        private static void WriteText(string path, string content)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private sealed class FileRecord
        {
            public string Path { get; set; }
            public int Lines { get; set; }
            public string Sha256 { get; set; }
        }

        private sealed class TypeRecord
        {
            public string Kind { get; set; }
            public string Namespace { get; set; }
            public string Name { get; set; }
            public string File { get; set; }
            public int LineStart { get; set; }
            public bool Abstract { get; set; }
            public bool Sealed { get; set; }
            public bool Static { get; set; }
            public List<string> BaseTypes { get; set; }
            public Dictionary<string, MemberRecord> Members { get; set; }
        }

        private sealed class MemberRecord
        {
            public string Kind { get; set; }
            public string Visibility { get; set; }
            public int Parameters { get; set; }
            public string Type { get; set; }
            public int Overloads { get; set; }
        }
    }
}