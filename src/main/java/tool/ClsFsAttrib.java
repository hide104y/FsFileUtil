package tool;

import java.io.File;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.nio.file.FileSystems;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.attribute.PosixFilePermission;
import java.nio.file.attribute.PosixFilePermissions;
import java.nio.file.attribute.UserPrincipal;
import java.time.LocalDateTime;
import java.util.Set;
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlDate;
import tool.cmnclslib.mdl.MdlFile;
import tool.cmnclslib.mdl.MdlUtil;

/**
 * ファイルシステム上のファイルおよびディレクトリの属性・サイズ・統計・所有者の取得およびログ出力を管理するクラスです。
 */
public class ClsFsAttrib {

    private final ClsLogger logger;
    private long directoryCount = 0;
    private long fileCount = 0;
    private long totalSize = 0;
    private long errorDirectoryCount = 0;
    private long errorFileCount = 0;
    private boolean isProgressEnabled = false;
    private int progressIntervalDirectories = 0;
    private int progressIntervalFiles = 0;

    /**
     * {@link ClsFsAttrib} クラスの新しいインスタンスを初期化します。
     *
     * @param logger ログ出力に使用するロガーインスタンス
     */
    public ClsFsAttrib(ClsLogger logger) {
        this.logger = logger != null ? logger : new ClsLogger();
    }

    /**
     * ディレクトリ集計件数を取得します。
     *
     * @return ディレクトリ集計件数
     */
    public long getDirectoryCount() {
        return directoryCount;
    }

    /**
     * ディレクトリ集計件数を設定します。
     *
     * @param directoryCount ディレクトリ集計件数
     */
    public void setDirectoryCount(long directoryCount) {
        this.directoryCount = directoryCount;
    }

    /**
     * ファイル集計件数を取得します。
     *
     * @return ファイル集計件数
     */
    public long getFileCount() {
        return fileCount;
    }

    /**
     * ファイル集計件数を設定します。
     *
     * @param fileCount ファイル集計件数
     */
    public void setFileCount(long fileCount) {
        this.fileCount = fileCount;
    }

    /**
     * 合計ファイルサイズ（バイト）を取得します。
     *
     * @return 合計ファイルサイズ
     */
    public long getTotalSize() {
        return totalSize;
    }

    /**
     * 合計ファイルサイズ（バイト）を設定します。
     *
     * @param totalSize 合計ファイルサイズ
     */
    public void setTotalSize(long totalSize) {
        this.totalSize = totalSize;
    }

    /**
     * エラーが発生したディレクトリ件数を取得します。
     *
     * @return エラーディレクトリ件数
     */
    public long getErrorDirectoryCount() {
        return errorDirectoryCount;
    }

    /**
     * エラーが発生したディレクトリ件数を設定します。
     *
     * @param errorDirectoryCount エラーディレクトリ件数
     */
    public void setErrorDirectoryCount(long errorDirectoryCount) {
        this.errorDirectoryCount = errorDirectoryCount;
    }

    /**
     * エラーが発生したファイル件数を取得します。
     *
     * @return エラーファイル件数
     */
    public long getErrorFileCount() {
        return errorFileCount;
    }

    /**
     * エラーが発生したファイル件数を設定します。
     *
     * @param errorFileCount エラーファイル件数
     */
    public void setErrorFileCount(long errorFileCount) {
        this.errorFileCount = errorFileCount;
    }

    /**
     * 進捗ログ出力が有効かどうかを取得します。
     *
     * @return 進捗ログが有効な場合は true
     */
    public boolean isProgressEnabled() {
        return isProgressEnabled;
    }

    /**
     * 進捗ログ出力の有効フラグを設定します。
     *
     * @param progressEnabled 進捗ログの有効フラグ
     */
    public void setProgressEnabled(boolean progressEnabled) {
        isProgressEnabled = progressEnabled;
    }

    /**
     * ディレクトリ処理時の進捗表示間隔（件数）を取得します。
     *
     * @return ディレクトリ進捗表示間隔
     */
    public int getProgressIntervalDirectories() {
        return progressIntervalDirectories;
    }

    /**
     * ディレクトリ処理時の進捗表示間隔（件数）を設定します。
     *
     * @param progressIntervalDirectories ディレクトリ進捗表示間隔
     */
    public void setProgressIntervalDirectories(int progressIntervalDirectories) {
        this.progressIntervalDirectories = progressIntervalDirectories;
    }

    /**
     * ファイル処理時の進捗表示間隔（件数）を取得します。
     *
     * @return ファイル進捗表示間隔
     */
    public int getProgressIntervalFiles() {
        return progressIntervalFiles;
    }

    /**
     * ファイル処理時の進捗表示間隔（件数）を設定します。
     *
     * @param progressIntervalFiles ファイル進捗表示間隔
     */
    public void setProgressIntervalFiles(int progressIntervalFiles) {
        this.progressIntervalFiles = progressIntervalFiles;
    }

    /**
     * 各種集計カウンター（ディレクトリ数、ファイル数、合計サイズ、エラー数）を 0 にリセットします。
     */
    public void clearCounter() {
        directoryCount = 0;
        fileCount = 0;
        totalSize = 0;
        errorDirectoryCount = 0;
        errorFileCount = 0;
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

    /**
     * 指定されたパスのディレクトリおよび配下ファイルのサイズと件数を再帰的に取得・計算します。
     *
     * @param targetPath 対象のディレクトリパス
     * @param checkSymlink シンボリックリンク判定を行うかどうか
     * @param verboseLevel 詳細ログレベル
     * @param isStackTrace スタックトレース出力フラグ
     * @return 正常完了時は true
     */
    public boolean calculateDirectorySize(String targetPath, boolean checkSymlink, int verboseLevel, boolean isStackTrace) {
        if (targetPath == null || targetPath.isBlank()) {
            return false;
        }
        return calculateDirectorySize(new File(targetPath), checkSymlink, verboseLevel, isStackTrace);
    }

    /**
     * 指定された File オブジェクトに基づいてディレクトリおよび配下ファイルのサイズと件数を再帰的に取得・計算します。
     *
     * @param targetDirectory 対象ディレクトリ
     * @param checkSymlink シンボリックリンク判定を行うかどうか
     * @param verboseLevel 詳細ログレベル
     * @param isStackTrace スタックトレース出力フラグ
     * @return 正常完了時は true
     */
    public boolean calculateDirectorySize(File targetDirectory, boolean checkSymlink, int verboseLevel, boolean isStackTrace) {
        boolean isSuccess = true;
        try {
            if (targetDirectory == null || !targetDirectory.exists() || !targetDirectory.isDirectory()) {
                errorDirectoryCount++;
                directoryCount++;
                if (verboseLevel > 0) {
                    logger.writeLine(MdlConst.LVL_NONE, " => SKIP DIR  : " + (targetDirectory != null ? targetDirectory.getAbsolutePath() : "") + " : Directory not found or not accessible");
                }
                return false;
            }

            directoryCount++;
            if (isProgressEnabled) {
                if (progressIntervalDirectories > 0 && directoryCount > 0 && (directoryCount % progressIntervalDirectories) == 0) {
                    logger.writeLine(MdlConst.LVL_NONE, " => CURRENT STATUS : " + MdlDate.getFormattedDate(LocalDateTime.now(), "yyyy/MM/dd HH:mm:ss") + " : DIRS=" + directoryCount + " / FILES=" + fileCount + " / SIZE=" + MdlUtil.getHumanReadableBytes(totalSize));
                } else if (progressIntervalFiles > 0 && fileCount > 0 && (fileCount % progressIntervalFiles) == 0) {
                    logger.writeLine(MdlConst.LVL_NONE, " => CURRENT STATUS : " + MdlDate.getFormattedDate(LocalDateTime.now(), "yyyy/MM/dd HH:mm:ss") + " : DIRS=" + directoryCount + " / FILES=" + fileCount + " / SIZE=" + MdlUtil.getHumanReadableBytes(totalSize));
                }
            }

            if (checkSymlink && Files.isSymbolicLink(targetDirectory.toPath())) {
                return true;
            }

            File[] files = targetDirectory.listFiles(File::isFile);
            if (files != null) {
                for (File fileInfo : files) {
                    fileCount++;
                    try {
                        if (!checkSymlink || !Files.isSymbolicLink(fileInfo.toPath())) {
                            totalSize += fileInfo.length();
                        }
                    } catch (Exception fileException) {
                        isSuccess = false;
                        errorFileCount++;
                        if (verboseLevel > 0) {
                            logger.writeLine(MdlConst.LVL_NONE, " => SKIP FILE : " + fileInfo.getAbsolutePath() + " : " + fileException.getMessage());
                        }
                        if (isStackTrace) {
                            logger.writeLine(MdlConst.LVL_NONE, "");
                            logger.writeLine(MdlConst.LVL_NONE, getStackTraceString(fileException));
                            logger.writeLine(MdlConst.LVL_NONE, "");
                        }
                    }
                }
            }

            File[] subDirs = targetDirectory.listFiles(File::isDirectory);
            if (subDirs != null) {
                for (File subDir : subDirs) {
                    if (!calculateDirectorySize(subDir, checkSymlink, verboseLevel, isStackTrace)) {
                        isSuccess = false;
                    }
                }
            }
        } catch (Exception directoryException) {
            isSuccess = false;
            errorDirectoryCount++;
            if (verboseLevel > 0) {
                logger.writeLine(MdlConst.LVL_NONE, " => SKIP DIR  : " + (targetDirectory != null ? targetDirectory.getAbsolutePath() : "") + " : " + directoryException.getMessage());
            }
            if (isStackTrace) {
                logger.writeLine(MdlConst.LVL_NONE, "");
                logger.writeLine(MdlConst.LVL_NONE, getStackTraceString(directoryException));
                logger.writeLine(MdlConst.LVL_NONE, "");
            }
        }
        return isSuccess;
    }

    /**
     * 指定されたパスの単一ファイルサイズを取得し、合計サイズおよび件数に加算します。
     *
     * @param targetPath 対象ファイルパス
     * @param checkSymlink シンボリックリンク判定フラグ
     * @param verboseLevel 詳細ログレベル
     * @param isStackTrace スタックトレース出力フラグ
     * @return 正常完了時は true
     */
    public boolean calculateFileSize(String targetPath, boolean checkSymlink, int verboseLevel, boolean isStackTrace) {
        if (targetPath == null || targetPath.isBlank()) {
            return false;
        }
        return calculateFileSize(new File(targetPath), checkSymlink, verboseLevel, isStackTrace);
    }

    /**
     * 指定された File オブジェクトのファイルサイズを取得し、合計サイズおよび件数に加算します。
     *
     * @param targetFile 対象ファイル
     * @param checkSymlink シンボリックリンク判定フラグ
     * @param verboseLevel 詳細ログレベル
     * @param isStackTrace スタックトレース出力フラグ
     * @return 正常完了時は true
     */
    public boolean calculateFileSize(File targetFile, boolean checkSymlink, int verboseLevel, boolean isStackTrace) {
        boolean isSuccess = true;
        try {
            if (targetFile == null || !targetFile.exists() || !targetFile.isFile()) {
                errorFileCount++;
                if (verboseLevel > 0) {
                    logger.writeLine(MdlConst.LVL_NONE, " => SKIP FILE : " + (targetFile != null ? targetFile.getAbsolutePath() : "") + " : File not found");
                }
                return false;
            }

            if (!checkSymlink || !Files.isSymbolicLink(targetFile.toPath())) {
                totalSize += targetFile.length();
                fileCount++;
            }
        } catch (Exception fileException) {
            isSuccess = false;
            errorFileCount++;
            if (verboseLevel > 0) {
                logger.writeLine(MdlConst.LVL_NONE, " => SKIP FILE : " + (targetFile != null ? targetFile.getAbsolutePath() : "") + " : " + fileException.getMessage());
            }
            if (isStackTrace) {
                logger.writeLine(MdlConst.LVL_NONE, "");
                logger.writeLine(MdlConst.LVL_NONE, getStackTraceString(fileException));
                logger.writeLine(MdlConst.LVL_NONE, "");
            }
        }
        return isSuccess;
    }

    /**
     * 指定されたパスのディレクトリ所有者アカウントを取得し、ログに出力します。
     *
     * @param targetPath 対象ディレクトリパス
     * @param verboseLevel 詳細ログレベル
     * @param showPath パス表示フラグ
     * @param isStackTrace スタックトレース出力フラグ
     * @return 処理成功時は true
     */
    public boolean outputDirectoryOwner(String targetPath, int verboseLevel, boolean showPath, boolean isStackTrace) {
        try {
            if (targetPath == null || targetPath.isBlank() || !Files.exists(Path.of(targetPath))) {
                if (verboseLevel > 0) {
                    if (showPath) {
                        logger.writeLine(MdlConst.LVL_NONE, targetPath + ",FAILED TO GET OWNER(" + targetPath + ")：EXCEPTION：Directory not found");
                    } else {
                        logger.writeLine(MdlConst.LVL_NONE, " => FAILED TO GET OWNER(" + targetPath + ")：EXCEPTION：Directory not found");
                    }
                }
                return false;
            }
            Path path = Path.of(targetPath);
            UserPrincipal owner = Files.getOwner(path);
            String ownerName = owner != null ? owner.getName() : "UNKNOWN";
            if (showPath) {
                logger.writeLine(MdlConst.LVL_NONE, targetPath + ",OWNER,OWNER," + ownerName);
            } else {
                logger.writeLine(MdlConst.LVL_NONE, ownerName);
            }
            return true;
        } catch (Exception exception) {
            if (verboseLevel > 0) {
                if (showPath) {
                    logger.writeLine(MdlConst.LVL_NONE, targetPath + ",FAILED TO GET OWNER(" + targetPath + ")：EXCEPTION：" + exception.getMessage());
                } else {
                    logger.writeLine(MdlConst.LVL_NONE, " => FAILED TO GET OWNER(" + targetPath + ")：EXCEPTION：" + exception.getMessage());
                }
            }
            if (isStackTrace) {
                logger.writeLine(MdlConst.LVL_NONE, "");
                logger.writeLine(MdlConst.LVL_NONE, getStackTraceString(exception));
                logger.writeLine(MdlConst.LVL_NONE, "");
            }
            return false;
        }
    }

    /**
     * 指定されたパスのディレクトリのアクセス許可情報を出力します（Windows ACLはプラットフォーム非依存で安全にフォールバック）。
     *
     * @param targetPath 対象ディレクトリパス
     * @param verboseLevel 詳細ログレベル
     * @param showPath パス表示フラグ
     * @param isStackTrace スタックトレース出力フラグ
     * @return 処理成功時は true
     */
    public boolean outputDirectoryPermission(String targetPath, int verboseLevel, boolean showPath, boolean isStackTrace) {
        try {
            if (targetPath == null || targetPath.isBlank() || !Files.exists(Path.of(targetPath))) {
                if (verboseLevel > 0) {
                    if (showPath) {
                        logger.writeLine(MdlConst.LVL_NONE, targetPath + ",FAILED TO GET PERMISSION(" + targetPath + ")：EXCEPTION 1：Directory not found");
                    } else {
                        logger.writeLine(MdlConst.LVL_NONE, " => FAILED TO GET PERMISSION(" + targetPath + ")：EXCEPTION 1：Directory not found");
                    }
                }
                return false;
            }
            Path path = Path.of(targetPath);
            String line;
            if (FileSystems.getDefault().supportedFileAttributeViews().contains("posix")) {
                try {
                    Set<PosixFilePermission> posixPerms = Files.getPosixFilePermissions(path);
                    String posixStr = PosixFilePermissions.toString(posixPerms);
                    UserPrincipal owner = Files.getOwner(path);
                    String ownerName = owner != null ? owner.getName() : "UNKNOWN";
                    line = "POSIX,\"" + posixStr + "\"," + ownerName;
                } catch (Exception e) {
                    File dir = new File(targetPath);
                    String perms = (dir.canRead() ? "R" : "-") + (dir.canWrite() ? "W" : "-") + (dir.canExecute() ? "X" : "-");
                    line = "Allow,\"" + perms + "\",Everyone";
                }
            } else {
                File dir = new File(targetPath);
                String perms = (dir.canRead() ? "R" : "-") + (dir.canWrite() ? "W" : "-") + (dir.canExecute() ? "X" : "-");
                line = "Allow,\"" + perms + "\",Everyone,継承,\"このフォルダー以下すべてに適用\",このフォルダー以下すべてに適用";
            }
            if (showPath) {
                logger.writeLine(MdlConst.LVL_NONE, targetPath + "," + line);
            } else {
                logger.writeLine(MdlConst.LVL_NONE, line);
            }
            return true;
        } catch (Exception exception) {
            if (verboseLevel > 0) {
                if (showPath) {
                    logger.writeLine(MdlConst.LVL_NONE, targetPath + ",FAILED TO GET PERMISSION(" + targetPath + ")：EXCEPTION 1：" + exception.getMessage());
                } else {
                    logger.writeLine(MdlConst.LVL_NONE, " => FAILED TO GET PERMISSION(" + targetPath + ")：EXCEPTION 1：" + exception.getMessage());
                }
            }
            if (isStackTrace) {
                logger.writeLine(MdlConst.LVL_NONE, "");
                logger.writeLine(MdlConst.LVL_NONE, getStackTraceString(exception));
                logger.writeLine(MdlConst.LVL_NONE, "");
            }
            return false;
        }
    }
}