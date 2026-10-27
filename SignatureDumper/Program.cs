using System;
using System.Collections.Generic;
using System.IO;

namespace SignatureDumper
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
            {
                PrintUsage();
                return 0;
            }

            string repoRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));

            string gameAssembliesDir = args.Length > 0
                ? args[0]
                : ResolveDefaultAssembliesDirectory();

            string outputDir = args.Length > 1
                ? args[1]
                : Path.Combine(repoRoot, "Decompiled");

            string[] targets = args.Length > 2
                ? args[2].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                : new[] { "Assembly-CSharp.dll", "Assembly-CSharp-firstpass.dll" };

            if (!Directory.Exists(gameAssembliesDir))
            {
                Console.Error.WriteLine($"[오류] Il2Cpp 시그니처 어셈블리 폴더를 찾을 수 없습니다: {gameAssembliesDir}");
                return 1;
            }

            Directory.CreateDirectory(outputDir);

            Console.WriteLine($"입력 폴더 : {gameAssembliesDir}");
            Console.WriteLine($"출력 폴더 : {outputDir}");
            Console.WriteLine();

            int failedCount = 0;

            foreach (string targetName in targets)
            {
                string dllPath = Path.Combine(gameAssembliesDir, targetName.Trim());
                if (!File.Exists(dllPath))
                {
                    // 요청한 대상이 없으면 건너뛰지 않고 실패로 셉니다. 자동화가 성공으로 오인하지 않게 합니다.
                    Console.Error.WriteLine($"[실패] 파일 없음: {dllPath}");
                    failedCount++;
                    continue;
                }

                string moduleOutputDir = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(targetName.Trim()));

                // 덤프는 임시 폴더에 쓰고, 성공했을 때만 기존 결과와 바꿉니다. 중간에 실패해도 이전 결과가 남습니다.
                string stagingDir = moduleOutputDir + ".staging";
                if (Directory.Exists(stagingDir))
                {
                    Directory.Delete(stagingDir, recursive: true);
                }

                Console.WriteLine($"시그니처 덤프 중: {targetName} -> {moduleOutputDir}");

                try
                {
                    var options = new SignatureDumperOptions
                    {
                        TargetAssemblyPath = dllPath,
                        SearchDirectory = gameAssembliesDir,
                        OutputDirectory = stagingDir
                    };

                    var dumper = new AssemblySignatureDumper(options);
                    dumper.Dump();

                    if (Directory.Exists(moduleOutputDir))
                    {
                        Directory.Delete(moduleOutputDir, recursive: true);
                    }
                    Directory.Move(stagingDir, moduleOutputDir);

                    Console.WriteLine($"  완료: {targetName}");
                }
                catch (Exception ex)
                {
                    failedCount++;
                    Console.Error.WriteLine($"  [실패] {targetName}: {ex.Message} (이전 결과는 그대로 남아 있습니다)");
                    if (Directory.Exists(stagingDir))
                    {
                        Directory.Delete(stagingDir, recursive: true);
                    }
                }
            }

            Console.WriteLine();
            if (failedCount > 0)
            {
                Console.Error.WriteLine($"{failedCount}개 대상이 실패했습니다.");
                return 1;
            }

            Console.WriteLine("모든 작업이 끝났습니다.");
            return 0;
        }

        static string ResolveDefaultAssembliesDirectory()
        {
            // 2편 작업용 사본 경로와 Steam 설치 폴더 이름은 출시 전 추정입니다. 실제 이름을 확인하면 고치십시오
            // (docs/MD2_LAUNCH_CHECKLIST.md 참고).
            var candidates = new List<string>
            {
                Path.Combine("H:\\muse dash 2 hwa", "MelonLoader", "Il2CppAssemblies"),
                Path.Combine("H:\\steam", "steamapps", "common", "Muse Dash 2", "MelonLoader", "Il2CppAssemblies")
            };

            foreach (string candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine("H:\\muse dash 2 hwa", "MelonLoader", "Il2CppAssemblies");
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  SignatureDumper [inputDir] [outputDir] [target1,target2,...]");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  SignatureDumper");
            Console.WriteLine("  SignatureDumper H:\\muse dash 2 hwa\\MelonLoader\\Il2CppAssemblies");
            Console.WriteLine("  SignatureDumper H:\\muse dash 2 hwa\\MelonLoader\\Il2CppAssemblies H:\\out\\decompiled Assembly-CSharp.dll,Assembly-CSharp-firstpass.dll");
        }
    }
}
