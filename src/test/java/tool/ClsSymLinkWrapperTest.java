package tool;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import tool.cmnclslib.cls.ClsLogger;

@DisplayName("ClsSymLinkWrapper 単体テスト")
class ClsSymLinkWrapperTest {

    @Test
    @DisplayName("プロパティのテスト")
    void testProperties() {
        ClsLogger logger = new ClsLogger();
        ClsSymLinkWrapper wrapper = new ClsSymLinkWrapper(logger);
        wrapper.setMessage("Initial Message");
        wrapper.setVerbose(2);
        wrapper.setSilent(true);

        Assertions.assertEquals("Initial Message", wrapper.getMessage());
        Assertions.assertEquals(2, wrapper.getVerbose());
        Assertions.assertTrue(wrapper.isSilent());
    }

    @Test
    @DisplayName("存在しないパスに対する GetRealPathIfExists テスト")
    void testGetRealPathIfExistsInvalidPath() {
        ClsLogger logger = new ClsLogger();
        ClsSymLinkWrapper wrapper = new ClsSymLinkWrapper(logger);
        wrapper.setVerbose(3);

        String nonExistentPath = "C:\\NonExistentDirectory_12345\\TestLink.lnk";
        String result = wrapper.getRealPathIfExists(nonExistentPath, false);

        Assertions.assertEquals("", result);
        Assertions.assertTrue(wrapper.getMessage().contains("NO SUCH A FILE OR DIRECTORY"));
    }

    @Test
    @DisplayName("空文字または空白パスに対する GetRealPathIfExists テスト")
    void testGetRealPathIfExistsNullOrEmptyPath() {
        ClsLogger logger = new ClsLogger();
        ClsSymLinkWrapper wrapper = new ClsSymLinkWrapper(logger);

        String resultEmpty = wrapper.getRealPathIfExists("", false);
        Assertions.assertEquals("", resultEmpty);

        String resultWhitespace = wrapper.getRealPathIfExists("   ", false);
        Assertions.assertEquals("", resultWhitespace);
    }

    @Test
    @DisplayName("サイレントモードでの WriteLine テスト")
    void testWriteLineSilentMode() {
        ClsLogger logger = new ClsLogger();
        ClsSymLinkWrapper wrapper = new ClsSymLinkWrapper(logger);
        wrapper.setSilent(true);

        wrapper.writeLine(0, "Test Silent Message");
    }
}