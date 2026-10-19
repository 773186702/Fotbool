using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Fotbool.Verification
{
    /// <summary>
    /// Structural checks for the Unity side of the repository. Unity stores asset identity in
    /// <c>.meta</c> files, so a missing, duplicated or mismatched guid silently breaks script, scene and
    /// sprite references at import time. These checks run without Unity so they can gate every change.
    /// </summary>
    internal static class UnityIntegrity
    {
        private static readonly Regex GuidRegex = new Regex(@"guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);
        private static readonly Regex MetaGuidRegex = new Regex(@"^guid:\s*([0-9a-fA-F]{32})\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex ScriptReferenceRegex =
            new Regex(@"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);
        private static readonly string[] IgnoredNames = { "Library", "Temp", "obj", "bin", "Logs", "UserSettings", ".git", ".vs" };
        private static readonly string[] RequiredPackages = { "Packages/manifest.json" };
        private static readonly string[] RecommendedProjectSettings =
        {
            "ProjectSettings/ProjectVersion.txt",
            "ProjectSettings/ProjectSettings.asset",
            "ProjectSettings/GraphicsSettings.asset",
            "ProjectSettings/QualitySettings.asset",
            "ProjectSettings/EditorBuildSettings.asset",
            "ProjectSettings/TagManager.asset",
            "ProjectSettings/DynamicsManager.asset",
            "ProjectSettings/InputManager.asset",
            "ProjectSettings/TimeManager.asset",
            "ProjectSettings/Physics2DSettings.asset"
        };

        public static int Run(string projectRoot, StringBuilder report)
        {
            projectRoot = Path.GetFullPath(projectRoot);
            report.AppendLine("Unity project: " + projectRoot);

            var errors = new List<string>();
            var warnings = new List<string>();
            var metrics = new List<string>();

            CheckRequiredFiles(projectRoot, errors, warnings);
            CheckPackagesManifest(projectRoot, errors, warnings, metrics);

            string assetsRoot = Path.Combine(projectRoot, "Assets");
            if (!Directory.Exists(assetsRoot))
            {
                errors.Add("Assets folder is missing.");
                Append(report, errors, warnings, metrics);
                return 1;
            }

            Dictionary<string, string> guids = CollectAssetGuids(projectRoot, assetsRoot, errors, metrics);
            CheckAssemblyDefinitions(assetsRoot, guids, errors, metrics);
            CheckScriptsHaveAssembly(assetsRoot, errors, metrics);
            CheckScenes(projectRoot, assetsRoot, guids, errors, metrics);
            CheckBuildSettingsScenes(projectRoot, guids, errors, warnings);

            Append(report, errors, warnings, metrics);
            return errors.Count == 0 ? 0 : 1;
        }

        private static void Append(StringBuilder report, List<string> errors, List<string> warnings, List<string> metrics)
        {
            report.AppendLine("Project metrics: " + string.Join(", ", metrics));
            foreach (string warning in warnings) report.AppendLine("  WARN  " + warning);
            foreach (string error in errors) report.AppendLine("  ERROR " + error);
            report.AppendLine(errors.Count == 0
                ? "[PASS] Unity project structure is consistent"
                : "[FAIL] " + errors.Count.ToString(CultureInfo.InvariantCulture) + " project integrity error(s)");
        }

        private static void CheckRequiredFiles(string projectRoot, List<string> errors, List<string> warnings)
        {
            foreach (string relative in RequiredPackages)
                if (!File.Exists(Path.Combine(projectRoot, relative.Replace('/', Path.DirectorySeparatorChar))))
                    errors.Add("required file missing: " + relative);

            foreach (string relative in RecommendedProjectSettings)
                if (!File.Exists(Path.Combine(projectRoot, relative.Replace('/', Path.DirectorySeparatorChar))))
                    warnings.Add("ProjectSettings file missing (Unity recreates it on open, commit it once generated): " + relative);
        }

        private static void CheckPackagesManifest(string projectRoot, List<string> errors, List<string> warnings, List<string> metrics)
        {
            string manifest = Path.Combine(projectRoot, "Packages", "manifest.json");
            if (!File.Exists(manifest)) return;

            try
            {
                using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifest)))
                {
                    if (!document.RootElement.TryGetProperty("dependencies", out JsonElement dependencies))
                    {
                        errors.Add("Packages/manifest.json has no dependencies block.");
                        return;
                    }

                    metrics.Add("package dependencies: " + dependencies.EnumerateObject().Count().ToString(CultureInfo.InvariantCulture));
                    foreach (JsonProperty dependency in dependencies.EnumerateObject())
                    {
                        string value = dependency.Value.GetString() ?? string.Empty;
                        if (value.Length == 0) errors.Add("package " + dependency.Name + " has an empty version.");
                        else if (value == "exclude") warnings.Add("package " + dependency.Name + " is excluded from the build.");
                    }
                }
            }
            catch (JsonException exception)
            {
                errors.Add("Packages/manifest.json is not valid JSON: " + exception.Message);
            }
        }

        private static Dictionary<string, string> CollectAssetGuids(string projectRoot, string assetsRoot,
            List<string> errors, List<string> metrics)
        {
            var guids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var missingMetas = new List<string>();
            int assets = 0;

            foreach (string file in EnumerateAssetFiles(assetsRoot))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    if (!File.Exists(file.Substring(0, file.Length - ".meta".Length)))
                        errors.Add("orphan .meta (asset was deleted): " + Relative(projectRoot, file));
                    continue;
                }

                assets++;
                string metaPath = file + ".meta";
                if (!File.Exists(metaPath))
                {
                    missingMetas.Add(Relative(projectRoot, file));
                    continue;
                }

                string guid = ReadGuid(metaPath);
                if (string.IsNullOrEmpty(guid))
                {
                    errors.Add("meta without a valid guid (Unity overwrites it on import): " + Relative(projectRoot, metaPath));
                    continue;
                }

                if (guids.TryGetValue(guid, out string owner))
                    errors.Add("duplicate guid " + guid + " shared by " + owner + " and " + Relative(projectRoot, file));
                else
                    guids[guid] = file;
            }

            foreach (string directory in EnumerateDirectories(assetsRoot))
                if (!File.Exists(directory + ".meta"))
                    missingMetas.Add(Relative(projectRoot, directory) + "/");

            foreach (string missing in missingMetas.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                errors.Add("missing .meta for " + missing);

            metrics.Add("asset files: " + assets.ToString(CultureInfo.InvariantCulture));
            metrics.Add("meta guids: " + guids.Count.ToString(CultureInfo.InvariantCulture));
            return guids;
        }

        private static void CheckAssemblyDefinitions(string assetsRoot, Dictionary<string, string> guids,
            List<string> errors, List<string> metrics)
        {
            var definitions = new List<KeyValuePair<string, JsonDocument>>();
            foreach (string file in Directory.GetFiles(assetsRoot, "*.asmdef", SearchOption.AllDirectories))
            {
                try
                {
                    definitions.Add(new KeyValuePair<string, JsonDocument>(file, JsonDocument.Parse(File.ReadAllText(file))));
                }
                catch (JsonException exception)
                {
                    errors.Add("asmdef is not valid JSON: " + Relative(assetsRoot, file) + " (" + exception.Message + ")");
                }
            }

            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, JsonDocument> definition in definitions)
            {
                string name = definition.Value.RootElement.TryGetProperty("name", out JsonElement nameElement)
                    ? nameElement.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(name)) errors.Add("asmdef without a name: " + Relative(assetsRoot, definition.Key));
                else if (names.ContainsKey(name)) errors.Add("duplicate asmdef name " + name + " in " + Relative(assetsRoot, definition.Key));
                else names[name] = definition.Key;
            }

            foreach (KeyValuePair<string, JsonDocument> definition in definitions)
            {
                string name = definition.Value.RootElement.TryGetProperty("name", out JsonElement nameElement)
                    ? nameElement.GetString() ?? Relative(assetsRoot, definition.Key)
                    : Relative(assetsRoot, definition.Key);

                if (!definition.Value.RootElement.TryGetProperty("references", out JsonElement references) ||
                    references.ValueKind != JsonValueKind.Array)
                {
                    errors.Add("asmdef " + name + " has no references array (Unity always writes it).");
                }
                else
                {
                    foreach (JsonElement reference in references.EnumerateArray())
                        ValidateReference(name, reference.GetString(), guids, names, errors);
                }

                if (definition.Value.RootElement.TryGetProperty("precompiledReferences", out JsonElement precompiled) &&
                    precompiled.ValueKind == JsonValueKind.Array)
                    foreach (JsonElement reference in precompiled.EnumerateArray())
                        if (string.IsNullOrWhiteSpace(reference.GetString()))
                            errors.Add("asmdef " + name + " declares an empty precompiled reference.");
            }

            metrics.Add("assemblies: " + names.Count.ToString(CultureInfo.InvariantCulture));
            foreach (KeyValuePair<string, JsonDocument> definition in definitions) definition.Value.Dispose();
        }

        private static void ValidateReference(string owner, string reference, Dictionary<string, string> guids,
            Dictionary<string, string> names, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                errors.Add("asmdef " + owner + " declares an empty reference.");
                return;
            }

            const string guidPrefix = "GUID:";
            if (reference.StartsWith(guidPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string guid = reference.Substring(guidPrefix.Length).Trim();
                if (!guids.ContainsKey(guid)) errors.Add("asmdef " + owner + " references unknown guid " + guid);
                return;
            }

            if (reference.StartsWith("UnityEngine", StringComparison.Ordinal) ||
                reference.StartsWith("Unity.", StringComparison.Ordinal) ||
                reference.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return;

            if (!names.ContainsKey(reference))
                errors.Add("asmdef " + owner + " references assembly \"" + reference + "\" which is not defined in this project.");
        }

        private static void CheckScriptsHaveAssembly(string assetsRoot, List<string> errors, List<string> metrics)
        {
            int owned = 0;
            var orphans = new List<string>();
            foreach (string script in Directory.GetFiles(assetsRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (HasAssemblyAncestor(Path.GetDirectoryName(script), assetsRoot)) owned++;
                else orphans.Add(Relative(assetsRoot, script));
            }

            foreach (string orphan in orphans)
                errors.Add("script outside any asmdef (it would compile into the Assembly-CSharp catch-all): " + orphan);

            metrics.Add("scripts inside asmdef: " + owned.ToString(CultureInfo.InvariantCulture));
        }

        private static bool HasAssemblyAncestor(string directory, string assetsRoot)
        {
            string current = Path.GetFullPath(directory);
            string root = Path.GetFullPath(assetsRoot);
            while (!string.IsNullOrEmpty(current) && current.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                if (Directory.GetFiles(current, "*.asmdef").Length > 0) return true;
                string parent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent) || string.Equals(parent, current, StringComparison.OrdinalIgnoreCase)) break;
                current = parent;
            }
            return false;
        }

        private static void CheckScenes(string projectRoot, string assetsRoot, Dictionary<string, string> guids,
            List<string> errors, List<string> metrics)
        {
            List<string> scenes = Directory.GetFiles(assetsRoot, "*.unity", SearchOption.AllDirectories).ToList();
            metrics.Add("scenes: " + scenes.Count.ToString(CultureInfo.InvariantCulture));
            if (scenes.Count == 0)
            {
                errors.Add("no .unity scene exists under Assets, so there is nothing to open, play or build.");
                return;
            }

            var scriptGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string script in Directory.GetFiles(assetsRoot, "*.cs", SearchOption.AllDirectories))
            {
                string guid = ReadGuid(script + ".meta");
                if (!string.IsNullOrEmpty(guid)) scriptGuids.Add(guid);
            }

            foreach (string scene in scenes)
            {
                string text = File.ReadAllText(scene);
                string relative = Relative(projectRoot, scene);
                if (!text.StartsWith("%YAML", StringComparison.Ordinal))
                    errors.Add(relative + " is not a Unity YAML scene (it must start with %YAML).");

                foreach (Match match in GuidRegex.Matches(text))
                {
                    string guid = match.Groups[1].Value;
                    if (guid.StartsWith("0000000000000000", StringComparison.Ordinal)) continue; // Unity built-in extras
                    if (!guids.ContainsKey(guid)) errors.Add(relative + " references unknown guid " + guid);
                }

                int attachments = 0;
                foreach (Match match in ScriptReferenceRegex.Matches(text))
                {
                    attachments++;
                    if (!scriptGuids.Contains(match.Groups[1].Value))
                        errors.Add(relative + " attaches a MonoBehaviour whose script guid is not a project .cs: " +
                                   match.Groups[1].Value);
                }

                if (attachments == 0) errors.Add(relative + " contains no MonoBehaviour script reference (the scene is empty).");
            }
        }

        private static void CheckBuildSettingsScenes(string projectRoot, Dictionary<string, string> guids,
            List<string> errors, List<string> warnings)
        {
            string settings = Path.Combine(projectRoot, "ProjectSettings", "EditorBuildSettings.asset");
            if (!File.Exists(settings))
            {
                warnings.Add("EditorBuildSettings.asset is missing, so the build scene list could not be verified.");
                return;
            }

            string text = File.ReadAllText(settings);
            int matches = 0;
            foreach (Match match in GuidRegex.Matches(text))
            {
                matches++;
                if (!guids.ContainsKey(match.Groups[1].Value))
                    errors.Add("EditorBuildSettings references an unknown scene guid " + match.Groups[1].Value);
            }

            if (matches == 0) warnings.Add("EditorBuildSettings.asset lists no scenes (the build would be empty).");
        }

        private static List<string> EnumerateAssetFiles(string assetsRoot)
        {
            var results = new List<string>();
            var pending = new Stack<string>();
            pending.Push(assetsRoot);
            while (pending.Count > 0)
            {
                string directory = pending.Pop();
                foreach (string sub in Directory.GetDirectories(directory))
                {
                    string name = Path.GetFileName(sub);
                    if (IgnoredNames.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                    if (name.StartsWith(".", StringComparison.Ordinal)) continue;
                    pending.Push(sub);
                }

                foreach (string file in Directory.GetFiles(directory))
                {
                    string name = Path.GetFileName(file);
                    if (name.StartsWith(".", StringComparison.Ordinal)) continue;
                    if (name.EndsWith(".keep", StringComparison.OrdinalIgnoreCase)) continue;
                    results.Add(file);
                }
            }

            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        private static List<string> EnumerateDirectories(string assetsRoot)
        {
            var results = new List<string>();
            var pending = new Stack<string>();
            pending.Push(assetsRoot);
            while (pending.Count > 0)
            {
                string directory = pending.Pop();
                foreach (string sub in Directory.GetDirectories(directory))
                {
                    string name = Path.GetFileName(sub);
                    if (IgnoredNames.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                    if (name.StartsWith(".", StringComparison.Ordinal)) continue;
                    results.Add(sub);
                    pending.Push(sub);
                }
            }

            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        private static string ReadGuid(string metaPath)
        {
            if (!File.Exists(metaPath)) return null;
            Match match = MetaGuidRegex.Match(File.ReadAllText(metaPath).Replace("\r\n", "\n"));
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string Relative(string root, string path)
        {
            string full = Path.GetFullPath(path);
            string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return full.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
                ? full.Substring(normalizedRoot.Length).Replace(Path.DirectorySeparatorChar, '/')
                : full.Replace(Path.DirectorySeparatorChar, '/');
        }
    }
}