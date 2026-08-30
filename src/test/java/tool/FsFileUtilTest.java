package tool;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.UUID;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlFile;

@DisplayName("FsFileUtil メインエントリポイント単体テスト")
class FsFileUtilTest {

    private Path testRoot;

    @BeforeEach
    void setUp() throws IOException {
        testRoot = Files.createTempDirectory("UnitTest_FsFileUtil_Main_" + UUID.randomUUID().toString());
    }

    @AfterEach
    void tearDown() {
        try {
            if (Files.exists(testRoot)) {
                MdlFile.deleteRecursively(testRoot.toString(), 0);
            }
        } catch (Exception ignored) {
        }
    }

    @Test
    @DisplayName("引数なし実行時はエラー終了コード (LVL_E) を返却")
    void testMainWithNoArgs() {
        int exitCode = FsFileUtil.run(new String[0]);
        Assertions.assertEquals(MdlConst.LVL_E, exitCode);
    }

    @Test
    @DisplayName("不正な引数の場合はエラー終了コード (LVL_E) を返却")
    void testMainWithInvalidArgument() {
        int exitCode = FsFileUtil.run(new String[] { "--invalid-argument-test" });
        Assertions.assertEquals(MdlConst.LVL_E, exitCode);
    }

    @Test
    @DisplayName("-h ヘルプのみの場合はエラー終了コード (LVL_E) を返却")
    void testMainWithHelpOnly() {
        int exitCode = FsFileUtil.run(new String[] { "-h" });
        Assertions.assertEquals(MdlConst.LVL_E, exitCode);
    }

    @Test
    @DisplayName("有効な引数にヘルプフラグが付いた場合は警告終了コード (LVL_W) を返却")
    void testMainWithValidArgsAndHelp() {
        String[] args = new String[] { "-a", "copy", "-f", "C:\\test.txt", "-t", "C:\\dst.txt", "-h" };
        int exitCode = FsFileUtil.run(args);
        Assertions.assertEquals(MdlConst.LVL_W, exitCode);
    }

    @Test
    @DisplayName("コマンドライン引数による実際のファイルコピー処理 (E2E)")
    void testMainFileCopyE2E() throws IOException {
        Path srcFile = testRoot.resolve("src_e2e.txt");
        Path dstFile = testRoot.resolve("dst_e2e.txt");
        Files.writeString(srcFile, "e2e content", StandardCharsets.UTF_8);

        String[] args = new String[] {
            "-f", srcFile.toString(),
            "-t", dstFile.toString(),
            "-a", "copy",
            "-v", "0"
        };

        int exitCode = FsFileUtil.run(args);

        Assertions.assertEquals(MdlConst.LVL_I, exitCode);
        Assertions.assertTrue(Files.exists(dstFile));
        Assertions.assertEquals("e2e content", Files.readString(dstFile, StandardCharsets.UTF_8));
    }

    @Test
    @DisplayName("コマンドライン引数によるディレクトリ作成処理 (E2E)")
    void testMainMkdirE2E() {
        Path newDir = testRoot.resolve("e2e_mkdir");

        String[] args = new String[] {
            "-f", newDir.toString(),
            "-a", "mkdir",
            "-v", "0"
        };

        int exitCode = FsFileUtil.run(args);

        Assertions.assertEquals(MdlConst.LVL_I, exitCode);
        Assertions.assertTrue(Files.exists(newDir));
    }
}