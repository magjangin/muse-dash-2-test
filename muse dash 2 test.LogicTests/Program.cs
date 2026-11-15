using System.Reflection;

namespace muse_dash_2_test.LogicTests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string filter = args.Length > 0 ? args[0] : null;
            var tests = DiscoverTests(filter);

            if (tests.Count == 0)
            {
                Console.WriteLine(filter is null
                    ? "[ERROR] 실행할 테스트를 찾지 못했습니다."
                    : $"[ERROR] 필터 '{filter}'에 해당하는 테스트가 없습니다.");
                return 1;
            }

            var failures = new List<string>();

            foreach (var (name, run) in tests)
            {
                try
                {
                    run();
                    Console.WriteLine($"[PASS] {name}");
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    failures.Add($"{name}: {ex.InnerException.Message}");
                    Console.WriteLine($"[FAIL] {name} - {ex.InnerException.Message}");
                }
                catch (Exception ex)
                {
                    failures.Add($"{name}: {ex.Message}");
                    Console.WriteLine($"[FAIL] {name} - {ex.Message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"총 {tests.Count}개 테스트, 실패 {failures.Count}개");

            if (failures.Count == 0)
            {
                return 0;
            }

            Console.WriteLine();
            foreach (var failure in failures)
            {
                Console.WriteLine($" - {failure}");
            }

            return 1;
        }

        // 이름이 "Tests"로 끝나는 타입의 public static void 메서드를 테스트로 간주합니다.
        // (테스트를 추가할 때 목록을 따로 갱신할 필요가 없도록)
        private static List<(string Name, Action Run)> DiscoverTests(string filter)
        {
            var discovered = new List<(string, Action)>();

            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsClass && type.Name.EndsWith("Tests", StringComparison.Ordinal))
                .OrderBy(type => type.Name, StringComparer.Ordinal);

            foreach (var type in types)
            {
                var methods = type
                    .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(method => method.ReturnType == typeof(void) && method.GetParameters().Length == 0)
                    .OrderBy(method => method.MetadataToken);

                foreach (var method in methods)
                {
                    string name = $"{type.Name}.{method.Name}";
                    if (filter is not null && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    var target = method;
                    discovered.Add((name, () => target.Invoke(null, null)));
                }
            }

            return discovered;
        }
    }
}
