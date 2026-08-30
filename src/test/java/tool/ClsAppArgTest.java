package tool;

import java.io.ByteArrayOutputStream;
import java.io.PrintStream;
import java.nio.charset.StandardCharsets;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import tool.cmnclslib.cls.ClsLogger;

@DisplayName("ClsAppArg 単体テスト")
class ClsAppArgTest {

    private ClsLogger logger;
    private ClsBaseDir prop;
    private ClsAppArg appArg;
    private ByteArrayOutputStream outContent;
    private PrintStream originalOut;

    @BeforeEach
    void setUp() {
        originalOut = System.out;
        outContent = new ByteArrayOutputStream();
        System.setOut(new PrintStream(outContent, true, StandardCharsets.UTF_8));

        logger = new ClsLogger();
        prop = new ClsBaseDir();
        appArg = new ClsAppArg(logger, prop);
    }

    @AfterEach
    void tearDown() {
        System.setOut(originalOut);
    }

    @Test
    @DisplayName("デフォルトコンストラクタおよびプロパティテスト")
    void testConstructorAndProperties() {
        Assertions.assertSame(prop, appArg.getProperties());
        Assertions.assertFalse(appArg.isUsage());
        Assertions.assertFalse(appArg.isEchoRetcode());

        appArg.setUsage(true);
        appArg.setEchoRetcode(true);
        appArg.setExeBaseName("CustomFsUtil");
        appArg.setExeDir("/custom/dir");

        Assertions.assertTrue(appArg.isUsage());
        Assertions.assertTrue(appArg.isEchoRetcode());
        Assertions.assertEquals("CustomFsUtil", appArg.getExeBaseName());
        Assertions.assertEquals("/custom/dir", appArg.getExeDir());
    }

    @Test
    @DisplayName("基本コピー引数解析テスト (-f, -t, -a copy)")
    void testParseBasicCopyArgs() {
        String[] args = new String[] { "-f", "C:\\source", "-t", "C:\\dest", "-a", "copy" };
        boolean result = appArg.parse(args);

        Assertions.assertTrue(result);
        Assertions.assertEquals(ClsBaseDir.ACTION_COPY, prop.getActionCode());
        Assertions.assertEquals("copy", prop.getAction());
        Assertions.assertTrue(prop.getSourcePath().contains("source"));
        Assertions.assertTrue(prop.getDestinationPath().contains("dest"));
    }

    @Test
    @DisplayName("差分コピー引数解析テスト (-m sha1, -ts, -list)")
    void testParseDiffCopyArgs() {
        String[] args = new String[] { "-f", "C:\\src", "-t", "C:\\dst", "-m", "sha1", "-ts", "-list" };
        boolean result = appArg.parse(args);

        Assertions.assertTrue(result);
        Assertions.assertEquals(ClsBaseDir.CHECK_SHA1, prop.getCheckLogic());
        Assertions.assertEquals(3, prop.getCpTimestamp());
        Assertions.assertTrue(prop.isList());
    }

    @Test
    @DisplayName("フィルター引数解析テスト (-max 5, -min 2, -if *.txt)")
    void testParseFilterArgs() {
        String[] args = new String[] { "-f", "C:\\src", "-a", "find", "-max", "5", "-min", "2" };
        boolean result = appArg.parse(args);

        Assertions.assertTrue(result);
        Assertions.assertEquals(ClsBaseDir.ACTION_FIND, prop.getActionCode());
        Assertions.assertEquals(5, prop.getMaxDepth());
        Assertions.assertEquals(2, prop.getMinDepth());
    }

    @Test
    @DisplayName("拡張引数パーステスト (-fileshare, -cat-*, -sort, -desc, -show-dir, -echo-retcd, -ret-files)")
    void testParseExtendedArgs() {
        String[] args = new String[] {
            "-f", "C:\\src", "-t", "C:\\dst",
            "-fileshare", "readwrite|delete",
            "-cat-i", "search_text", "-cat-n",
            "-sort", "mtime", "-desc",
            "-show-dir", "5",
            "-echo-retcd",
            "-ret-files",
            "-show-cmd", "false",
            "-show-output", "no",
            "-show-retcd", "n"
        };
        boolean result = appArg.parse(args);

        Assertions.assertTrue(result);
        Assertions.assertEquals(ClsBaseDir.FILE_SHARE_READ_WRITE | ClsBaseDir.FILE_SHARE_DELETE, prop.getFileShare());
        Assertions.assertTrue(prop.isCat());
        Assertions.assertEquals("search_text", prop.getCatI());
        Assertions.assertTrue(prop.getCatOptions().contains("-n"));
        Assertions.assertEquals(3, prop.getSortType()); // SORT_BY_MTIME = 3
        Assertions.assertFalse(prop.isAscending());
        Assertions.assertEquals(5, prop.getShowCurDir());
        Assertions.assertTrue(appArg.isEchoRetcode());
        Assertions.assertTrue(prop.isRetFiles());
        Assertions.assertFalse(prop.isShowCmd());
        Assertions.assertFalse(prop.isShowOutput());
        Assertions.assertFalse(prop.isShowExitCode());
    }

    @Test
    @DisplayName("showUsage() の全オプションカテゴリ出力検証テスト")
    void testShowUsageContainsAllOptionCategories() {
        appArg.showUsage();
        String output = outContent.toString(StandardCharsets.UTF_8);

        Assertions.assertTrue(output.contains("Usage : java -jar"));
        Assertions.assertTrue(output.contains("Basic Option："));
        Assertions.assertTrue(output.contains("-path|-f path"));
        Assertions.assertTrue(output.contains("-a    action"));
        Assertions.assertTrue(output.contains("Copy Option："));
        Assertions.assertTrue(output.contains("-t    path"));
        Assertions.assertTrue(output.contains("-m    check"));
        Assertions.assertTrue(output.contains("-fileshare mode"));
        Assertions.assertTrue(output.contains("-wait-retry-copy n"));
        Assertions.assertTrue(output.contains("-retry-syscopy n"));
        Assertions.assertTrue(output.contains("Symbolic Link Option："));
        Assertions.assertTrue(output.contains("-sym [0|1|2]"));
        Assertions.assertTrue(output.contains("-rel"));
        Assertions.assertTrue(output.contains("Backup Option："));
        Assertions.assertTrue(output.contains("-backup <path>"));
        Assertions.assertTrue(output.contains("-force"));
        Assertions.assertTrue(output.contains("Replace to path string Option："));
        Assertions.assertTrue(output.contains("-replace a:b,c:d"));
        Assertions.assertTrue(output.contains("-ts-f|-ts-t|-ts-b n"));
        Assertions.assertTrue(output.contains("Filter Option："));
        Assertions.assertTrue(output.contains("-max"));
        Assertions.assertTrue(output.contains("-min"));
        Assertions.assertTrue(output.contains("-term|-days value"));
        Assertions.assertTrue(output.contains("-period d|h|m|s"));
        Assertions.assertTrue(output.contains("-id|-idf 正規表現"));
        Assertions.assertTrue(output.contains("-xd|-xdf 正規表現"));
        Assertions.assertTrue(output.contains("-if 正規表現"));
        Assertions.assertTrue(output.contains("-xf 正規表現"));
        Assertions.assertTrue(output.contains("Copy With List File Option："));
        Assertions.assertTrue(output.contains("-files path"));
        Assertions.assertTrue(output.contains("-files-type type"));
        Assertions.assertTrue(output.contains("-files-Regex regex"));
        Assertions.assertTrue(output.contains("Find Or Commnad Exec Cmd Option："));
        Assertions.assertTrue(output.contains("-dq"));
        Assertions.assertTrue(output.contains("-type f|d|a"));
        Assertions.assertTrue(output.contains("-exec|-ps cmd {}"));
        Assertions.assertTrue(output.contains("-cat-i|x|p|e|xml-nl"));
        Assertions.assertTrue(output.contains("Wait Option："));
        Assertions.assertTrue(output.contains("-i interval"));
        Assertions.assertTrue(output.contains("-c count"));
        Assertions.assertTrue(output.contains("Rotate Option："));
        Assertions.assertTrue(output.contains("-k keep max"));
        Assertions.assertTrue(output.contains("Network Option："));
        Assertions.assertTrue(output.contains("-sec-range"));
        Assertions.assertFalse(output.contains("-su"));
        Assertions.assertFalse(output.contains("-mount path"));
        Assertions.assertFalse(output.contains("-drive [A-Z]"));
        Assertions.assertTrue(output.contains("Subfolder Sorting Option："));
        Assertions.assertTrue(output.contains("-sort type"));
        Assertions.assertTrue(output.contains("-desc"));
        Assertions.assertTrue(output.contains("Output Option："));
        Assertions.assertTrue(output.contains("-v|-vv|-brief"));
        Assertions.assertTrue(output.contains("-progress"));
        Assertions.assertTrue(output.contains("-diff"));
        Assertions.assertTrue(output.contains("-show show"));
        Assertions.assertTrue(output.contains("-op-path r|f|t|b"));
        Assertions.assertTrue(output.contains("-echo-retcd"));
        Assertions.assertTrue(output.contains("Other Option："));
        Assertions.assertTrue(output.contains("-ldir path"));
        Assertions.assertTrue(output.contains("-log  path"));
        Assertions.assertTrue(output.contains("-dumpargs"));
        Assertions.assertTrue(output.contains("-ret-files"));
        Assertions.assertTrue(output.contains("Format specifier conversion："));
        Assertions.assertTrue(output.contains("Return Code"));
    }

    @Test
    @DisplayName("printDefinition() の出力検証テスト")
    void testPrintDefinition() {
        String[] args = new String[] {
            "-f", "C:\\source", "-t", "C:\\dest", "-a", "sync", "-list"
        };
        appArg.parse(args);
        outContent.reset();

        appArg.printDefinition();
        String output = outContent.toString(StandardCharsets.UTF_8);

        Assertions.assertTrue(output.contains("TARGET PATH :"));
        Assertions.assertTrue(output.contains("TO PATH     :"));
        Assertions.assertTrue(output.contains("ACTION      : sync"));
        Assertions.assertTrue(output.contains("FILTER INC  :"));
        Assertions.assertTrue(output.contains("FILTER EXC  :"));
        Assertions.assertTrue(output.contains("LIST ONLY   : TRUE"));
    }
}