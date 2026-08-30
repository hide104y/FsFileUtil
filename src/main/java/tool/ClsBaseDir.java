package tool;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlFile;

/**
 * アプリケーションの動作パラメータ・設定情報を保持するデータクラスタです。
 */
public class ClsBaseDir {

    // パス出力モード定数
    public static final int RELATIVE = 0;
    public static final int FROM = 1;
    public static final int TO = 2;
    public static final int BOTH = 3;

    // ファイルリスト種別定数
    public static final int FILES_RELATIVE = 0;
    public static final int FILES_FULL = 1;

    // 比較演算子定数
    public static final int COMPARISON_NO = 0;
    public static final int COMPARISON_EQ = 1;
    public static final int COMPARISON_GT = 2;
    public static final int COMPARISON_GE = 3;
    public static final int COMPARISON_LT = 4;
    public static final int COMPARISON_LE = 5;

    // 日時基準定数
    public static final int DATETIME_NOW = 0;
    public static final int DATETIME_TODAY = 1;
    public static final int DATETIME_YESTERDAY = 2;
    public static final int DATETIME_FILEINFO = 3;

    // 外部コマンド実行モード定数
    public static final int EXEC_MODE_NORMAL = 0;
    public static final int EXEC_MODE_CMD = 1;
    public static final int EXEC_MODE_PS = 2;
    public static final int EXEC_MODE_PSC = 3;
    public static final int EXEC_MODE_EXE = 4;

    // ファイルロック検査モード定数
    public static final int CHECK_FILE_LOCK_NONE = 0;
    public static final int CHECK_FILE_LOCK_SAMPLE = 1;
    public static final int CHECK_FILE_LOCK_SKIP = 2;

    // ファイル共有モード定数
    public static final int FILE_SHARE_NONE = 0;
    public static final int FILE_SHARE_READ = 1;
    public static final int FILE_SHARE_WRITE = 2;
    public static final int FILE_SHARE_READ_WRITE = 3;
    public static final int FILE_SHARE_DELETE = 4;
    public static final int FILE_SHARE_INHERITABLE = 16;

    // コピー方式定数
    public static final int COPY_ASYNC = 0;
    public static final int COPY_BINARY = 1;
    public static final int COPY_OS_CMD = 2;

    // アクションコード定数
    public static final int ACTION_NONE = -1;
    public static final int ACTION_COPY = 0;
    public static final int ACTION_MOVE = 1;
    public static final int ACTION_SYNC = 2;
    public static final int ACTION_MKDIR = 10;
    public static final int ACTION_TOUCH = 11;
    public static final int ACTION_DELETE = 12;
    public static final int ACTION_MKLINK = 13;
    public static final int ACTION_LS = 15;
    public static final int ACTION_FIND = 16;
    public static final int ACTION_GET_REAL_PATH = 17;
    public static final int ACTION_LIST_LOCK_PROC = 18;
    public static final int ACTION_EXIST = 20;
    public static final int ACTION_EXIST_DIR = 21;
    public static final int ACTION_EXIST_FILE = 22;
    public static final int ACTION_WAIT = 23;
    public static final int ACTION_FILE_LOCKED = 24;
    public static final int ACTION_RENAME = 30;
    public static final int ACTION_ROTATE = 31;
    public static final int ACTION_GET_ATTRIB = 41;
    public static final int ACTION_GET_SIZE = 42;
    public static final int ACTION_GET_PERM = 43;
    public static final int ACTION_GET_OWNER = 44;
    public static final int ACTION_EXEC = 91;
    public static final int ACTION_ETC = 99;

    // 差分検査ロジック定数
    public static final int CHECK_NONE = 0;
    public static final int CHECK_SIZE = 1;
    public static final int CHECK_MTIME = 2;
    public static final int CHECK_MTIME_NEW = 3;
    public static final int CHECK_MTIME_OLD = 4;
    public static final int CHECK_CKSUM = 5;
    public static final int CHECK_SHA1 = 6;
    public static final int CHECK_ADLER32 = 7;
    public static final int CHECK_EXIST = 8;

    // タスク種別定数
    public static final int TASK_CP = 0;
    public static final int TASK_MV = 1;
    public static final int TASK_RM = 2;
    public static final int TASK_PRINT = 3;
    public static final int TASK_RENAME = 4;

    private String exeBaseName = "";
    private String exeDir = "";
    private String sourcePath = "";
    private String destinationPath = "";
    private String workDir = "";
    private String action = "copy";
    private String mode = "";
    private int copyCmdType = COPY_BINARY;
    private int actionCode = ACTION_COPY;
    private int task = TASK_CP;
    private int checkLogic = CHECK_NONE;
    private int compOpe = COMPARISON_NO;
    private int pathType = MdlFile.PATH_IS_NULL;
    private boolean isNeedPathFr = false;
    private boolean isNeedPathTo = false;
    private boolean isList = false;
    private boolean isReverse = false;
    private boolean isSizeCheck = true;
    private boolean isSyncRmOnly = false;
    private boolean isFlat = false;
    private boolean isDirTerm = false;
    private boolean isAlwaysMkDir = false;
    private boolean isFileCopy = true;
    private int checkFileLock = CHECK_FILE_LOCK_NONE;
    private boolean isSourceCheck = false;
    private boolean isFrPathCheck = true;
    private boolean isRetFiles = false;
    private long skipSize = 0;
    private long copySize = 0;
    private long compSize = 0;
    private int interval = 60;
    private int maxLoop = 1;
    private boolean isBackup = false;
    private boolean isErrorIfBackupFailed = true;
    private String backupDir = "";
    private int fileShare = FILE_SHARE_READ_WRITE;
    private int waitMSecForRetryCopy = 200;
    private int retrySystemCopyMax = 0;
    private double secRange = 0.0;
    private int verbose = 0;
    private int showCurDir = 0;
    private int outputPathCode = RELATIVE;
    private int progressIntervalDirs = 0;
    private int progressIntervalFiles = 0;
    private boolean isRelative = false;
    private boolean isProgress = false;
    private boolean isStackTrace = false;
    private boolean isShowNewFile = true;
    private boolean isShowUpdatedFile = true;
    private boolean isShowSameFile = true;
    private String outputPathPrefix = "";
    private boolean isShowPath = false;
    private boolean isShowSize = false;
    private boolean isShowDirNum = false;
    private boolean isShowFileNum = false;
    private boolean isShowPerm = false;
    private boolean isShowOwner = false;
    private boolean isSymLink = false;
    private int overwriteLevel = 0;
    private int maxKeep = 7;
    private String cmdPath = "";
    private String cmdArgs = "";
    private boolean isDq = false;
    private int warnThreshold = MdlConst.INT_NULL;
    private int errorThreshold = MdlConst.INT_NULL;
    private boolean isExecCmd = false;
    private int execModeCode = EXEC_MODE_EXE;
    private int priority = 3;
    private int timeout = 86400;
    private boolean isErrorAtNegativeValue = false;
    private boolean isAlwaysNormal = false;
    private boolean isShowCmd = false;
    private boolean isShowOutput = false;
    private boolean isShowExitCode = false;
    private boolean isCat = false;
    private boolean isCatRetWcl = false;
    private String catI = "";
    private String catX = "";
    private String catP = "";
    private String catE = "";
    private String catXmlNl = "";
    private String catOptions = "";
    private long files = 0;
    private long lines = 0;
    private int typeCode = MdlConst.INT_TYPE_ALL;
    private long maxDepth = MdlConst.LNG_MAX;
    private long minDepth = 0;
    private boolean isBefore = false;
    private boolean isAfter = false;
    private LocalDateTime beforeTime = null;
    private LocalDateTime afterTime = null;
    private boolean isRegIncBasename = false;
    private boolean isRegExcBasename = false;
    private boolean isIncHitRecursive = false;
    private boolean isExcHitRecursive = false;
    private boolean isDirFilterOr = false;
    private boolean isXdOnlyFiles = false;
    private boolean isRmNohit = false;
    private List<String> incFilesList = new ArrayList<>();
    private List<String> excFilesList = new ArrayList<>();
    private List<String> incDirsList = new ArrayList<>();
    private List<String> excDirsList = new ArrayList<>();
    private int filesTypeCode = FILES_RELATIVE;
    private String fileListPath = "";
    private String fileListType = "rel";
    private String fileListRegex = "[,|]";
    private List<String> fileList = new ArrayList<>();
    private int cpTimestamp = 0;
    private String tsSource = "";
    private String tsDestination = "";
    private String tsBackup = "";
    private int sortType = MdlFile.SORT_BY_NONE;
    private boolean isAscending = true;
    private boolean isShowDirList = false;
    private boolean isShowFileList = false;

    public ClsBaseDir() {
    }

    public String getExeBaseName() {
        return exeBaseName;
    }

    public void setExeBaseName(String exeBaseName) {
        this.exeBaseName = exeBaseName != null ? exeBaseName : "";
    }

    public String getExeDir() {
        return exeDir;
    }

    public void setExeDir(String exeDir) {
        this.exeDir = exeDir != null ? exeDir : "";
    }

    public String getSourcePath() {
        return sourcePath;
    }

    public void setSourcePath(String sourcePath) {
        this.sourcePath = sourcePath != null ? sourcePath : "";
    }

    public String getDestinationPath() {
        return destinationPath;
    }

    public void setDestinationPath(String destinationPath) {
        this.destinationPath = destinationPath != null ? destinationPath : "";
    }

    public String getWorkDir() {
        return workDir;
    }

    public void setWorkDir(String workDir) {
        this.workDir = workDir != null ? workDir : "";
    }

    public String getAction() {
        return action;
    }

    public void setAction(String action) {
        this.action = action != null ? action : "";
    }

    public String getMode() {
        return mode;
    }

    public void setMode(String mode) {
        this.mode = mode != null ? mode : "";
    }

    public int getCopyCmdType() {
        return copyCmdType;
    }

    public void setCopyCmdType(int copyCmdType) {
        this.copyCmdType = copyCmdType;
    }

    public int getActionCode() {
        return actionCode;
    }

    public void setActionCode(int actionCode) {
        this.actionCode = actionCode;
    }

    public int getTask() {
        return task;
    }

    public void setTask(int task) {
        this.task = task;
    }

    public int getCheckLogic() {
        return checkLogic;
    }

    public void setCheckLogic(int checkLogic) {
        this.checkLogic = checkLogic;
    }

    public int getCompOpe() {
        return compOpe;
    }

    public void setCompOpe(int compOpe) {
        this.compOpe = compOpe;
    }

    public int getPathType() {
        return pathType;
    }

    public void setPathType(int pathType) {
        this.pathType = pathType;
    }

    public boolean isNeedPathFr() {
        return isNeedPathFr;
    }

    public void setNeedPathFr(boolean needPathFr) {
        isNeedPathFr = needPathFr;
    }

    public boolean isNeedPathTo() {
        return isNeedPathTo;
    }

    public void setNeedPathTo(boolean needPathTo) {
        isNeedPathTo = needPathTo;
    }

    public boolean isList() {
        return isList;
    }

    public void setList(boolean list) {
        isList = list;
    }

    public boolean isReverse() {
        return isReverse;
    }

    public void setReverse(boolean reverse) {
        isReverse = reverse;
    }

    public boolean isSizeCheck() {
        return isSizeCheck;
    }

    public void setSizeCheck(boolean sizeCheck) {
        isSizeCheck = sizeCheck;
    }

    public boolean isSyncRmOnly() {
        return isSyncRmOnly;
    }

    public void setSyncRmOnly(boolean syncRmOnly) {
        isSyncRmOnly = syncRmOnly;
    }

    public boolean isFlat() {
        return isFlat;
    }

    public void setFlat(boolean flat) {
        isFlat = flat;
    }

    public boolean isDirTerm() {
        return isDirTerm;
    }

    public void setDirTerm(boolean dirTerm) {
        isDirTerm = dirTerm;
    }

    public boolean isAlwaysMkDir() {
        return isAlwaysMkDir;
    }

    public void setAlwaysMkDir(boolean alwaysMkDir) {
        isAlwaysMkDir = alwaysMkDir;
    }

    public boolean isFileCopy() {
        return isFileCopy;
    }

    public void setFileCopy(boolean fileCopy) {
        isFileCopy = fileCopy;
    }

    public int getCheckFileLock() {
        return checkFileLock;
    }

    public void setCheckFileLock(int checkFileLock) {
        this.checkFileLock = checkFileLock;
    }

    public boolean isSourceCheck() {
        return isSourceCheck;
    }

    public void setSourceCheck(boolean sourceCheck) {
        isSourceCheck = sourceCheck;
    }

    public boolean isFrPathCheck() {
        return isFrPathCheck;
    }

    public void setFrPathCheck(boolean frPathCheck) {
        isFrPathCheck = frPathCheck;
    }

    public boolean isRetFiles() {
        return isRetFiles;
    }

    public void setRetFiles(boolean retFiles) {
        isRetFiles = retFiles;
    }

    public long getSkipSize() {
        return skipSize;
    }

    public void setSkipSize(long skipSize) {
        this.skipSize = skipSize;
    }

    public long getCopySize() {
        return copySize;
    }

    public void setCopySize(long copySize) {
        this.copySize = copySize;
    }

    public long getCompSize() {
        return compSize;
    }

    public void setCompSize(long compSize) {
        this.compSize = compSize;
    }

    public int getInterval() {
        return interval;
    }

    public void setInterval(int interval) {
        this.interval = interval;
    }

    public int getMaxLoop() {
        return maxLoop;
    }

    public void setMaxLoop(int maxLoop) {
        this.maxLoop = maxLoop;
    }

    public boolean isBackup() {
        return isBackup;
    }

    public void setBackup(boolean backup) {
        isBackup = backup;
    }

    public boolean isErrorIfBackupFailed() {
        return isErrorIfBackupFailed;
    }

    public void setErrorIfBackupFailed(boolean errorIfBackupFailed) {
        isErrorIfBackupFailed = errorIfBackupFailed;
    }

    public String getBackupDir() {
        return backupDir;
    }

    public void setBackupDir(String backupDir) {
        this.backupDir = backupDir != null ? backupDir : "";
    }

    public int getFileShare() {
        return fileShare;
    }

    public void setFileShare(int fileShare) {
        this.fileShare = fileShare;
    }

    public int getWaitMSecForRetryCopy() {
        return waitMSecForRetryCopy;
    }

    public void setWaitMSecForRetryCopy(int waitMSecForRetryCopy) {
        this.waitMSecForRetryCopy = waitMSecForRetryCopy;
    }

    public int getRetrySystemCopyMax() {
        return retrySystemCopyMax;
    }

    public void setRetrySystemCopyMax(int retrySystemCopyMax) {
        this.retrySystemCopyMax = retrySystemCopyMax;
    }

    public double getSecRange() {
        return secRange;
    }

    public void setSecRange(double secRange) {
        this.secRange = secRange;
    }

    public int getVerbose() {
        return verbose;
    }

    public void setVerbose(int verbose) {
        this.verbose = verbose;
    }

    public int getShowCurDir() {
        return showCurDir;
    }

    public void setShowCurDir(int showCurDir) {
        this.showCurDir = showCurDir;
    }

    public int getOutputPathCode() {
        return outputPathCode;
    }

    public void setOutputPathCode(int outputPathCode) {
        this.outputPathCode = outputPathCode;
    }

    public int getProgressIntervalDirs() {
        return progressIntervalDirs;
    }

    public void setProgressIntervalDirs(int progressIntervalDirs) {
        this.progressIntervalDirs = progressIntervalDirs;
    }

    public int getProgressIntervalFiles() {
        return progressIntervalFiles;
    }

    public void setProgressIntervalFiles(int progressIntervalFiles) {
        this.progressIntervalFiles = progressIntervalFiles;
    }

    public boolean isRelative() {
        return isRelative;
    }

    public void setRelative(boolean relative) {
        isRelative = relative;
    }

    public boolean isProgress() {
        return isProgress;
    }

    public void setProgress(boolean progress) {
        isProgress = progress;
    }

    public boolean isStackTrace() {
        return isStackTrace;
    }

    public void setStackTrace(boolean stackTrace) {
        isStackTrace = stackTrace;
    }

    public boolean isShowNewFile() {
        return isShowNewFile;
    }

    public void setShowNewFile(boolean showNewFile) {
        isShowNewFile = showNewFile;
    }

    public boolean isShowUpdatedFile() {
        return isShowUpdatedFile;
    }

    public void setShowUpdatedFile(boolean showUpdatedFile) {
        isShowUpdatedFile = showUpdatedFile;
    }

    public boolean isShowSameFile() {
        return isShowSameFile;
    }

    public void setShowSameFile(boolean showSameFile) {
        isShowSameFile = showSameFile;
    }

    public String getOutputPathPrefix() {
        return outputPathPrefix;
    }

    public void setOutputPathPrefix(String outputPathPrefix) {
        this.outputPathPrefix = outputPathPrefix != null ? outputPathPrefix : "";
    }

    public boolean isShowPath() {
        return isShowPath;
    }

    public void setShowPath(boolean showPath) {
        isShowPath = showPath;
    }

    public boolean isShowSize() {
        return isShowSize;
    }

    public void setShowSize(boolean showSize) {
        isShowSize = showSize;
    }

    public boolean isShowDirNum() {
        return isShowDirNum;
    }

    public void setShowDirNum(boolean showDirNum) {
        isShowDirNum = showDirNum;
    }

    public boolean isShowFileNum() {
        return isShowFileNum;
    }

    public void setShowFileNum(boolean showFileNum) {
        isShowFileNum = showFileNum;
    }

    public boolean isShowPerm() {
        return isShowPerm;
    }

    public void setShowPerm(boolean showPerm) {
        isShowPerm = showPerm;
    }

    public boolean isShowOwner() {
        return isShowOwner;
    }

    public void setShowOwner(boolean showOwner) {
        isShowOwner = showOwner;
    }

    public boolean isSymLink() {
        return isSymLink;
    }

    public void setSymLink(boolean symLink) {
        isSymLink = symLink;
    }

    public int getOverwriteLevel() {
        return overwriteLevel;
    }

    public void setOverwriteLevel(int overwriteLevel) {
        this.overwriteLevel = overwriteLevel;
    }

    public int getMaxKeep() {
        return maxKeep;
    }

    public void setMaxKeep(int maxKeep) {
        this.maxKeep = maxKeep;
    }

    public String getCmdPath() {
        return cmdPath;
    }

    public void setCmdPath(String cmdPath) {
        this.cmdPath = cmdPath != null ? cmdPath : "";
    }

    public String getCmdArgs() {
        return cmdArgs;
    }

    public void setCmdArgs(String cmdArgs) {
        this.cmdArgs = cmdArgs != null ? cmdArgs : "";
    }

    public boolean isDq() {
        return isDq;
    }

    public void setDq(boolean dq) {
        isDq = dq;
    }

    public int getWarnThreshold() {
        return warnThreshold;
    }

    public void setWarnThreshold(int warnThreshold) {
        this.warnThreshold = warnThreshold;
    }

    public int getErrorThreshold() {
        return errorThreshold;
    }

    public void setErrorThreshold(int errorThreshold) {
        this.errorThreshold = errorThreshold;
    }

    public boolean isExecCmd() {
        return isExecCmd;
    }

    public void setExecCmd(boolean execCmd) {
        isExecCmd = execCmd;
    }

    public int getExecModeCode() {
        return execModeCode;
    }

    public void setExecModeCode(int execModeCode) {
        this.execModeCode = execModeCode;
    }

    public int getPriority() {
        return priority;
    }

    public void setPriority(int priority) {
        this.priority = priority;
    }

    public int getTimeout() {
        return timeout;
    }

    public void setTimeout(int timeout) {
        this.timeout = timeout;
    }

    public boolean isErrorAtNegativeValue() {
        return isErrorAtNegativeValue;
    }

    public void setErrorAtNegativeValue(boolean errorAtNegativeValue) {
        isErrorAtNegativeValue = errorAtNegativeValue;
    }

    public boolean isAlwaysNormal() {
        return isAlwaysNormal;
    }

    public void setAlwaysNormal(boolean alwaysNormal) {
        isAlwaysNormal = alwaysNormal;
    }

    public boolean isShowCmd() {
        return isShowCmd;
    }

    public void setShowCmd(boolean showCmd) {
        isShowCmd = showCmd;
    }

    public boolean isShowOutput() {
        return isShowOutput;
    }

    public void setShowOutput(boolean showOutput) {
        isShowOutput = showOutput;
    }

    public boolean isShowExitCode() {
        return isShowExitCode;
    }

    public void setShowExitCode(boolean showExitCode) {
        isShowExitCode = showExitCode;
    }

    public boolean isCat() {
        return isCat;
    }

    public void setCat(boolean cat) {
        isCat = cat;
    }

    public boolean isCatRetWcl() {
        return isCatRetWcl;
    }

    public void setCatRetWcl(boolean catRetWcl) {
        isCatRetWcl = catRetWcl;
    }

    public String getCatI() {
        return catI;
    }

    public void setCatI(String catI) {
        this.catI = catI != null ? catI : "";
    }

    public String getCatX() {
        return catX;
    }

    public void setCatX(String catX) {
        this.catX = catX != null ? catX : "";
    }

    public String getCatP() {
        return catP;
    }

    public void setCatP(String catP) {
        this.catP = catP != null ? catP : "";
    }

    public String getCatE() {
        return catE;
    }

    public void setCatE(String catE) {
        this.catE = catE != null ? catE : "";
    }

    public String getCatXmlNl() {
        return catXmlNl;
    }

    public void setCatXmlNl(String catXmlNl) {
        this.catXmlNl = catXmlNl != null ? catXmlNl : "";
    }

    public String getCatOptions() {
        return catOptions;
    }

    public void setCatOptions(String catOptions) {
        this.catOptions = catOptions != null ? catOptions : "";
    }

    public long getFiles() {
        return files;
    }

    public void setFiles(long files) {
        this.files = files;
    }

    public long getLines() {
        return lines;
    }

    public void setLines(long lines) {
        this.lines = lines;
    }

    public int getTypeCode() {
        return typeCode;
    }

    public void setTypeCode(int typeCode) {
        this.typeCode = typeCode;
    }

    public long getMaxDepth() {
        return maxDepth;
    }

    public void setMaxDepth(long maxDepth) {
        this.maxDepth = maxDepth;
    }

    public long getMinDepth() {
        return minDepth;
    }

    public void setMinDepth(long minDepth) {
        this.minDepth = minDepth;
    }

    public boolean isBefore() {
        return isBefore;
    }

    public void setBefore(boolean before) {
        isBefore = before;
    }

    public boolean isAfter() {
        return isAfter;
    }

    public void setAfter(boolean after) {
        isAfter = after;
    }

    public LocalDateTime getBeforeTime() {
        return beforeTime;
    }

    public void setBeforeTime(LocalDateTime beforeTime) {
        this.beforeTime = beforeTime;
    }

    public LocalDateTime getAfterTime() {
        return afterTime;
    }

    public void setAfterTime(LocalDateTime afterTime) {
        this.afterTime = afterTime;
    }

    public boolean isRegIncBasename() {
        return isRegIncBasename;
    }

    public void setRegIncBasename(boolean regIncBasename) {
        isRegIncBasename = regIncBasename;
    }

    public boolean isRegExcBasename() {
        return isRegExcBasename;
    }

    public void setRegExcBasename(boolean regExcBasename) {
        isRegExcBasename = regExcBasename;
    }

    public boolean isIncHitRecursive() {
        return isIncHitRecursive;
    }

    public void setIncHitRecursive(boolean incHitRecursive) {
        isIncHitRecursive = incHitRecursive;
    }

    public boolean isExcHitRecursive() {
        return isExcHitRecursive;
    }

    public void setExcHitRecursive(boolean excHitRecursive) {
        isExcHitRecursive = excHitRecursive;
    }

    public boolean isDirFilterOr() {
        return isDirFilterOr;
    }

    public void setDirFilterOr(boolean dirFilterOr) {
        isDirFilterOr = dirFilterOr;
    }

    public boolean isXdOnlyFiles() {
        return isXdOnlyFiles;
    }

    public void setXdOnlyFiles(boolean xdOnlyFiles) {
        isXdOnlyFiles = xdOnlyFiles;
    }

    public boolean isRmNohit() {
        return isRmNohit;
    }

    public void setRmNohit(boolean rmNohit) {
        isRmNohit = rmNohit;
    }

    public List<String> getIncFilesList() {
        return incFilesList;
    }

    public void setIncFilesList(List<String> incFilesList) {
        this.incFilesList = incFilesList != null ? incFilesList : new ArrayList<>();
    }

    public List<String> getExcFilesList() {
        return excFilesList;
    }

    public void setExcFilesList(List<String> excFilesList) {
        this.excFilesList = excFilesList != null ? excFilesList : new ArrayList<>();
    }

    public List<String> getIncDirsList() {
        return incDirsList;
    }

    public void setIncDirsList(List<String> incDirsList) {
        this.incDirsList = incDirsList != null ? incDirsList : new ArrayList<>();
    }

    public List<String> getExcDirsList() {
        return excDirsList;
    }

    public void setExcDirsList(List<String> excDirsList) {
        this.excDirsList = excDirsList != null ? excDirsList : new ArrayList<>();
    }

    public int getFilesTypeCode() {
        return filesTypeCode;
    }

    public void setFilesTypeCode(int filesTypeCode) {
        this.filesTypeCode = filesTypeCode;
    }

    public String getFileListPath() {
        return fileListPath;
    }

    public void setFileListPath(String fileListPath) {
        this.fileListPath = fileListPath != null ? fileListPath : "";
    }

    public String getFileListType() {
        return fileListType;
    }

    public void setFileListType(String fileListType) {
        this.fileListType = fileListType != null ? fileListType : "";
    }

    public String getFileListRegex() {
        return fileListRegex;
    }

    public void setFileListRegex(String fileListRegex) {
        this.fileListRegex = fileListRegex != null ? fileListRegex : "";
    }

    public List<String> getFileList() {
        return fileList;
    }

    public void setFileList(List<String> fileList) {
        this.fileList = fileList != null ? fileList : new ArrayList<>();
    }

    public int getCpTimestamp() {
        return cpTimestamp;
    }

    public void setCpTimestamp(int cpTimestamp) {
        this.cpTimestamp = cpTimestamp;
    }

    public String getTsSource() {
        return tsSource;
    }

    public void setTsSource(String tsSource) {
        this.tsSource = tsSource != null ? tsSource : "";
    }

    public String getTsDestination() {
        return tsDestination;
    }

    public void setTsDestination(String tsDestination) {
        this.tsDestination = tsDestination != null ? tsDestination : "";
    }

    public String getTsBackup() {
        return tsBackup;
    }

    public void setTsBackup(String tsBackup) {
        this.tsBackup = tsBackup != null ? tsBackup : "";
    }

    public int getSortType() {
        return sortType;
    }

    public void setSortType(int sortType) {
        this.sortType = sortType;
    }

    public boolean isAscending() {
        return isAscending;
    }

    public void setAscending(boolean ascending) {
        isAscending = ascending;
    }

    public boolean isShowDirList() {
        return isShowDirList;
    }

    public void setShowDirList(boolean showDirList) {
        isShowDirList = showDirList;
    }

    public boolean isShowFileList() {
        return isShowFileList;
    }

    public void setShowFileList(boolean showFileList) {
        isShowFileList = showFileList;
    }

    public String getOutputModeStr(int mode) {
        switch (mode) {
            case FROM:
                return "fr";
            case TO:
                return "to";
            case BOTH:
                return "both";
            default:
                return "rel";
        }
    }

    public int getOutputModeCode(String mode) {
        if (mode == null || mode.isBlank()) {
            return RELATIVE;
        }
        switch (mode.toLowerCase(Locale.ROOT)) {
            case "from":
            case "fr":
            case "f":
            case "1":
                return FROM;
            case "to":
            case "t":
            case "2":
                return TO;
            case "both":
            case "b":
            case "3":
                return BOTH;
            default:
                return RELATIVE;
        }
    }

    public String getCheckLockFileModeStr(int mode) {
        switch (mode) {
            case CHECK_FILE_LOCK_NONE:
                return "false";
            case CHECK_FILE_LOCK_SAMPLE:
                return "sample";
            default:
                return "skip";
        }
    }

    public String getExecModeStr(int mode) {
        switch (mode) {
            case EXEC_MODE_CMD:
                return "cmd";
            case EXEC_MODE_PS:
                return "ps";
            case EXEC_MODE_PSC:
                return "psc";
            case EXEC_MODE_EXE:
                return "exe";
            default:
                return "normal";
        }
    }
}
