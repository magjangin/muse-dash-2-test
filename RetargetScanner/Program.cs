using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RetargetScanner
{
    /// <summary>
    /// 1편 모드가 기대는 게임 API(훅 대상 + 컴파일 참조)를 뽑아 두고, 2편 빌드에 한 번에 대조하는 독립 콘솔 툴입니다.
    /// 게임도 모드도 실행하지 않습니다.
    /// </summary>
    internal static class Program
    {
        private const string HooksFile = "hooks.tsv";
        private const string RefsFile = "api-refs.tsv";

        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                if (args.Length == 4 && args[0] == "extract") return Extract(args[1], args[2], args[3]);
                if (args.Length == 4 && args[0] == "check") return Check(args[1], args[2], args[3]);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[실패] {ex.GetType().Name}: {ex.Message}");
                return 1;
            }

            PrintUsage();
            return args.Length == 0 || args[0] is "-h" or "--help" ? 0 : 1;
        }

        private static int Extract(string modDll, string md1AssembliesDir, string outDir)
        {
            GameAssemblies md1 = GameAssemblies.Load(md1AssembliesDir);
            Assembly mod = Assembly.LoadFrom(Path.GetFullPath(modDll));

            List<HookRow> hooks = HookExtractor.Extract(mod, md1);
            List<ApiRefRow> refs = ApiRefExtractor.Extract(modDll);

            Directory.CreateDirectory(outDir);
            File.WriteAllLines(Path.Combine(outDir, HooksFile), new[] { HookRow.Header }.Concat(hooks.Select(h => h.ToTsv())), new UTF8Encoding(false));
            File.WriteAllLines(Path.Combine(outDir, RefsFile), new[] { ApiRefRow.Header }.Concat(refs.Select(r => r.ToTsv())), new UTF8Encoding(false));

            int dynamicCount = hooks.Count(h => h.PatchMethod == "(dynamic)");
            int unresolved = hooks.Count(h => h.PatchMethod != "(dynamic)" && h.Md1Signature != null && h.Md1Signature.StartsWith("("));
            Console.WriteLine($"훅 {hooks.Count}건(런타임 대상 {dynamicCount}건, 1편에서도 해석 안 된 것 {unresolved}건), " +
                              $"게임 API 참조 {refs.Count}건 → {outDir}");
            return 0;
        }

        private static int Check(string listDir, string assembliesDir, string outMd)
        {
            List<HookRow> hooks = ReadTsv(Path.Combine(listDir, HooksFile)).Select(HookRow.FromTsv).ToList();
            List<ApiRefRow> refs = ReadTsv(Path.Combine(listDir, RefsFile)).Select(ApiRefRow.FromTsv).ToList();
            GameAssemblies game = GameAssemblies.Load(assembliesDir);

            string report = RetargetChecker.Check(hooks, refs, game, assembliesDir);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outMd)));
            File.WriteAllText(outMd, report, new UTF8Encoding(false));

            // 요약 표만 콘솔에도 찍습니다.
            foreach (string line in report.Split('\n').SkipWhile(l => !l.StartsWith("## 요약")).Skip(1).TakeWhile(l => !l.StartsWith("## ")))
            {
                if (line.StartsWith("| ") && !line.StartsWith("| 구분")) Console.WriteLine(line.TrimEnd());
            }
            Console.WriteLine($"보고서: {outMd}");
            return 0;
        }

        private static IEnumerable<string> ReadTsv(string path) =>
            File.ReadAllLines(path).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l));

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  RetargetScanner extract <1편 모드 DLL> <1편 Il2CppAssemblies> <출력 폴더>");
            Console.WriteLine("  RetargetScanner check   <목록 폴더(hooks.tsv, api-refs.tsv)> <대상 Il2CppAssemblies> <보고서.md>");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  RetargetScanner extract \"H:\\muse dash hwa\\Mods\\muse-dash-custom-chart.dll\" \"H:\\muse dash hwa\\MelonLoader\\Il2CppAssemblies\" docs\\md1-retarget");
            Console.WriteLine("  RetargetScanner check docs\\md1-retarget \"H:\\muse dash 2 hwa\\MelonLoader\\Il2CppAssemblies\" scratch\\retarget-report.md");
        }
    }
}
