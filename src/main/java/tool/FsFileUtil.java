package tool;

import java.io.File;
import java.time.Duration;
import java.time.LocalDateTime;
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlDate;
import tool.cmnclslib.mdl.MdlFile;

/**
 * FsFileUtil アプリケーションのメインエントリーポイントクラスです。
 */
public class FsFileUtil {

    /**
     * アプリケーションのメインメソッドです。
     *
     * @param args コマンドライン引数
     */
    public static void main(String[] args) {
        int exitCode = run(args);
        System.exit(exitCode);
    }

    /**
     * コマンドライン引数を解析し、各種ファイル操作処理を実行します。
     *
     * @param args コマンドライン引数
     * @return 終了コード（0: 正常, 10: 警告, 20: 異常など）
     */
    public static int run(String[] args) {
        LocalDateTime startTime = LocalDateTime.now();
        ClsLogger logger = new ClsLogger();
        ClsBaseDir prop = new ClsBaseDir();
        ClsAppArg argsParser = new ClsAppArg(logger, prop);
        int exitCode = MdlConst.LVL_I;
        boolean isSuccess = argsParser.parse(args);

        if (isSuccess) {
            if (prop.getVerbose() > 0) {
                logger.writeLine(MdlConst.LVL_NONE, "===<<< [" + argsParser.getExeBaseName() + "] START : " + MdlDate.getFormattedDate(startTime, "yyyy/MM/dd HH:mm:ss") + ">===");
            }
            if (prop.getVerbose() > 1) {
                argsParser.printDefinition();
            }
            if (argsParser.isUsage()) {
                exitCode = MdlConst.LVL_W;
                argsParser.showUsage();
            } else {
                // パスの文字列置換：-f
                if (prop.isNeedPathFr() && prop.getSourcePath() != null && !prop.getSourcePath().isBlank()) {
                    LocalDateTime timestamp = argsParser.getTimestamp(prop.getTsSource(), "", MdlFile.PATH_IS_NULL);
                    prop.setSourcePath(MdlDate.replaceWithDateTime(prop.getSourcePath().replace("%%", "%"), timestamp));

                    if (prop.getActionCode() != ClsBaseDir.ACTION_LS) {
                        if (MdlFile.getDirectoryPath(prop.getSourcePath()).isEmpty()) {
                            prop.setSourcePath(prop.getSourcePath() + File.separator + ".");
                        }
                    }

                    int pathType = MdlFile.getPathType(prop.getSourcePath());
                    if (pathType == MdlFile.PATH_IS_DIRECTORY || pathType == MdlFile.PATH_IS_FILE) {
                        prop.setPathType(pathType);
                    }
                }

                // パスの文字列置換：-t
                if (prop.getDestinationPath() != null && !prop.getDestinationPath().isBlank()) {
                    LocalDateTime timestamp = argsParser.getTimestamp(prop.getTsDestination(), prop.getSourcePath(), prop.getPathType());
                    prop.setDestinationPath(MdlDate.replaceWithDateTime(prop.getDestinationPath().replace("%%", "%"), timestamp));
                    if (MdlFile.getDirectoryPath(prop.getDestinationPath()).isEmpty()) {
                        prop.setDestinationPath(prop.getDestinationPath() + File.separator + ".");
                    }
                }

                // 上書きファイル退避先
                if (prop.isBackup() && prop.getBackupDir() != null && !prop.getBackupDir().isBlank()) {
                    LocalDateTime timestamp = argsParser.getTimestamp(prop.getTsBackup(), prop.getSourcePath(), prop.getPathType());
                    prop.setBackupDir(MdlDate.replaceWithDateTime(prop.getBackupDir().replace("%%", "%"), timestamp));
                    if (MdlFile.getDirectoryPath(prop.getBackupDir()).isEmpty()) {
                        prop.setBackupDir(prop.getBackupDir() + File.separator + ".");
                    }
                }

                // 処理の実行
                if (isSuccess && ClsBaseDir.ACTION_NONE != prop.getActionCode()) {
                    try {
                        ClsActionCtrl actionController = new ClsActionCtrl(logger, prop);
                        exitCode = actionController.execute();
                    } catch (Exception ex) {
                        exitCode = MdlConst.LVL_E;
                        logger.writeLine(MdlConst.LVL_NONE, "[ERR] CALL actionController.Execute() : " + ex.getMessage());
                        if (prop.isStackTrace()) {
                            logger.writeLine(MdlConst.LVL_NONE, "");
                            ex.printStackTrace();
                            logger.writeLine(MdlConst.LVL_NONE, "");
                        }
                    }
                }

                // 戻り値の調整
                if (ClsBaseDir.ACTION_NONE == prop.getActionCode()) {
                    if (MdlConst.LVL_I == exitCode && !isSuccess) {
                        exitCode = MdlConst.LVL_E;
                    }
                } else {
                    if (MdlConst.LVL_I == exitCode && !isSuccess) {
                        exitCode = MdlConst.LVL_W;
                    }
                }
            }

            if (prop.isCatRetWcl()) {
                exitCode = (int) Math.min(prop.getLines(), (long) MdlConst.INT_MAX);
            }
            if (prop.isRetFiles()) {
                exitCode = (int) Math.min(prop.getFiles(), (long) MdlConst.INT_MAX);
            }

            if (prop.getVerbose() > 0) {
                LocalDateTime endTime = LocalDateTime.now();
                double elapsedTime = (double) Duration.between(startTime, endTime).toMillis() / 1000.0;
                logger.writeLine(MdlConst.LVL_NONE, String.format("===<<< [%s] EXIT (%d) : %s : %.3f sec>>>===",
                        argsParser.getExeBaseName(), exitCode, MdlDate.getFormattedDate(endTime, "yyyy/MM/dd HH:mm:ss"), elapsedTime));
            }
        } else {
            exitCode = MdlConst.LVL_E;
            if (argsParser.isUsage()) {
                argsParser.showUsage();
            }
        }

        if (argsParser.isEchoRetcode()) {
            logger.writeLine(MdlConst.LVL_NONE, Integer.toString(exitCode));
        }

        return exitCode;
    }
}