package tool;

import java.io.File;
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
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlFile;

@DisplayName("ClsFsDiffCopy 単体テスト")
class ClsFsDiffCopyTest {

    private Path testRoot;
    private ClsLogger logger;
    private ClsBaseDir prop;
    private ClsFsUtil fsUtil;
    private ClsSymLinkWrapper symLink;

    @BeforeEach
    void setUp() throws IOException {
        testRoot = Files.createTempDirectory("UnitTest_FsFileUtil_ClsFsDiffCopy_" + UUID.randomUUID().toString());
        logger = new ClsLogger();
        prop = new ClsBaseDir();
        fsUtil = new ClsFsUtil(logger);
        symLink = new ClsSymLinkWrapper(logger);
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

    private String createTestFile(String relativePath, String content) throws IOException {
        Path filePath = testRoot.resolve(relativePath);
        if (filePath.getParent() != null && !Files.exists(filePath.getParent())) {
            Files.createDirectories(filePath.getParent());
        }
        Files.writeString(filePath, content, StandardCharsets.UTF_8);
        return filePath.toString();
    }

    @Test
    @DisplayName("コンストラクタ初期値およびGetter/Setterテスト")
    void testConstructorAndProperties() {
        ClsFsDiffCopy diffCopy = new ClsFsDiffCopy(logger, prop, fsUtil, symLink);

        Assertions.assertSame(prop, diffCopy.getProperties());
        Assertions.assertEquals(0L, diffCopy.getCopyNewCount());
        Assertions.assertEquals(0L, diffCopy.getCopyUpdateCount());
        Assertions.assertEquals(0L, diffCopy.getCopySkipCount());
        Assertions.assertEquals(0L, diffCopy.getCopyErrorCount());
        Assertions.assertEquals(0L, diffCopy.getCopyTotalCount());
        Assertions.assertEquals(0L, diffCopy.getRmOkCount());
        Assertions.assertEquals(0L, diffCopy.getRmNgCount());
        Assertions.assertEquals(0L, diffCopy.getRmSkipCount());
        Assertions.assertEquals(0L, diffCopy.getRmTotalCount());
        Assertions.assertEquals(0L, diffCopy.getMkdirOkCount());
        Assertions.assertEquals(0L, diffCopy.getMkdirNgCount());
        Assertions.assertEquals(0L, diffCopy.getNotFoundCount());

        diffCopy.setCopyNewCount(1);
        diffCopy.setCopyUpdateCount(2);
        diffCopy.setCopySkipCount(3);
        diffCopy.setCopyErrorCount(4);
        diffCopy.setCopyTotalCount(5);
        diffCopy.setRmOkCount(6);
        diffCopy.setRmNgCount(7);
        diffCopy.setRmSkipCount(8);
        diffCopy.setRmTotalCount(9);
        diffCopy.setMkdirOkCount(10);
        diffCopy.setMkdirNgCount(11);
        diffCopy.setNotFoundCount(12);

        Assertions.assertEquals(1L, diffCopy.getCopyNewCount());
        Assertions.assertEquals(2L, diffCopy.getCopyUpdateCount());
        Assertions.assertEquals(3L, diffCopy.getCopySkipCount());
        Assertions.assertEquals(4L, diffCopy.getCopyErrorCount());
        Assertions.assertEquals(5L, diffCopy.getCopyTotalCount());
        Assertions.assertEquals(6L, diffCopy.getRmOkCount());
        Assertions.assertEquals(7L, diffCopy.getRmNgCount());
        Assertions.assertEquals(8L, diffCopy.getRmSkipCount());
        Assertions.assertEquals(9L, diffCopy.getRmTotalCount());
        Assertions.assertEquals(10L, diffCopy.getMkdirOkCount());
        Assertions.assertEquals(11L, diffCopy.getMkdirNgCount());
        Assertions.assertEquals(12L, diffCopy.getNotFoundCount());
    }

    @Test
    @DisplayName("Mkdir ディレクトリ作成テスト")
    void testMkdir() {
        ClsFsDiffCopy diffCopy = new ClsFsDiffCopy(logger, prop, fsUtil, symLink);
        String targetDir = testRoot.resolve("new_dir").toString();

        boolean result = diffCopy.mkdir(testRoot.toString(), targetDir, "new_dir");

        Assertions.assertTrue(result);
        Assertions.assertTrue(Files.exists(Path.of(targetDir)));
        Assertions.assertEquals(1L, diffCopy.getMkdirOkCount());
    }

    @Test
    @DisplayName("MkParentDir 親ディレクトリ作成テスト")
    void testMkParentDir() {
        ClsFsDiffCopy diffCopy = new ClsFsDiffCopy(logger, prop, fsUtil, symLink);
        String targetFile = testRoot.resolve("parent_dir/sub_dir/test.txt").toString();

        boolean result = diffCopy.mkParentDir(targetFile, true);

        Assertions.assertTrue(result);
        Assertions.assertTrue(Files.exists(Path.of(testRoot.toString(), "parent_dir/sub_dir")));
        Assertions.assertEquals(1L, diffCopy.getMkdirOkCount());
    }

    @Test
    @DisplayName("DiffCopyFileMain 新規ファイルコピーテスト")
    void testDiffCopyFileMainNewFile() throws IOException {
        ClsFsDiffCopy diffCopy = new ClsFsDiffCopy(logger, prop, fsUtil, symLink);
        String srcFile = createTestFile("src/file1.txt", "content 1");
        String dstFile = testRoot.resolve("dst/file1.txt").toString();

        boolean result = diffCopy.diffCopyFileMain(srcFile, dstFile, "file1.txt");

        Assertions.assertTrue(result);
        Assertions.assertTrue(Files.exists(Path.of(dstFile)));
        Assertions.assertEquals("content 1", Files.readString(Path.of(dstFile), StandardCharsets.UTF_8));
        Assertions.assertEquals(1L, diffCopy.getCopyNewCount());
    }

    @Test
    @DisplayName("DiffCopyFileMain 同一ファイルスキップテスト")
    void testDiffCopyFileMainSameFile() throws IOException {
        prop.setCheckLogic(ClsBaseDir.CHECK_SHA1);
        ClsFsDiffCopy diffCopy = new ClsFsDiffCopy(logger, prop, fsUtil, symLink);
        String srcFile = createTestFile("src/same.txt", "identical");
        String dstFile = createTestFile("dst/same.txt", "identical");

        boolean result = diffCopy.diffCopyFileMain(srcFile, dstFile, "same.txt");

        Assertions.assertTrue(result);
        Assertions.assertEquals(1L, diffCopy.getCopySkipCount());
        Assertions.assertEquals(0L, diffCopy.getCopyNewCount());
        Assertions.assertEquals(0L, diffCopy.getCopyUpdateCount());
    }

    @Test
    @DisplayName("DiffCopyFileMain 更新ファイルコピーテスト")
    void testDiffCopyFileMainUpdatedFile() throws IOException {
        prop.setCheckLogic(ClsBaseDir.CHECK_SHA1);
        ClsFsDiffCopy diffCopy = new ClsFsDiffCopy(logger, prop, fsUtil, symLink);
        String srcFile = createTestFile("src/update.txt", "new version");
        String dstFile = createTestFile("dst/update.txt", "old version");

        boolean result = diffCopy.diffCopyFileMain(srcFile, dstFile, "update.txt");

        Assertions.assertTrue(result);
        Assertions.assertEquals("new version", Files.readString(Path.of(dstFile), StandardCharsets.UTF_8));
        Assertions.assertEquals(1L, diffCopy.getCopyUpdateCount());
    }

    @Test
    @DisplayName("RemoveRecursive ファイル削除テスト")
    void testRemoveRecursiveFile() throws IOException {
        ClsFsDiffCopy diffCopy = new ClsFsDiffCopy(logger, prop, fsUtil, symLink);
        String file = createTestFile("to_delete.txt", "delete me");

        boolean result = diffCopy.removeRecursive(file, "to_delete.txt", false);

        Assertions.assertTrue(result);
        Assertions.assertFalse(Files.exists(Path.of(file)));
        Assertions.assertEquals(1L, diffCopy.getRmOkCount());
        Assertions.assertEquals(1L, diffCopy.getRmTotalCount());
    }

    @Test
    @DisplayName("RemoveRecursive ディレクトリ再帰削除テスト")
    void testRemoveRecursiveDir() throws IOException {
        ClsFsDiffCopy diffCopy = new ClsFsDiffCopy(logger, prop, fsUtil, symLink);
        String root = testRoot.resolve("delete_dir").toString();
        createTestFile("delete_dir/f1.txt", "1");
        createTestFile("delete_dir/sub/f2.txt", "2");

        boolean result = diffCopy.removeRecursive(root, "delete_dir", false);

        Assertions.assertTrue(result);
        Assertions.assertFalse(Files.exists(Path.of(root)));
        Assertions.assertEquals(2L, diffCopy.getRmOkCount());
    }
}