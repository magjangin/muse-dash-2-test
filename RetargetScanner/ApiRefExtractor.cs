using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace RetargetScanner
{
    /// <summary>모드가 컴파일 때 참조한 게임 타입·멤버 한 건. TSV 한 줄과 같습니다.</summary>
    internal sealed class ApiRefRow
    {
        public string Assembly;
        public string Type;
        public string Member;   // 타입만 쓰인 경우 빈칸
        public string Kind;     // type / method / field

        public const string Header = "Assembly\tType\tMember\tKind";

        public string ToTsv() => string.Join("\t", Assembly, Type, Member, Kind);

        public static ApiRefRow FromTsv(string line)
        {
            string[] c = line.Split('\t');
            return new ApiRefRow { Assembly = c[0], Type = c[1], Member = c[2], Kind = c[3] };
        }
    }

    /// <summary>
    /// 모드 DLL의 메타데이터(TypeRef / MemberRef 표)에서 게임 어셈블리를 가리키는 참조를 전부 뽑습니다.
    /// 2편에서 "빌드하면 나올 컴파일 오류 목록"을 빌드 전에, 후보와 함께 미리 보는 용도입니다.
    /// 프로퍼티는 접근자 메서드(get_X / set_X)로 나옵니다. Il2CppInterop은 게임 필드도 프로퍼티로 감쌉니다.
    /// 잡지 못하는 것: 리플렉션·래퍼에 문자열로 쓴 멤버 이름(1편 ModReflection), 제네릭 인스턴스 타입의 멤버.
    /// </summary>
    internal static class ApiRefExtractor
    {
        public static List<ApiRefRow> Extract(string modDllPath)
        {
            using var stream = File.OpenRead(modDllPath);
            using var pe = new PEReader(stream);
            MetadataReader md = pe.GetMetadataReader();

            var rows = new Dictionary<string, ApiRefRow>(StringComparer.Ordinal);

            foreach (TypeReferenceHandle h in md.TypeReferences)
            {
                if (TryDescribe(md, h, out string asm, out string type) && GameAssemblies.IsGameAssembly(asm))
                {
                    AddRow(rows, new ApiRefRow { Assembly = asm, Type = type, Member = "", Kind = "type" });
                }
            }

            foreach (MemberReferenceHandle h in md.MemberReferences)
            {
                MemberReference mr = md.GetMemberReference(h);
                if (mr.Parent.Kind != HandleKind.TypeReference) continue; // 제네릭 인스턴스(TypeSpec) 등은 건너뜀
                if (!TryDescribe(md, (TypeReferenceHandle)mr.Parent, out string asm, out string type)) continue;
                if (!GameAssemblies.IsGameAssembly(asm)) continue;

                string kind = mr.GetKind() == MemberReferenceKind.Field ? "field" : "method";
                AddRow(rows, new ApiRefRow { Assembly = asm, Type = type, Member = md.GetString(mr.Name), Kind = kind });
            }

            return rows.Values.OrderBy(r => r.Assembly).ThenBy(r => r.Type).ThenBy(r => r.Member).ToList();
        }

        private static void AddRow(Dictionary<string, ApiRefRow> rows, ApiRefRow row) => rows.TryAdd(row.ToTsv(), row);

        /// <summary>TypeRef의 전체 이름(중첩은 Outer+Inner)과 그 타입이 사는 어셈블리 이름을 구합니다.</summary>
        private static bool TryDescribe(MetadataReader md, TypeReferenceHandle h, out string assembly, out string fullName)
        {
            assembly = null;
            fullName = null;
            var names = new Stack<string>();
            TypeReference tr = md.GetTypeReference(h);

            for (int depth = 0; depth < 16; depth++)
            {
                string ns = md.GetString(tr.Namespace);
                string name = md.GetString(tr.Name);
                names.Push(string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}");

                EntityHandle scope = tr.ResolutionScope;
                if (scope.Kind == HandleKind.TypeReference)
                {
                    tr = md.GetTypeReference((TypeReferenceHandle)scope);
                    continue;
                }
                if (scope.Kind != HandleKind.AssemblyReference) return false;

                assembly = md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)scope).Name);
                fullName = string.Join("+", names);
                return true;
            }
            return false;
        }
    }
}
