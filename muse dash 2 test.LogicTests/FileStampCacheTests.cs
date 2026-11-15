namespace muse_dash_2_test.LogicTests
{
    // FileStampCache 검증.
    // 기록 파일을 패널 갱신마다 다시 읽지 않게 하려고 넣은 캐시입니다. "안 읽는다"보다 중요한 것은
    // "읽어야 할 때는 반드시 다시 읽는다"입니다 — 낡은 기록이 화면에 남으면 기록이 안 저장된 것처럼 보입니다.
    public static class FileStampCacheTests
    {
        private sealed class Box
        {
            public string Text;
        }

        public static void GetOrLoad_ReadsUnchangedFileOnlyOnce()
        {
            WithTempFile("first", path =>
            {
                var cache = new FileStampCache<Box>();
                int loads = 0;
                Func<string, Box> load = p => { loads++; return new Box { Text = File.ReadAllText(p) }; };

                Box first = cache.GetOrLoad(path, load, out bool firstLoaded);
                Assert.True(firstLoaded, "첫 조회는 파일을 읽어야 합니다.");
                Assert.Equal("first", first.Text);

                for (int i = 0; i < 100; i++)
                {
                    Box again = cache.GetOrLoad(path, load, out bool loaded);
                    Assert.False(loaded, "파일이 그대로면 다시 읽지 않아야 합니다.");
                    Assert.Same(first, again);
                }

                Assert.Equal(1, loads);
            });
        }

        public static void GetOrLoad_RereadsWhenSizeChanges()
        {
            WithTempFile("short", path =>
            {
                var cache = new FileStampCache<Box>();
                Func<string, Box> load = p => new Box { Text = File.ReadAllText(p) };

                cache.GetOrLoad(path, load, out _);

                // 게임 밖에서 기록 파일을 고친 경우입니다. 수정시각은 일부러 되돌려 크기만 다르게 둡니다.
                DateTime stamp = File.GetLastWriteTimeUtc(path);
                File.WriteAllText(path, "much longer content");
                File.SetLastWriteTimeUtc(path, stamp);

                Box reread = cache.GetOrLoad(path, load, out bool loaded);
                Assert.True(loaded, "크기가 바뀌면 다시 읽어야 합니다.");
                Assert.Equal("much longer content", reread.Text);
            });
        }

        public static void GetOrLoad_RereadsWhenOnlyWriteTimeChanges()
        {
            WithTempFile("aaaa", path =>
            {
                var cache = new FileStampCache<Box>();
                Func<string, Box> load = p => new Box { Text = File.ReadAllText(p) };

                cache.GetOrLoad(path, load, out _);

                // 점수만 바뀌어 크기가 같은 기록이 다시 쓰인 경우입니다.
                File.WriteAllText(path, "bbbb");
                File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddSeconds(10));

                Box reread = cache.GetOrLoad(path, load, out bool loaded);
                Assert.True(loaded, "수정시각이 바뀌면 다시 읽어야 합니다.");
                Assert.Equal("bbbb", reread.Text);
            });
        }

        // 같은 크기·같은 수정시각으로 다시 쓰면 속성만으로는 바뀐 것을 알 수 없습니다.
        // 그래서 저장하는 쪽(CustomRecordStore.SaveResult)이 직접 Invalidate를 부릅니다.
        public static void Invalidate_ForcesRereadEvenWhenStampIsIdentical()
        {
            WithTempFile("aaaa", path =>
            {
                var cache = new FileStampCache<Box>();
                Func<string, Box> load = p => new Box { Text = File.ReadAllText(p) };

                cache.GetOrLoad(path, load, out _);

                DateTime stamp = File.GetLastWriteTimeUtc(path);
                File.WriteAllText(path, "bbbb");
                File.SetLastWriteTimeUtc(path, stamp);

                Box stale = cache.GetOrLoad(path, load, out bool loadedBefore);
                Assert.False(loadedBefore, "이 테스트의 전제: 속성이 같으면 바뀐 것을 알 수 없습니다.");
                Assert.Equal("aaaa", stale.Text);

                cache.Invalidate(path);

                Box fresh = cache.GetOrLoad(path, load, out bool loadedAfter);
                Assert.True(loadedAfter);
                Assert.Equal("bbbb", fresh.Text);
            });
        }

        public static void GetOrLoad_ReturnsNullForMissingFileAndForgetsIt()
        {
            WithTempFile("first", path =>
            {
                var cache = new FileStampCache<Box>();
                int loads = 0;
                Func<string, Box> load = p => { loads++; return new Box { Text = File.ReadAllText(p) }; };

                cache.GetOrLoad(path, load, out _);
                DateTime stamp = File.GetLastWriteTimeUtc(path);

                File.Delete(path);
                Assert.Null(cache.GetOrLoad(path, load, out bool loadedWhileMissing));
                Assert.False(loadedWhileMissing, "없는 파일은 읽으려 들지 않아야 합니다.");

                // 지웠다가 같은 크기·같은 수정시각으로 되살려도 예전 결과가 나오면 안 됩니다.
                File.WriteAllText(path, "other");
                File.SetLastWriteTimeUtc(path, stamp);

                Box revived = cache.GetOrLoad(path, load, out bool loadedAfterRevive);
                Assert.True(loadedAfterRevive);
                Assert.Equal("other", revived.Text);
                Assert.Equal(2, loads);
            });
        }

        public static void GetOrLoad_DoesNotKeepNullResult()
        {
            WithTempFile("first", path =>
            {
                var cache = new FileStampCache<Box>();
                int loads = 0;

                Assert.Null(cache.GetOrLoad(path, p => { loads++; return null; }, out _));
                Assert.Null(cache.GetOrLoad(path, p => { loads++; return null; }, out bool loaded));

                Assert.True(loaded, "읽기에 실패한 결과(null)를 보관하면 다음 조회도 계속 실패합니다.");
                Assert.Equal(2, loads);
            });
        }

        public static void GetOrLoad_DoesNotKeepAnythingWhenLoadThrows()
        {
            WithTempFile("first", path =>
            {
                var cache = new FileStampCache<Box>();

                Assert.Throws<InvalidOperationException>(
                    () => cache.GetOrLoad(path, p => throw new InvalidOperationException("읽기 실패"), out _));

                Box value = cache.GetOrLoad(path, p => new Box { Text = File.ReadAllText(p) }, out bool loaded);
                Assert.True(loaded);
                Assert.Equal("first", value.Text);
            });
        }

        private static void WithTempFile(string content, Action<string> body)
        {
            string path = Path.Combine(Path.GetTempPath(), "mdt-filestampcache-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, content);
            try
            {
                body(path);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
