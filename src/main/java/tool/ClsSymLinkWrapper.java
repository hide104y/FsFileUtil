package tool;

import java.nio.file.Files;
import java.nio.file.Path;
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlFile;

/**
 * シンボリックリンク操作のラッパークラスです。
 */
public class ClsSymLinkWrapper {

    private final ClsLogger logger;
    private String message = "";
    private int verbose = 0;
    private boolean isSilent = false;

    /**
     * {@link ClsSymLinkWrapper} クラスの新しいインスタンスを初期化します。
     *
     * @param logger ログ出力に使用するロガーインスタンス
     */
    public ClsSymLinkWrapper(ClsLogger logger) {
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
     * サイレントモード（ログ出力抑制）フラグを取得します。
     *
     * @return サイレントモードの場合は true
     */
    public boolean isSilent() {
        return isSilent;
    }

    /**
     * サイレントモード（ログ出力抑制）フラグを設定します。
     *
     * @param silent サイレントモードフラグ
     */
    public void setSilent(boolean silent) {
        isSilent = silent;
    }

    /**
     * 指定されたレベルでログメッセージを出力します（サイレントモード時は出力されません）。
     *
     * @param level ログレベル
     * @param msg 出力するメッセージ
     */
    public void writeLine(int level, String msg) {
        if (!isSilent) {
            logger.writeLine(level, msg);
        }
    }

    /**
     * シンボリックリンクをコピーします（相対パス指定フラグ対応）。
     *
     * @param sourcePath コピー元パス
     * @param destinationPath コピー先パス
     * @param overwrite 上書きフラグ
     * @param isRelative 相対パスフラグ
     * @return 成功時は true
     */
    public boolean copy(String sourcePath, String destinationPath, boolean overwrite, boolean isRelative) {
        try {
            if (sourcePath == null || sourcePath.isBlank() || destinationPath == null || destinationPath.isBlank()) {
                return false;
            }
            Path src = Path.of(sourcePath);
            if (!Files.isSymbolicLink(src)) {
                return false;
            }
            Path target = Files.readSymbolicLink(src);
            Path dst = Path.of(destinationPath);
            if (overwrite && Files.exists(dst)) {
                Files.delete(dst);
            }
            Files.createSymbolicLink(dst, target);
            return true;
        } catch (Exception e) {
            message = " => ERROR : ClsSymLink.Copy() : EXCEPTION : " + e.getMessage();
            if (verbose > 1) {
                writeLine(MdlConst.LVL_NONE, message);
            }
            return false;
        }
    }

    /**
     * シンボリックリンクをコピーします（絶対パス固定）。
     *
     * @param sourcePath コピー元パス
     * @param destinationPath コピー先パス
     * @param overwrite 上書きフラグ
     * @return 成功時は true
     */
    public boolean copy(String sourcePath, String destinationPath, boolean overwrite) {
        return copy(sourcePath, destinationPath, overwrite, false);
    }

    /**
     * 指定したパスにシンボリックリンクを作成します。
     *
     * @param linkPath 作成するシンボリックリンクのパス
     * @param targetPath リンク先のターゲットパス
     * @param pathType パス種別
     * @param overwrite 上書きフラグ
     * @return 成功時は true
     */
    public boolean createSymbolicLink(String linkPath, String targetPath, int pathType, boolean overwrite) {
        try {
            if (linkPath == null || linkPath.isBlank() || targetPath == null || targetPath.isBlank()) {
                return false;
            }
            Path link = Path.of(linkPath);
            Path target = Path.of(targetPath);
            if (overwrite && Files.exists(link)) {
                Files.delete(link);
            }
            Files.createSymbolicLink(link, target);
            return true;
        } catch (Exception e) {
            message = " => ERROR : ClsSymLink.CreateSymbolicLink() : EXCEPTION : " + e.getMessage();
            if (verbose > 1) {
                writeLine(MdlConst.LVL_NONE, message);
            }
            return false;
        }
    }


    /**
     * ファイル・ディレクトリが存在し、かつシンボリックリンクである場合にその参照先の実パスを取得します。
     *
     * @param linkPath 対象のシンボリックリンクパス
     * @param isRelative 相対パスフラグ
     * @return 実パス文字列
     */
    public String getRealPathIfExists(String linkPath, boolean isRelative) {
        if (linkPath == null || linkPath.isBlank()) {
            return "";
        }
        message = "";

        switch (MdlFile.getPathType(linkPath)) {
            case MdlFile.PATH_IS_DIRECTORY:
            case MdlFile.PATH_IS_FILE:
                break;
            default:
                message = " => ERROR : ClsSymLink.GetRealPathIfExists() : NO SUCH A FILE OR DIRECTORY : " + linkPath;
                if (verbose > 1) {
                    writeLine(MdlConst.LVL_NONE, message);
                }
                return "";
        }
        if (!Files.isSymbolicLink(Path.of(linkPath))) {
            return "";
        }
        return getRealPath(linkPath, isRelative);
    }

    /**
     * ファイル・ディレクトリが存在し、かつシンボリックリンクである場合にその参照先の実パスを取得します（絶対パス固定）。
     *
     * @param linkPath 対象のシンボリックリンクパス
     * @return 実パス文字列
     */
    public String getRealPathIfExists(String linkPath) {
        return getRealPathIfExists(linkPath, false);
    }

    /**
     * 指定されたシンボリックリンクが参照しているターゲットの実パスを取得します。
     *
     * @param linkPath 対象のシンボリックリンクパス
     * @param isRelative 相対パスフラグ
     * @return 実パス文字列
     */
    public String getRealPath(String linkPath, boolean isRelative) {
        try {
            if (linkPath == null || linkPath.isBlank()) {
                return "";
            }
            Path path = Path.of(linkPath);
            if (!Files.isSymbolicLink(path)) {
                return "";
            }
            Path real = path.toRealPath();
            if (isRelative) {
                Path base = Path.of(".").toAbsolutePath().normalize();
                return base.relativize(real).toString();
            }
            return real.toString();
        } catch (Exception e) {
            return "";
        }
    }

    /**
     * 指定されたシンボリックリンクが参照しているターゲットの実パスを取得します（絶対パス固定）。
     *
     * @param linkPath 対象のシンボリックリンクパス
     * @return 実パス文字列
     */
    public String getRealPath(String linkPath) {
        return getRealPath(linkPath, false);
    }
}