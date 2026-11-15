using System.Text;

namespace muse_dash_2_test.LogicTests
{
    // AtomicFile 검증. 저장이 끝나면 새 내용만 남고 임시 파일이 없어야 하며,
    // 임시 쓰기가 실패하면 옛 파일이 그대로 남아야 합니다(기록 파일이 잘리지 않게 하려고 넣은 헬퍼입니다).
    public static class AtomicFileTests
    {
        public static void WriteAllText_CreatesNewFileWithoutTempLeftover()
        {
            WithTempDir(dir =>
            {
                string path = Path.Combine(dir, "record.json");

                AtomicFile.WriteAllText(path, "{\"score\": 1}", Encoding.UTF8);

                Assert.Equal("{\"score\": 1}", File.ReadAllText(path, Encoding.UTF8));
                Assert.False(File.Exists(path + ".tmp"), "임시 파일이 남으면 안 됩니다.");
            });
        }

        public static void WriteAllText_ReplacesLongerExistingContent()
        {
            WithTempDir(dir =>
            {
                string path = Path.Combine(dir, "record.json");
                File.WriteAllText(path, "old contents that are much longer than the new one", Encoding.UTF8);

                AtomicFile.WriteAllText(path, "new", Encoding.UTF8);

                Assert.Equal("new", File.ReadAllText(path, Encoding.UTF8));
                Assert.False(File.Exists(path + ".tmp"), "임시 파일이 남으면 안 됩니다.");
            });
        }

        public static void WriteAllText_KeepsOldFileWhenTempWriteFails()
        {
            WithTempDir(dir =>
            {
                string path = Path.Combine(dir, "record.json");
                File.WriteAllText(path, "old", Encoding.UTF8);
                // 임시 파일 자리에 폴더를 만들어 임시 쓰기를 실패시킵니다. 원본은 건드려지면 안 됩니다.
                Directory.CreateDirectory(path + ".tmp");

                bool threw = false;
                try
                {
                    AtomicFile.WriteAllText(path, "new", Encoding.UTF8);
                }
                catch (Exception)
                {
                    threw = true;
                }

                Assert.True(threw, "임시 쓰기가 실패하면 예외가 호출자에게 전달돼야 합니다.");
                Assert.Equal("old", File.ReadAllText(path, Encoding.UTF8));
            });
        }

        private static void WithTempDir(Action<string> body)
        {
            string dir = Path.Combine(Path.GetTempPath(), "atomicfile-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                body(dir);
            }
            finally
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (IOException)
                {
                    // 테스트 임시 폴더 정리 실패는 결과에 영향을 주지 않습니다.
                }
            }
        }
    }
}
