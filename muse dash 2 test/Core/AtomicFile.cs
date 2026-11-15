using System.IO;
using System.Text;

namespace muse_dash_2_test
{
    /// <summary>
    /// 사용자 데이터 파일(기록, 설정, 플래그)을 안전하게 덮어씁니다.
    ///
    /// <para><c>File.WriteAllText</c>는 파일을 여는 순간 내용을 비우므로, 쓰는 도중 게임이 죽으면
    /// 파일이 빈 상태나 잘린 상태로 남습니다. 그래서 같은 폴더의 임시 파일에 먼저 다 쓰고,
    /// 끝난 뒤에 이름을 바꿔 덮어씁니다. 이름 바꾸기는 같은 볼륨 안에서 한 번에 끝나므로,
    /// 읽는 쪽은 옛 내용이나 새 내용 중 하나만 보게 됩니다.</para>
    /// </summary>
    public static class AtomicFile
    {
        public static void WriteAllText(string path, string contents, Encoding encoding)
        {
            string tempPath = path + ".tmp";
            try
            {
                File.WriteAllText(tempPath, contents, encoding);
                File.Move(tempPath, path, overwrite: true);
            }
            finally
            {
                // 성공하면 임시 파일은 이미 이름이 바뀌어 없습니다. 실패했을 때만 남은 찌꺼기를 지웁니다.
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
    }
}
