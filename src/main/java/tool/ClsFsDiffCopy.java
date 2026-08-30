package tool;

import java.io.File;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.attribute.FileTime;
import java.time.Instant;
import java.time.LocalDateTime;
import java.time.ZoneId;
import tool.cmnclslib.cls.ClsAdler32;
import tool.cmnclslib.cls.ClsCksum;
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlDate;
import tool.cmnclslib.mdl.MdlFile;
import tool.cmnclslib.mdl.MdlUtil;

/**
 * 差分コピー、同期、ディレクトリ作成、バックアップ処理を行うクラスです。
 */
public class ClsFsDiffCopy {

    private final ClsLogger logger;
    private ClsBaseDir prop;
    private final ClsFsUtil fsUtil;
    private final ClsSymLinkWrapper symLink;

    private long copyNewCount = 0;
    private long copyUpdateCount = 0;
    private long copySkipCount = 0;
    private long copyErrorCount = 0;
    private long copyTotalCount = 0;
    private long rmOkCount = 0;
    private long rmNgCount = 0;
    private long rmSkipCount = 0;
    private long rmTotalCount = 0;
    private long mkdirOkCount = 0;
    private long mkdirNgCount = 0;
    private long notFoundCount = 0;

    /**
     * ロガー、設定プロパティ、ファイルシステムユーティリティ、シンボリックリンクラッパーを指定して {@link ClsFsDiffCopy} の新しいインスタンスを初期化します。
     *
     * @param logger ログ出力に使用するロガーインスタンス
     * @param prop アプリケーション設定プロパティ
     * @param fsUtil ファイルシステムユーティリティ
     * @param symLink シンボリックリンクラッパー
     */
    public ClsFsDiffCopy(ClsLogger logger, ClsBaseDir prop, ClsFsUtil fsUtil, ClsSymLinkWrapper symLink) {
        this.logger = logger != null ? logger : new ClsLogger();
        this.prop = prop != null ? prop : new ClsBaseDir();
        this.fsUtil = fsUtil != null ? fsUtil : new ClsFsUtil(this.logger);
        this.symLink = symLink != null ? symLink : new ClsSymLinkWrapper(this.logger);
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

    public long getCopyNewCount() {
        return copyNewCount;
    }

    public void setCopyNewCount(long copyNewCount) {
        this.copyNewCount = copyNewCount;
    }

    public long getCopyUpdateCount() {
        return copyUpdateCount;
    }

    public void setCopyUpdateCount(long copyUpdateCount) {
        this.copyUpdateCount = copyUpdateCount;
    }

    public long getCopySkipCount() {
        return copySkipCount;
    }

    public void setCopySkipCount(long copySkipCount) {
        this.copySkipCount = copySkipCount;
    }

    public long getCopyErrorCount() {
        return copyErrorCount;
    }

    public void setCopyErrorCount(long copyErrorCount) {
        this.copyErrorCount = copyErrorCount;
    }

    public long getCopyTotalCount() {
        return copyTotalCount;
    }

    public void setCopyTotalCount(long copyTotalCount) {
        this.copyTotalCount = copyTotalCount;
    }

    public long getRmOkCount() {
        return rmOkCount;
    }

    public void setRmOkCount(long rmOkCount) {
        this.rmOkCount = rmOkCount;
    }

    public long getRmNgCount() {
        return rmNgCount;
    }

    public void setRmNgCount(long rmNgCount) {
        this.rmNgCount = rmNgCount;
    }

    public long getRmSkipCount() {
        return rmSkipCount;
    }

    public void setRmSkipCount(long rmSkipCount) {
        this.rmSkipCount = rmSkipCount;
    }

    public long getRmTotalCount() {
        return rmTotalCount;
    }

    public void setRmTotalCount(long rmTotalCount) {
        this.rmTotalCount = rmTotalCount;
    }

    public long getMkdirOkCount() {
        return mkdirOkCount;
    }

    public void setMkdirOkCount(long mkdirOkCount) {
        this.mkdirOkCount = mkdirOkCount;
    }

    public long getMkdirNgCount() {
        return mkdirNgCount;
    }

    public void setMkdirNgCount(long mkdirNgCount) {
        this.mkdirNgCount = mkdirNgCount;
    }

    public long getNotFoundCount() {
        return notFoundCount;
    }

    public void setNotFoundCount(long notFoundCount) {
        this.notFoundCount = notFoundCount;
    }

    private static LocalDateTime toLdt(long epochMillis) {
        if (epochMillis <= 0) {
            return LocalDateTime.now();
        }
        return LocalDateTime.ofInstant(Instant.ofEpochMilli(epochMillis), ZoneId.systemDefault());
    }

    private static String getStackTraceString(Throwable t) {
        if (t == null) {
            return "";
        }
        StringWriter sw = new StringWriter();
        PrintWriter pw = new PrintWriter(sw);
        t.printStackTrace(pw);
        return sw.toString();
    }

    private static void setFileTime(String path, long millis) {
        try {
            Path p = Path.of(path);
            if (Files.exists(p)) {
                Files.setLastModifiedTime(p, FileTime.fromMillis(millis));
            }
        } catch (Exception ignored) {
        }
    }

    /**
     * 指定されたパス種別（ファイルまたはディレクトリ）に応じた差分コピーまたはシンボリックリンク作成を実行します。
     *
     * @param sourcePath コピー元パス
     * @param destinationPath コピー先パス
     * @param relativePath 相対パス
     * @param pathType パス種別（PATH_IS_DIRECTORY, PATH_IS_FILE 等）
     * @param isSymLink シンボリックリンク判定（-1: 自動判定, 0: 通常, 1: シンボリックリンク）
     * @return 処理が成功した場合は true
     */
    public boolean copy(String sourcePath, String destinationPath, String relativePath, int pathType, int isSymLink) {
        boolean isOk = true;
        boolean isSymLinkFlag = false;
        if (prop.isSymLink()) {
            isSymLinkFlag = isSymLink == -1 ? Files.isSymbolicLink(Path.of(sourcePath)) : (isSymLink != 0);
        }
        switch (pathType) {
            case MdlFile.PATH_IS_DIRECTORY:
                if (prop.isAlwaysMkDir()) {
                    if (isSymLinkFlag) {
                        copyTotalCount++;
                        if (prop.getVerbose() > 4) {
                            logger.writeLine(MdlConst.LVL_NONE, "[TRY] MkLink(" + sourcePath + ", " + destinationPath + ", " + relativePath + ", MdlFile.PATH_IS_DIRECTORY)");
                        }
                        isOk = mkLink(sourcePath, destinationPath, relativePath, MdlFile.PATH_IS_DIRECTORY);
                    } else {
                        if (prop.getVerbose() > 5) {
                            logger.writeLine(MdlConst.LVL_NONE, "[TRY] Mkdir(" + sourcePath + ", " + destinationPath + ", " + relativePath + ", MdlFile.PATH_IS_DIRECTORY)");
                        }
                        isOk = mkdir(sourcePath, destinationPath, relativePath);
                    }
                }
                break;
            case MdlFile.PATH_IS_FILE:
                if (prop.isFileCopy()) {
                    copyTotalCount++;
                    if (isSymLinkFlag) {
                        if (prop.getVerbose() > 4) {
                            logger.writeLine(MdlConst.LVL_NONE, "[TRY] MkLink(" + sourcePath + ", " + destinationPath + ", " + relativePath + ", MdlFile.PATH_IS_FILE)");
                        }
                        isOk = mkLink(sourcePath, destinationPath, relativePath, MdlFile.PATH_IS_FILE);
                    } else {
                        if (prop.getVerbose() > 5) {
                            logger.writeLine(MdlConst.LVL_NONE, "[TRY] DiffCopyFileMain(" + sourcePath + ", " + destinationPath + ", " + relativePath + ", MdlFile.PATH_IS_FILE)");
                        }
                        isOk = diffCopyFileMain(sourcePath, destinationPath, relativePath);
                    }
                }
                break;
            default:
                break;
        }
        return isOk;
    }

    /**
     * コピー先ディレクトリを作成し、タイムスタンプ同期を行います。
     *
     * @param sourcePath コピー元パス
     * @param destinationPath コピー先パス
     * @param relativePath 相対パス
     * @return 作成成功時は true
     */
    public boolean mkdir(String sourcePath, String destinationPath, String relativePath) {
        boolean isOk = true;
        if (!prop.isList()) {
            switch (MdlFile.createDirectory(destinationPath)) {
                case MdlFile.OK_MKDIR_ALREADY_EXIST:
                    setDateToDir(sourcePath, destinationPath, relativePath, "---");
                    break;
                case MdlFile.OK_MKDIR_CREATE:
                    setDateToDir(sourcePath, destinationPath, relativePath, "NEW");
                    mkdirOkCount++;
                    break;
                default:
                    isOk = false;
                    mkdirNgCount++;
                    logger.writeLine(MdlConst.LVL_NONE, "NG : FAILED TO MKDIR : " + destinationPath);
                    break;
            }
        }
        return isOk;
    }

    public boolean mkLink(String sourcePath, String destinationPath, String relativePath, int pathType) {
        boolean isOk = true;
        boolean isExistTo = false;
        boolean isNew = false;
        boolean isUpdate = false;
        LocalDateTime srcTime = LocalDateTime.now();
        LocalDateTime dstTime = LocalDateTime.now();

        symLink.setVerbose(0);
        String realPath = symLink.getRealPath(sourcePath, prop.isRelative());
        symLink.setVerbose(prop.getVerbose());
        if (realPath.isEmpty()) {
            realPath = "FAILED TO GET REALPATH";
        }

        String outputPath;
        switch (prop.getOutputPathCode()) {
            case ClsBaseDir.FROM:
                outputPath = sourcePath + " [" + realPath + "]";
                break;
            case ClsBaseDir.TO:
                outputPath = destinationPath + " [" + realPath + "]";
                break;
            case ClsBaseDir.BOTH:
                outputPath = sourcePath + " => " + destinationPath + " [" + realPath + "]";
                break;
            default:
                outputPath = getOutputRelativePath(relativePath) + " [" + realPath + "]";
                break;
        }

        File srcFile = new File(sourcePath);
        if (srcFile.exists()) {
            srcTime = toLdt(srcFile.lastModified());
        }
        String srcLastWriteTimeStr = MdlDate.getFormattedDate(srcTime, "yyyy/MM/dd HH:mm:ss");

        switch (MdlFile.getPathType(destinationPath)) {
            case MdlFile.PATH_IS_DIRECTORY:
                if (MdlFile.PATH_IS_DIRECTORY != pathType) {
                    isUpdate = true;
                }
                if (!Files.isSymbolicLink(Path.of(destinationPath))) {
                    isUpdate = true;
                }
                isExistTo = true;
                File dstDir = new File(destinationPath);
                dstTime = toLdt(dstDir.lastModified());
                break;
            case MdlFile.PATH_IS_FILE:
                if (MdlFile.PATH_IS_FILE != pathType) {
                    isUpdate = true;
                }
                if (!Files.isSymbolicLink(Path.of(destinationPath))) {
                    isUpdate = true;
                }
                isExistTo = true;
                File dstF = new File(destinationPath);
                dstTime = toLdt(dstF.lastModified());
                break;
            default:
                isNew = true;
                break;
        }
        String dstLastWriteTimeStr = MdlDate.getFormattedDate(dstTime, "yyyy/MM/dd HH:mm:ss");

        if (isExistTo) {
            if (MdlFile.PATH_IS_FILE == pathType) {
                switch (prop.getCheckLogic()) {
                    case ClsBaseDir.CHECK_MTIME:
                        if (0 != MdlDate.compareDateTime(srcTime, dstTime, prop.getSecRange())) {
                            isUpdate = true;
                        }
                        break;
                    case ClsBaseDir.CHECK_MTIME_NEW:
                        if (MdlDate.compareDateTime(srcTime, dstTime, prop.getSecRange()) > 0) {
                            isUpdate = true;
                        }
                        break;
                    case ClsBaseDir.CHECK_MTIME_OLD:
                        if (MdlDate.compareDateTime(dstTime, srcTime, prop.getSecRange()) > 0) {
                            isUpdate = true;
                        }
                        break;
                    default:
                        break;
                }
            }
            if (prop.getOverwriteLevel() > 0) {
                symLink.setVerbose(0);
                String realPathTo = symLink.getRealPath(destinationPath, prop.isRelative());
                if (!realPath.isEmpty() && !realPathTo.isEmpty()) {
                    isUpdate = !realPath.equals(realPathTo);
                }
                symLink.setVerbose(prop.getVerbose());
            }
            if (prop.getOverwriteLevel() > 1) {
                isUpdate = true;
            }
        }

        if (isNew) {
            if (prop.isShowNewFile()) {
                if (prop.getVerbose() >= 0) {
                    String action = prop.isList() ? "-N-" : "NEW";
                    echoTitle("[" + action + "][" + srcLastWriteTimeStr + "] " + outputPath);
                } else if (prop.getVerbose() == -1) {
                    echoTitle("[C P] " + outputPath);
                } else {
                    echoTitle(outputPath);
                }
            }
        } else if (isUpdate) {
            if (prop.isShowUpdatedFile()) {
                if (prop.getVerbose() >= 0) {
                    String action = prop.isList() ? "-U-" : "UPD";
                    echoTitle("[" + action + "][" + dstLastWriteTimeStr + "=>" + srcLastWriteTimeStr + "] " + outputPath);
                } else if (prop.getVerbose() == -1) {
                    echoTitle("[C P] " + outputPath);
                } else {
                    echoTitle(outputPath);
                }
            }
        } else {
            if (prop.isShowSameFile()) {
                if (prop.getVerbose() >= 0) {
                    echoTitle("[---][" + srcLastWriteTimeStr + "] " + outputPath);
                } else if (prop.getVerbose() == -1) {
                    echoTitle("[C P] " + outputPath);
                } else {
                    echoTitle(outputPath);
                }
            }
        }

        if (!prop.isList()) {
            if (isNew || isUpdate) {
                if (isNew) {
                    mkParentDir(destinationPath, true);
                }
                if (symLink.copy(sourcePath, destinationPath, isUpdate, prop.isRelative())) {
                    boolean isExistResult = Files.exists(Path.of(destinationPath));
                    switch (pathType) {
                        case MdlFile.PATH_IS_DIRECTORY:
                            if (isExistResult) {
                                setDateToDir(sourcePath, destinationPath, relativePath, "");
                                if (isNew) {
                                    copyNewCount++;
                                }
                                if (isUpdate) {
                                    copyUpdateCount++;
                                }
                            } else {
                                isOk = false;
                                logger.writeLine(MdlConst.LVL_NONE, " -> NG : FAILED TO MKLINK : " + relativePath + symLink.getMessage());
                            }
                            break;
                        case MdlFile.PATH_IS_FILE:
                            if (isExistResult) {
                                setDateToFile(sourcePath, destinationPath, relativePath, "");
                                if (isNew) {
                                    copyNewCount++;
                                }
                                if (isUpdate) {
                                    copyUpdateCount++;
                                }
                            } else {
                                isOk = false;
                                logger.writeLine(MdlConst.LVL_NONE, " -> NG : FAILED TO MKLINK : " + relativePath + symLink.getMessage());
                            }
                            break;
                        default:
                            break;
                    }
                } else {
                    isOk = false;
                    logger.writeLine(MdlConst.LVL_NONE, " -> NG : FAILED TO MKLINK : " + relativePath + symLink.getMessage());
                }
            } else {
                copySkipCount++;
            }
        } else {
            if (isNew) {
                copyNewCount++;
            }
            if (isUpdate) {
                copyUpdateCount++;
            }
        }
        if (!isOk) {
            copyErrorCount++;
        }
        return isOk;
    }

    public boolean diffCopyFileMain(String sourceFilePath, String destFilePath, String relativePath) {
        if (prop.isProgress() && fsUtil == null) {
            logger.writeLine(MdlConst.LVL_E, "[ClsFsDiffCopy.DiffCopyFileMain()] null == _objFile");
            copyErrorCount++;
            return false;
        }
        boolean isOk = true;
        boolean isCopy = false;
        boolean isNew = false;
        boolean isShowCksum = false;
        String srcCheckStr = "";
        String dstCheckStr = "";

        File srcFileInfo = new File(sourceFilePath);
        File dstFileInfo = new File(destFilePath);

        LocalDateTime srcLdt = toLdt(srcFileInfo.lastModified());
        String srcLastWriteTimeStr = MdlDate.getFormattedDate(srcLdt, "yyyy/MM/dd HH:mm:ss");
        String dstLastWriteTimeStr = "";

        String outputPath;
        switch (prop.getOutputPathCode()) {
            case ClsBaseDir.FROM:
                outputPath = sourceFilePath;
                break;
            case ClsBaseDir.TO:
                outputPath = destFilePath;
                break;
            case ClsBaseDir.BOTH:
                outputPath = sourceFilePath + " => " + destFilePath;
                break;
            default:
                outputPath = getOutputRelativePath(relativePath);
                break;
        }

        switch (MdlFile.getPathType(destFilePath)) {
            case MdlFile.PATH_IS_DIRECTORY:
                removeRecursive(destFilePath, relativePath, Files.isSymbolicLink(Path.of(destFilePath)));
                isCopy = true;
                isNew = true;
                break;
            case MdlFile.PATH_IS_FILE:
                boolean isCheckUpdates = false;
                LocalDateTime dstLdt = toLdt(dstFileInfo.lastModified());
                dstLastWriteTimeStr = MdlDate.getFormattedDate(dstLdt, "yyyy/MM/dd HH:mm:ss");

                if (ClsBaseDir.CHECK_NONE == prop.getCheckLogic()) {
                    isCopy = true;
                } else if (ClsBaseDir.CHECK_EXIST == prop.getCheckLogic()) {
                    isCopy = false;
                } else {
                    if (prop.isSizeCheck()) {
                        if (dstFileInfo.exists() && srcFileInfo.length() == dstFileInfo.length()) {
                            isCheckUpdates = true;
                        } else {
                            isCopy = true;
                            if (prop.getVerbose() > 1) {
                                srcCheckStr = "size:" + srcFileInfo.length();
                                dstCheckStr = "size:" + (dstFileInfo.exists() ? dstFileInfo.length() : 0);
                                isShowCksum = true;
                            }
                        }
                    } else {
                        isCheckUpdates = true;
                    }

                    if (isCheckUpdates) {
                        switch (prop.getCheckLogic()) {
                            case ClsBaseDir.CHECK_ADLER32:
                                switch (checkIsSkipBySize(srcFileInfo.length())) {
                                    case 0:
                                        ClsAdler32 adler32 = new ClsAdler32();
                                        srcCheckStr = "adler:" + adler32.computeChecksum(sourceFilePath);
                                        if (prop.getVerbose() > 6) {
                                            logger.writeLine(MdlConst.LVL_NONE, "[ADLER32] " + srcCheckStr + " : " + sourceFilePath);
                                        }
                                        dstCheckStr = "adler:" + adler32.computeChecksum(destFilePath);
                                        if (prop.getVerbose() > 6) {
                                            logger.writeLine(MdlConst.LVL_NONE, "[ADLER32] " + dstCheckStr + " : " + destFilePath);
                                        }
                                        if (!srcCheckStr.equals(dstCheckStr)) {
                                            isCopy = true;
                                        }
                                        if (prop.getVerbose() > 1) {
                                            isShowCksum = true;
                                        }
                                        break;
                                    case 1:
                                        isCopy = true;
                                        if (prop.getVerbose() > 1) {
                                            srcCheckStr = "filesize:" + String.format("%.0f", (double) srcFileInfo.length() / 1024 / 1024) + "MB";
                                            dstCheckStr = "copysize:" + String.format("%.0f", (double) prop.getCopySize() / 1024 / 1024) + "MB";
                                            isShowCksum = true;
                                        }
                                        break;
                                    case 10:
                                        if (prop.getVerbose() > 1) {
                                            srcCheckStr = "skipsize:" + String.format("%.0f", (double) prop.getSkipSize() / 1024 / 1024) + "MB=>filesize:" + String.format("%.0f", (double) srcFileInfo.length() / 1024 / 1024) + "MB";
                                            dstCheckStr = "";
                                            isShowCksum = true;
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case ClsBaseDir.CHECK_CKSUM:
                                switch (checkIsSkipBySize(srcFileInfo.length())) {
                                    case 0:
                                        isCopy = false;
                                        ClsCksum cksum = new ClsCksum();
                                        srcCheckStr = "cksum:" + cksum.getChecksum(sourceFilePath);
                                        dstCheckStr = "cksum:" + cksum.getChecksum(destFilePath);
                                        if (!srcCheckStr.equals(dstCheckStr)) {
                                            isCopy = true;
                                        }
                                        if (prop.getVerbose() > 1) {
                                            isShowCksum = true;
                                        }
                                        break;
                                    case 1:
                                        isCopy = true;
                                        if (prop.getVerbose() > 1) {
                                            srcCheckStr = "filesize:" + String.format("%.0f", (double) srcFileInfo.length() / 1024 / 1024) + "MB";
                                            dstCheckStr = "copysize:" + String.format("%.0f", (double) prop.getCopySize() / 1024 / 1024) + "MB";
                                            isShowCksum = true;
                                        }
                                        break;
                                    case 10:
                                        if (prop.getVerbose() > 1) {
                                            srcCheckStr = "skipsize:" + String.format("%.0f", (double) prop.getSkipSize() / 1024 / 1024) + "MB=>filesize:" + String.format("%.0f", (double) srcFileInfo.length() / 1024 / 1024) + "MB";
                                            dstCheckStr = "";
                                            isShowCksum = true;
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case ClsBaseDir.CHECK_SHA1:
                                switch (checkIsSkipBySize(srcFileInfo.length())) {
                                    case 0:
                                        srcCheckStr = "sha1:" + fsUtil.computeSha1Hash(sourceFilePath);
                                        dstCheckStr = "sha1:" + fsUtil.computeSha1Hash(destFilePath);
                                        if (!srcCheckStr.equals(dstCheckStr)) {
                                            isCopy = true;
                                        }
                                        if (prop.getVerbose() > 1) {
                                            isShowCksum = true;
                                        }
                                        break;
                                    case 1:
                                        isCopy = true;
                                        if (prop.getVerbose() > 1) {
                                            srcCheckStr = "filesize:" + String.format("%.0f", (double) srcFileInfo.length() / 1024 / 1024) + "MB";
                                            dstCheckStr = "copysize:" + String.format("%.0f", (double) prop.getCopySize() / 1024 / 1024) + "MB";
                                            isShowCksum = true;
                                        }
                                        break;
                                    case 10:
                                        if (prop.getVerbose() > 1) {
                                            srcCheckStr = "skipsize:" + String.format("%.0f", (double) prop.getSkipSize() / 1024 / 1024) + "MB=>filesize:" + String.format("%.0f", (double) srcFileInfo.length() / 1024 / 1024) + "MB";
                                            dstCheckStr = "";
                                            isShowCksum = true;
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case ClsBaseDir.CHECK_MTIME:
                                if (0 != MdlDate.compareDateTime(srcLdt, dstLdt, prop.getSecRange())) {
                                    isCopy = true;
                                }
                                break;
                            case ClsBaseDir.CHECK_MTIME_NEW:
                                if (MdlDate.compareDateTime(srcLdt, dstLdt, prop.getSecRange()) > 0) {
                                    isCopy = true;
                                }
                                break;
                            case ClsBaseDir.CHECK_MTIME_OLD:
                                if (MdlDate.compareDateTime(dstLdt, srcLdt, prop.getSecRange()) > 0) {
                                    isCopy = true;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                break;
            default:
                isCopy = true;
                isNew = true;
                break;
        }

        if (isCopy) {
            if (prop.getVerbose() > 5) {
                logger.writeLine(MdlConst.LVL_NONE, "---[DEBUG]--------------------------------------------------");
                logger.writeLine(MdlConst.LVL_NONE, "DiffCopyFileMain(" + sourceFilePath + ", " + destFilePath + ", " + relativePath + ")");
                logger.writeLine(MdlConst.LVL_NONE, "isCopy = " + isCopy + " / isNew = " + isNew);
                logger.writeLine(MdlConst.LVL_NONE, "MdlFile.GetPathType(" + destFilePath + ") = " + MdlFile.getPathType(destFilePath));
                if (dstFileInfo.exists()) {
                    logger.writeLine(MdlConst.LVL_NONE, "[dstFileInfo] Exists=" + dstFileInfo.exists() + " / LastWriteTime = " + dstFileInfo.lastModified() + " / Length = " + dstFileInfo.length());
                }
                logger.writeLine(MdlConst.LVL_NONE, "------------------------------------------------------------");
            }
            if (isNew) {
                copyNewCount++;
                if (prop.isShowNewFile()) {
                    if (prop.getVerbose() >= 0) {
                        String action = prop.isList() ? "-N-" : "NEW";
                        echoTitle("[" + action + "][" + srcLastWriteTimeStr + "] " + outputPath);
                    } else if (prop.getVerbose() == -1) {
                        echoTitle("[C P] " + outputPath);
                    } else {
                        echoTitle(outputPath);
                    }
                }
            } else {
                copyUpdateCount++;
                if (prop.isShowUpdatedFile()) {
                    if (prop.getVerbose() >= 0) {
                        String action = prop.isList() ? "-U-" : "UPD";
                        if (isShowCksum) {
                            echoTitle("[" + action + "][" + dstLastWriteTimeStr + "=>" + srcLastWriteTimeStr + "][" + dstCheckStr + "=>" + srcCheckStr + "] " + outputPath);
                        } else {
                            echoTitle("[" + action + "][" + dstLastWriteTimeStr + "=>" + srcLastWriteTimeStr + "] " + outputPath);
                        }
                    } else if (prop.getVerbose() == -1) {
                        echoTitle("[C P] " + outputPath);
                    } else {
                        echoTitle(outputPath);
                    }
                }
            }
            isOk = copyFile(sourceFilePath, destFilePath, relativePath, isNew);
        } else {
            copySkipCount++;
            String modeStr = prop.isShowSameFile() ? "---" : "";
            setDateToFile(srcFileInfo, dstFileInfo, relativePath, modeStr, isShowCksum, srcCheckStr);
        }
        return isOk;
    }

    public int checkIsSkipBySize(long fileSize) {
        boolean isSkip = prop.getSkipSize() > 0 && fileSize > prop.getSkipSize();
        boolean isCopy = prop.getCopySize() > 0 && fileSize > prop.getCopySize();
        int result = 0;
        if (isSkip) {
            result = 10;
        }
        if (isCopy) {
            result = 1;
        }
        if (isSkip && isCopy) {
            result = 10;
        }
        return result;
    }

    public String getOutputRelativePath(String relativePath) {
        return (prop.getOutputPathPrefix() == null || prop.getOutputPathPrefix().isBlank())
                ? relativePath
                : prop.getOutputPathPrefix() + relativePath;
    }

    public void echoTitle(String msg) {
        String effectiveMsg = msg;
        switch (prop.getTask()) {
            case ClsBaseDir.TASK_CP:
            case ClsBaseDir.TASK_MV:
                if (prop.isProgress()) {
                    if (fsUtil != null && !fsUtil.getResult().isEmpty()) {
                        int progressSize = 86;
                        int length = MdlUtil.getShiftJisByteCount(fsUtil.getResult());
                        if (length < progressSize) {
                            length = progressSize;
                        }
                        if (MdlUtil.getShiftJisByteCount(effectiveMsg) < length) {
                            effectiveMsg = String.format("%-" + length + "s", effectiveMsg);
                            logger.setValueByKey(ClsLogger.IS_TRIM_CONSOLE, "false");
                        } else {
                            logger.setValueByKey(ClsLogger.IS_TRIM_CONSOLE, "true");
                        }
                    }
                }
                break;
            default:
                break;
        }
        logger.writeLine(MdlConst.LVL_NONE, effectiveMsg);
        if (!logger.getValueByKey(ClsLogger.IS_TRIM_CONSOLE, false)) {
            logger.setValueByKey(ClsLogger.IS_TRIM_CONSOLE, "true");
        }
    }

    public boolean copyFile(String sourceFilePath, String destFilePath, String relativePath, boolean isNew) {
        boolean isSuccess = true;
        if (prop.isList()) {
            return true;
        }
        boolean isSymLink = prop.isSymLink() && Files.isSymbolicLink(Path.of(sourceFilePath));
        try {
            File srcFile = new File(sourceFilePath);
            long creationTime = srcFile.lastModified();
            long lastWriteTime = srcFile.lastModified();

            if (isNew) {
                mkParentDir(destFilePath, true);
            } else {
                File dstF = new File(destFilePath);
                dstF.setWritable(true);
                if (prop.isBackup()) {
                    String backupPath = prop.getBackupDir() + File.separator + relativePath;
                    mkParentDir(backupPath, false);
                    try {
                        File bakF = new File(backupPath);
                        if (bakF.exists()) {
                            bakF.delete();
                        }
                        if (!dstF.renameTo(bakF)) {
                            fsUtil.rename(destFilePath, backupPath);
                        }
                        setFileTime(backupPath, creationTime);
                        setFileTime(backupPath, lastWriteTime);
                    } catch (Exception backupException) {
                        logger.writeLine(MdlConst.LVL_NONE, "[ERR] FAILED TO BACKUP(" + destFilePath + " => " + backupPath + ") : " + backupException.getMessage());
                        if (prop.isStackTrace()) {
                            logger.writeLine(MdlConst.LVL_NONE, "");
                            logger.writeLine(MdlConst.LVL_NONE, getStackTraceString(backupException));
                            logger.writeLine(MdlConst.LVL_NONE, "");
                        }
                        if (prop.isErrorIfBackupFailed()) {
                            throw new IllegalStateException("上書ファイルの退避に失敗しました。");
                        }
                    }
                }
            }

            switch (prop.getTask()) {
                case ClsBaseDir.TASK_CP:
                    try {
                        switch (prop.getCopyCmdType()) {
                            case ClsBaseDir.COPY_BINARY:
                                if (prop.getVerbose() > 5) {
                                    logger.writeLine(MdlConst.LVL_NONE, "[TRY] _objFile.BinaryCopy(" + sourceFilePath + ", " + destFilePath + ")");
                                }
                                fsUtil.binaryCopy(sourceFilePath, destFilePath, prop.isProgress(), prop.getFileShare());
                                break;
                            case ClsBaseDir.COPY_ASYNC:
                                if (prop.getVerbose() > 5) {
                                    logger.writeLine(MdlConst.LVL_NONE, "[TRY] _objFile.AsyncCopy(" + sourceFilePath + ", " + destFilePath + ")");
                                }
                                fsUtil.asyncCopy(sourceFilePath, destFilePath, prop.isProgress(), prop.getFileShare());
                                break;
                            default:
                                fsUtil.copyFileWithRetry(sourceFilePath, destFilePath);
                                break;
                        }
                    } catch (Exception copyException) {
                        logger.writeLine(MdlConst.LVL_NONE, fsUtil.getMessage());
                        throw new IllegalStateException(copyException.getMessage());
                    }
                    break;
                case ClsBaseDir.TASK_MV:
                    if (!isNew) {
                        removeRecursive(destFilePath, relativePath, isSymLink);
                    }
                    if (prop.isProgress()) {
                        try {
                            switch (prop.getCopyCmdType()) {
                                case ClsBaseDir.COPY_BINARY:
                                    if (prop.getVerbose() > 5) {
                                        logger.writeLine(MdlConst.LVL_NONE, "[TRY] _objFile.BinaryCopy(" + sourceFilePath + ", " + destFilePath + ")");
                                    }
                                    fsUtil.binaryCopy(sourceFilePath, destFilePath, prop.isProgress(), prop.getFileShare());
                                    break;
                                case ClsBaseDir.COPY_ASYNC:
                                    if (prop.getVerbose() > 5) {
                                        logger.writeLine(MdlConst.LVL_NONE, "[TRY] _objFile.AsyncCopy(" + sourceFilePath + ", " + destFilePath + ")");
                                    }
                                    fsUtil.asyncCopy(sourceFilePath, destFilePath, prop.isProgress(), prop.getFileShare());
                                    break;
                                default:
                                    fsUtil.rename(sourceFilePath, destFilePath);
                                    break;
                            }
                        } catch (Exception copyException) {
                            logger.writeLine(MdlConst.LVL_NONE, fsUtil.getMessage());
                            throw new IllegalStateException(copyException.getMessage());
                        }
                        if (!removeRecursive(sourceFilePath, relativePath, isSymLink)) {
                            throw new IllegalStateException("[ERR] CopyFile() : CAN NOT DELETE FILE : " + sourceFilePath);
                        }
                    } else {
                        if (prop.getVerbose() > 5) {
                            logger.writeLine(MdlConst.LVL_NONE, "[TRY] System.IO.File.Move(" + sourceFilePath + ", " + destFilePath + ")");
                        }
                        fsUtil.rename(sourceFilePath, destFilePath);
                    }
                    break;
                case ClsBaseDir.TASK_RENAME:
                    try {
                        if (prop.getVerbose() > 5) {
                            logger.writeLine(MdlConst.LVL_NONE, "[TRY] _objFile.Rename(" + sourceFilePath + ", " + destFilePath + ")");
                        }
                        fsUtil.rename(sourceFilePath, destFilePath);
                    } catch (Exception renameException) {
                        logger.writeLine(MdlConst.LVL_NONE, fsUtil.getMessage());
                        throw new IllegalStateException(renameException.getMessage());
                    }
                    break;
                default:
                    break;
            }

            setFileTime(destFilePath, creationTime);
            setFileTime(destFilePath, lastWriteTime);
        } catch (Exception exception) {
            isSuccess = false;
            copyErrorCount++;
            logger.writeLine(MdlConst.LVL_NONE, "[ERR] CopyFile(" + sourceFilePath + ", " + destFilePath + ") : " + exception.getMessage());
            if (prop.isStackTrace()) {
                logger.writeLine(MdlConst.LVL_NONE, "");
                logger.writeLine(MdlConst.LVL_NONE, getStackTraceString(exception));
                logger.writeLine(MdlConst.LVL_NONE, "");
            }
        }
        return isSuccess;
    }

    public boolean mkParentDir(String path, boolean count) {
        boolean success = true;
        String parentDir = MdlFile.getDirectoryPath(path);
        if (parentDir != null && !parentDir.isBlank()) {
            switch (MdlFile.createDirectory(parentDir)) {
                case MdlFile.OK_MKDIR_ALREADY_EXIST:
                    break;
                case MdlFile.OK_MKDIR_CREATE:
                    if (count) {
                        mkdirOkCount++;
                    }
                    break;
                default:
                    success = false;
                    if (count) {
                        mkdirNgCount++;
                    }
                    logger.writeLine(MdlConst.LVL_NONE, "[ERR] ClsFsDiffCopy.MkParentDir() : FAILED TO MKDIR : " + parentDir);
                    break;
            }
        }
        return success;
    }

    public boolean removeRecursive(String path, String relativePath, boolean isSymLink) {
        boolean isSuccess = true;
        String outputPath = getOutputRelativePath(relativePath);
        switch (prop.getOutputPathCode()) {
            case ClsBaseDir.FROM:
            case ClsBaseDir.TO:
            case ClsBaseDir.BOTH:
                outputPath = path;
                break;
            default:
                break;
        }

        switch (MdlFile.getPathType(path)) {
            case MdlFile.PATH_IS_DIRECTORY:
                isSuccess = removeRecursive(new File(path), relativePath, isSymLink);
                break;
            case MdlFile.PATH_IS_FILE:
                try {
                    File fileInfo = new File(path);
                    rmTotalCount++;
                    if (!prop.isList()) {
                        fileInfo.setWritable(true);
                        if (prop.getVerbose() >= 0) {
                            try {
                                logger.writeLine(MdlConst.LVL_NONE, "[DEL][" + MdlDate.getFormattedDate(toLdt(fileInfo.lastModified()), "yyyy/MM/dd HH:mm:ss") + "] " + outputPath);
                            } catch (Exception e) {
                                logger.writeLine(MdlConst.LVL_NONE, "[DEL][更新日時の取得失敗] " + outputPath);
                            }
                        } else if (prop.getVerbose() == -1) {
                            echoTitle("[DEL] " + outputPath);
                        } else {
                            echoTitle(outputPath);
                        }
                        if (fileInfo.delete()) {
                            rmOkCount++;
                        } else {
                            throw new RuntimeException("delete() returned false");
                        }
                    } else {
                        if (prop.getVerbose() >= 0) {
                            logger.writeLine(MdlConst.LVL_NONE, "[-D-][" + MdlDate.getFormattedDate(toLdt(fileInfo.lastModified()), "yyyy/MM/dd HH:mm:ss") + "] " + outputPath);
                        } else if (prop.getVerbose() == -1) {
                            echoTitle("[-D-] " + outputPath);
                        } else {
                            echoTitle(outputPath);
                        }
                    }
                } catch (Exception ex) {
                    isSuccess = false;
                    rmNgCount++;
                    logger.writeLine(MdlConst.LVL_NONE, "[ERR] RemoveRecursive(" + path + ") 1 : " + ex.getMessage() + " : " + path);
                    if (prop.isStackTrace()) {
                        logger.writeLine(MdlConst.LVL_NONE, "");
                        logger.writeLine(MdlConst.LVL_NONE, getStackTraceString(ex));
                        logger.writeLine(MdlConst.LVL_NONE, "");
                    }
                }
                break;
            default:
                break;
        }
        return isSuccess;
    }

    public boolean removeRecursive(File dirInfo, String relativePath, boolean isSymLink) {
        boolean isSuccess = true;
        long fileCount = 0;
        String outputPath = getOutputRelativePath(relativePath);
        switch (prop.getOutputPathCode()) {
            case ClsBaseDir.FROM:
            case ClsBaseDir.TO:
            case ClsBaseDir.BOTH:
                outputPath = dirInfo != null ? dirInfo.getAbsolutePath() : "";
                break;
            default:
                break;
        }

        if (dirInfo == null) {
            return false;
        }

        if (!prop.isList()) {
            try {
                if (!isSymLink) {
                    File[] files = dirInfo.listFiles(File::isFile);
                    if (files != null) {
                        for (File fileInfo : files) {
                            fileCount++;
                            fileInfo.setWritable(true);
                        }
                    }
                    File[] subDirs = dirInfo.listFiles(File::isDirectory);
                    if (subDirs != null) {
                        for (File subDirInfo : subDirs) {
                            removeRecursive(subDirInfo, (relativePath == null || relativePath.isBlank() ? "" : relativePath + File.separator) + subDirInfo.getName(), isSymLink);
                        }
                    }
                }
                dirInfo.setWritable(true);
            } catch (Exception ex) {
                logger.writeLine(MdlConst.LVL_NONE, "[ERR] RemoveRecursive(" + dirInfo.getAbsolutePath() + ") 2-RW : " + ex.getMessage());
            }
        }

        try {
            if (fileCount == 0) {
                fileCount = 1;
            }
            rmTotalCount += fileCount;
            if (!prop.isList()) {
                if (prop.getVerbose() >= 0) {
                    try {
                        logger.writeLine(MdlConst.LVL_NONE, "[DEL][" + MdlDate.getFormattedDate(toLdt(dirInfo.lastModified()), "yyyy/MM/dd HH:mm:ss") + "] " + outputPath);
                    } catch (Exception e) {
                        logger.writeLine(MdlConst.LVL_NONE, "[DEL][更新日時の取得失敗] " + outputPath);
                    }
                } else if (prop.getVerbose() == -1) {
                    echoTitle("[DEL] " + outputPath);
                } else {
                    echoTitle(outputPath);
                }

                if (isSymLink) {
                    dirInfo.delete();
                } else {
                    MdlFile.deleteRecursively(dirInfo.getAbsolutePath(), 0);
                }
                rmOkCount += fileCount;
            } else {
                if (prop.getVerbose() >= 0) {
                    logger.writeLine(MdlConst.LVL_NONE, "[-D-][" + MdlDate.getFormattedDate(toLdt(dirInfo.lastModified()), "yyyy/MM/dd HH:mm:ss") + "] " + outputPath);
                } else if (prop.getVerbose() == -1) {
                    echoTitle("[-D-] " + outputPath);
                } else {
                    echoTitle(outputPath);
                }
            }
        } catch (Exception ex) {
            isSuccess = false;
            rmNgCount += fileCount;
            logger.writeLine(MdlConst.LVL_NONE, "[ERR] RemoveRecursive(" + dirInfo.getAbsolutePath() + ") 2-RM : " + ex.getMessage());
            if (prop.isStackTrace()) {
                logger.writeLine(MdlConst.LVL_NONE, "");
                logger.writeLine(MdlConst.LVL_NONE, getStackTraceString(ex));
                logger.writeLine(MdlConst.LVL_NONE, "");
            }
        }
        return isSuccess;
    }

    /**
     * 指定されたパスのファイルまたはディレクトリを再帰的に削除します。
     *
     * @param path 削除対象のパス
     * @return 削除が成功した場合は true
     */
    public boolean removeRecursive(String path) {
        boolean isOk = true;
        if (!prop.isList()) {
            if (prop.getVerbose() >= 0) {
                logger.writeLine(MdlConst.LVL_NONE, "[DEL] " + path);
            } else if (prop.getVerbose() == -1) {
                echoTitle("[DEL] " + path);
            } else {
                echoTitle(path);
            }
            isOk = MdlFile.deleteRecursively(path, prop.getVerbose());
        } else {
            if (prop.getVerbose() >= 0) {
                logger.writeLine(MdlConst.LVL_NONE, "[-D-] " + path);
            } else if (prop.getVerbose() == -1) {
                echoTitle("[-D-] " + path);
            } else {
                echoTitle(path);
            }
        }
        return isOk;
    }

    /**
     * コピー元ディレクトリの更新日時をコピー先ディレクトリに反映します。
     *
     * @param sourcePath コピー元パス
     * @param destinationPath コピー先パス
     * @param relativePath 相対パス
     * @param modeStr ログ表示用モード文字列（NEW, --- 等）
     */
    public void setDateToDir(String sourcePath, String destinationPath, String relativePath, String modeStr) {
        setDateToDir(new File(sourcePath), new File(destinationPath), relativePath, modeStr);
    }

    /**
     * コピー元ディレクトリの更新日時をコピー先ディレクトリに反映します（Fileオブジェクト指定）。
     *
     * @param sourceDirInfo コピー元ディレクトリ情報
     * @param destinationDirInfo コピー先ディレクトリ情報
     * @param relativePath 相対パス
     * @param modeStr ログ表示用モード文字列
     */
    public void setDateToDir(File sourceDirInfo, File destinationDirInfo, String relativePath, String modeStr) {
        boolean isSetTimestamp = false;
        String sourceLastWriteTimeStr = MdlDate.getFormattedDate(toLdt(sourceDirInfo != null ? sourceDirInfo.lastModified() : 0), "yyyy/MM/dd HH:mm:ss");
        String destinationLastWriteTimeStr = MdlDate.getFormattedDate(toLdt(destinationDirInfo != null ? destinationDirInfo.lastModified() : 0), "yyyy/MM/dd HH:mm:ss");
        String result = ">";
        String outputPath = getOutputRelativePath(relativePath);

        if (sourceDirInfo != null && destinationDirInfo != null) {
            switch (prop.getOutputPathCode()) {
                case ClsBaseDir.FROM:
                    outputPath = sourceDirInfo.getAbsolutePath();
                    break;
                case ClsBaseDir.TO:
                    outputPath = destinationDirInfo.getAbsolutePath();
                    break;
                case ClsBaseDir.BOTH:
                    outputPath = sourceDirInfo.getAbsolutePath() + " => " + destinationDirInfo.getAbsolutePath();
                    break;
                default:
                    break;
            }

            if (prop.getCpTimestamp() > 0 && sourceDirInfo.exists() && destinationDirInfo.exists()) {
                long diffSec = Math.abs((sourceDirInfo.lastModified() - destinationDirInfo.lastModified()) / 1000);
                isSetTimestamp = diffSec > prop.getSecRange();
                if (isSetTimestamp) {
                    try {
                        if (!prop.isList()) {
                            setFileTime(destinationDirInfo.getAbsolutePath(), sourceDirInfo.lastModified());
                        }
                    } catch (Exception e) {
                        result = "X";
                    }
                }
            }
        }

        if (modeStr != null && !modeStr.isBlank()) {
            if (!prop.isFileCopy()) {
                if (prop.getVerbose() >= 0) {
                    if (isSetTimestamp) {
                        echoTitle("[" + modeStr + "][" + destinationLastWriteTimeStr + "=" + result + sourceLastWriteTimeStr + "] " + outputPath);
                    } else {
                        echoTitle("[" + modeStr + "] " + outputPath);
                    }
                } else if (prop.getVerbose() == -1) {
                    echoTitle("[" + modeStr + "] " + outputPath);
                } else {
                    echoTitle(outputPath);
                }
            }
        }
    }

    public void setDateToFile(String sourcePath, String destinationPath, String relativePath, String modeStr) {
        setDateToFile(sourcePath, destinationPath, relativePath, modeStr, false, "");
    }

    public void setDateToFile(String sourcePath, String destinationPath, String relativePath, String modeStr, boolean isShowCksum, String srcCheckStr) {
        setDateToFile(new File(sourcePath), new File(destinationPath), relativePath, modeStr, isShowCksum, srcCheckStr);
    }

    public void setDateToFile(File sourceFileInfo, File destinationFileInfo, String relativePath, String modeStr) {
        setDateToFile(sourceFileInfo, destinationFileInfo, relativePath, modeStr, false, "");
    }

    public void setDateToFile(File sourceFileInfo, File destinationFileInfo, String relativePath, String modeStr, boolean isShowCksum, String srcCheckStr) {
        boolean isSetTimestamp = false;
        long srcMtime = sourceFileInfo != null ? sourceFileInfo.lastModified() : 0;
        long dstMtime = destinationFileInfo != null ? destinationFileInfo.lastModified() : 0;
        String sourceLastWriteTimeStr = MdlDate.getFormattedDate(toLdt(srcMtime), "yyyy/MM/dd HH:mm:ss");
        String destinationLastWriteTimeStr = MdlDate.getFormattedDate(toLdt(dstMtime), "yyyy/MM/dd HH:mm:ss");
        String resultStr = ">";

        if (sourceFileInfo != null && destinationFileInfo != null && prop.getCpTimestamp() > 0 && sourceFileInfo.exists() && destinationFileInfo.exists()) {
            long diffSec = Math.abs((srcMtime - dstMtime) / 1000);
            isSetTimestamp = diffSec > prop.getSecRange();
            if (isSetTimestamp) {
                try {
                    if (!prop.isList()) {
                        setFileTime(destinationFileInfo.getAbsolutePath(), srcMtime);
                    }
                } catch (Exception e) {
                    resultStr = "X";
                }
            }
        }

        if (modeStr != null && !modeStr.isBlank()) {
            String outputPath = getOutputRelativePath(relativePath);
            if (sourceFileInfo != null && destinationFileInfo != null) {
                switch (prop.getOutputPathCode()) {
                    case ClsBaseDir.FROM:
                        outputPath = sourceFileInfo.getAbsolutePath();
                        break;
                    case ClsBaseDir.TO:
                        outputPath = destinationFileInfo.getAbsolutePath();
                        break;
                    case ClsBaseDir.BOTH:
                        outputPath = sourceFileInfo.getAbsolutePath() + " => " + destinationFileInfo.getAbsolutePath();
                        break;
                    default:
                        break;
                }
            }

            if (prop.getVerbose() >= 0) {
                if (isSetTimestamp) {
                    if (isShowCksum) {
                        echoTitle("[" + modeStr + "][" + destinationLastWriteTimeStr + "=" + resultStr + sourceLastWriteTimeStr + "][" + srcCheckStr + "] " + outputPath);
                    } else {
                        echoTitle("[" + modeStr + "][" + destinationLastWriteTimeStr + "=" + resultStr + sourceLastWriteTimeStr + "] " + outputPath);
                    }
                } else {
                    if (isShowCksum) {
                        echoTitle("[" + modeStr + "][" + sourceLastWriteTimeStr + "][" + srcCheckStr + "] " + outputPath);
                    } else {
                        echoTitle("[" + modeStr + "][" + sourceLastWriteTimeStr + "] " + outputPath);
                    }
                }
            } else if (prop.getVerbose() == -1) {
                echoTitle("[" + modeStr + "] " + outputPath);
            } else {
                echoTitle(outputPath);
            }
        }
    }
}
