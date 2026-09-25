using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Bounded, read-only scene and console reports for connected agents.</summary>
    [InitializeOnLoad]
    public static class AgentDiagnostics
    {
        sealed class ConsoleEntry
        {
            public int sequence;
            public string type;
            public string message;
            public string stack;
        }

        static readonly List<ConsoleEntry> Console = new List<ConsoleEntry>();
        static readonly string ConsoleEpoch = Guid.NewGuid().ToString("N");
        static int sequence;

        static AgentDiagnostics()
        {
            Application.logMessageReceived += CaptureLog;
        }

        static void CaptureLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert &&
                type != LogType.Warning) return;
            Console.Add(new ConsoleEntry { sequence = ++sequence, type = type.ToString(),
                message = message, stack = stack });
            if (Console.Count > 500) Console.RemoveAt(0);
        }

        [CliCommand("topaz_console_summary", "Deduplicated warning/error messages captured since a cursor. Read only.",
            Tags = new[] { "topaz/diagnostics" })]
        public static object ConsoleSummary(
            [CliArg("since", "Previous nextCursor; empty reads the retained buffer.", DefaultValue = "")]
            string since = "",
            [CliArg("max", "Maximum distinct messages, 1-50.", DefaultValue = 20)] int max = 20)
        {
            // A domain reload creates a new epoch. Old cursors read the new buffer.
            int previous = 0;
            var parts = (since ?? "").Split(':');
            if (parts.Length == 2 && parts[0] == ConsoleEpoch &&
                int.TryParse(parts[1], out int parsed) && parsed <= sequence)
                previous = parsed;
            var entries = Console.Where(entry => entry.sequence > previous).ToArray();
            var groups = entries.GroupBy(entry => entry.type + "\n" + entry.message)
                .Take(Mathf.Clamp(max, 1, 50))
                .Select(group => new { type = group.First().type, count = group.Count(),
                    message = group.First().message, firstStackLine =
                        (group.First().stack ?? "").Split('\n').FirstOrDefault(line =>
                            line.Contains("Assets/Topaz/")) ?? "" }).ToArray();
            return new { nextCursor = ConsoleEpoch + ":" + sequence, captured = entries.Length,
                distinct = entries.Select(entry => entry.type + "\n" + entry.message).Distinct().Count(),
                messages = groups };
        }

        [CliCommand("topaz_scene_inventory", "Filtered object/component summary of an open scene or prefab. Read only.",
            Tags = new[] { "topaz/diagnostics" })]
        public static object SceneInventory(
            [CliArg("scene_path", "Open scene asset path; defaults to active scene.")] string scenePath = null,
            [CliArg("prefab_path", "Prefab asset path, instead of a scene.")] string prefabPath = null,
            [CliArg("filter", "Case-insensitive substring of object hierarchy path.")] string filter = null,
            [CliArg("max", "Maximum matching objects returned, 1-100.", DefaultValue = 40)] int max = 40)
        {
            if (!string.IsNullOrEmpty(scenePath) && !string.IsNullOrEmpty(prefabPath))
                throw new ArgumentException("Choose a scene or prefab, not both.");
            IEnumerable<GameObject> roots;
            string source;
            if (!string.IsNullOrEmpty(prefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) throw new ArgumentException("Prefab not found: " + prefabPath);
                roots = new[] { prefab };
                source = prefabPath;
            }
            else
            {
                Scene scene = string.IsNullOrEmpty(scenePath) ? SceneManager.GetActiveScene() :
                    SceneManager.GetSceneByPath(scenePath);
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new ArgumentException("Scene must already be open: " + scenePath);
                roots = scene.GetRootGameObjects();
                source = scene.path;
            }

            var matches = new List<object>();
            int total = 0;
            int limit = Mathf.Clamp(max, 1, 100);
            foreach (var root in roots)
                Visit(root.transform, root.name, filter, limit, matches, ref total);
            return new { source, filter = filter ?? "", totalMatches = total,
                truncated = total > matches.Count, objects = matches };
        }

        [CliCommand("topaz_asset_references", "Direct reverse dependencies for one asset, bounded and read only.",
            Tags = new[] { "topaz/diagnostics" })]
        public static object AssetReferences(
            [CliArg("asset_path", "Asset path whose direct users should be found.")] string assetPath,
            [CliArg("scope", "Folder to search; defaults to Assets/Topaz.", DefaultValue = "Assets/Topaz")]
            string scope = "Assets/Topaz",
            [CliArg("max", "Maximum returned asset paths, 1-100.", DefaultValue = 30)] int max = 30)
        {
            if (string.IsNullOrWhiteSpace(assetPath) ||
                AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
                throw new ArgumentException("Asset not found: " + assetPath);
            if (!AssetDatabase.IsValidFolder(scope))
                throw new ArgumentException("Scope folder not found: " + scope);
            int limit = Mathf.Clamp(max, 1, 100);
            var matches = new List<string>();
            int total = 0;
            foreach (var guid in AssetDatabase.FindAssets("", new[] { scope }))
            {
                string candidate = AssetDatabase.GUIDToAssetPath(guid);
                if (candidate == assetPath || AssetDatabase.IsValidFolder(candidate)) continue;
                if (!AssetDatabase.GetDependencies(candidate, false).Contains(assetPath)) continue;
                total++;
                if (matches.Count < limit) matches.Add(candidate);
            }
            return new { assetPath, guid = AssetDatabase.AssetPathToGUID(assetPath), scope,
                totalMatches = total, truncated = total > matches.Count,
                assets = matches.OrderBy(path => path, StringComparer.Ordinal).ToArray() };
        }

        [CliCommand("topaz_missing_references", "Missing scripts and object references in an open scene or prefab. Read only.",
            Tags = new[] { "topaz/diagnostics" })]
        public static object MissingReferences(
            [CliArg("scene_path", "Open scene asset path; defaults to active scene.")] string scenePath = null,
            [CliArg("prefab_path", "Prefab asset path, instead of a scene.")] string prefabPath = null,
            [CliArg("max", "Maximum findings, 1-100.", DefaultValue = 30)] int max = 30)
        {
            if (!string.IsNullOrEmpty(scenePath) && !string.IsNullOrEmpty(prefabPath))
                throw new ArgumentException("Choose a scene or prefab, not both.");
            IEnumerable<GameObject> roots;
            string source;
            if (!string.IsNullOrEmpty(prefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) throw new ArgumentException("Prefab not found: " + prefabPath);
                roots = new[] { prefab };
                source = prefabPath;
            }
            else
            {
                Scene scene = string.IsNullOrEmpty(scenePath) ? SceneManager.GetActiveScene() :
                    SceneManager.GetSceneByPath(scenePath);
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new ArgumentException("Scene must already be open: " + scenePath);
                roots = scene.GetRootGameObjects();
                source = scene.path;
            }
            var findings = new List<object>();
            int total = 0;
            int limit = Mathf.Clamp(max, 1, 100);
            foreach (var root in roots)
                FindMissing(root.transform, root.name, limit, findings, ref total);
            return new { source, totalFindings = total, truncated = total > findings.Count,
                findings };
        }

        static void FindMissing(Transform item, string path, int limit,
            List<object> findings, ref int total)
        {
            var gameObject = item.gameObject;
            int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (missingScripts > 0)
            {
                total += missingScripts;
                if (findings.Count < limit)
                    findings.Add(new { path, component = "Missing Script", property = "m_Script",
                        id = GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString(), count = missingScripts });
            }
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference ||
                        property.objectReferenceValue != null ||
                        property.objectReferenceEntityIdValue == EntityId.None) continue;
                    total++;
                    if (findings.Count < limit)
                        findings.Add(new { path, component = component.GetType().Name,
                            property = property.propertyPath,
                            id = GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString(), count = 1 });
                }
            }
            foreach (Transform child in item)
                FindMissing(child, path + "/" + child.name, limit, findings, ref total);
        }

        static void Visit(Transform item, string path, string filter, int limit,
            List<object> matches, ref int total)
        {
            if (string.IsNullOrEmpty(filter) ||
                path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                total++;
                if (matches.Count < limit)
                {
                    var gameObject = item.gameObject;
                    var components = gameObject.GetComponents<Component>();
                    matches.Add(new { path, active = gameObject.activeSelf,
                        components = components.Where(component => component != null)
                            .Select(component => component.GetType().Name).ToArray(),
                        missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject),
                        id = GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString() });
                }
            }
            foreach (Transform child in item)
                Visit(child, path + "/" + child.name, filter, limit, matches, ref total);
        }
    }
}
