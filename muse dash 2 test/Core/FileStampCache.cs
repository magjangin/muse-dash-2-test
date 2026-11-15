using System;
using System.Collections.Generic;
using System.IO;

namespace muse_dash_2_test
{
    /// <summary>
    /// 파일을 읽어 가공한 결과를 경로별로 보관하고, 파일의 크기·수정시각이 그대로면 다시 읽지 않습니다.
    ///
    /// <para><b>왜 필요한가</b>: 곡 선택/준비 패널은 같은 기록 파일을 한 세션에 수십 번 다시 읽습니다(실측 86회).
    /// 그때마다 파일을 열어 파싱하는 대신 속성 조회 한 번으로 "그대로인지"만 확인합니다.
    /// 만료 시간이나 폴더 감시에 기대지 않으므로, 게임 밖에서 파일을 고치거나 지워도 다음 조회에 바로 반영됩니다.</para>
    ///
    /// <para>보관한 인스턴스를 그대로 돌려줍니다. 호출자가 값을 고칠 수 있는 타입이면 받은 쪽에서 복사해 쓰십시오.</para>
    ///
    /// <para>게임에 의존하지 않으며 로직 테스트(<c>FileStampCacheTests</c>)로 검증됩니다.</para>
    /// </summary>
    public sealed class FileStampCache<T> where T : class
    {
        private sealed class Entry
        {
            public long Length;
            public long LastWriteUtcTicks;
            public T Value;
        }

        private readonly Dictionary<string, Entry> entries =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// <paramref name="path"/>의 가공 결과를 돌려줍니다. 파일이 지난번과 같으면 <paramref name="load"/>를 부르지 않습니다.
        /// 파일이 없으면 <c>null</c>입니다. <paramref name="load"/>가 <c>null</c>을 돌려주거나 예외를 던지면 보관하지 않습니다.
        /// </summary>
        /// <param name="loaded">이번 호출에서 <paramref name="load"/>를 실제로 불렀는지 여부입니다.</param>
        public T GetOrLoad(string path, Func<string, T> load, out bool loaded)
        {
            loaded = false;

            var info = new FileInfo(path);
            if (!info.Exists)
            {
                Invalidate(path);
                return null;
            }

            long length = info.Length;
            long lastWriteUtcTicks = info.LastWriteTimeUtc.Ticks;

            lock (entries)
            {
                if (entries.TryGetValue(path, out Entry cached)
                    && cached.Length == length
                    && cached.LastWriteUtcTicks == lastWriteUtcTicks)
                {
                    return cached.Value;
                }
            }

            T value = load(path);
            loaded = true;

            lock (entries)
            {
                if (value == null)
                {
                    entries.Remove(path);
                }
                else
                {
                    entries[path] = new Entry
                    {
                        Length = length,
                        LastWriteUtcTicks = lastWriteUtcTicks,
                        Value = value,
                    };
                }
            }

            return value;
        }

        /// <summary>
        /// 보관한 결과를 버립니다. 파일을 직접 고쳐 쓴 쪽에서 부릅니다.
        /// 수정시각 해상도가 거친 파일 시스템(FAT 계열은 2초)에서 같은 크기로 곧바로 다시 쓰면
        /// 크기·수정시각만으로는 바뀐 것을 알 수 없기 때문입니다.
        /// </summary>
        public void Invalidate(string path)
        {
            lock (entries)
            {
                entries.Remove(path);
            }
        }
    }
}
