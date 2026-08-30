package tool;

import java.io.File;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import tool.cmnclslib.cls.ClsCmmnArgs;
import tool.cmnclslib.cls.ClsConfigFile;
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlArg;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlDate;
import tool.cmnclslib.mdl.MdlFile;
import tool.cmnclslib.mdl.MdlUtil;

/**
 * コマンドライン引数を解析し、アプリケーションプロパティを設定するクラスです。
 */
public class ClsAppArg {

    private static final Pattern SIZE_PATTERN_REGEX = Pattern.compile("^(?<SIGN>[+-]*)(?<VALUE>[\\.\\d]+)(?<UNIT>\\D*)$");

    private final ClsLogger logger;
    private final ClsCmmnArgs cmmnArgs;
    private ClsBaseDir prop;
    private String exeDir = "";
    private String exeBaseName = "FsFileUtil";
    private final long pid;
    private boolean isUsage = false;
    private boolean isEchoRetcode = false;
    private String showMessageMode = "all";
    private String showTypeMode = "a";
    private String periodUnit = "day";
    private double periodTerm = 0.0;
    private boolean isNew = false;
    private String fileShareMode = "3:ReadWrite";
    private String formattedCompSize = "";

    /**
     * ロガーおよび設定プロパティを指定して {@link ClsAppArg} の新しいインスタンスを初期化します。
     *
     * @param logger ログ出力に使用するロガーインスタンス
     * @param prop アプリケーション設定プロパティ
     */
    public ClsAppArg(ClsLogger logger, ClsBaseDir prop) {
        this.logger = logger != null ? logger : new ClsLogger();
        this.prop = prop != null ? prop : new ClsBaseDir();
        this.cmmnArgs = new ClsCmmnArgs(this.logger);
        this.exeDir = System.getProperty("user.dir", "");
        this.pid = ProcessHandle.current().pid();
    }

    /**
     * アプリケーション設定プロパティを取得します。
     *
     * @return 設定プロパティインスタンス
     */
    public ClsBaseDir getProperties() {
        return prop;
    }

    /**
     * アプリケーション設定プロパティを設定します。
     *
     * @param prop 設定プロパティインスタンス
     */
    public void setProperties(ClsBaseDir prop) {
        this.prop = prop != null ? prop : new ClsBaseDir();
    }

    /**
     * 実行可能ファイルの名前（ベース名）を取得します。
     *
     * @return 実行可能ファイル名
     */
    public String getExeBaseName() {
        return exeBaseName;
    }

    /**
     * 実行可能ファイルの名前（ベース名）を設定します。
     *
     * @param exeBaseName 実行可能ファイル名
     */
    public void setExeBaseName(String exeBaseName) {
        this.exeBaseName = exeBaseName != null ? exeBaseName : "";
    }

    /**
     * 実行ディレクトリパスを取得します。
     *
     * @return 実行ディレクトリパス
     */
    public String getExeDir() {
        return exeDir;
    }

    /**
     * 実行ディレクトリパスを設定します。
     *
     * @param exeDir 実行ディレクトリパス
     */
    public void setExeDir(String exeDir) {
        this.exeDir = exeDir != null ? exeDir : "";
    }

    /**
     * ヘルプ（使用法）表示フラグを取得します。
     *
     * @return ヘルプ表示フラグ
     */
    public boolean isUsage() {
        return isUsage;
    }

    /**
     * ヘルプ（使用法）表示フラグを設定します。
     *
     * @param isUsage ヘルプ表示フラグ
     */
    public void setUsage(boolean isUsage) {
        this.isUsage = isUsage;
    }

    /**
     * 終了コードエコー出力フラグを取得します。
     *
     * @return 終了コードエコー出力フラグ
     */
    public boolean isEchoRetcode() {
        return isEchoRetcode;
    }

    /**
     * 終了コードエコー出力フラグを設定します。
     *
     * @param isEchoRetcode 終了コードエコー出力フラグ
     */
    public void setEchoRetcode(boolean isEchoRetcode) {
        this.isEchoRetcode = isEchoRetcode;
    }

    /**
     * 共通引数解析インスタンスを取得します。
     *
     * @return 共通引数解析インスタンス
     */
    public ClsCmmnArgs getCmmnArgs() {
        return cmmnArgs;
    }

    /**
     * コマンドライン引数を解析し、各プロパティに設定します。
     *
     * @param args コマンドライン引数配列
     * @return 解析に成功した場合は true、不正な引数等が存在する場合は false
     */
    public boolean parse(String[] args) {
        Map<String, String> namedArgs = MdlArg.getNamedArgs(args);
        cmmnArgs.setNamedArgs(namedArgs);
        boolean isOk = cmmnArgs.getCommonArgs();

        // -----------------------------------------------------------------
        // ClsCmmnParams引数取得：ETC
        // -----------------------------------------------------------------
        isUsage = cmmnArgs.isUsage();
        prop.setVerbose(cmmnArgs.getVerbose());
        prop.setStackTrace(cmmnArgs.isStackTrace());
        if (cmmnArgs.isDiff()) {
            prop.setShowSameFile(false);
        }
        prop.setTimeout(cmmnArgs.getTimeout());

        // -----------------------------------------------------------------
        // Basic Option：
        // -----------------------------------------------------------------
        // -a action
        boolean isParamFound = false;
        if (MdlArg.containsKey(namedArgs, "a")) {
            String paramValue = MdlArg.getValue(namedArgs, "a");
            if (paramValue != null && !paramValue.isBlank()) {
                isParamFound = true;
                prop.setAction(paramValue.toLowerCase(Locale.ROOT));
                switch (prop.getAction()) {
                    case "move":
                        prop.setActionCode(ClsBaseDir.ACTION_MOVE);
                        prop.setAlwaysMkDir(true);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(true);
                        break;
                    case "sync":
                        prop.setActionCode(ClsBaseDir.ACTION_SYNC);
                        prop.setAlwaysMkDir(true);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(true);
                        break;
                    case "syncrm":
                        prop.setActionCode(ClsBaseDir.ACTION_SYNC);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(true);
                        prop.setSyncRmOnly(true);
                        prop.setFileCopy(false);
                        break;
                    case "ls":
                        prop.setActionCode(ClsBaseDir.ACTION_LS);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        break;
                    case "find":
                        prop.setActionCode(ClsBaseDir.ACTION_FIND);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        prop.setTypeCode(MdlConst.INT_TYPE_FILE);
                        prop.setShowOutput(true);
                        break;
                    case "mkdir":
                        prop.setActionCode(ClsBaseDir.ACTION_MKDIR);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        break;
                    case "touch":
                        prop.setActionCode(ClsBaseDir.ACTION_TOUCH);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        break;
                    case "delete":
                        prop.setActionCode(ClsBaseDir.ACTION_DELETE);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        prop.setTypeCode(MdlConst.INT_TYPE_ALL);
                        break;
                    case "delete-dir":
                        prop.setActionCode(ClsBaseDir.ACTION_DELETE);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        prop.setTypeCode(MdlConst.INT_TYPE_DIRECTORY);
                        break;
                    case "delete-file":
                        prop.setActionCode(ClsBaseDir.ACTION_DELETE);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        prop.setTypeCode(MdlConst.INT_TYPE_FILE);
                        break;
                    case "exist":
                        prop.setActionCode(ClsBaseDir.ACTION_EXIST);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        break;
                    case "lock-proc":
                        prop.setActionCode(ClsBaseDir.ACTION_LIST_LOCK_PROC);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        prop.setCheckFileLock(ClsBaseDir.CHECK_FILE_LOCK_SAMPLE);
                        logger.writeLine(MdlConst.LVL_E, "INVALID ARGUMENT -a lock-proc : This feature is no longer supported.");
                        isOk = false;
                        break;
                    case "isdir":
                        prop.setActionCode(ClsBaseDir.ACTION_EXIST_DIR);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        break;
                    case "isfile":
                        prop.setActionCode(ClsBaseDir.ACTION_EXIST_FILE);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        break;
                    case "islocked":
                        prop.setActionCode(ClsBaseDir.ACTION_FILE_LOCKED);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        prop.setCheckFileLock(ClsBaseDir.CHECK_FILE_LOCK_SAMPLE);
                        break;
                    case "wait":
                        prop.setActionCode(ClsBaseDir.ACTION_WAIT);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        break;
                    case "rename":
                        prop.setActionCode(ClsBaseDir.ACTION_RENAME);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(true);
                        break;
                    case "rotate":
                        prop.setActionCode(ClsBaseDir.ACTION_ROTATE);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        break;
                    case "reverse":
                        prop.setActionCode(ClsBaseDir.ACTION_COPY);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(true);
                        prop.setReverse(true);
                        prop.setAlwaysMkDir(false);
                        break;
                    case "flatcopy":
                        prop.setActionCode(ClsBaseDir.ACTION_COPY);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(true);
                        prop.setAlwaysMkDir(false);
                        prop.setFlat(true);
                        break;
                    case "dircopy":
                        prop.setActionCode(ClsBaseDir.ACTION_COPY);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(true);
                        prop.setFileCopy(false);
                        prop.setAlwaysMkDir(true);
                        break;
                    case "mklink":
                        prop.setActionCode(ClsBaseDir.ACTION_MKLINK);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(true);
                        prop.setSymLink(true);
                        break;
                    case "realpath":
                        prop.setActionCode(ClsBaseDir.ACTION_GET_REAL_PATH);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(false);
                        prop.setSymLink(true);
                        break;
                    case "exec":
                        prop.setActionCode(ClsBaseDir.ACTION_EXEC);
                        prop.setShowOutput(true);
                        prop.setNeedPathFr(true);
                        break;
                    case "size":
                        prop.setActionCode(ClsBaseDir.ACTION_GET_SIZE);
                        prop.setNeedPathFr(true);
                        break;
                    case "perm":
                        prop.setActionCode(ClsBaseDir.ACTION_GET_PERM);
                        prop.setNeedPathFr(true);
                        break;
                    case "owner":
                        prop.setActionCode(ClsBaseDir.ACTION_GET_OWNER);
                        prop.setNeedPathFr(true);
                        break;
                    default:
                        prop.setAction("copy");
                        prop.setActionCode(ClsBaseDir.ACTION_COPY);
                        prop.setNeedPathFr(true);
                        prop.setNeedPathTo(true);
                        prop.setAlwaysMkDir(true);
                        break;
                }
            }
        }
        if (!isParamFound) {
            prop.setAction("copy");
            prop.setActionCode(ClsBaseDir.ACTION_COPY);
            prop.setNeedPathFr(true);
            prop.setNeedPathTo(true);
            prop.setAlwaysMkDir(true);
        }

        // -path|-f path
        if (prop.isNeedPathFr()) {
            isParamFound = false;
            for (String key : new String[] { "f", "path" }) {
                if (MdlArg.containsKey(namedArgs, key)) {
                    String paramValue = MdlArg.getValue(namedArgs, key);
                    if (paramValue != null && !paramValue.isBlank()) {
                        isParamFound = true;
                        switch (prop.getAction()) {
                            case "mount":
                            case "umount":
                                prop.setSourcePath(paramValue);
                                prop.setNeedPathFr(false);
                                break;
                            default:
                                prop.setSourcePath(MdlFile.trimPathSeparator(MdlFile.getAbsolutePath(paramValue)));
                                break;
                        }
                        break;
                    }
                }
            }
            if (!isParamFound) {
                switch (prop.getAction()) {
                    case "exec":
                    case "mount":
                    case "umount":
                        break;
                    default:
                        logger.writeLine(MdlConst.LVL_E, "INVALID ARGUMENT -path|-f");
                        isOk = false;
                        break;
                }
            }
        }
        if (prop.isNeedPathFr() && prop.getSourcePath() != null && !prop.getSourcePath().isEmpty()) {
            if (!cmmnArgs.getReplaceMap().isEmpty()) {
                prop.setSourcePath(cmmnArgs.replaceByDictionary(prop.getSourcePath()));
            }
        }

        // -----------------------------------------------------------------
        // Copy Option：
        // -----------------------------------------------------------------
        // -m check
        if (MdlArg.containsKey(namedArgs, "m")) {
            String paramValue = MdlArg.getValue(namedArgs, "m");
            if (paramValue != null && !paramValue.isBlank()) {
                prop.setMode(paramValue.toLowerCase(Locale.ROOT));
                switch (prop.getMode()) {
                    case "size":
                        prop.setCheckLogic(ClsBaseDir.CHECK_SIZE);
                        break;
                    case "mtime":
                        prop.setCheckLogic(ClsBaseDir.CHECK_MTIME);
                        break;
                    case "new":
                        prop.setCheckLogic(ClsBaseDir.CHECK_MTIME_NEW);
                        break;
                    case "old":
                        prop.setCheckLogic(ClsBaseDir.CHECK_MTIME_OLD);
                        break;
                    case "cksum":
                        prop.setCheckLogic(ClsBaseDir.CHECK_CKSUM);
                        break;
                    case "adler32":
                        prop.setCheckLogic(ClsBaseDir.CHECK_ADLER32);
                        break;
                    case "sha1":
                        prop.setCheckLogic(ClsBaseDir.CHECK_SHA1);
                        break;
                    case "date":
                        prop.setCheckLogic(ClsBaseDir.CHECK_MTIME);
                        prop.setSizeCheck(false);
                        break;
                    case "newer":
                        prop.setCheckLogic(ClsBaseDir.CHECK_MTIME_NEW);
                        prop.setSizeCheck(false);
                        break;
                    case "older":
                        prop.setCheckLogic(ClsBaseDir.CHECK_MTIME_OLD);
                        prop.setSizeCheck(false);
                        break;
                    case "exist":
                        prop.setCheckLogic(ClsBaseDir.CHECK_EXIST);
                        prop.setSizeCheck(false);
                        break;
                    default:
                        prop.setCheckLogic(ClsBaseDir.CHECK_NONE);
                        prop.setMode("none");
                        break;
                }
            }
        }

        // -t path
        if (prop.isNeedPathTo()) {
            isParamFound = false;
            if (MdlArg.containsKey(namedArgs, "t")) {
                String paramValue = MdlArg.getValue(namedArgs, "t");
                if (paramValue != null && !paramValue.isBlank()) {
                    isParamFound = true;
                    if (".".equals(MdlFile.getFileName(paramValue))) {
                        paramValue = MdlFile.getDirectoryPath(paramValue) + File.separator + MdlFile.getFileName(prop.getSourcePath());
                    }
                    prop.setDestinationPath(MdlFile.trimPathSeparator(MdlFile.getAbsolutePath(paramValue)));
                }
            }
            if (!isParamFound) {
                logger.writeLine(MdlConst.LVL_E, "INVALID ARGUMENT -t");
                isOk = false;
            }
        }
        if (prop.getDestinationPath() != null && !prop.getDestinationPath().isEmpty()) {
            if (!cmmnArgs.getReplaceMap().isEmpty()) {
                prop.setDestinationPath(cmmnArgs.replaceByDictionary(prop.getDestinationPath()));
            }
        }

        // -list
        if (MdlArg.containsKey(namedArgs, "list")) {
            prop.setList(true);
        }

        // -tsc|-tsm|-ts
        if (MdlArg.containsKey(namedArgs, "tsc")) {
            prop.setCpTimestamp(1);
        }
        if (MdlArg.containsKey(namedArgs, "tsm")) {
            prop.setCpTimestamp(2);
        }
        if (MdlArg.containsKey(namedArgs, "ts")) {
            prop.setCpTimestamp(3);
        }

        // -fchk
        if (MdlArg.containsKey(namedArgs, "fchk")) {
            prop.setSourceCheck(true);
        }

        // -rmnohit
        if (MdlArg.containsKey(namedArgs, "rmnohit")) {
            prop.setRmNohit(true);
        }

        // -no-emptydir
        if (MdlArg.containsKey(namedArgs, "no-emptydir")) {
            prop.setAlwaysMkDir(false);
        }

        // コピーコマンド区分
        if (MdlArg.containsKey(namedArgs, "async")) {
            prop.setCopyCmdType(ClsBaseDir.COPY_ASYNC);
        }
        if (MdlArg.containsKey(namedArgs, "os")) {
            prop.setCopyCmdType(ClsBaseDir.COPY_OS_CMD);
            prop.setProgress(false);
        }

        // -skipsize|-copysize
        if (MdlArg.containsKey(namedArgs, "skipsize")) {
            String paramValue = MdlArg.getValue(namedArgs, "skipsize");
            int parsed = MdlUtil.parseInt(paramValue, MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setSkipSize((long) parsed * 1024 * 1024);
            }
        }
        if (MdlArg.containsKey(namedArgs, "copysize")) {
            String paramValue = MdlArg.getValue(namedArgs, "copysize");
            int parsed = MdlUtil.parseInt(paramValue, MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setCopySize((long) parsed * 1024 * 1024);
            }
        }

        // -fileshare mode
        if (MdlArg.containsKey(namedArgs, "fileshare")) {
            prop.setCheckFileLock(ClsBaseDir.CHECK_FILE_LOCK_SKIP);
            String paramValue = MdlArg.getValue(namedArgs, "fileshare");
            if (paramValue != null && !paramValue.isBlank()) {
                switch (paramValue.toLowerCase(Locale.ROOT)) {
                    case "none":
                    case "0":
                        fileShareMode = "0:None";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_NONE);
                        break;
                    case "read":
                    case "1":
                        fileShareMode = "1:Read";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_READ);
                        break;
                    case "write":
                    case "2":
                        fileShareMode = "2:Write";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_WRITE);
                        break;
                    case "readwrite":
                    case "3":
                        fileShareMode = "3:ReadWrite";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_READ_WRITE);
                        break;
                    case "delete":
                    case "4":
                        fileShareMode = "4:Delete";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_DELETE);
                        break;
                    case "read|delete":
                    case "5":
                        fileShareMode = "5:Read|Delete";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_READ | ClsBaseDir.FILE_SHARE_DELETE);
                        break;
                    case "write|delete":
                    case "6":
                        fileShareMode = "6:Write|Delete";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_WRITE | ClsBaseDir.FILE_SHARE_DELETE);
                        break;
                    case "readwrite|delete":
                    case "7":
                        fileShareMode = "7:ReadWrite|Delete";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_READ_WRITE | ClsBaseDir.FILE_SHARE_DELETE);
                        break;
                    case "inheritable":
                    case "16":
                        fileShareMode = "16:Inheritable";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_INHERITABLE);
                        break;
                    case "read|inheritable":
                    case "17":
                        fileShareMode = "17:Read|Inheritable";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_READ | ClsBaseDir.FILE_SHARE_INHERITABLE);
                        break;
                    case "write|inheritable":
                    case "18":
                        fileShareMode = "18:Write|Inheritable";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_WRITE | ClsBaseDir.FILE_SHARE_INHERITABLE);
                        break;
                    case "readwrite|inheritable":
                    case "19":
                        fileShareMode = "19:ReadWrite|Inheritable";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_READ_WRITE | ClsBaseDir.FILE_SHARE_INHERITABLE);
                        break;
                    case "delete|inheritable":
                    case "20":
                        fileShareMode = "20:Delete|Inheritable";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_DELETE | ClsBaseDir.FILE_SHARE_INHERITABLE);
                        break;
                    case "read|delete|inheritable":
                    case "21":
                        fileShareMode = "21:Read|Delete|Inheritable";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_READ | ClsBaseDir.FILE_SHARE_DELETE | ClsBaseDir.FILE_SHARE_INHERITABLE);
                        break;
                    case "write|delete|inheritable":
                    case "22":
                        fileShareMode = "22:Write|Delete|Inheritable";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_WRITE | ClsBaseDir.FILE_SHARE_DELETE | ClsBaseDir.FILE_SHARE_INHERITABLE);
                        break;
                    case "readwrite|delete|inheritable":
                    case "23":
                        fileShareMode = "23:ReadWrite|Delete|Inheritable";
                        prop.setFileShare(ClsBaseDir.FILE_SHARE_READ_WRITE | ClsBaseDir.FILE_SHARE_DELETE | ClsBaseDir.FILE_SHARE_INHERITABLE);
                        break;
                    default:
                        fileShareMode = "3:ReadWrite";
                        break;
                }
            }
        }

        // wait-retry-copy n
        if (MdlArg.containsKey(namedArgs, "wait-retry-copy")) {
            String paramValue = MdlArg.getValue(namedArgs, "wait-retry-copy");
            int parsed = MdlUtil.parseInt(paramValue, MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setWaitMSecForRetryCopy(parsed);
            }
        }

        // -retry-syscopy n
        if (MdlArg.containsKey(namedArgs, "retry-syscopy")) {
            String paramValue = MdlArg.getValue(namedArgs, "retry-syscopy");
            int parsed = MdlUtil.parseInt(paramValue, MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setRetrySystemCopyMax(parsed);
            }
        }

        // -----------------------------------------------------------------
        // Symbolic Link Option：
        // -----------------------------------------------------------------
        // -sym [0|1|2]
        if (MdlArg.containsKey(namedArgs, "sym")) {
            String paramValue = MdlArg.getValue(namedArgs, "sym");
            int parsed = MdlUtil.parseInt(paramValue, MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setOverwriteLevel(parsed);
            }
        }

        // -rel
        if (MdlArg.containsKey(namedArgs, "rel")) {
            prop.setRelative(true);
        }

        // -----------------------------------------------------------------
        // Backup Option：
        // -----------------------------------------------------------------
        // -backup <path>
        if (prop.isNeedPathTo()) {
            isParamFound = false;
            if (MdlArg.containsKey(namedArgs, "backup")) {
                String paramValue = MdlArg.getValue(namedArgs, "backup");
                if (paramValue != null && !paramValue.isBlank()) {
                    isParamFound = true;
                    prop.setBackupDir(MdlFile.trimPathSeparator(MdlFile.getAbsolutePath(paramValue)));
                    if (!cmmnArgs.getReplaceMap().isEmpty() && !prop.getBackupDir().isEmpty()) {
                        prop.setBackupDir(cmmnArgs.replaceByDictionary(prop.getBackupDir()));
                    }
                }
            }
            if (!isParamFound) {
                String strPathDBk = MdlFile.getDirectoryPath(prop.getDestinationPath());
                String strNameFBk = MdlFile.getFileName(prop.getDestinationPath());
                prop.setBackupDir(strPathDBk + File.separator + strNameFBk + ".%Y%m%d.%H%M%S." + pid);
            }
        }

        // -force
        prop.setErrorIfBackupFailed(cmmnArgs.isForce());

        // -----------------------------------------------------------------
        // Replace to path string Option：
        // -----------------------------------------------------------------
        // -ts-f|-ts-t|-ts-b n
        if (MdlArg.containsKey(namedArgs, "ts-f")) {
            prop.setTsSource(MdlArg.getValue(namedArgs, "ts-f"));
        }
        if (MdlArg.containsKey(namedArgs, "ts-t")) {
            prop.setTsDestination(MdlArg.getValue(namedArgs, "ts-t"));
        }
        if (MdlArg.containsKey(namedArgs, "ts-b")) {
            prop.setTsBackup(MdlArg.getValue(namedArgs, "ts-b"));
        }

        // -----------------------------------------------------------------
        // Filter Option：
        // -----------------------------------------------------------------
        // -max / -min
        if (MdlArg.containsKey(namedArgs, "max")) {
            int parsed = MdlUtil.parseInt(MdlArg.getValue(namedArgs, "max"), MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setMaxDepth(parsed);
            }
        }
        if (MdlArg.containsKey(namedArgs, "min")) {
            int parsed = MdlUtil.parseInt(MdlArg.getValue(namedArgs, "min"), MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setMinDepth(parsed);
            }
        }
        if (prop.getMinDepth() > prop.getMaxDepth()) {
            isOk = false;
            logger.writeLine(MdlConst.LVL_E, "INVALID ARGUMENT : -min " + prop.getMinDepth() + " > -max : " + prop.getMaxDepth());
        }

        // -period d|h|m|s
        if (MdlArg.containsKey(namedArgs, "period")) {
            String paramValue = MdlArg.getValue(namedArgs, "period");
            if (paramValue != null && !paramValue.isBlank()) {
                periodUnit = paramValue.toLowerCase(Locale.ROOT);
            }
        }

        // -term|-days value / -new
        for (String key : new String[] { "term", "days" }) {
            if (MdlArg.containsKey(namedArgs, key)) {
                boolean isBaseTimeNow = "term".equals(key);
                String paramValue = MdlArg.getValue(namedArgs, key);
                if (paramValue != null && !paramValue.isBlank()) {
                    double parsedDbl = MdlUtil.parseDouble(paramValue, MdlConst.DBL_NULL);
                    if (parsedDbl != MdlConst.DBL_NULL) {
                        periodTerm = parsedDbl;
                        long secondsOffset;
                        switch (periodUnit) {
                            case "h":
                                secondsOffset = (long) (parsedDbl * 3600);
                                break;
                            case "m":
                                secondsOffset = (long) (parsedDbl * 60);
                                break;
                            case "s":
                                secondsOffset = (long) parsedDbl;
                                break;
                            default:
                                secondsOffset = (long) (parsedDbl * 86400);
                                break;
                        }
                        LocalDateTime baseTime = isBaseTimeNow ? LocalDateTime.now() : LocalDateTime.now().toLocalDate().atStartOfDay();
                        if (MdlArg.containsKey(namedArgs, "new")) {
                            prop.setAfter(true);
                            isNew = true;
                            prop.setAfterTime(baseTime.minusSeconds(secondsOffset));
                        } else {
                            prop.setBefore(true);
                            prop.setBeforeTime(baseTime.minusSeconds(secondsOffset));
                        }
                    }
                }
            }
        }

        // -before yyyyMMdd
        if (MdlArg.containsKey(namedArgs, "before")) {
            String paramValue = MdlArg.getValue(namedArgs, "before");
            if (paramValue != null && !paramValue.isBlank()) {
                switch (paramValue) {
                    case "now":
                        prop.setBeforeTime(LocalDateTime.now());
                        prop.setBefore(true);
                        break;
                    case "today":
                        prop.setBeforeTime(LocalDateTime.now().toLocalDate().atStartOfDay());
                        prop.setBefore(true);
                        break;
                    case "lastday":
                    case "yesterday":
                        prop.setBeforeTime(LocalDateTime.now().toLocalDate().minusDays(1).atStartOfDay());
                        prop.setBefore(true);
                        break;
                    case "tomorrow":
                    case "nextday":
                        prop.setBeforeTime(LocalDateTime.now().toLocalDate().plusDays(1).atStartOfDay());
                        prop.setBefore(true);
                        break;
                    default:
                        LocalDateTime ldt = MdlDate.parseDateTime(paramValue);
                        if (ldt != null) {
                            prop.setBeforeTime(ldt);
                            prop.setBefore(true);
                        } else {
                            double dbl = MdlUtil.parseDouble(paramValue, MdlConst.DBL_NULL);
                            if (dbl != MdlConst.DBL_NULL && dbl < 19700101.0) {
                                prop.setBeforeTime(LocalDateTime.now().toLocalDate().plusDays((long) dbl).atStartOfDay());
                                prop.setBefore(true);
                            }
                        }
                        break;
                }
            }
        }

        // -after yyyyMMdd
        if (MdlArg.containsKey(namedArgs, "after")) {
            String paramValue = MdlArg.getValue(namedArgs, "after");
            if (paramValue != null && !paramValue.isBlank()) {
                switch (paramValue) {
                    case "now":
                        prop.setAfterTime(LocalDateTime.now());
                        prop.setAfter(true);
                        break;
                    case "today":
                        prop.setAfterTime(LocalDateTime.now().toLocalDate().atStartOfDay());
                        prop.setAfter(true);
                        break;
                    case "lastday":
                    case "yesterday":
                        prop.setAfterTime(LocalDateTime.now().toLocalDate().minusDays(1).atStartOfDay());
                        prop.setAfter(true);
                        break;
                    case "tomorrow":
                    case "nextday":
                        prop.setAfterTime(LocalDateTime.now().toLocalDate().plusDays(1).atStartOfDay());
                        prop.setAfter(true);
                        break;
                    default:
                        LocalDateTime ldt = MdlDate.parseDateTime(paramValue);
                        if (ldt != null) {
                            prop.setAfterTime(ldt);
                            prop.setAfter(true);
                        } else {
                            double dbl = MdlUtil.parseDouble(paramValue, MdlConst.DBL_NULL);
                            if (dbl != MdlConst.DBL_NULL && dbl < 10101.0) {
                                prop.setAfterTime(LocalDateTime.now().toLocalDate().plusDays((long) dbl).atStartOfDay());
                                prop.setAfter(true);
                            }
                        }
                        break;
                }
            }
        }

        // -dirterm
        if (MdlArg.containsKey(namedArgs, "dirterm")) {
            prop.setDirTerm(true);
        }

        // -size value
        if (MdlArg.containsKey(namedArgs, "size")) {
            String paramValue = MdlArg.getValue(namedArgs, "size");
            if (paramValue != null && !paramValue.isBlank()) {
                Matcher sizeMatcher = SIZE_PATTERN_REGEX.matcher(paramValue);
                if (sizeMatcher.find()) {
                    String sign = sizeMatcher.group("SIGN");
                    String valueStr = sizeMatcher.group("VALUE");
                    String unit = sizeMatcher.group("UNIT");
                    double dblVal = MdlUtil.parseDouble(valueStr, 0.0);
                    switch (unit.toUpperCase(Locale.ROOT)) {
                        case "KB":
                            dblVal *= 1024.0;
                            break;
                        case "MB":
                            dblVal *= 1024.0 * 1024.0;
                            break;
                        case "GB":
                            dblVal *= 1024.0 * 1024.0 * 1024.0;
                            break;
                        case "TB":
                            dblVal *= 1024.0 * 1024.0 * 1024.0 * 1024.0;
                            break;
                        default:
                            break;
                    }
                    prop.setCompSize((long) dblVal);
                    if ("-".equals(sign)) {
                        prop.setCompOpe(ClsBaseDir.COMPARISON_LE);
                        formattedCompSize = "-" + MdlUtil.formatByteSize(prop.getCompSize());
                    } else {
                        prop.setCompOpe(ClsBaseDir.COMPARISON_GE);
                        formattedCompSize = MdlUtil.formatByteSize(prop.getCompSize());
                    }
                }
            }
        }

        // フィルター引数取得
        cmmnArgs.getFilterLists();
        prop.setIncFilesList(cmmnArgs.getIncFilesList());
        prop.setIncDirsList(cmmnArgs.getIncDirsList());
        prop.setExcFilesList(cmmnArgs.getExcFilesList());
        prop.setExcDirsList(cmmnArgs.getExcDirsList());
        prop.setRegIncBasename(cmmnArgs.isRegIncBasename());
        prop.setRegExcBasename(cmmnArgs.isRegExcBasename());
        prop.setDirFilterOr(cmmnArgs.isDirFilterOr());
        prop.setIncHitRecursive(cmmnArgs.isIncHitRecursive());
        prop.setExcHitRecursive(cmmnArgs.isExcHitRecursive());

        // -xd-exc-p-dir
        if (MdlArg.containsKey(namedArgs, "xd-exc-p-dir")) {
            prop.setXdOnlyFiles(true);
        }

        // -locked [sample]
        if (MdlArg.containsKey(namedArgs, "locked")) {
            prop.setCheckFileLock(ClsBaseDir.CHECK_FILE_LOCK_SKIP);
            String paramValue = MdlArg.getValue(namedArgs, "locked");
            if ("sample".equalsIgnoreCase(paramValue)) {
                prop.setCheckFileLock(ClsBaseDir.CHECK_FILE_LOCK_SAMPLE);
            }
        }

        // -----------------------------------------------------------------
        // Copy With List File Option：
        // -----------------------------------------------------------------
        // -files path
        if (MdlArg.containsKey(namedArgs, "files")) {
            String paramValue = MdlArg.getValue(namedArgs, "files");
            if (paramValue != null && !paramValue.isBlank()) {
                prop.setFileListPath(paramValue);
                ClsConfigFile configFile = new ClsConfigFile(logger);
                configFile.setConfigList(prop.getFileList());
                if (configFile.loadToList(prop.getFileListPath(), true) < 1) {
                    isOk = false;
                    logger.writeLine(MdlConst.LVL_E, "INVALID ARGUMENT : -files " + prop.getFileListPath());
                }
            }
        }

        // -files-type type
        if (MdlArg.containsKey(namedArgs, "files-type")) {
            String paramValue = MdlArg.getValue(namedArgs, "files-type");
            if ("full".equalsIgnoreCase(paramValue)) {
                prop.setFileListType("full");
                prop.setFilesTypeCode(ClsBaseDir.FILES_FULL);
                prop.setNeedPathFr(false);
                prop.setNeedPathTo(false);
                prop.setFrPathCheck(false);
            }
        }

        // -files-Regex regex
        for (String key : new String[] { "files-regex", "files-Regex" }) {
            if (MdlArg.containsKey(namedArgs, key)) {
                String paramValue = MdlArg.getValue(namedArgs, key);
                if (paramValue != null && !paramValue.isBlank()) {
                    prop.setFileListRegex(paramValue);
                    break;
                }
            }
        }

        // -----------------------------------------------------------------
        // Find Or Commnad Exec Cmd Option：
        // -----------------------------------------------------------------
        // -dq
        if (MdlArg.containsKey(namedArgs, "dq")) {
            prop.setDq(true);
        }

        // -type f|d|a
        if (MdlArg.containsKey(namedArgs, "type")) {
            String paramValue = MdlArg.getValue(namedArgs, "type");
            if ("f".equalsIgnoreCase(paramValue)) {
                showTypeMode = paramValue;
                prop.setTypeCode(MdlConst.INT_TYPE_FILE);
            } else if ("d".equalsIgnoreCase(paramValue)) {
                showTypeMode = paramValue;
                prop.setTypeCode(MdlConst.INT_TYPE_DIRECTORY);
                if (prop.getActionCode() == ClsBaseDir.ACTION_COPY) {
                    prop.setFileCopy(false);
                }
            } else if ("a".equalsIgnoreCase(paramValue) || "b".equalsIgnoreCase(paramValue)) {
                showTypeMode = paramValue;
                prop.setTypeCode(MdlConst.INT_TYPE_ALL);
            }
        }

        // -exec|-ps cmd {}
        if (MdlArg.containsKey(namedArgs, "exec")) {
            String paramValue = MdlArg.getValue(namedArgs, "exec");
            if (paramValue != null && !paramValue.isBlank()) {
                prop.setCmdPath(paramValue.replace("_FSFILEUTIL_", exeDir + File.separator + exeBaseName + ".jar"));
            }
        }
        if (MdlArg.containsKey(namedArgs, "ps")) {
            String paramValue = MdlArg.getValue(namedArgs, "ps");
            if (paramValue != null && !paramValue.isBlank()) {
                prop.setCmdPath(paramValue);
                prop.setExecModeCode(ClsBaseDir.EXEC_MODE_PS);
            }
        }

        // -exec-args args
        if (MdlArg.containsKey(namedArgs, "exec-args")) {
            prop.setCmdArgs(MdlArg.getValue(namedArgs, "exec-args"));
        }

        // -exec-mode mode
        for (String key : new String[] { "exec-mode", "cnd-mode" }) {
            if (MdlArg.containsKey(namedArgs, key)) {
                String paramValue = MdlArg.getValue(namedArgs, key);
                if (paramValue != null && !paramValue.isBlank()) {
                    switch (paramValue.toLowerCase(Locale.ROOT)) {
                        case "cmd":
                            prop.setExecModeCode(ClsBaseDir.EXEC_MODE_CMD);
                            break;
                        case "c":
                        case "exe":
                            prop.setExecModeCode(ClsBaseDir.EXEC_MODE_EXE);
                            break;
                        case "ps":
                            prop.setExecModeCode(ClsBaseDir.EXEC_MODE_PS);
                            break;
                        default:
                            break;
                    }
                    break;
                }
            }
        }

        // -cwd [path]
        if (MdlArg.containsKey(namedArgs, "cwd")) {
            String paramValue = MdlArg.getValue(namedArgs, "cwd");
            prop.setWorkDir(paramValue != null && !paramValue.isBlank() ? paramValue : prop.getSourcePath());
        }

        // -w int / -e int
        if (MdlArg.containsKey(namedArgs, "w")) {
            int parsed = MdlUtil.parseInt(MdlArg.getValue(namedArgs, "w"), MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setWarnThreshold(parsed);
            }
        }
        if (MdlArg.containsKey(namedArgs, "e")) {
            int parsed = MdlUtil.parseInt(MdlArg.getValue(namedArgs, "e"), MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setErrorThreshold(parsed);
            }
        }

        // -normal / -negative
        if (MdlArg.containsKey(namedArgs, "normal")) {
            prop.setAlwaysNormal(true);
        }
        if (MdlArg.containsKey(namedArgs, "negative")) {
            prop.setErrorAtNegativeValue(true);
        }

        // -show-cmd / -show-output / -show-retcd
        if (MdlArg.containsKey(namedArgs, "show-cmd")) {
            String paramValue = MdlArg.getValue(namedArgs, "show-cmd");
            if (paramValue != null && !paramValue.isBlank()) {
                switch (paramValue.toLowerCase(Locale.ROOT)) {
                    case "false":
                    case "no":
                    case "n":
                        prop.setShowCmd(false);
                        break;
                    default:
                        prop.setShowCmd(true);
                        break;
                }
            }
        }
        if (MdlArg.containsKey(namedArgs, "show-output")) {
            String paramValue = MdlArg.getValue(namedArgs, "show-output");
            if (paramValue != null && !paramValue.isBlank()) {
                switch (paramValue.toLowerCase(Locale.ROOT)) {
                    case "false":
                    case "no":
                    case "n":
                        prop.setShowOutput(false);
                        break;
                    default:
                        prop.setShowOutput(true);
                        break;
                }
            }
        }
        if (MdlArg.containsKey(namedArgs, "show-retcd")) {
            String paramValue = MdlArg.getValue(namedArgs, "show-retcd");
            if (paramValue != null && !paramValue.isBlank()) {
                switch (paramValue.toLowerCase(Locale.ROOT)) {
                    case "false":
                    case "no":
                    case "n":
                        prop.setShowExitCode(false);
                        break;
                    default:
                        prop.setShowExitCode(true);
                        break;
                }
            }
        }

        // -cat-options o1,o2
        List<String> listCatOptions = new ArrayList<>();
        if (MdlArg.containsKey(namedArgs, "cat-options")) {
            String paramValue = MdlArg.getValue(namedArgs, "cat-options");
            if (paramValue != null && !paramValue.isBlank()) {
                prop.setCat(true);
                listCatOptions = MdlUtil.parseCsvToList(listCatOptions, paramValue, "[,\\/|]", 0, false);
            }
        }

        // -cat-i|x|p|e|xml-nl
        if (MdlArg.containsKey(namedArgs, "cat")) {
            prop.setCat(true);
        }
        if (MdlArg.containsKey(namedArgs, "cat-i")) {
            String paramValue = MdlArg.getValue(namedArgs, "cat-i");
            if (paramValue != null && !paramValue.isBlank()) {
                prop.setCat(true);
                prop.setCatI(paramValue);
            }
        }
        if (MdlArg.containsKey(namedArgs, "cat-x")) {
            String paramValue = MdlArg.getValue(namedArgs, "cat-x");
            if (paramValue != null && !paramValue.isBlank()) {
                prop.setCat(true);
                prop.setCatX(paramValue);
            }
        }
        if (MdlArg.containsKey(namedArgs, "cat-p")) {
            String paramValue = MdlArg.getValue(namedArgs, "cat-p");
            if (paramValue != null && !paramValue.isBlank()) {
                prop.setCat(true);
                prop.setCatP(paramValue);
            }
        }
        if (MdlArg.containsKey(namedArgs, "cat-xml-nl")) {
            String paramValue = MdlArg.getValue(namedArgs, "cat-xml-nl");
            if (paramValue != null && !paramValue.isBlank()) {
                prop.setCat(true);
                prop.setCatP("xml");
                prop.setCatXmlNl(paramValue);
            }
        }
        if (MdlArg.containsKey(namedArgs, "cat-e")) {
            String paramValue = MdlArg.getValue(namedArgs, "cat-e");
            if (paramValue != null && !paramValue.isBlank()) {
                prop.setCat(true);
                prop.setCatE(paramValue);
            }
        }
        if (MdlArg.containsKey(namedArgs, "cat-a")) {
            prop.setCat(true);
            if (!listCatOptions.contains("a")) {
                listCatOptions.add("a");
            }
        }
        if (MdlArg.containsKey(namedArgs, "cat-wcl")) {
            prop.setCat(true);
            if (!listCatOptions.contains("wcl")) {
                listCatOptions.add("wcl");
            }
        }
        if (MdlArg.containsKey(namedArgs, "cat-ret-wcl")) {
            prop.setCat(true);
            prop.setCatRetWcl(true);
        }
        if (MdlArg.containsKey(namedArgs, "cat-n")) {
            prop.setCat(true);
            if (!listCatOptions.contains("n")) {
                listCatOptions.add("n");
            }
        }
        if (MdlArg.containsKey(namedArgs, "cat-h")) {
            prop.setCat(true);
            if (!listCatOptions.contains("h")) {
                listCatOptions.add("h");
            }
        }
        for (String opt : listCatOptions) {
            if (prop.getCatOptions().isEmpty()) {
                prop.setCatOptions("-" + opt);
            } else {
                prop.setCatOptions(prop.getCatOptions() + " -" + opt);
            }
        }
        if (prop.isCat() && prop.getCmdPath().isEmpty()) {
            boolean isWindows = System.getProperty("os.name", "").toLowerCase(Locale.ROOT).contains("win");
            String defaultCatName = isWindows ? "cat.exe" : "cat";
            prop.setCmdPath(exeDir + File.separator + defaultCatName);
        }

        // -nice / -priority
        for (String key : new String[] { "priority", "nice" }) {
            if (MdlArg.containsKey(namedArgs, key)) {
                String paramValue = MdlArg.getValue(namedArgs, key);
                if (paramValue != null && !paramValue.isBlank()) {
                    int parsed = MdlUtil.parseInt(paramValue, MdlConst.INT_NULL);
                    if (parsed != MdlConst.INT_NULL) {
                        prop.setPriority(parsed);
                        break;
                    }
                }
            }
        }

        // -n
        if (MdlArg.containsKey(namedArgs, "n")) {
            prop.setShowPath(true);
            if (!listCatOptions.contains("n")) {
                listCatOptions.add("n");
            }
        }

        // -----------------------------------------------------------------
        // Wait Option：
        // -----------------------------------------------------------------
        // -c count / -i interval
        if (MdlArg.containsKey(namedArgs, "c")) {
            int parsed = MdlUtil.parseInt(MdlArg.getValue(namedArgs, "c"), MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setMaxLoop(parsed);
            }
        }
        if (MdlArg.containsKey(namedArgs, "i")) {
            int parsed = MdlUtil.parseInt(MdlArg.getValue(namedArgs, "i"), MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setInterval(parsed);
            }
        }

        // -----------------------------------------------------------------
        // Rotate Option：
        // -----------------------------------------------------------------
        // -k keep max
        if (MdlArg.containsKey(namedArgs, "k")) {
            int parsed = MdlUtil.parseInt(MdlArg.getValue(namedArgs, "k"), MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setMaxKeep(parsed);
            }
        }

        // -----------------------------------------------------------------
        // Network Option：
        // -----------------------------------------------------------------
        // -sec-range
        if (MdlArg.containsKey(namedArgs, "sec-range")) {
            int parsed = MdlUtil.parseInt(MdlArg.getValue(namedArgs, "sec-range"), MdlConst.INT_NULL);
            if (parsed != MdlConst.INT_NULL) {
                prop.setSecRange(parsed);
            }
        }

        // -----------------------------------------------------------------
        // Subfolder Sorting Option：
        // -----------------------------------------------------------------
        if (MdlArg.containsKey(namedArgs, "sort")) {
            String paramValue = MdlArg.getValue(namedArgs, "sort");
            if (paramValue != null && !paramValue.isBlank()) {
                prop.setSortType(MdlFile.getSortTypeNum(paramValue));
            }
        }
        if (MdlArg.containsKey(namedArgs, "desc")) {
            prop.setAscending(false);
        }

        // -----------------------------------------------------------------
        // Output Option：
        // -----------------------------------------------------------------
        // -progress
        if (MdlArg.containsKey(namedArgs, "progress")) {
            prop.setProgress(true);
            String paramValue = MdlArg.getValue(namedArgs, "progress");
            if (paramValue != null && !paramValue.isBlank()) {
                List<String> listTmp = new ArrayList<>();
                listTmp = MdlUtil.parseCsvToList(listTmp, paramValue, "[,\\/\\.\\-|]", 0, false);
                if (!listTmp.isEmpty() && MdlUtil.isNumeric(listTmp.get(0))) {
                    prop.setProgressIntervalDirs(MdlUtil.parseInt(listTmp.get(0), 0));
                }
                if (listTmp.size() > 1 && MdlUtil.isNumeric(listTmp.get(1))) {
                    prop.setProgressIntervalFiles(MdlUtil.parseInt(listTmp.get(1), 0));
                }
            }
            if (prop.getProgressIntervalDirs() == 0 && prop.getProgressIntervalFiles() == 0) {
                prop.setProgressIntervalDirs(1000);
                prop.setProgressIntervalFiles(100000);
            }
        }

        // -show show
        if (MdlArg.containsKey(namedArgs, "show")) {
            String paramValue = MdlArg.getValue(namedArgs, "show");
            if (paramValue != null && !paramValue.isBlank()) {
                switch (prop.getActionCode()) {
                    case ClsBaseDir.ACTION_GET_SIZE:
                        if ("all".equalsIgnoreCase(paramValue)) {
                            prop.setShowPath(true);
                            prop.setShowDirNum(true);
                            prop.setShowFileNum(true);
                            prop.setShowSize(true);
                        } else {
                            String lower = paramValue.toLowerCase(Locale.ROOT);
                            if (lower.contains("p")) prop.setShowPath(true);
                            if (lower.contains("d")) prop.setShowDirNum(true);
                            if (lower.contains("f")) prop.setShowFileNum(true);
                            if (lower.contains("s")) prop.setShowSize(true);
                        }
                        break;
                    case ClsBaseDir.ACTION_GET_PERM:
                        if ("all".equalsIgnoreCase(paramValue)) {
                            prop.setShowPath(true);
                            prop.setShowOwner(true);
                            prop.setShowPerm(true);
                        } else {
                            String lower = paramValue.toLowerCase(Locale.ROOT);
                            if (lower.contains("p")) prop.setShowPath(true);
                            if (lower.contains("o")) prop.setShowOwner(true);
                            if (lower.contains("r")) prop.setShowPerm(true);
                        }
                        break;
                    case ClsBaseDir.ACTION_GET_OWNER:
                        if ("all".equalsIgnoreCase(paramValue)) {
                            prop.setShowPath(true);
                            prop.setShowOwner(true);
                        } else {
                            String lower = paramValue.toLowerCase(Locale.ROOT);
                            if (lower.contains("p")) prop.setShowPath(true);
                            if (lower.contains("o")) prop.setShowOwner(true);
                            if (lower.contains("r")) prop.setShowPerm(true);
                        }
                        break;
                    default:
                        switch (paramValue.toLowerCase(Locale.ROOT)) {
                            case "new":
                            case "n":
                                showMessageMode = "new";
                                prop.setShowUpdatedFile(false);
                                prop.setShowSameFile(false);
                                break;
                            case "updated":
                            case "u":
                                showMessageMode = "updated";
                                prop.setShowNewFile(false);
                                prop.setShowSameFile(false);
                                break;
                            case "diff":
                            case "modified":
                            case "m":
                            case "nu":
                            case "un":
                                showMessageMode = "diff";
                                prop.setShowSameFile(false);
                                break;
                            default:
                                showMessageMode = "all";
                                break;
                        }
                        break;
                }
            } else {
                switch (prop.getActionCode()) {
                    case ClsBaseDir.ACTION_GET_SIZE:
                        prop.setShowSize(true);
                        break;
                    case ClsBaseDir.ACTION_GET_PERM:
                        prop.setShowPerm(true);
                        break;
                    case ClsBaseDir.ACTION_GET_OWNER:
                        prop.setShowOwner(true);
                        break;
                    default:
                        break;
                }
            }
        }

        // -op-path r|f|t|b
        if (MdlArg.containsKey(namedArgs, "op-path")) {
            prop.setOutputPathCode(prop.getOutputModeCode(MdlArg.getValue(namedArgs, "op-path")));
        }

        // -op-prefix
        if (MdlArg.containsKey(namedArgs, "op-prefix")) {
            prop.setOutputPathPrefix(MdlArg.getValue(namedArgs, "op-prefix"));
        }

        // -show-dir max
        if (MdlArg.containsKey(namedArgs, "show-dir")) {
            String paramValue = MdlArg.getValue(namedArgs, "show-dir");
            if (paramValue != null && !paramValue.isBlank()) {
                int parsed = MdlUtil.parseInt(paramValue, MdlConst.INT_NULL);
                if (parsed != MdlConst.INT_NULL) {
                    prop.setShowCurDir(parsed);
                }
            }
        }

        // -echo-retcd
        if (MdlArg.containsKey(namedArgs, "echo-retcd")) {
            isEchoRetcode = true;
        }

        // -ret-files
        if (MdlArg.containsKey(namedArgs, "ret-files")) {
            prop.setRetFiles(true);
        }

        namedArgs.clear();
        return isOk;
    }

    /**
     * コマンドライン引数の使用方法（Usage）および指定可能な各種オプション一覧をログに出力します。
     */
    public void showUsage() {
        logger.writeLine(MdlConst.LVL_NONE, "");
        logger.writeLine(MdlConst.LVL_NONE, "Usage : java -jar " + exeBaseName + ".jar -f <path> -t <path> [Option] [Option]...");
        logger.writeLine(MdlConst.LVL_NONE, "");
        logger.writeLine(MdlConst.LVL_NONE, "Basic Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -path|-f path      ：操作対象パス                    （現在値=" + prop.getSourcePath() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -a    action       ：操作内容                        （現在値=" + prop.getAction() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "                        コピー      ：copy(初期値)|sync|move|reverse|dircopy|syncrm|flatcopy");
        logger.writeLine(MdlConst.LVL_NONE, "                        ファイル操作：ls|find|mkdir|touch|delete|delete-dir|delete-file|rename|rotate|isLocked");
        logger.writeLine(MdlConst.LVL_NONE, "                        コマンド実行：exec");
        logger.writeLine(MdlConst.LVL_NONE, "                        SYMLINK     ：mklink|realpath");
        logger.writeLine(MdlConst.LVL_NONE, "                        存在確認    ：exist|isdir|isfile|wait");
        logger.writeLine(MdlConst.LVL_NONE, "                        属性表示    ：size|perm|owner");
        logger.writeLine(MdlConst.LVL_NONE, "Copy Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -t    path         ：コピー・移動先パス              （現在値=" + prop.getDestinationPath() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -m    check        ：差分更新モード                  （現在値=" + prop.getMode() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "                        サイズチェック有：size | new | old | mtime | cksum | adler32 | sha1");
        logger.writeLine(MdlConst.LVL_NONE, "                        サイズチェック無：date | newer | older | exist");
        logger.writeLine(MdlConst.LVL_NONE, "   -list              ：対象リストの表示                （現在値=" + prop.isList() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -tsc|-tsm|-ts      ：タイムスタンプ同期(1:作成日のみ|2:修正日のみ|3:全部)（現在値=" + prop.getCpTimestamp() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -fchk              ：コピー元が存在しなければ異常終了（現在値=" + prop.isSourceCheck() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -rmnohit           ：同期削除時の除外設定無効化フラグ（現在値=" + prop.isRmNohit() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -no-emptydir       ：空ディレクトリ非コピーフラグ    （現在値=" + (prop.isAlwaysMkDir() ? "False" : "True") + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -async             ：非同期コピーフラグ              （現在値=" + (prop.getCopyCmdType() == ClsBaseDir.COPY_ASYNC) + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -os                ：OSコピー/移動フラグ             （現在値=" + (prop.getCopyCmdType() == ClsBaseDir.COPY_OS_CMD) + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -skipsize|-copysize：cksum計算除外サイズ(MB)         （現在値=" + (prop.getSkipSize() / 1024 / 1024) + " / " + (prop.getCopySize() / 1024 / 1024) + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -fileshare mode    ：3:ReadWrite、7:ReadWrite|Delete （現在値=" + fileShareMode + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -wait-retry-copy n ：Wait msec before retry copy     （現在値=" + prop.getWaitMSecForRetryCopy() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -retry-syscopy n   ：例外時system copyリトライ回数   （現在値=" + prop.getRetrySystemCopyMax() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "Symbolic Link Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -sym [0|1|2]       ：シンボリックリンク判定有効化    （現在値=" + prop.isSymLink() + " OverWrite=" + prop.getOverwriteLevel() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -rel               ：シンボリックリンク相対パス化    （現在値=" + prop.isRelative() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "Backup Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -backup <path>     ：上書きファイル退避フラグ        （現在値=" + (prop.isBackup() ? prop.getBackupDir() : "False") + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -force             ：退避失敗時処理強行フラグ        （現在値=" + (prop.isErrorIfBackupFailed() ? "False" : "True") + "）");
        logger.writeLine(MdlConst.LVL_NONE, "Replace to path string Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -replace a:b,c:d   ：-f|-tの文字列置換CSVリスト");
        logger.writeLine(MdlConst.LVL_NONE, "   -ts-f|-ts-t|-ts-b n：-f|-t|-backup日付変換マクロ置換日付：n:now|t:today|y:yesterday|nextday:nextday|fotm:firstofthismonth|eolm:endoflastmonth|f:file");
        logger.writeLine(MdlConst.LVL_NONE, "Filter Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -max               ：最大ディレクトリ階層            （現在値=" + prop.getMaxDepth() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -min               ：最小ディレクトリ階層            （現在値=" + prop.getMinDepth() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -term|-days value  ：更新経過期間                    （現在値=" + periodTerm + ")");
        logger.writeLine(MdlConst.LVL_NONE, "   -period d|h|m|s    ：期間単位                        （現在値=" + periodUnit + ")");
        logger.writeLine(MdlConst.LVL_NONE, "   -new               ：経過日数(-term)以内の場合       （現在値=" + isNew + ")");
        logger.writeLine(MdlConst.LVL_NONE, "   -before yyyyMMdd   ：更新日付が指定日以前            （現在値=" + (prop.isBefore() ? MdlDate.getFormattedDate(prop.getBeforeTime(), "yyyyMMdd") + "：" + MdlDate.getFormattedDate(prop.getBeforeTime(), "yyyy/MM/dd HH:mm:ss") : "") + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -after  yyyyMMdd   ：更新日付が指定日以降            （現在値=" + (prop.isAfter() ? MdlDate.getFormattedDate(prop.getAfterTime(), "yyyyMMdd") + "：" + MdlDate.getFormattedDate(prop.getAfterTime(), "yyyy/MM/dd HH:mm:ss") : "") + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -dirterm           ：ディレクトリ日付判定フラグ      （現在値=" + prop.isDirTerm() + ")");
        logger.writeLine(MdlConst.LVL_NONE, "   -size value        ：サイズ比較 >= +val | <= -val    （現在値=" + formattedCompSize + ")");
        logger.writeLine(MdlConst.LVL_NONE, "   -id|-idf 正規表現  ：絞込ディレクトリ名(,|/区切り）  （現在値=[" + String.join("|", prop.getIncDirsList()) + "] FullPath=" + !prop.isRegIncBasename() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -xd|-xdf 正規表現  ：除外ディレクトリ名(,|/区切り）  （現在値=[" + String.join("|", prop.getExcDirsList()) + "] FullPath=" + !prop.isRegExcBasename() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -if 正規表現       ：絞込ファイル名(,|/区切り)     (例：\\.log$,\\.dat$）（現在値=[" + String.join("|", prop.getIncFilesList()) + "])");
        logger.writeLine(MdlConst.LVL_NONE, "   -xf 正規表現       ：除外ファイル名(,|/区切り)     (例：\\.exe$,\\.dll$）（現在値=[" + String.join("|", prop.getExcFilesList()) + "])");
        logger.writeLine(MdlConst.LVL_NONE, "   -idorxd            ：-id or -xdフラグ                （現在値=" + prop.isDirFilterOr() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -no-id-rec         ：-id結果の階層下への非適用フラグ （現在値=" + !prop.isIncHitRecursive() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -no-xd-rec         ：-xd結果の階層下への非適用フラグ （現在値=" + !prop.isExcHitRecursive() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -xd-exc-p-dir      ：-xd該当時親DIRコピーフラグ      （現在値=" + prop.isXdOnlyFiles() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -locked [sample]   ：ロックファイル除外、又は抽出    （現在値=" + prop.getCheckLockFileModeStr(prop.getCheckFileLock()) + "）");
        logger.writeLine(MdlConst.LVL_NONE, "Copy With List File Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -files path        ：コピー対象相対パスリスト        （現在値=" + prop.getFileListPath() + " / ファイル数=" + prop.getFileList().size() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -files-type type   ：パスリスト形式(rel|full)        （現在値=" + prop.getFileListType() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -files-Regex regex ：パスリストデリミタ正規表現      （現在値=" + prop.getFileListRegex() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "Find Or Commnad Exec Cmd Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -dq                ：-a find|ls時のDQ囲み有無        （現在値=" + prop.isDq() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -type f|d|a        ：-a find|ls時の表示対象          （現在値=" + showTypeMode + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -exec|-ps cmd {}   ：実行コマンド                    （現在値=" + prop.getCmdPath().trim() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -exec-args args    ：実行コマンド引数                （現在値=" + prop.getCmdArgs().trim() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -exec-mode mode    ：cmd|exe|ps                      （現在値=" + prop.getExecModeStr(prop.getExecModeCode()) + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -cwd [path]        ：ワーキングディレクトリ          （現在値=" + prop.getWorkDir() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -w int             ：警告閾値                        （現在値=" + prop.getWarnThreshold() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -e int             ：異常閾値                        （現在値=" + prop.getErrorThreshold() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -normal            ：常に正常終了                    （現在値=" + prop.isAlwaysNormal() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -negative          ：負値のエラー判定有無            （現在値=" + prop.isErrorAtNegativeValue() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -show-cmd y|n      ：実行コマンド表示                （現在値=" + prop.isShowCmd() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -show-output y|n   ：実行コマンド出力表示            （現在値=" + prop.isShowOutput() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -show-retcd y|n    ：実行コマンド結果表示            （現在値=" + prop.isShowExitCode() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -cat-i|x|p|e|xml-nl：cat.exe実行オプション           （現在値=" + prop.isCat() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -cat-options o1,o2 ：cat.exe実行オプションリスト     （現在値=" + prop.getCatOptions() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -cat-ret-wcl       ：cat.exe -ret-wcl行数戻値フラグ  （現在値=" + prop.isCatRetWcl() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -nice int          ：プロセス優先度（0:RealTime - 5:Idle：現在値=" + prop.getPriority() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -n                 ：パス表示フラグ                  （現在値=" + prop.isShowPath() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -timeout           ：TIMEOUT(秒)                     （現在値=" + prop.getTimeout() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "Wait Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -i interval        ：確認間隔(秒)                    （現在値=" + prop.getInterval() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -c count           ：確認回数(回)                    （現在値=" + prop.getMaxLoop() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "Rotate Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -k keep max        ：最大保存世代数(個)              （現在値=" + prop.getMaxKeep() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "Network Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -sec-range         ：タイムスタンプずれ許容範囲（秒）（現在値=" + prop.getSecRange() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "Subfolder Sorting Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -sort type         ：ソート=none|name|ctime|mtime    （現在値=" + MdlFile.getSortTypeName(prop.getSortType()) + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -desc              ：降順フラグ                      （現在値=" + !prop.isAscending() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "Output Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -v|-vv|-brief      ：冗長表示|簡素表示               （現在値=" + prop.getVerbose() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -progress          ：進捗表示                        （現在値=" + prop.isProgress() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -diff              ：差分表示フラグ                  （現在値=" + !prop.isShowSameFile() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -show show         ：表示内容：new|updated|diff      （現在値=" + showMessageMode + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -op-path r|f|t|b   ：画面表示パス種別                （現在値=" + prop.getOutputModeStr(prop.getOutputPathCode()) + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -op-prefix         ：画面表示相対パス付加PREFIX      （現在値=" + prop.getOutputPathPrefix() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -stacktrace        ：例外時スタックトレース表示      （現在値=" + prop.isStackTrace() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -show-dir max      ：処理中ディレクトリの表示        （現在値=" + prop.getShowCurDir() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -echo-retcd        ：終了コード表示フラグ            （現在値=" + isEchoRetcode + "）");
        logger.writeLine(MdlConst.LVL_NONE, "   -console mode      ：メッセージ表示 off|stdout|stderr");
        logger.writeLine(MdlConst.LVL_NONE, "Other Option：");
        logger.writeLine(MdlConst.LVL_NONE, "   -ldir path         ：ログ出力先ディレクトリパス(日付付ファイル名で出力)");
        logger.writeLine(MdlConst.LVL_NONE, "   -log  path         ：ログ出力ファイルパス(-ldirより優先)");
        logger.writeLine(MdlConst.LVL_NONE, "   -dumpargs          ：引数の表示");
        logger.writeLine(MdlConst.LVL_NONE, "   -ret-files         ：ファイル数戻値フラグ            （現在値=" + prop.isRetFiles() + "）");
        logger.writeLine(MdlConst.LVL_NONE, "Format specifier conversion：");
        logger.writeLine(MdlConst.LVL_NONE, "   ファイルパス       ：{}、_PATH_、_RELPATH_、_RELFLAT_");
        logger.writeLine(MdlConst.LVL_NONE, "   ディレクトリパス   ：_BASEDIR_、_DIR_、_RELDIR_、_RELDIRFLAT_");
        logger.writeLine(MdlConst.LVL_NONE, "   ファイル・他       ：_FILENAME_、_BASENAME_、_COMPUTERNAME_、_USERNAME_");
        logger.writeLine(MdlConst.LVL_NONE, "   日時               ：%Y、%m、%d、%H、%M、%S、%w、%pid");
        logger.writeLine(MdlConst.LVL_NONE, "");
        logger.writeLine(MdlConst.LVL_NONE, "Return Code           ：" + MdlConst.LVL_I + ":SUCCESS / " + MdlConst.LVL_W + ":WARN / " + MdlConst.LVL_E + ":ERROR");
        logger.writeLine(MdlConst.LVL_NONE, "");
    }

    /**
     * 解析・設定されたパラメータの定義一覧をログ出力します。
     */
    public void printDefinition() {
        String temp = "";
        logger.writeLine(MdlConst.LVL_NONE, "------------------------------------------------------------");
        if (ClsBaseDir.ACTION_NONE != prop.getActionCode()) {
            logger.writeLine(MdlConst.LVL_NONE, "TARGET PATH : " + prop.getSourcePath());
        }
        if (prop.isNeedPathTo()) {
            logger.writeLine(MdlConst.LVL_NONE, "TO PATH     : " + prop.getDestinationPath());
        }
        switch (prop.getActionCode()) {
            case ClsBaseDir.ACTION_SYNC:
                temp = "sync (-rmnohit=" + prop.isRmNohit() + ")";
                break;
            default:
                temp = prop.getAction();
                break;
        }
        logger.writeLine(MdlConst.LVL_NONE, "ACTION      : " + temp);
        if (prop.isNeedPathTo()) {
            switch (prop.getCheckLogic()) {
                case ClsBaseDir.CHECK_SIZE:
                    temp = "CHECK : FILE SIZE";
                    break;
                case ClsBaseDir.CHECK_MTIME_NEW:
                    temp = (prop.isSizeCheck() ? "CHECK : FILE SIZE | MTIME(NEW)" : "CHECK : MTIME(NEW)");
                    break;
                case ClsBaseDir.CHECK_MTIME_OLD:
                    temp = (prop.isSizeCheck() ? "CHECK : FILE SIZE | MTIME(OLD)" : "CHECK : MTIME(OLD)");
                    break;
                case ClsBaseDir.CHECK_MTIME:
                    temp = (prop.isSizeCheck() ? "CHECK : FILE SIZE | MTIME" : "CHECK : MTIME");
                    break;
                case ClsBaseDir.CHECK_CKSUM:
                    temp = "CHECK : FILE SIZE | cksum";
                    break;
                case ClsBaseDir.CHECK_SHA1:
                    temp = "CHECK : FILE SIZE | sha1";
                    break;
                case ClsBaseDir.CHECK_ADLER32:
                    temp = "CHECK : FILE SIZE | adler32";
                    break;
                case ClsBaseDir.CHECK_EXIST:
                    temp = "CHECK : FILE EXIST OR NOT";
                    break;
                default:
                    temp = "NONE";
                    break;
            }
            logger.writeLine(MdlConst.LVL_NONE, "DIFF MODE   : " + temp);
            logger.writeLine(MdlConst.LVL_NONE, "FILTER INC  : DIR = [" + String.join("|", prop.getIncDirsList()) + "] / FILE = [" + String.join("|", prop.getIncFilesList()) + "]");
            logger.writeLine(MdlConst.LVL_NONE, "FILTER EXC  : DIR = [" + String.join("|", prop.getExcDirsList()) + "] / FILE = [" + String.join("|", prop.getExcFilesList()) + "]");
        }
        if (prop.getActionCode() == ClsBaseDir.ACTION_WAIT) {
            logger.writeLine(MdlConst.LVL_NONE, "MAX COUNT   : " + prop.getMaxLoop());
            logger.writeLine(MdlConst.LVL_NONE, "INTERVAL    : " + prop.getInterval());
            logger.writeLine(MdlConst.LVL_NONE, "SKIP LOCKED : " + (ClsBaseDir.CHECK_FILE_LOCK_SKIP == prop.getCheckFileLock() ? "True" : "False"));
        }
        if (prop.getActionCode() == ClsBaseDir.ACTION_ROTATE) {
            logger.writeLine(MdlConst.LVL_NONE, "MAX KEEP    : " + prop.getMaxKeep());
        }
        if (prop.isList()) {
            logger.writeLine(MdlConst.LVL_NONE, "LIST ONLY   : TRUE");
        }
        logger.writeLine(MdlConst.LVL_NONE, "------------------------------------------------------------\n");
    }

    /**
     * 指定されたタイムスタンプ指定モード・パスに基づいて基準日時を取得します。
     *
     * @param timestampMode タイムスタンプ指定モード文字列（t, today, y, yesterday, f, file 等）
     * @param path 対象ファイルパス
     * @param pathType パス種別
     * @return 算出した {@link LocalDateTime}
     */
    public LocalDateTime getTimestamp(String timestampMode, String path, int pathType) {
        LocalDateTime ts = LocalDateTime.now();
        if (timestampMode != null && !timestampMode.isBlank()) {
            LocalDateTime today = LocalDateTime.now().toLocalDate().atStartOfDay();
            switch (timestampMode.toLowerCase(Locale.ROOT)) {
                case "t":
                case "today":
                    ts = today;
                    break;
                case "y":
                case "yesterday":
                    ts = today.minusDays(1);
                    break;
                case "nextday":
                    ts = today.plusDays(1);
                    break;
                case "fotm":
                case "firstofthismonth":
                    ts = today.withDayOfMonth(1);
                    break;
                case "eolm":
                case "endoflastmonth":
                    ts = today.withDayOfMonth(1).minusDays(1);
                    break;
                case "f":
                case "file":
                    if (MdlFile.pathExists(path)) {
                        File file = new File(path);
                        ts = LocalDateTime.ofInstant(java.time.Instant.ofEpochMilli(file.lastModified()), java.time.ZoneId.systemDefault());
                    }
                    break;
                default:
                    break;
            }
        }
        return ts;
    }
}
