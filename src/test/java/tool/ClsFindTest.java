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
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlFile;

@DisplayName("ClsFind 単体テスト")
class ClsFindTest {

    private Path testRoot;
    private ClsLogger logger;
    private ClsBaseDir prop;
    private ClsFsUtil fsUtil;
    private ClsFsDiffCopy diffCopy;
    private ClsFind finder;

    @BeforeEach
    void setUp() throws IOException {
        testRoot = Files.createTempDirectory("UnitTest_FsFileUtil_ClsFind_" + UUID.randomUUID().toString());
        logger = new ClsLogger();
        prop = new ClsBaseDir();
        fsUtil = new ClsFsUtil(logger);
        diffCopy = new ClsFsDiffCopy(logger, prop, fsUtil, new ClsSymLinkWrapper(logger));
        finder = new ClsFind(logger, prop, fsUtil, diffCopy);
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
    @DisplayName("TASK_CP ディレクトリ再帰コピーテスト")
    void testExecuteCopy() throws IOException {
        String srcDir = testRoot.resolve("src").toString();
        String dstDir = testRoot.resolve("dst").toString();

        createTestFile("src/file1.txt", "content 1");
        createTestFile("src/sub/file2.txt", "content 2");

        prop.setSourcePath(srcDir);
        prop.setDestinationPath(dstDir);
        prop.setPathType(MdlFile.PATH_IS_DIRECTORY);

        boolean result = finder.execute(ClsBaseDir.TASK_CP);

        Assertions.assertTrue(result);
        Assertions.assertTrue(Files.exists(Path.of(dstDir, "file1.txt")));
        Assertions.assertTrue(Files.exists(Path.of(dstDir, "sub/file2.txt")));
    }

    @Test
    @DisplayName("TASK_PRINT ディレクトリ探索・ファイルカウントテスト")
    void testExecutePrint() throws IOException {
        String srcDir = testRoot.resolve("src").toString();

        createTestFile("src/file1.txt", "1");
        createTestFile("src/sub/file2.txt", "2");

        prop.setSourcePath(srcDir);
        prop.setDestinationPath(srcDir);
        prop.setPathType(MdlFile.PATH_IS_DIRECTORY);
        prop.setTypeCode(MdlConst.INT_TYPE_FILE);
        prop.setFiles(0);

        boolean result = finder.execute(ClsBaseDir.TASK_PRINT);

        Assertions.assertTrue(result);
        Assertions.assertEquals(2L, prop.getFiles());
    }

    @Test
    @DisplayName("FileList による複数ファイル処理テスト")
    void testExecuteFileList() throws IOException {
        String srcFile1 = createTestFile("src/a.txt", "A");
        String srcFile2 = createTestFile("src/b.txt", "B");
        String dstDir = testRoot.resolve("dst").toString();

        prop.setSourcePath(testRoot.resolve("src").toString());
        prop.setDestinationPath(dstDir);
        prop.setFilesTypeCode(ClsBaseDir.FILES_RELATIVE);
        prop.getFileList().add("a.txt");
        prop.getFileList().add("b.txt");

        boolean result = finder.executeFileList();

        Assertions.assertTrue(result);
        Assertions.assertTrue(Files.exists(Path.of(dstDir, "a.txt")));
        Assertions.assertTrue(Files.exists(Path.of(dstDir, "b.txt")));
    }
}