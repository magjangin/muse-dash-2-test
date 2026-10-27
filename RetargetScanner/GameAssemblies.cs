using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RetargetScanner
{
    /// <summary>
    /// MelonLoader가 만든 Il2CppAssemblies 폴더를 리플렉션으로 읽고, 게임 타입을 이름으로 찾는 색인을 만듭니다.
    /// 게임은 실행하지 않습니다(SignatureDumper와 같은 LoadFrom + 리졸버 방식).
    /// </summary>
    internal sealed class GameAssemblies
    {
        /// <summary>게임 코드가 아닌 어셈블리 접두사입니다. 남은 것만 "게임 어셈블리"로 봅니다.</summary>
        private static readonly string[] NonGamePrefixes =
        {
            "System", "mscorlib", "netstandard", "Microsoft", "MelonLoader", "0Harmony", "Harmony",
            "Il2CppInterop", "Il2Cppmscorlib", "Il2CppSystem", "Il2CppMono", "UnityEngine", "Unity.",
            "Mono.", "Newtonsoft", "Il2CppNewtonsoft", "Iced", "AssetRipper", "Tomlet", "WebSocket"
        };

        private static readonly HashSet<string> ResolverDirs = new(StringComparer.OrdinalIgnoreCase);
        private static bool _resolverInstalled;

        public readonly Dictionary<string, Type> ByFullName = new(StringComparer.Ordinal);
        public readonly Dictionary<string, List<Type>> BySimpleName = new(StringComparer.Ordinal);
        public readonly Dictionary<string, List<Type>> ByDeclaredMemberName = new(StringComparer.Ordinal);
        public readonly List<string> LoadedAssemblies = new();

        public static bool IsGameAssembly(string assemblyName)
        {
            foreach (string prefix in NonGamePrefixes)
            {
                if (assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        /// <summary>
        /// Il2CppAssemblies 폴더(와 그 위 MelonLoader 폴더의 net6 등)를 리졸버 경로로 등록합니다.
        /// 한 프로세스에서 1편·2편 폴더를 둘 다 등록하면 먼저 로드된 쪽이 이깁니다. check는 한 폴더만 쓰십시오.
        /// </summary>
        public static void InstallResolver(string assembliesDir)
        {
            ResolverDirs.Add(assembliesDir);
            string melonRoot = Directory.GetParent(assembliesDir)?.FullName;
            if (melonRoot != null)
            {
                ResolverDirs.Add(Path.Combine(melonRoot, "net6"));
                ResolverDirs.Add(Path.Combine(melonRoot, "net8"));
                ResolverDirs.Add(Path.Combine(melonRoot, "Dependencies", "SupportModules"));
                ResolverDirs.Add(melonRoot);
            }

            if (_resolverInstalled) return;
            _resolverInstalled = true;
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                string file = new AssemblyName(args.Name).Name + ".dll";
                foreach (string dir in ResolverDirs)
                {
                    string path = Path.Combine(dir, file);
                    if (File.Exists(path))
                    {
                        try { return Assembly.LoadFrom(path); } catch { }
                    }
                }
                return null;
            };
        }

        public static GameAssemblies Load(string assembliesDir)
        {
            if (!Directory.Exists(assembliesDir))
            {
                throw new DirectoryNotFoundException($"Il2CppAssemblies 폴더가 없습니다: {assembliesDir}");
            }

            InstallResolver(assembliesDir);
            var result = new GameAssemblies();

            foreach (string path in Directory.GetFiles(assembliesDir, "*.dll").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                bool isGame = IsGameAssembly(name);
                // 게임 밖 어셈블리(UnityEngine 등)도 "이름으로 찾기"에는 필요합니다. 1편은 UnityEngine.AudioSource도 훅했습니다.
                // 다만 후보 검색 색인에 넣으면 단서가 엔진 타입으로 덮이므로 게임 어셈블리만 넣습니다.
                if (!isGame && !name.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase)
                            && !name.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase)) continue;

                Assembly asm;
                try { asm = Assembly.LoadFrom(path); }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[경고] {name} 로드 실패: {ex.Message}");
                    continue;
                }

                if (isGame) result.LoadedAssemblies.Add(name);
                foreach (Type type in GetTypesSafe(asm))
                {
                    if (isGame) result.Index(type);
                    else if (type.FullName != null) result.ByFullName.TryAdd(type.FullName, type);
                }
            }

            return result;
        }

        public static IEnumerable<Type> GetTypesSafe(Assembly asm)
        {
            try { return asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
        }

        private void Index(Type type)
        {
            string fullName = type.FullName;
            if (fullName == null) return;

            ByFullName[fullName] = type;
            Add(BySimpleName, type.Name, type);

            MemberInfo[] members;
            try
            {
                members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                                          | BindingFlags.Static | BindingFlags.DeclaredOnly);
            }
            catch
            {
                return;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (MemberInfo m in members)
            {
                if (seen.Add(m.Name)) Add(ByDeclaredMemberName, m.Name, type);
            }
        }

        private static void Add(Dictionary<string, List<Type>> map, string key, Type type)
        {
            if (!map.TryGetValue(key, out var list)) map[key] = list = new List<Type>();
            list.Add(type);
        }

        /// <summary>후보 타입 이름을 짧게 나열합니다. 너무 많으면 앞 몇 개와 총수만.</summary>
        public static string Candidates(IEnumerable<Type> types, int max = 5)
        {
            var names = types?.Select(t => t.FullName).Where(n => n != null).Distinct().OrderBy(n => n.Length).ToList();
            if (names == null || names.Count == 0) return "";
            string head = string.Join(", ", names.Take(max));
            return names.Count > max ? $"{head} 외 {names.Count - max}개" : head;
        }
    }
}
