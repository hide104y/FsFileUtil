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

@DisplayName("ClsActionCtrl 単体テスト")
class ClsActionCtrlTest {

    private Path testRoot;
    private ClsLogger logger;
    private ClsBaseDir prop;
    private ClsActionCtrl actionCtrl;

    @BeforeEach
    void setUp() throws IOException {
        testRoot = Files.createTempDirectory("UnitTest_FsFileUtil_ClsActionCtrl_" + UUID.randomUUID().toString());
        logger = new ClsLogger();
        prop = new ClsBaseDir();
        actionCtrl = new ClsActionCtrl(logger, prop);
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
    @DisplayName("ACTION_MKDIR 動作テスト")
    void testActionMkdir() {
        String newDir = testRoot.resolve("mkdir_test").toString();
        prop.setActionCode(ClsBaseDir.ACTION_MKDIR);
        prop.setSourcePath(newDir);

        int result = actionCtrl.execute();

        Assertions.assertEquals(MdlConst.LVL_I, result);
        Assertions.assertTrue(Files.exists(Path.of(newDir)));
    }

    @Test
    @DisplayName("ACTION_TOUCH 動作テスト")
    void testActionTouch() {
        String newFile = testRoot.resolve("touch_test.txt").toString();
        prop.setActionCode(ClsBaseDir.ACTION_TOUCH);
        prop.setSourcePath(newFile);

        int result = actionCtrl.execute();

        Assertions.assertEquals(MdlConst.LVL_I, result);
        Assertions.assertTrue(Files.exists(Path.of(newFile)));
    }

    @Test
    @DisplayName("ACTION_EXIST 動作テスト")
    void testActionExist() throws IOException {
        String existingFile = createTestFile("exist.txt", "data");
        prop.setActionCode(ClsBaseDir.ACTION_EXIST);
        prop.setSourcePath(existingFile);

        int result = actionCtrl.execute();

        Assertions.assertEquals(MdlConst.LVL_I, result);

        prop.setSourcePath(testRoot.resolve("not_found.txt").toString());
        int notFoundResult = actionCtrl.execute();
        Assertions.assertEquals(MdlConst.LVL_E, notFoundResult);
    }

    @Test
    @DisplayName("ACTION_COPY 動作テスト")
    void testActionCopy() throws IOException {
        String srcFile = createTestFile("src_file.txt", "hello copy");
        String dstFile = testRoot.resolve("dst_file.txt").toString();

        prop.setActionCode(ClsBaseDir.ACTION_COPY);
        prop.setSourcePath(srcFile);
        prop.setDestinationPath(dstFile);
        prop.setPathType(MdlFile.PATH_IS_FILE);

        int result = actionCtrl.execute();

        Assertions.assertEquals(MdlConst.LVL_I, result);
        Assertions.assertTrue(Files.exists(Path.of(dstFile)));
        Assertions.assertEquals("hello copy", Files.readString(Path.of(dstFile), StandardCharsets.UTF_8));
    }

    @Test
    @DisplayName("ACTION_DELETE 動作テスト")
    void testActionDelete() throws IOException {
        String fileToDelete = createTestFile("delete_me.txt", "delete me");

        prop.setActionCode(ClsBaseDir.ACTION_DELETE);
        prop.setSourcePath(fileToDelete);
        prop.setTypeCode(MdlConst.INT_TYPE_ALL);

        int result = actionCtrl.execute();

        Assertions.assertEquals(MdlConst.LVL_I, result);
        Assertions.assertFalse(Files.exists(Path.of(fileToDelete)));
    }

    @Test
    @DisplayName("ACTION_GET_REAL_PATH 動作テスト")
    void testActionGetRealPath() throws IOException {
        String testFile = createTestFile("real_path_test.txt", "content");

        prop.setActionCode(ClsBaseDir.ACTION_GET_REAL_PATH);
        prop.setSourcePath(testFile);
        prop.setPathType(MdlFile.PATH_IS_FILE);
        prop.setVerbose(0);

        int result = actionCtrl.execute();
        Assertions.assertEquals(MdlConst.LVL_I, result);
    }

    @Test
    @DisplayName("ACTION_LS 動作テスト")
    void testActionLs() throws IOException {
        createTestFile("ls_dir/file1.txt", "f1");
        createTestFile("ls_dir/file2.txt", "f2");

        prop.setActionCode(ClsBaseDir.ACTION_LS);
        prop.setSourcePath(testRoot.resolve("ls_dir").toString());
        prop.setTypeCode(MdlConst.INT_TYPE_ALL);

        int result = actionCtrl.execute();
        Assertions.assertEquals(MdlConst.LVL_I, result);
        Assertions.assertTrue(prop.getFiles() >= 2);
    }

    @Test
    @DisplayName("ACTION_GET_SIZE 動作テスト")
    void testActionGetSize() throws IOException {
        createTestFile("size_dir/file.txt", "12345");

        prop.setActionCode(ClsBaseDir.ACTION_GET_SIZE);
        prop.setSourcePath(testRoot.resolve("size_dir").toString());
        prop.setShowSize(true);

        int result = actionCtrl.execute();
        Assertions.assertEquals(MdlConst.LVL_I, result);
    }

    @Test
    @DisplayName("ACTION_GET_PERM および ACTION_GET_OWNER 動作テスト")
    void testActionGetPermAndOwner() throws IOException {
        String dir = testRoot.resolve("perm_test").toString();
        Files.createDirectories(Path.of(dir));

        prop.setActionCode(ClsBaseDir.ACTION_GET_PERM);
        prop.setSourcePath(dir);
        prop.setShowPerm(true);
        prop.setShowOwner(true);

        int result = actionCtrl.execute();
        Assertions.assertEquals(MdlConst.LVL_I, result);
    }
}