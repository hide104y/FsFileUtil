package tool;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Arrays;
import java.util.UUID;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlFile;

@DisplayName("ClsFsAttrib 単体テスト")
class ClsFsAttribTest {

    private Path testRoot;
    private String logFile;
    private ClsLogger logger;

    @BeforeEach
    void setUp() throws IOException {
        testRoot = Files.createTempDirectory("UnitTest_FsFileUtil_ClsFsAttrib_" + UUID.randomUUID().toString());
        logFile = testRoot.resolve("test.log").toString();
        logger = new ClsLogger();
        logger.setValueByKey(ClsLogger.IS_FILE, "true");
        logger.setValueByKey(ClsLogger.PATH, logFile);
        logger.setValueByKey(ClsLogger.IS_CONSOLE, "false");
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

    private String createFileWithSize(String dirPath, String fileName, int byteSize) throws IOException {
        Path dir = Path.of(dirPath);
        Files.createDirectories(dir);
        Path file = dir.resolve(fileName);
        byte[] bytes = new byte[byteSize];
        Arrays.fill(bytes, (byte) 0x41);
        Files.write(file, bytes);
        return file.toString();
    }

    private String getLogContent() {
        try {
            File f = new File(logFile);
            if (f.exists()) {
                return Files.readString(f.toPath());
            }
        } catch (Exception ignored) {
        }
        return "";
    }

    @Test
    @DisplayName("コンストラクタ初期値のテスト")
    void testConstructorInitialValues() {
        ClsFsAttrib attrib = new ClsFsAttrib(logger);

        Assertions.assertEquals(0L, attrib.getDirectoryCount());
        Assertions.assertEquals(0L, attrib.getFileCount());
        Assertions.assertEquals(0L, attrib.getTotalSize());
        Assertions.assertEquals(0L, attrib.getErrorDirectoryCount());
        Assertions.assertEquals(0L, attrib.getErrorFileCount());
        Assertions.assertFalse(attrib.isProgressEnabled());
        Assertions.assertEquals(0, attrib.getProgressIntervalDirectories());
        Assertions.assertEquals(0, attrib.getProgressIntervalFiles());
    }

    @Test
    @DisplayName("プロパティの Getter/Setter テスト")
    void testPropertiesGetAndSet() {
        ClsFsAttrib attrib = new ClsFsAttrib(logger);

        attrib.setDirectoryCount(10L);
        attrib.setFileCount(20L);
        attrib.setTotalSize(3000L);
        attrib.setErrorDirectoryCount(1L);
        attrib.setErrorFileCount(2L);
        attrib.setProgressEnabled(true);
        attrib.setProgressIntervalDirectories(100);
        attrib.setProgressIntervalFiles(200);

        Assertions.assertEquals(10L, attrib.getDirectoryCount());
        Assertions.assertEquals(20L, attrib.getFileCount());
        Assertions.assertEquals(3000L, attrib.getTotalSize());
        Assertions.assertEquals(1L, attrib.getErrorDirectoryCount());
        Assertions.assertEquals(2L, attrib.getErrorFileCount());
        Assertions.assertTrue(attrib.isProgressEnabled());
        Assertions.assertEquals(100, attrib.getProgressIntervalDirectories());
        Assertions.assertEquals(200, attrib.getProgressIntervalFiles());
    }

    @Test
    @DisplayName("ClearCounter のテスト")
    void testClearCounter() {
        ClsFsAttrib attrib = new ClsFsAttrib(logger);
        attrib.setDirectoryCount(15L);
        attrib.setFileCount(25L);
        attrib.setTotalSize(9999L);
        attrib.setErrorDirectoryCount(3L);
        attrib.setErrorFileCount(4L);
        attrib.setProgressEnabled(true);
        attrib.setProgressIntervalDirectories(50);
        attrib.setProgressIntervalFiles(100);

        attrib.clearCounter();

        Assertions.assertEquals(0L, attrib.getDirectoryCount());
        Assertions.assertEquals(0L, attrib.getFileCount());
        Assertions.assertEquals(0L, attrib.getTotalSize());
        Assertions.assertEquals(0L, attrib.getErrorDirectoryCount());
        Assertions.assertEquals(0L, attrib.getErrorFileCount());
        Assertions.assertTrue(attrib.isProgressEnabled());
        Assertions.assertEquals(50, attrib.getProgressIntervalDirectories());
        Assertions.assertEquals(100, attrib.getProgressIntervalFiles());
    }

    @Test
    @DisplayName("空ディレクトリに対する CalculateDirectorySize テスト")
    void testCalculateDirectorySizeEmptyDirectory() throws IOException {
        Path emptyDir = testRoot.resolve("empty_dir");
        Files.createDirectories(emptyDir);
        ClsFsAttrib attrib = new ClsFsAttrib(logger);

        boolean result = attrib.calculateDirectorySize(emptyDir.toString(), false, 0, false);

        Assertions.assertTrue(result);
        Assertions.assertEquals(1L, attrib.getDirectoryCount());
        Assertions.assertEquals(0L, attrib.getFileCount());
        Assertions.assertEquals(0L, attrib.getTotalSize());
        Assertions.assertEquals(0L, attrib.getErrorDirectoryCount());
        Assertions.assertEquals(0L, attrib.getErrorFileCount());
    }

    @Test
    @DisplayName("単一階層ディレクトリに対する CalculateDirectorySize テスト")
    void testCalculateDirectorySizeSingleLevel() throws IOException {
        String dir = testRoot.resolve("single_level").toString();
        createFileWithSize(dir, "file1.txt", 100);
        createFileWithSize(dir, "file2.txt", 250);
        createFileWithSize(dir, "file3.txt", 50);

        ClsFsAttrib attrib = new ClsFsAttrib(logger);
        boolean result = attrib.calculateDirectorySize(dir, false, 0, false);

        Assertions.assertTrue(result);
        Assertions.assertEquals(1L, attrib.getDirectoryCount());
        Assertions.assertEquals(3L, attrib.getFileCount());
        Assertions.assertEquals(400L, attrib.getTotalSize());
        Assertions.assertEquals(0L, attrib.getErrorDirectoryCount());
        Assertions.assertEquals(0L, attrib.getErrorFileCount());
    }

    @Test
    @DisplayName("再帰的階層ディレクトリに対する CalculateDirectorySize テスト")
    void testCalculateDirectorySizeNested() throws IOException {
        String rootDir = testRoot.resolve("nested_root").toString();
        String subDir1 = Path.of(rootDir, "sub1").toString();
        String subDir2 = Path.of(subDir1, "sub2").toString();

        createFileWithSize(rootDir, "root_file.txt", 100);
        createFileWithSize(subDir1, "sub1_file.txt", 200);
        createFileWithSize(subDir2, "sub2_file.txt", 300);

        ClsFsAttrib attrib = new ClsFsAttrib(logger);
        boolean result = attrib.calculateDirectorySize(rootDir, false, 0, false);

        Assertions.assertTrue(result);
        Assertions.assertEquals(3L, attrib.getDirectoryCount());
        Assertions.assertEquals(3L, attrib.getFileCount());
        Assertions.assertEquals(600L, attrib.getTotalSize());
        Assertions.assertEquals(0L, attrib.getErrorDirectoryCount());
        Assertions.assertEquals(0L, attrib.getErrorFileCount());
    }

    @Test
    @DisplayName("存在しないディレクトリに対する CalculateDirectorySize テスト")
    void testCalculateDirectorySizeNonExistent() {
        String nonExistentDir = testRoot.resolve("does_not_exist_" + UUID.randomUUID().toString()).toString();
        ClsFsAttrib attrib = new ClsFsAttrib(logger);

        boolean result = attrib.calculateDirectorySize(nonExistentDir, false, 1, true);

        Assertions.assertFalse(result);
        Assertions.assertEquals(1L, attrib.getDirectoryCount());
        Assertions.assertEquals(1L, attrib.getErrorDirectoryCount());

        String logs = getLogContent();
        Assertions.assertTrue(logs.contains("SKIP DIR"));
    }

    @Test
    @DisplayName("単一ファイルに対する CalculateFileSize テスト")
    void testCalculateFileSizeSingleFile() throws IOException {
        String dir = testRoot.resolve("file_calc").toString();
        String file = createFileWithSize(dir, "test.dat", 1024);

        ClsFsAttrib attrib = new ClsFsAttrib(logger);
        boolean result = attrib.calculateFileSize(file, false, 0, false);

        Assertions.assertTrue(result);
        Assertions.assertEquals(1L, attrib.getFileCount());
        Assertions.assertEquals(1024L, attrib.getTotalSize());
        Assertions.assertEquals(0L, attrib.getErrorFileCount());
    }

    @Test
    @DisplayName("存在しないファイルに対する CalculateFileSize テスト")
    void testCalculateFileSizeNonExistent() {
        String nonExistentFile = testRoot.resolve("no_file_" + UUID.randomUUID().toString() + ".tmp").toString();
        ClsFsAttrib attrib = new ClsFsAttrib(logger);

        boolean result = attrib.calculateFileSize(nonExistentFile, false, 1, true);

        Assertions.assertFalse(result);
        Assertions.assertEquals(1L, attrib.getErrorFileCount());
        Assertions.assertEquals(0L, attrib.getFileCount());

        String logs = getLogContent();
        Assertions.assertTrue(logs.contains("SKIP FILE"));
    }

    @Test
    @DisplayName("所有者出力 OutputDirectoryOwner テスト")
    void testOutputDirectoryOwner() throws IOException {
        String dir = testRoot.resolve("owner_dir").toString();
        Files.createDirectories(Path.of(dir));

        ClsFsAttrib attrib = new ClsFsAttrib(logger);
        boolean result = attrib.outputDirectoryOwner(dir, 0, true, false);

        Assertions.assertTrue(result);
        String logs = getLogContent();
        Assertions.assertTrue(logs.contains("OWNER"));
    }

    @Test
    @DisplayName("アクセス許可出力 OutputDirectoryPermission テスト")
    void testOutputDirectoryPermission() throws IOException {
        String dir = testRoot.resolve("perm_dir").toString();
        Files.createDirectories(Path.of(dir));

        ClsFsAttrib attrib = new ClsFsAttrib(logger);
        boolean result = attrib.outputDirectoryPermission(dir, 0, true, false);

        Assertions.assertTrue(result);
        String logs = getLogContent();
        Assertions.assertTrue(logs.contains(dir));
    }
}