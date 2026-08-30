package tool;

import java.time.LocalDateTime;
import java.util.List;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.CsvSource;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlFile;

@DisplayName("ClsBaseDir 単体テスト")
class ClsBaseDirTest {

    @Test
    @DisplayName("定数値の検証テスト")
    void testConstants() {
        Assertions.assertEquals(0, ClsBaseDir.RELATIVE);
        Assertions.assertEquals(1, ClsBaseDir.FROM);
        Assertions.assertEquals(2, ClsBaseDir.TO);
        Assertions.assertEquals(3, ClsBaseDir.BOTH);

        Assertions.assertEquals(0, ClsBaseDir.FILES_RELATIVE);
        Assertions.assertEquals(1, ClsBaseDir.FILES_FULL);

        Assertions.assertEquals(0, ClsBaseDir.COMPARISON_NO);
        Assertions.assertEquals(1, ClsBaseDir.COMPARISON_EQ);
        Assertions.assertEquals(2, ClsBaseDir.COMPARISON_GT);
        Assertions.assertEquals(3, ClsBaseDir.COMPARISON_GE);
        Assertions.assertEquals(4, ClsBaseDir.COMPARISON_LT);
        Assertions.assertEquals(5, ClsBaseDir.COMPARISON_LE);

        Assertions.assertEquals(0, ClsBaseDir.DATETIME_NOW);
        Assertions.assertEquals(1, ClsBaseDir.DATETIME_TODAY);
        Assertions.assertEquals(2, ClsBaseDir.DATETIME_YESTERDAY);
        Assertions.assertEquals(3, ClsBaseDir.DATETIME_FILEINFO);

        Assertions.assertEquals(0, ClsBaseDir.EXEC_MODE_NORMAL);
        Assertions.assertEquals(1, ClsBaseDir.EXEC_MODE_CMD);
        Assertions.assertEquals(2, ClsBaseDir.EXEC_MODE_PS);
        Assertions.assertEquals(3, ClsBaseDir.EXEC_MODE_PSC);
        Assertions.assertEquals(4, ClsBaseDir.EXEC_MODE_EXE);

        Assertions.assertEquals(0, ClsBaseDir.CHECK_FILE_LOCK_NONE);
        Assertions.assertEquals(1, ClsBaseDir.CHECK_FILE_LOCK_SAMPLE);
        Assertions.assertEquals(2, ClsBaseDir.CHECK_FILE_LOCK_SKIP);

        Assertions.assertEquals(0, ClsBaseDir.COPY_ASYNC);
        Assertions.assertEquals(1, ClsBaseDir.COPY_BINARY);
        Assertions.assertEquals(2, ClsBaseDir.COPY_OS_CMD);

        Assertions.assertEquals(-1, ClsBaseDir.ACTION_NONE);
        Assertions.assertEquals(0, ClsBaseDir.ACTION_COPY);
        Assertions.assertEquals(1, ClsBaseDir.ACTION_MOVE);
        Assertions.assertEquals(2, ClsBaseDir.ACTION_SYNC);
        Assertions.assertEquals(10, ClsBaseDir.ACTION_MKDIR);
        Assertions.assertEquals(11, ClsBaseDir.ACTION_TOUCH);
        Assertions.assertEquals(12, ClsBaseDir.ACTION_DELETE);
        Assertions.assertEquals(13, ClsBaseDir.ACTION_MKLINK);
        Assertions.assertEquals(15, ClsBaseDir.ACTION_LS);
        Assertions.assertEquals(16, ClsBaseDir.ACTION_FIND);
        Assertions.assertEquals(17, ClsBaseDir.ACTION_GET_REAL_PATH);
        Assertions.assertEquals(18, ClsBaseDir.ACTION_LIST_LOCK_PROC);
        Assertions.assertEquals(20, ClsBaseDir.ACTION_EXIST);
        Assertions.assertEquals(21, ClsBaseDir.ACTION_EXIST_DIR);
        Assertions.assertEquals(22, ClsBaseDir.ACTION_EXIST_FILE);
        Assertions.assertEquals(23, ClsBaseDir.ACTION_WAIT);
        Assertions.assertEquals(24, ClsBaseDir.ACTION_FILE_LOCKED);
        Assertions.assertEquals(30, ClsBaseDir.ACTION_RENAME);
        Assertions.assertEquals(31, ClsBaseDir.ACTION_ROTATE);
        Assertions.assertEquals(41, ClsBaseDir.ACTION_GET_ATTRIB);
        Assertions.assertEquals(42, ClsBaseDir.ACTION_GET_SIZE);
        Assertions.assertEquals(43, ClsBaseDir.ACTION_GET_PERM);
        Assertions.assertEquals(44, ClsBaseDir.ACTION_GET_OWNER);
        Assertions.assertEquals(91, ClsBaseDir.ACTION_EXEC);
        Assertions.assertEquals(99, ClsBaseDir.ACTION_ETC);

        Assertions.assertEquals(0, ClsBaseDir.CHECK_NONE);
        Assertions.assertEquals(1, ClsBaseDir.CHECK_SIZE);
        Assertions.assertEquals(2, ClsBaseDir.CHECK_MTIME);
        Assertions.assertEquals(3, ClsBaseDir.CHECK_MTIME_NEW);
        Assertions.assertEquals(4, ClsBaseDir.CHECK_MTIME_OLD);
        Assertions.assertEquals(5, ClsBaseDir.CHECK_CKSUM);
        Assertions.assertEquals(6, ClsBaseDir.CHECK_SHA1);
        Assertions.assertEquals(7, ClsBaseDir.CHECK_ADLER32);
        Assertions.assertEquals(8, ClsBaseDir.CHECK_EXIST);

        Assertions.assertEquals(0, ClsBaseDir.TASK_CP);
        Assertions.assertEquals(1, ClsBaseDir.TASK_MV);
        Assertions.assertEquals(2, ClsBaseDir.TASK_RM);
        Assertions.assertEquals(3, ClsBaseDir.TASK_PRINT);
        Assertions.assertEquals(4, ClsBaseDir.TASK_RENAME);
    }

    @Test
    @DisplayName("コンストラクタおよびプロパティ初期値のテスト")
    void testConstructorInitialValues() {
        ClsBaseDir prop = new ClsBaseDir();

        Assertions.assertEquals("", prop.getExeBaseName());
        Assertions.assertEquals("", prop.getExeDir());
        Assertions.assertEquals("", prop.getSourcePath());
        Assertions.assertEquals("", prop.getDestinationPath());
        Assertions.assertEquals("", prop.getWorkDir());
        Assertions.assertEquals("copy", prop.getAction());
        Assertions.assertEquals("", prop.getMode());
        Assertions.assertEquals("", prop.getBackupDir());
        Assertions.assertEquals("", prop.getOutputPathPrefix());
        Assertions.assertEquals("", prop.getCmdPath());
        Assertions.assertEquals("", prop.getCmdArgs());
        Assertions.assertEquals("", prop.getCatI());
        Assertions.assertEquals("", prop.getCatX());
        Assertions.assertEquals("", prop.getCatP());
        Assertions.assertEquals("", prop.getCatE());
        Assertions.assertEquals("", prop.getCatXmlNl());
        Assertions.assertEquals("", prop.getCatOptions());
        Assertions.assertEquals("", prop.getFileListPath());
        Assertions.assertEquals("rel", prop.getFileListType());
        Assertions.assertEquals("[,|]", prop.getFileListRegex());
        Assertions.assertEquals("", prop.getTsSource());
        Assertions.assertEquals("", prop.getTsDestination());
        Assertions.assertEquals("", prop.getTsBackup());

        Assertions.assertEquals(ClsBaseDir.COPY_BINARY, prop.getCopyCmdType());
        Assertions.assertEquals(ClsBaseDir.ACTION_COPY, prop.getActionCode());
        Assertions.assertEquals(ClsBaseDir.TASK_CP, prop.getTask());
        Assertions.assertEquals(ClsBaseDir.CHECK_NONE, prop.getCheckLogic());
        Assertions.assertEquals(ClsBaseDir.COMPARISON_NO, prop.getCompOpe());
        Assertions.assertEquals(MdlFile.PATH_IS_NULL, prop.getPathType());
        Assertions.assertEquals(ClsBaseDir.CHECK_FILE_LOCK_NONE, prop.getCheckFileLock());
        Assertions.assertEquals(0L, prop.getSkipSize());
        Assertions.assertEquals(0L, prop.getCopySize());
        Assertions.assertEquals(0L, prop.getCompSize());
        Assertions.assertEquals(60, prop.getInterval());
        Assertions.assertEquals(1, prop.getMaxLoop());
        Assertions.assertEquals(ClsBaseDir.FILE_SHARE_READ_WRITE, prop.getFileShare());
        Assertions.assertEquals(200, prop.getWaitMSecForRetryCopy());
        Assertions.assertEquals(0, prop.getRetrySystemCopyMax());
        Assertions.assertEquals(0.0, prop.getSecRange());
        Assertions.assertEquals(0, prop.getVerbose());
        Assertions.assertEquals(0, prop.getShowCurDir());
        Assertions.assertEquals(ClsBaseDir.RELATIVE, prop.getOutputPathCode());
        Assertions.assertEquals(0, prop.getProgressIntervalDirs());
        Assertions.assertEquals(0, prop.getProgressIntervalFiles());
        Assertions.assertEquals(0, prop.getOverwriteLevel());
        Assertions.assertEquals(7, prop.getMaxKeep());
        Assertions.assertEquals(MdlConst.INT_NULL, prop.getWarnThreshold());
        Assertions.assertEquals(MdlConst.INT_NULL, prop.getErrorThreshold());
        Assertions.assertEquals(ClsBaseDir.EXEC_MODE_EXE, prop.getExecModeCode());
        Assertions.assertEquals(3, prop.getPriority());
        Assertions.assertEquals(86400, prop.getTimeout());
        Assertions.assertEquals(0L, prop.getFiles());
        Assertions.assertEquals(0L, prop.getLines());
        Assertions.assertEquals(MdlConst.INT_TYPE_ALL, prop.getTypeCode());
        Assertions.assertEquals(MdlConst.LNG_MAX, prop.getMaxDepth());
        Assertions.assertEquals(0L, prop.getMinDepth());
        Assertions.assertEquals(ClsBaseDir.FILES_RELATIVE, prop.getFilesTypeCode());
        Assertions.assertEquals(0, prop.getCpTimestamp());
        Assertions.assertEquals(MdlFile.SORT_BY_NONE, prop.getSortType());

        Assertions.assertNull(prop.getBeforeTime());
        Assertions.assertNull(prop.getAfterTime());

        Assertions.assertFalse(prop.isNeedPathFr());
        Assertions.assertFalse(prop.isNeedPathTo());
        Assertions.assertFalse(prop.isList());
        Assertions.assertFalse(prop.isReverse());
        Assertions.assertTrue(prop.isSizeCheck());
        Assertions.assertFalse(prop.isSyncRmOnly());
        Assertions.assertFalse(prop.isFlat());
        Assertions.assertFalse(prop.isDirTerm());
        Assertions.assertFalse(prop.isAlwaysMkDir());
        Assertions.assertTrue(prop.isFileCopy());
        Assertions.assertFalse(prop.isSourceCheck());
        Assertions.assertTrue(prop.isFrPathCheck());
        Assertions.assertFalse(prop.isRetFiles());
        Assertions.assertFalse(prop.isBackup());
        Assertions.assertTrue(prop.isErrorIfBackupFailed());
        Assertions.assertFalse(prop.isRelative());
        Assertions.assertFalse(prop.isProgress());
        Assertions.assertFalse(prop.isStackTrace());
        Assertions.assertTrue(prop.isShowNewFile());
        Assertions.assertTrue(prop.isShowUpdatedFile());
        Assertions.assertTrue(prop.isShowSameFile());
        Assertions.assertFalse(prop.isShowPath());
        Assertions.assertFalse(prop.isShowSize());
        Assertions.assertFalse(prop.isShowDirNum());
        Assertions.assertFalse(prop.isShowFileNum());
        Assertions.assertFalse(prop.isShowPerm());
        Assertions.assertFalse(prop.isShowOwner());
        Assertions.assertFalse(prop.isSymLink());
        Assertions.assertFalse(prop.isDq());
        Assertions.assertFalse(prop.isExecCmd());
        Assertions.assertFalse(prop.isErrorAtNegativeValue());
        Assertions.assertFalse(prop.isAlwaysNormal());
        Assertions.assertFalse(prop.isShowCmd());
        Assertions.assertFalse(prop.isShowOutput());
        Assertions.assertFalse(prop.isShowExitCode());
        Assertions.assertFalse(prop.isCat());
        Assertions.assertFalse(prop.isCatRetWcl());
        Assertions.assertFalse(prop.isBefore());
        Assertions.assertFalse(prop.isAfter());
        Assertions.assertFalse(prop.isRegIncBasename());
        Assertions.assertFalse(prop.isRegExcBasename());
        Assertions.assertFalse(prop.isIncHitRecursive());
        Assertions.assertFalse(prop.isExcHitRecursive());
        Assertions.assertFalse(prop.isDirFilterOr());
        Assertions.assertFalse(prop.isXdOnlyFiles());
        Assertions.assertFalse(prop.isRmNohit());
        Assertions.assertTrue(prop.isAscending());
        Assertions.assertFalse(prop.isShowDirList());
        Assertions.assertFalse(prop.isShowFileList());

        Assertions.assertNotNull(prop.getIncFilesList());
        Assertions.assertTrue(prop.getIncFilesList().isEmpty());
        Assertions.assertNotNull(prop.getExcFilesList());
        Assertions.assertTrue(prop.getExcFilesList().isEmpty());
        Assertions.assertNotNull(prop.getIncDirsList());
        Assertions.assertTrue(prop.getIncDirsList().isEmpty());
        Assertions.assertNotNull(prop.getExcDirsList());
        Assertions.assertTrue(prop.getExcDirsList().isEmpty());
        Assertions.assertNotNull(prop.getFileList());
        Assertions.assertTrue(prop.getFileList().isEmpty());
    }

    @Test
    @DisplayName("プロパティの読み書き（Getter/Setter）テスト")
    void testGettersAndSetters() {
        ClsBaseDir prop = new ClsBaseDir();
        LocalDateTime testTime = LocalDateTime.of(2026, 8, 15, 12, 0, 0);

        prop.setExeBaseName("test_exe");
        prop.setExeDir("C:\\app");
        prop.setSourcePath("C:\\src");
        prop.setDestinationPath("C:\\dst");
        prop.setWorkDir("C:\\work");
        prop.setAction("sync");
        prop.setMode("fast");
        prop.setCopyCmdType(ClsBaseDir.COPY_OS_CMD);
        prop.setActionCode(ClsBaseDir.ACTION_SYNC);
        prop.setTask(ClsBaseDir.TASK_MV);
        prop.setCheckLogic(ClsBaseDir.CHECK_SHA1);
        prop.setCompOpe(ClsBaseDir.COMPARISON_GT);
        prop.setPathType(MdlFile.PATH_IS_DIRECTORY);
        prop.setNeedPathFr(true);
        prop.setNeedPathTo(true);
        prop.setList(true);
        prop.setReverse(true);
        prop.setSizeCheck(false);
        prop.setSyncRmOnly(true);
        prop.setFlat(true);
        prop.setDirTerm(true);
        prop.setAlwaysMkDir(true);
        prop.setFileCopy(false);
        prop.setCheckFileLock(ClsBaseDir.CHECK_FILE_LOCK_SAMPLE);
        prop.setSourceCheck(true);
        prop.setFrPathCheck(false);
        prop.setRetFiles(true);
        prop.setSkipSize(1000L);
        prop.setCopySize(2000L);
        prop.setCompSize(3000L);
        prop.setInterval(120);
        prop.setMaxLoop(5);
        prop.setBackup(true);
        prop.setErrorIfBackupFailed(false);
        prop.setBackupDir("C:\\backup");
        prop.setFileShare(ClsBaseDir.FILE_SHARE_NONE);
        prop.setWaitMSecForRetryCopy(500);
        prop.setRetrySystemCopyMax(3);
        prop.setSecRange(1.5);
        prop.setVerbose(2);
        prop.setShowCurDir(1);
        prop.setOutputPathCode(ClsBaseDir.BOTH);
        prop.setProgressIntervalDirs(10);
        prop.setProgressIntervalFiles(50);
        prop.setRelative(true);
        prop.setProgress(true);
        prop.setStackTrace(true);
        prop.setShowNewFile(false);
        prop.setShowUpdatedFile(false);
        prop.setShowSameFile(false);
        prop.setOutputPathPrefix(">> ");
        prop.setShowPath(true);
        prop.setShowSize(true);
        prop.setShowDirNum(true);
        prop.setShowFileNum(true);
        prop.setShowPerm(true);
        prop.setShowOwner(true);
        prop.setSymLink(true);
        prop.setOverwriteLevel(1);
        prop.setMaxKeep(14);
        prop.setCmdPath("C:\\cmd.exe");
        prop.setCmdArgs("/c dir");
        prop.setDq(true);
        prop.setWarnThreshold(10);
        prop.setErrorThreshold(20);
        prop.setExecCmd(true);
        prop.setExecModeCode(ClsBaseDir.EXEC_MODE_PS);
        prop.setPriority(1);
        prop.setTimeout(3600);
        prop.setErrorAtNegativeValue(true);
        prop.setAlwaysNormal(true);
        prop.setShowCmd(true);
        prop.setShowOutput(true);
        prop.setShowExitCode(true);
        prop.setCat(true);
        prop.setCatRetWcl(true);
        prop.setCatI("include");
        prop.setCatX("exclude");
        prop.setCatP("pattern");
        prop.setCatE("encoding");
        prop.setCatXmlNl("xmlnl");
        prop.setCatOptions("-v");
        prop.setFiles(100L);
        prop.setLines(5000L);
        prop.setTypeCode(MdlConst.INT_TYPE_FILE);
        prop.setMaxDepth(3L);
        prop.setMinDepth(1L);
        prop.setBefore(true);
        prop.setAfter(true);
        prop.setBeforeTime(testTime);
        prop.setAfterTime(testTime.minusDays(1));
        prop.setRegIncBasename(true);
        prop.setRegExcBasename(true);
        prop.setIncHitRecursive(true);
        prop.setExcHitRecursive(true);
        prop.setDirFilterOr(true);
        prop.setXdOnlyFiles(true);
        prop.setRmNohit(true);
        prop.setIncFilesList(List.of("*.txt", "*.log"));
        prop.setExcFilesList(List.of("*.tmp"));
        prop.setIncDirsList(List.of("dir1", "dir2"));
        prop.setExcDirsList(List.of("obj", "bin"));
        prop.setFilesTypeCode(ClsBaseDir.FILES_FULL);
        prop.setFileListPath("C:\\list.txt");
        prop.setFileListType("full");
        prop.setFileListRegex("\\t");
        prop.setFileList(List.of("item1", "item2"));
        prop.setCpTimestamp(1);
        prop.setTsSource("ts_src");
        prop.setTsDestination("ts_dst");
        prop.setTsBackup("ts_bak");
        prop.setSortType(MdlFile.SORT_BY_NAME);
        prop.setAscending(false);
        prop.setShowDirList(true);
        prop.setShowFileList(true);

        Assertions.assertEquals("test_exe", prop.getExeBaseName());
        Assertions.assertEquals("C:\\app", prop.getExeDir());
        Assertions.assertEquals("C:\\src", prop.getSourcePath());
        Assertions.assertEquals("C:\\dst", prop.getDestinationPath());
        Assertions.assertEquals("C:\\work", prop.getWorkDir());
        Assertions.assertEquals("sync", prop.getAction());
        Assertions.assertEquals("fast", prop.getMode());
        Assertions.assertEquals(ClsBaseDir.COPY_OS_CMD, prop.getCopyCmdType());
        Assertions.assertEquals(ClsBaseDir.ACTION_SYNC, prop.getActionCode());
        Assertions.assertEquals(ClsBaseDir.TASK_MV, prop.getTask());
        Assertions.assertEquals(ClsBaseDir.CHECK_SHA1, prop.getCheckLogic());
        Assertions.assertEquals(ClsBaseDir.COMPARISON_GT, prop.getCompOpe());
        Assertions.assertEquals(MdlFile.PATH_IS_DIRECTORY, prop.getPathType());
        Assertions.assertTrue(prop.isNeedPathFr());
        Assertions.assertTrue(prop.isNeedPathTo());
        Assertions.assertTrue(prop.isList());
        Assertions.assertTrue(prop.isReverse());
        Assertions.assertFalse(prop.isSizeCheck());
        Assertions.assertTrue(prop.isSyncRmOnly());
        Assertions.assertTrue(prop.isFlat());
        Assertions.assertTrue(prop.isDirTerm());
        Assertions.assertTrue(prop.isAlwaysMkDir());
        Assertions.assertFalse(prop.isFileCopy());
        Assertions.assertEquals(ClsBaseDir.CHECK_FILE_LOCK_SAMPLE, prop.getCheckFileLock());
        Assertions.assertTrue(prop.isSourceCheck());
        Assertions.assertFalse(prop.isFrPathCheck());
        Assertions.assertTrue(prop.isRetFiles());
        Assertions.assertEquals(1000L, prop.getSkipSize());
        Assertions.assertEquals(2000L, prop.getCopySize());
        Assertions.assertEquals(3000L, prop.getCompSize());
        Assertions.assertEquals(120, prop.getInterval());
        Assertions.assertEquals(5, prop.getMaxLoop());
        Assertions.assertTrue(prop.isBackup());
        Assertions.assertFalse(prop.isErrorIfBackupFailed());
        Assertions.assertEquals("C:\\backup", prop.getBackupDir());
        Assertions.assertEquals(ClsBaseDir.FILE_SHARE_NONE, prop.getFileShare());
        Assertions.assertEquals(500, prop.getWaitMSecForRetryCopy());
        Assertions.assertEquals(3, prop.getRetrySystemCopyMax());
        Assertions.assertEquals(1.5, prop.getSecRange());
        Assertions.assertEquals(2, prop.getVerbose());
        Assertions.assertEquals(1, prop.getShowCurDir());
        Assertions.assertEquals(ClsBaseDir.BOTH, prop.getOutputPathCode());
        Assertions.assertEquals(10, prop.getProgressIntervalDirs());
        Assertions.assertEquals(50, prop.getProgressIntervalFiles());
        Assertions.assertTrue(prop.isRelative());
        Assertions.assertTrue(prop.isProgress());
        Assertions.assertTrue(prop.isStackTrace());
        Assertions.assertFalse(prop.isShowNewFile());
        Assertions.assertFalse(prop.isShowUpdatedFile());
        Assertions.assertFalse(prop.isShowSameFile());
        Assertions.assertEquals(">> ", prop.getOutputPathPrefix());
        Assertions.assertTrue(prop.isShowPath());
        Assertions.assertTrue(prop.isShowSize());
        Assertions.assertTrue(prop.isShowDirNum());
        Assertions.assertTrue(prop.isShowFileNum());
        Assertions.assertTrue(prop.isShowPerm());
        Assertions.assertTrue(prop.isShowOwner());
        Assertions.assertTrue(prop.isSymLink());
        Assertions.assertEquals(1, prop.getOverwriteLevel());
        Assertions.assertEquals(14, prop.getMaxKeep());
        Assertions.assertEquals("C:\\cmd.exe", prop.getCmdPath());
        Assertions.assertEquals("/c dir", prop.getCmdArgs());
        Assertions.assertTrue(prop.isDq());
        Assertions.assertEquals(10, prop.getWarnThreshold());
        Assertions.assertEquals(20, prop.getErrorThreshold());
        Assertions.assertTrue(prop.isExecCmd());
        Assertions.assertEquals(ClsBaseDir.EXEC_MODE_PS, prop.getExecModeCode());
        Assertions.assertEquals(1, prop.getPriority());
        Assertions.assertEquals(3600, prop.getTimeout());
        Assertions.assertTrue(prop.isErrorAtNegativeValue());
        Assertions.assertTrue(prop.isAlwaysNormal());
        Assertions.assertTrue(prop.isShowCmd());
        Assertions.assertTrue(prop.isShowOutput());
        Assertions.assertTrue(prop.isShowExitCode());
        Assertions.assertTrue(prop.isCat());
        Assertions.assertTrue(prop.isCatRetWcl());
        Assertions.assertEquals("include", prop.getCatI());
        Assertions.assertEquals("exclude", prop.getCatX());
        Assertions.assertEquals("pattern", prop.getCatP());
        Assertions.assertEquals("encoding", prop.getCatE());
        Assertions.assertEquals("xmlnl", prop.getCatXmlNl());
        Assertions.assertEquals("-v", prop.getCatOptions());
        Assertions.assertEquals(100L, prop.getFiles());
        Assertions.assertEquals(5000L, prop.getLines());
        Assertions.assertEquals(MdlConst.INT_TYPE_FILE, prop.getTypeCode());
        Assertions.assertEquals(3L, prop.getMaxDepth());
        Assertions.assertEquals(1L, prop.getMinDepth());
        Assertions.assertTrue(prop.isBefore());
        Assertions.assertTrue(prop.isAfter());
        Assertions.assertEquals(testTime, prop.getBeforeTime());
        Assertions.assertEquals(testTime.minusDays(1), prop.getAfterTime());
        Assertions.assertTrue(prop.isRegIncBasename());
        Assertions.assertTrue(prop.isRegExcBasename());
        Assertions.assertTrue(prop.isIncHitRecursive());
        Assertions.assertTrue(prop.isExcHitRecursive());
        Assertions.assertTrue(prop.isDirFilterOr());
        Assertions.assertTrue(prop.isXdOnlyFiles());
        Assertions.assertTrue(prop.isRmNohit());
        Assertions.assertEquals(List.of("*.txt", "*.log"), prop.getIncFilesList());
        Assertions.assertEquals(List.of("*.tmp"), prop.getExcFilesList());
        Assertions.assertEquals(List.of("dir1", "dir2"), prop.getIncDirsList());
        Assertions.assertEquals(List.of("obj", "bin"), prop.getExcDirsList());
        Assertions.assertEquals(ClsBaseDir.FILES_FULL, prop.getFilesTypeCode());
        Assertions.assertEquals("C:\\list.txt", prop.getFileListPath());
        Assertions.assertEquals("full", prop.getFileListType());
        Assertions.assertEquals("\\t", prop.getFileListRegex());
        Assertions.assertEquals(List.of("item1", "item2"), prop.getFileList());
        Assertions.assertEquals(1, prop.getCpTimestamp());
        Assertions.assertEquals("ts_src", prop.getTsSource());
        Assertions.assertEquals("ts_dst", prop.getTsDestination());
        Assertions.assertEquals("ts_bak", prop.getTsBackup());
        Assertions.assertEquals(MdlFile.SORT_BY_NAME, prop.getSortType());
        Assertions.assertFalse(prop.isAscending());
        Assertions.assertTrue(prop.isShowDirList());
        Assertions.assertTrue(prop.isShowFileList());
    }

    @ParameterizedTest
    @CsvSource({
        "1, fr",
        "2, to",
        "3, both",
        "0, rel",
        "-1, rel",
        "999, rel"
    })
    @DisplayName("getOutputModeStr メソッドのテスト")
    void testGetOutputModeStr(int mode, String expected) {
        ClsBaseDir prop = new ClsBaseDir();
        Assertions.assertEquals(expected, prop.getOutputModeStr(mode));
    }

    @ParameterizedTest
    @CsvSource({
        "from, 1",
        "FROM, 1",
        "fr, 1",
        "FR, 1",
        "f, 1",
        "F, 1",
        "to, 2",
        "TO, 2",
        "t, 2",
        "T, 2",
        "both, 3",
        "BOTH, 3",
        "b, 3",
        "B, 3",
        "rel, 0",
        "REL, 0",
        "'', 0",
        "unknown, 0"
    })
    @DisplayName("getOutputModeCode メソッドのテスト")
    void testGetOutputModeCode(String mode, int expected) {
        ClsBaseDir prop = new ClsBaseDir();
        Assertions.assertEquals(expected, prop.getOutputModeCode(mode));
    }

    @Test
    @DisplayName("getOutputModeCode null テスト")
    void testGetOutputModeCodeNull() {
        ClsBaseDir prop = new ClsBaseDir();
        Assertions.assertEquals(ClsBaseDir.RELATIVE, prop.getOutputModeCode(null));
    }

    @ParameterizedTest
    @CsvSource({
        "0, false",
        "1, sample",
        "2, skip",
        "-1, skip",
        "100, skip"
    })
    @DisplayName("getCheckLockFileModeStr メソッドのテスト")
    void testGetCheckLockFileModeStr(int mode, String expected) {
        ClsBaseDir prop = new ClsBaseDir();
        Assertions.assertEquals(expected, prop.getCheckLockFileModeStr(mode));
    }

    @ParameterizedTest
    @CsvSource({
        "1, cmd",
        "2, ps",
        "3, psc",
        "4, exe",
        "0, normal",
        "-1, normal",
        "999, normal"
    })
    @DisplayName("getExecModeStr メソッドのテスト")
    void testGetExecModeStr(int mode, String expected) {
        ClsBaseDir prop = new ClsBaseDir();
        Assertions.assertEquals(expected, prop.getExecModeStr(mode));
    }
}
