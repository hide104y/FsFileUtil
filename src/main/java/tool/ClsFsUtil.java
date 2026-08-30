package tool;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.security.MessageDigest;
import tool.cmnclslib.cls.ClsFsAsyncCopyStatus;
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlFile;

/**
 * ファイルシステム上の各種操作（コピー、移動、リネーム、ローテーション、待機など）を行うユーティリティクラスです。
 */
public class ClsFsUtil {

    private final ClsLogger logger;
    private String message = "";
    private String result = "";
    private boolean isStackTrace = false;
    private int verbose = 0;
    private int waitMSecForRetryCopy = 200;
    private int retryMax = 0;

    /**
     * {@link ClsFsUtil} クラスの新しいインスタンスを初期化します。
     *
     * @param logger ログ出力用のロガーインスタンス
     */
    public ClsFsUtil(ClsLogger logger) {
        this.logger = logger != null ? logger : new ClsLogger();
    }

    /**
     * 最新の処理結果メッセージを取得します。
     *
     * @return 処理結果メッセージ
     */
    public String getMessage() {
        return message;
    }

    /**
     * 最新の処理結果メッセージを設定します。
     *
     * @param message 処理結果メッセージ
     */
    public void setMessage(String message) {
        this.message = message != null ? message : "";
    }

    /**
     * 最新の処理結果文字列を取得します。
     *
     * @return 処理結果文字列
     */
    public String getResult() {
        return result;
    }

    /**
     * 最新の処理結果文字列を設定します。
     *
     * @param result 処理結果文字列
     */
    public void setResult(String result) {
        this.result = result != null ? result : "";
    }

    /**
     * スタックトレース出力フラグを取得します。
     *
     * @return スタックトレース出力フラグ
     */
    public boolean isStackTrace() {
        return isStackTrace;
    }

    /**
     * スタックトレース出力フラグを設定します。
     *
     * @param stackTrace スタックトレース出力フラグ
     */
    public void setStackTrace(boolean stackTrace) {
        isStackTrace = stackTrace;
    }

    /**
     * 詳細ログ出力レベルを取得します。
     *
     * @return 詳細ログレベル
     */
    public int getVerbose() {
        return verbose;
    }

    /**
     * 詳細ログ出力レベルを設定します。
     *
     * @param verbose 詳細ログレベル
     */
    public void setVerbose(int verbose) {
        this.verbose = verbose;
    }

    /**
     * コピー再試行時の待機時間（ミリ秒）を取得します。
     *
     * @return コピー再試行待機時間（ミリ秒）
     */
    public int getWaitMSecForRetryCopy() {
        return waitMSecForRetryCopy;
    }

    /**
     * コピー再試行時の待機時間（ミリ秒）を設定します。
     *
     * @param waitMSecForRetryCopy コピー再試行待機時間（ミリ秒）
     */
    public void setWaitMSecForRetryCopy(int waitMSecForRetryCopy) {
        this.waitMSecForRetryCopy = waitMSecForRetryCopy;
    }

    /**
     * コピー再試行の最大回数を取得します。
     *
     * @return コピー再試行最大回数
     */
    public int getRetryMax() {
        return retryMax;
    }

    /**
     * コピー再試行の最大回数を設定します。
     *
     * @param retryMax コピー再試行最大回数
     */
    public void setRetryMax(int retryMax) {
        this.retryMax = retryMax;
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
     * 指定されたパスのファイルをローテーションします。
     *
     * @param path ローテーションするファイルのパス
     * @param keepMax 保持するファイルの最大世代数
     * @return 操作結果ステータス（{@link MdlConst#LVL_I}: 成功、{@link MdlConst#LVL_E}: 失敗）
     */
    public int rotate(String path, int keepMax) {
        if (path == null || path.isBlank()) {
            throw new IllegalArgumentException("Path cannot be null or blank");
        }
        message = "";
        int returnCode = MdlFile.deleteRecursively(path + "." + keepMax) ? MdlConst.LVL_I : MdlConst.LVL_E;
        if (returnCode == MdlConst.LVL_I) {
            for (int i = 0; i < keepMax; i++) {
                int suffixNo = keepMax - i;
                String sourcePath = suffixNo == 1 ? path : (path + "." + (suffixNo - 1));
                String destinationPath = path + "." + suffixNo;
                if (!rename(sourcePath, destinationPath)) {
                    returnCode = MdlConst.LVL_E;
                    break;
                }
            }
        } else {
            logger.writeLine(MdlConst.LVL_NONE, "NG : DELETE " + path + "." + keepMax);
            returnCode = MdlConst.LVL_E;
        }
        return returnCode;
    }

    /**
     * 指定されたファイルが存在するまで待機します。
     *
     * @param path 確認対象のファイルパス
     * @param maxLoop 最大確認回数
     * @param interval ループ間の待機秒数
     * @return ファイルが存在する場合は true、それ以外は false
     */
    public boolean waitUntilFileExists(String path, int maxLoop, int interval) {
        return waitUntilFileExists(path, maxLoop, interval, false);
    }

    /**
     * 指定されたファイルが存在するまで待機します。
     *
     * @param path 確認対象のファイルパス
     * @param maxLoop 最大確認回数
     * @param interval ループ間の待機秒数
     * @param checkFileLock ファイルロック状態も判定する場合は true
     * @return ファイルが存在し、ロックされていない場合は true
     */
    public boolean waitUntilFileExists(String path, int maxLoop, int interval, boolean checkFileLock) {
        if (path == null || path.isBlank()) {
            throw new IllegalArgumentException("Path cannot be null or blank");
        }
        boolean isOk = false;
        int effectiveMaxLoop = maxLoop < 1 ? 1 : maxLoop;
        for (int i = 0; i < effectiveMaxLoop; i++) {
            if (MdlFile.pathExists(path)) {
                if (checkFileLock && MdlFile.isFileLocked(path)) {
                    logger.writeLine(MdlConst.LVL_NONE, " => [" + (i + 1) + "][--] LOCKED    : " + path);
                } else {
                    logger.writeLine(MdlConst.LVL_NONE, " => [" + (i + 1) + "][OK] FOUND     : " + path);
                    isOk = true;
                    break;
                }
            } else {
                logger.writeLine(MdlConst.LVL_NONE, " => [" + (i + 1) + "][--] NOT FOUND");
            }
            if (i < effectiveMaxLoop - 1 && interval > 0) {
                try {
                    Thread.sleep((long) interval * 1000);
                } catch (InterruptedException ignored) {
                    Thread.currentThread().interrupt();
                }
            }
        }
        return isOk;
    }

    /**
     * ディレクトリまたはファイルの名前を変更（移動）します。
     *
     * @param sourcePath 移動元のパス
     * @param destinationPath 移動先のパス
     * @return 移動処理が正常完了した場合は true、失敗時は false
     */
    public boolean rename(String sourcePath, String destinationPath) {
        if (sourcePath == null || sourcePath.isBlank() || destinationPath == null || destinationPath.isBlank()) {
            throw new IllegalArgumentException("sourcePath and destinationPath cannot be null or blank");
        }
        boolean isOk = true;
        try {
            logger.writeLine(MdlConst.LVL_NONE, "TRY : MOVE : " + sourcePath + " => " + destinationPath);
            int check = MdlFile.getPathType(sourcePath);
            switch (check) {
                case MdlFile.PATH_IS_DIRECTORY:
                case MdlFile.PATH_IS_FILE:
                    File src = new File(sourcePath);
                    File dst = new File(destinationPath);
                    if (dst.exists()) {
                        throw new IOException("Destination already exists: " + destinationPath);
                    }
                    if (src.renameTo(dst)) {
                        logger.writeLine(MdlConst.LVL_NONE, check == MdlFile.PATH_IS_DIRECTORY ? " -> OK : MOVED THE DIRECTORY" : " -> OK : MOVED THE FILE");
                    } else {
                        Files.move(src.toPath(), dst.toPath());
                        logger.writeLine(MdlConst.LVL_NONE, check == MdlFile.PATH_IS_DIRECTORY ? " -> OK : MOVED THE DIRECTORY" : " -> OK : MOVED THE FILE");
                    }
                    break;
                default:
                    logger.writeLine(MdlConst.LVL_NONE, " -> SKIP : NOT FOUND(" + check + ")");
                    break;
            }
        } catch (Exception ex) {
            isOk = false;
            logger.writeLine(MdlConst.LVL_NONE, " -> EXCEPTION : " + ex.getMessage());
            if (isStackTrace) {
                logger.writeLine(MdlConst.LVL_NONE, "");
                logger.writeLine(MdlConst.LVL_NONE, getStackTraceString(ex));
                logger.writeLine(MdlConst.LVL_NONE, "");
            }
        }
        return isOk;
    }

    /**
     * 指定されたファイルの SHA-1 ハッシュ値を取得します。
     *
     * @param path ハッシュ値を計算する対象ファイルのパス
     * @return SHA-1 ハッシュ文字列（小文字16進数、例外発生時は空文字列）
     */
    public String computeSha1Hash(String path) {
        if (path == null || path.isBlank()) {
            throw new IllegalArgumentException("Path cannot be null or blank");
        }
        String res = "";
        try {
            Path targetPath = Path.of(path);
            if (!Files.exists(targetPath) || !Files.isRegularFile(targetPath)) {
                throw new IOException("File not found: " + path);
            }
            MessageDigest digest = MessageDigest.getInstance("SHA-1");
            try (InputStream is = Files.newInputStream(targetPath)) {
                byte[] buffer = new byte[8192];
                int read;
                while ((read = is.read(buffer)) != -1) {
                    digest.update(buffer, 0, read);
                }
            }
            byte[] hashBytes = digest.digest();
            StringBuilder sb = new StringBuilder();
            for (byte b : hashBytes) {
                sb.append(String.format("%02x", b));
            }
            res = sb.toString();
        } catch (Exception ex) {
            logger.writeLine(MdlConst.LVL_NONE, "NG : FAILED TO GET SHA1 : " + path + " => " + ex.getMessage());
            if (isStackTrace) {
                logger.writeLine(MdlConst.LVL_NONE, "");
                logger.writeLine(MdlConst.LVL_NONE, getStackTraceString(ex));
                logger.writeLine(MdlConst.LVL_NONE, "");
            }
        }
        return res;
    }


    /**
     * 設定されたリトライ回数および待機時間に基づいて、ファイルをコピーします。
     *
     * @param sourcePath コピー元パス
     * @param destinationPath コピー先パス
     * @throws IOException コピー失敗時
     */
    public void copyFileWithRetry(String sourcePath, String destinationPath) throws IOException {
        if (sourcePath == null || sourcePath.isBlank() || destinationPath == null || destinationPath.isBlank()) {
            throw new IllegalArgumentException("sourcePath and destinationPath cannot be null or blank");
        }
        for (int i = 0; i <= retryMax; i++) {
            try {
                logger.writeLine(MdlConst.LVL_NONE, " -> TRY " + i + "/" + retryMax + " Files.copy(" + sourcePath + ", " + destinationPath + ")");
                Path src = Path.of(sourcePath);
                Path dst = Path.of(destinationPath);
                if (dst.getParent() != null && !Files.exists(dst.getParent())) {
                    Files.createDirectories(dst.getParent());
                }
                Files.copy(src, dst, StandardCopyOption.REPLACE_EXISTING);
                return;
            } catch (IOException e) {
                if (i < retryMax) {
                    logger.writeLine(MdlConst.LVL_NONE, " -> RETRY SLEEP(" + waitMSecForRetryCopy + ")");
                    try {
                        Thread.sleep(waitMSecForRetryCopy);
                    } catch (InterruptedException ignored) {
                        Thread.currentThread().interrupt();
                    }
                } else {
                    throw e;
                }
            }
        }
    }

    /**
     * バイナリファイルをコピーします。
     *
     * @param sourcePath コピー元パス
     * @param destinationPath コピー先パス
     * @param showProgress 進捗表示フラグ
     * @param fileShare ファイル共有モード
     */
    public void binaryCopy(String sourcePath, String destinationPath, boolean showProgress, int fileShare) {
        if (sourcePath == null || sourcePath.isBlank() || destinationPath == null || destinationPath.isBlank()) {
            throw new IllegalArgumentException("sourcePath and destinationPath cannot be null or blank");
        }
        ClsFsAsyncCopyStatus asyncCpStatus = new ClsFsAsyncCopyStatus(sourcePath, destinationPath, false, fileShare);
        boolean isException = false;
        message = "[ClsFsUtil.BinaryCopy()] Called";
        result = "";

        if (asyncCpStatus.isOk() && asyncCpStatus.getSourceStream() != null && asyncCpStatus.getDestinationStream() != null) {
            try {
                asyncCpStatus.setShowProgress(showProgress);
                FileInputStream fis = asyncCpStatus.getSourceStream();
                FileOutputStream fos = asyncCpStatus.getDestinationStream();
                byte[] buf = asyncCpStatus.getBuffer();
                int read = fis.read(buf);
                while (read > 0) {
                    fos.write(buf, 0, read);
                    asyncCpStatus.setCurrentCount(asyncCpStatus.getCurrentCount() + 1);
                    if (showProgress && asyncCpStatus.getCheckCount() > 0 && asyncCpStatus.getCurrentCount() >= asyncCpStatus.getCheckCount()) {
                        asyncCpStatus.showProgress();
                    }
                    read = fis.read(buf);
                }
                if (showProgress) {
                    asyncCpStatus.showProgress();
                }
            } catch (Exception exception) {
                isException = true;
                if (verbose > 1) {
                    logger.writeLine(MdlConst.LVL_NONE, " -> EXCEPTION 2 : ClsFsUtil.BinaryCopy(" + sourcePath + ", " + destinationPath + ") : " + exception.getMessage());
                }
                if (isStackTrace) {
                    logger.writeLine(MdlConst.LVL_NONE, "");
                    logger.writeLine(MdlConst.LVL_NONE, "---[STACKTRACE]---");
                    logger.writeLine(MdlConst.LVL_NONE, asyncCpStatus.getStackTrace());
                    logger.writeLine(MdlConst.LVL_NONE, "");
                }
            } finally {
                asyncCpStatus.close();
                result = asyncCpStatus.getProgressLine();
            }
            if (isException) {
                try {
                    Thread.sleep(waitMSecForRetryCopy);
                    copyFileWithRetry(sourcePath, destinationPath);
                } catch (Exception ex) {
                    if (ex instanceof RuntimeException) {
                        throw (RuntimeException) ex;
                    }
                    throw new RuntimeException(ex);
                }
            }
        } else {
            asyncCpStatus.close();
            result = asyncCpStatus.getProgressLine();
            if (verbose > 1) {
                logger.writeLine(MdlConst.LVL_NONE, " -> EXCEPTION 1 : ClsFsUtil.BinaryCopy(" + sourcePath + ", " + destinationPath + ") : " + asyncCpStatus.getMessage());
            }
            if (isStackTrace) {
                logger.writeLine(MdlConst.LVL_NONE, "");
                logger.writeLine(MdlConst.LVL_NONE, "---[STACKTRACE]---");
                logger.writeLine(MdlConst.LVL_NONE, asyncCpStatus.getStackTrace());
                logger.writeLine(MdlConst.LVL_NONE, "");
            }
            try {
                Thread.sleep(waitMSecForRetryCopy);
                copyFileWithRetry(sourcePath, destinationPath);
            } catch (Exception ex) {
                if (ex instanceof RuntimeException) {
                    throw (RuntimeException) ex;
                }
                throw new RuntimeException(ex);
            }
        }
    }

    /**
     * バイナリファイルをコピーします（デフォルト共有モード）。
     *
     * @param sourcePath コピー元パス
     * @param destinationPath コピー先パス
     * @param showProgress 進捗表示フラグ
     */
    public void binaryCopy(String sourcePath, String destinationPath, boolean showProgress) {
        binaryCopy(sourcePath, destinationPath, showProgress, ClsBaseDir.FILE_SHARE_READ_WRITE);
    }

    /**
     * 非同期でファイルをコピーします。
     *
     * @param sourcePath コピー元パス
     * @param destinationPath コピー先パス
     * @param showProgress 進捗表示フラグ
     * @param fileShare ファイル共有モード
     */
    public void asyncCopy(String sourcePath, String destinationPath, boolean showProgress, int fileShare) {
        binaryCopy(sourcePath, destinationPath, showProgress, fileShare);
    }

    /**
     * 非同期でファイルをコピーします（デフォルト共有モード）。
     *
     * @param sourcePath コピー元パス
     * @param destinationPath コピー先パス
     * @param showProgress 進捗表示フラグ
     */
    public void asyncCopy(String sourcePath, String destinationPath, boolean showProgress) {
        asyncCopy(sourcePath, destinationPath, showProgress, ClsBaseDir.FILE_SHARE_READ_WRITE);
    }
}