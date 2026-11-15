using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace muse_dash_2_test
{
    /// <summary>
    /// "지금 그 슬롯에 들어 있는 채보"를 식별하는 지문을 만듭니다.
    ///
    /// <para><b>왜 필요한가</b>: 기록 파일은 <c>record/{uid}_{난이도}.json</c>으로 묶이는데,
    /// 이 uid는 <c>hwa</c> 폴더를 이름순 정렬한 <b>순번</b>입니다(<c>HwaResourceManager.PreloadHwaManifest</c>).
    /// 채보 내용은 물론이고 폴더 이름조차 기록과 묶여 있지 않아서, 폴더 안 BMS만 갈아끼우거나
    /// 곡 폴더를 추가·삭제해 순번이 밀리면 <b>예전 채보의 기록이 다른 채보에 그대로 붙습니다.</b>
    /// (2026-08-20 실측: 319노트 채보의 FC/AP 기록이 43노트짜리 새 채보의 최고 기록으로 표시됐습니다.)</para>
    ///
    /// <para>그래서 기록을 쓸 때 채보 지문을 같이 적어두고, 읽을 때 지금 채보의 지문과 대조합니다.</para>
    /// </summary>
    public static class ChartFingerprint
    {
        /// <summary>BMS가 없는 슬롯의 지문입니다. "아직 지문을 안 적던 시절의 기록"(빈 값)과 구분해야 하므로 별도 표식을 씁니다.</summary>
        public const string NoChart = "none";

        private sealed class CacheEntry
        {
            public long Length;
            public long LastWriteUtcTicks;
            public string Fingerprint;
        }

        // 곡 선택/준비 패널은 한 세션에 기록을 수십 번 다시 읽습니다(실측 86회).
        // 매번 46KB짜리 BMS를 해시하지 않도록 경로별로 캐시하고, 크기·수정시각이 바뀌면 다시 계산합니다.
        private static readonly Dictionary<string, CacheEntry> Cache =
            new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);

        // 1편의 ForUid(uid, difficulty)는 HwaResourceManager(게임 곡 DB에 묶인 클래스)에서 채보를 찾아
        // ForFile을 부르는 얇은 함수라 빼 두었습니다. HwaResourceManager를 옮길 때 같이 되살리십시오.
        // 1편 원본: H:\source\repos\muse dash test\muse dash test\Core\ChartFingerprint.cs

        /// <summary>
        /// BMS 파일 내용의 SHA-256 앞 16자리를 돌려줍니다. 파일이 없으면 <see cref="NoChart"/>,
        /// 읽기에 실패하면 <c>null</c>입니다.
        /// </summary>
        public static string ForFile(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return NoChart;

                var info = new FileInfo(path);
                lock (Cache)
                {
                    if (Cache.TryGetValue(path, out CacheEntry cached)
                        && cached.Length == info.Length
                        && cached.LastWriteUtcTicks == info.LastWriteTimeUtc.Ticks)
                    {
                        return cached.Fingerprint;
                    }
                }

                string fingerprint;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(stream);
                    var sb = new StringBuilder(16);
                    for (int i = 0; i < 8; i++) sb.Append(hash[i].ToString("x2"));
                    fingerprint = sb.ToString();
                }

                lock (Cache)
                {
                    Cache[path] = new CacheEntry
                    {
                        Length = info.Length,
                        LastWriteUtcTicks = info.LastWriteTimeUtc.Ticks,
                        Fingerprint = fingerprint,
                    };
                }

                return fingerprint;
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[ChartFingerprint] 파일 지문 계산 실패(판정을 건너뜁니다): path={path}, {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 메모리에 캐시된 모든 파일 지문(해시) 기록을 초기화합니다.
        /// </summary>
        public static void ClearCache()
        {
            lock (Cache)
            {
                Cache.Clear();
            }
        }
    }
}
