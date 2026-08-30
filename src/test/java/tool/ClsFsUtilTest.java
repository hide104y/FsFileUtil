package tool;

import java.io.File;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.MessageDigest;
import java.util.Random;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlFile;

@DisplayName("ClsFsUtil 単体テスト")
class ClsFsUtilTest {

    private Path testRoot;
    private ClsLogger logger;

    @BeforeEach
    void setUp() throws IOException {
        testRoot = Files.createTempDirectory("UnitTest_FsFileUtil_ClsFsUtil_" + UUID.randomUUID().toString());
        logger = new ClsLogger();
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

    private String createTestFile(String fileName, String content) throws IOException {
        Path filePath = testRoot.resolve(fileName);
        if (filePath.getParent() != null && !Files.exists(filePath.getParent())) {
            Files.createDirectories(filePath.getParent());
        }
        Files.writeString(filePath, content, StandardCharsets.UTF_8);
        return filePath.toString();
    }

    private String createTestFileWithBytes(String fileName, byte[] bytes) throws IOException {
        Path filePath = testRoot.resolve(fileName);
        if (filePath.getParent() != null && !Files.exists(filePath.getParent())) {
            Files.createDirectories(filePath.getParent());
        }
        Files.write(filePath, bytes);
        return filePath.toString();
    }

    @Test
    @DisplayName("コンストラクタおよびプロパティ初期値テスト")
    void testConstructorAndProperties() {
        ClsFsUtil util = new ClsFsUtil(logger);
        Assertions.assertEquals("", util.getMessage());
        Assertions.assertEquals("", util.getResult());
        Assertions.assertFalse(util.isStackTrace());
        Assertions.assertEquals(0, util.getVerbose());
        Assertions.assertEquals(200, util.getWaitMSecForRetryCopy());
        Assertions.assertEquals(0, util.getRetryMax());

        util.setMessage("CustomMessage");
        Assertions.assertEquals("CustomMessage", util.getMessage());

        util.setResult("CustomResult");
        Assertions.assertEquals("CustomResult", util.getResult());

        util.setStackTrace(true);
        Assertions.assertTrue(util.isStackTrace());

        util.setVerbose(2);
        Assertions.assertEquals(2, util.getVerbose());

        util.setWaitMSecForRetryCopy(500);
        Assertions.assertEquals(500, util.getWaitMSecForRetryCopy());

        util.setRetryMax(3);
        Assertions.assertEquals(3, util.getRetryMax());
    }

    @Test
    @DisplayName("Rotate 単一世代ローテーションテスト")
    void testRotateSingleGeneration() throws IOException {
        ClsFsUtil util = new ClsFsUtil(logger);
        String baseFile = createTestFile("rotate_single.log", "gen0");

        int status = util.rotate(baseFile, 1);

        Assertions.assertEquals(MdlConst.LVL_I, status);
        Assertions.assertFalse(Files.exists(Path.of(baseFile)));
        Assertions.assertTrue(Files.exists(Path.of(baseFile + ".1")));
        Assertions.assertEquals("gen0", Files.readString(Path.of(baseFile + ".1"), StandardCharsets.UTF_8));
    }

    @Test
    @DisplayName("Rotate 複数世代ローテーションテスト")
    void testRotateMultipleGenerations() throws IOException {
        ClsFsUtil util = new ClsFsUtil(logger);
        String baseFile = createTestFile("app.log", "generation 0");
        createTestFile("app.log.1", "generation 1");
        createTestFile("app.log.2", "generation 2");
        createTestFile("app.log.3", "generation 3 (to be deleted)");

        int status = util.rotate(baseFile, 3);

        Assertions.assertEquals(MdlConst.LVL_I, status);
        Assertions.assertFalse(Files.exists(Path.of(baseFile)));
        Assertions.assertTrue(Files.exists(Path.of(baseFile + ".1")));
        Assertions.assertTrue(Files.exists(Path.of(baseFile + ".2")));
        Assertions.assertTrue(Files.exists(Path.of(baseFile + ".3")));
        Assertions.assertFalse(Files.exists(Path.of(baseFile + ".4")));

        Assertions.assertEquals("generation 0", Files.readString(Path.of(baseFile + ".1"), StandardCharsets.UTF_8));
        Assertions.assertEquals("generation 1", Files.readString(Path.of(baseFile + ".2"), StandardCharsets.UTF_8));
        Assertions.assertEquals("generation 2", Files.readString(Path.of(baseFile + ".3"), StandardCharsets.UTF_8));
    }

    @Test
    @DisplayName("WaitUntilFileExists 存在するファイルテスト")
    void testWaitUntilFileExists() throws IOException {
        ClsFsUtil util = new ClsFsUtil(logger);
        String filePath = createTestFile("exists.txt", "content");

        boolean result = util.waitUntilFileExists(filePath, 3, 0);
        Assertions.assertTrue(result);
    }

    @Test
    @DisplayName("WaitUntilFileExists 存在しないファイルテスト")
    void testWaitUntilFileExistsNotFound() {
        ClsFsUtil util = new ClsFsUtil(logger);
        String filePath = testRoot.resolve("not_found.txt").toString();

        boolean result = util.waitUntilFileExists(filePath, 2, 0);
        Assertions.assertFalse(result);
    }

    @Test
    @DisplayName("WaitUntilFileExists 待機中に作成されるファイルテスト")
    void testWaitUntilFileExistsCreatedDuringWait() {
        ClsFsUtil util = new ClsFsUtil(logger);
        String delayedFilePath = testRoot.resolve("delayed.txt").toString();

        CompletableFuture.runAsync(() -> {
            try {
                Thread.sleep(300);
                Files.writeString(Path.of(delayedFilePath), "created");
            } catch (Exception ignored) {
            }
        });

        boolean result = util.waitUntilFileExists(delayedFilePath, 3, 1);
        Assertions.assertTrue(result);
    }

    @Test
    @DisplayName("Rename ファイル移動テスト")
    void testRenameFile() throws IOException {
        ClsFsUtil util = new ClsFsUtil(logger);
        String sourcePath = createTestFile("rename_src.txt", "hello rename");
        String destinationPath = testRoot.resolve("rename_dst.txt").toString();

        boolean result = util.rename(sourcePath, destinationPath);

        Assertions.assertTrue(result);
        Assertions.assertFalse(Files.exists(Path.of(sourcePath)));
        Assertions.assertTrue(Files.exists(Path.of(destinationPath)));
        Assertions.assertEquals("hello rename", Files.readString(Path.of(destinationPath), StandardCharsets.UTF_8));
    }

    @Test
    @DisplayName("Rename ディレクトリ移動テスト")
    void testRenameDirectory() throws IOException {
        ClsFsUtil util = new ClsFsUtil(logger);
        Path sourceDir = testRoot.resolve("src_dir");
        Files.createDirectories(sourceDir);
        createTestFile(Path.of("src_dir", "sub.txt").toString(), "sub content");
        String destinationDir = testRoot.resolve("dst_dir").toString();

        boolean result = util.rename(sourceDir.toString(), destinationDir);

        Assertions.assertTrue(result);
        Assertions.assertFalse(Files.exists(sourceDir));
        Assertions.assertTrue(Files.exists(Path.of(destinationDir)));
        Assertions.assertTrue(Files.exists(Path.of(destinationDir, "sub.txt")));
    }

    @Test
    @DisplayName("ComputeSha1Hash テスト")
    void testComputeSha1Hash() throws Exception {
        ClsFsUtil util = new ClsFsUtil(logger);
        byte[] testBytes = "SHA1 Test Data 12345".getBytes(StandardCharsets.UTF_8);
        String filePath = createTestFileWithBytes("sha1_test.bin", testBytes);

        MessageDigest md = MessageDigest.getInstance("SHA-1");
        byte[] hash = md.digest(testBytes);
        StringBuilder sb = new StringBuilder();
        for (byte b : hash) {
            sb.append(String.format("%02x", b));
        }
        String expectedHash = sb.toString();

        String actualHash = util.computeSha1Hash(filePath);
        Assertions.assertEquals(expectedHash, actualHash);
    }

    @Test
    @DisplayName("CopyFileWithRetry 正常コピーテスト")
    void testCopyFileWithRetry() throws IOException {
        ClsFsUtil util = new ClsFsUtil(logger);
        util.setRetryMax(2);
        util.setWaitMSecForRetryCopy(10);
        String sourcePath = createTestFile("retry_src.txt", "content to copy");
        String destinationPath = testRoot.resolve("retry_dst.txt").toString();

        util.copyFileWithRetry(sourcePath, destinationPath);

        Assertions.assertTrue(Files.exists(Path.of(destinationPath)));
        Assertions.assertEquals("content to copy", Files.readString(Path.of(destinationPath), StandardCharsets.UTF_8));
    }

    @Test
    @DisplayName("BinaryCopy 小さいファイルテスト")
    void testBinaryCopySmall() throws IOException {
        ClsFsUtil util = new ClsFsUtil(logger);
        byte[] data = "Small Binary Data String 1234567890".getBytes(StandardCharsets.UTF_8);
        String sourcePath = createTestFileWithBytes("small.bin", data);
        String destinationPath = testRoot.resolve("small_dst.bin").toString();

        util.binaryCopy(sourcePath, destinationPath, false);

        Assertions.assertTrue(Files.exists(Path.of(destinationPath)));
        Assertions.assertArrayEquals(data, Files.readAllBytes(Path.of(destinationPath)));
    }

    @Test
    @DisplayName("BinaryCopy 大きいファイルテスト")
    void testBinaryCopyLarge() throws IOException {
        ClsFsUtil util = new ClsFsUtil(logger);
        byte[] data = new byte[256 * 1024];
        new Random(42).nextBytes(data);
        String sourcePath = createTestFileWithBytes("large.bin", data);
        String destinationPath = testRoot.resolve("large_dst.bin").toString();

        util.binaryCopy(sourcePath, destinationPath, true, ClsBaseDir.FILE_SHARE_READ);

        Assertions.assertTrue(Files.exists(Path.of(destinationPath)));
        Assertions.assertArrayEquals(data, Files.readAllBytes(Path.of(destinationPath)));
    }

    @Test
    @DisplayName("AsyncCopy テスト")
    void testAsyncCopy() throws IOException {
        ClsFsUtil util = new ClsFsUtil(logger);
        byte[] data = "Async Copy Test Content".getBytes(StandardCharsets.UTF_8);
        String sourcePath = createTestFileWithBytes("async_small.bin", data);
        String destinationPath = testRoot.resolve("async_small_dst.bin").toString();

        util.asyncCopy(sourcePath, destinationPath, false);

        Assertions.assertTrue(Files.exists(Path.of(destinationPath)));
        Assertions.assertArrayEquals(data, Files.readAllBytes(Path.of(destinationPath)));
    }
}