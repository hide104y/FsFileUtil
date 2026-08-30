package tool;

import java.io.File;
import tool.cmnclslib.cls.ClsCmdExec;
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlFile;
import tool.cmnclslib.mdl.MdlUtil;

/**
 * コマンドライン指定に応じた各種ファイル操作アクション（検索、コピー、移動、同期、削除等）を制御するクラスです。
 */
public class ClsActionCtrl {

    private final ClsLogger logger;
    private final ClsBaseDir prop;
    private final ClsFind find;
    private final ClsFsDiffCopy fsDiffCopy;
    private final ClsFsUtil fsUtil;
    private final ClsSymLinkWrapper symLink;
    private final ClsCmdExec cmdExec;
    private final ClsFsAttrib fsAttrib;

    /**
     * ロガーおよび設定プロパティを指定して {@link ClsActionCtrl} の新しいインスタンスを初期化します。
     *
     * @param logger ログ出力に使用するロガーインスタンス
     * @param prop アプリケーション設定プロパティ
     */
    public ClsActionCtrl(ClsLogger logger, ClsBaseDir prop) {
        this.logger = logger != null ? logger : new ClsLogger();
        this.prop = prop != null ? prop : new ClsBaseDir();
        this.symLink = new ClsSymLinkWrapper(this.logger);
        this.cmdExec = new ClsCmdExec(this.logger);
        this.fsUtil = new ClsFsUtil(this.logger);
        this.fsAttrib = new ClsFsAttrib(this.logger);
        this.fsDiffCopy = new ClsFsDiffCopy(this.logger, this.prop, this.fsUtil, this.symLink);
        this.find = new ClsFind(this.logger, this.prop, this.fsUtil, this.fsDiffCopy);
        this.fsUtil.setVerbose(this.prop.getVerbose());
        this.fsUtil.setStackTrace(this.prop.isStackTrace());
        this.fsUtil.setWaitMSecForRetryCopy(this.prop.getWaitMSecForRetryCopy());
        this.fsUtil.setRetryMax(this.prop.getRetrySystemCopyMax());
    }

    /**
     * 設定プロパティのアクションコードに基づいて各種ファイル操作処理を実行します。
     *
     * @return 実行結果ステータスコード（0: 正常, 10: 警告, 20: 異常等）
     */
    public int execute() {
        int returnCode = MdlConst.LVL_I;
        boolean isOk = false;
        if (prop.isSymLink()) {
            symLink.setVerbose(prop.getVerbose());
        }

        switch (prop.getActionCode()) {
            case ClsBaseDir.ACTION_FIND:
            case ClsBaseDir.ACTION_COPY:
            case ClsBaseDir.ACTION_MOVE:
            case ClsBaseDir.ACTION_SYNC:
                if (prop.isFrPathCheck() && !MdlFile.pathExists(prop.getSourcePath())) {
                    if (prop.isSourceCheck()) {
                        logger.writeLine(MdlConst.LVL_NONE, "NG : NOT FOUND " + prop.getSourcePath());
                        return MdlConst.LVL_E;
                    } else {
                        logger.writeLine(MdlConst.LVL_NONE, "OK : NOT FOUND " + prop.getSourcePath());
                        return MdlConst.LVL_I;
                    }
                }
                break;
            default:
                break;
        }

        switch (prop.getActionCode()) {
            case ClsBaseDir.ACTION_FIND:
                if (prop.getVerbose() > 1) {
                    logger.writeLine(MdlConst.LVL_NONE, "START : FIND ------------------------------------------------------------");
                }
                returnCode = find.execute(ClsBaseDir.TASK_PRINT) ? MdlConst.LVL_I : MdlConst.LVL_E;
                if (prop.getVerbose() > 1) {
                    logger.writeLine(MdlConst.LVL_NONE, "E N D : FIND ------------------------------------------------------------");
                }
                break;

            case ClsBaseDir.ACTION_COPY:
                if (prop.getVerbose() > 1) {
                    logger.writeLine(MdlConst.LVL_NONE, "START : COPY ------------------------------------------------------------");
                }
                returnCode = find.execute(ClsBaseDir.TASK_CP) ? MdlConst.LVL_I : MdlConst.LVL_E;
                if (prop.getVerbose() > 1) {
                    logger.writeLine(MdlConst.LVL_NONE, "E N D : COPY ------------------------------------------------------------");
                }
                if (prop.getVerbose() > -1) {
                    String message = "=== COPY : NEW=" + fsDiffCopy.getCopyNewCount() + " UPDATE=" + fsDiffCopy.getCopyUpdateCount() + " SKIP=" + fsDiffCopy.getCopySkipCount() + " ERR=" + fsDiffCopy.getCopyErrorCount() + " / TOTAL=" + fsDiffCopy.getCopyTotalCount();
                    if (fsDiffCopy.getNotFoundCount() > 0) {
                        message += " / NOT FOUND=" + fsDiffCopy.getNotFoundCount();
                    }
                    logger.writeLine(MdlConst.LVL_NONE, message);
                } else if (prop.getVerbose() > -3) {
                    String message = "=== COPY : COPY=" + (fsDiffCopy.getCopyNewCount() + fsDiffCopy.getCopyUpdateCount()) + " SKIP=" + fsDiffCopy.getCopySkipCount() + " ERR=" + fsDiffCopy.getCopyErrorCount() + " / TOTAL=" + fsDiffCopy.getCopyTotalCount();
                    if (fsDiffCopy.getNotFoundCount() > 0) {
                        message += " / NOT FOUND=" + fsDiffCopy.getNotFoundCount();
                    }
                    logger.writeLine(MdlConst.LVL_NONE, message);
                }
                if (returnCode == MdlConst.LVL_I && fsDiffCopy.getCopyErrorCount() > 0) {
                    returnCode = MdlConst.LVL_E;
                }
                if (returnCode == MdlConst.LVL_I && fsDiffCopy.getMkdirNgCount() > 0) {
                    returnCode = MdlConst.LVL_W;
                }
                if (returnCode == MdlConst.LVL_I && fsDiffCopy.getNotFoundCount() > 0) {
                    returnCode = MdlConst.LVL_W;
                }
                prop.setFiles(fsDiffCopy.getCopyNewCount() + fsDiffCopy.getCopyUpdateCount());
                break;

            case ClsBaseDir.ACTION_MOVE:
                if (prop.getVerbose() > 1) {
                    logger.writeLine(MdlConst.LVL_NONE, "START : MOVE ------------------------------------------------------------");
                }
                isOk = find.execute(ClsBaseDir.TASK_MV);
                if (prop.getVerbose() > 1) {
                    logger.writeLine(MdlConst.LVL_NONE, "E N D : MOVE ------------------------------------------------------------");
                }
                returnCode = isOk ? MdlConst.LVL_I : MdlConst.LVL_E;
                if (prop.getVerbose() > -1) {
                    String message = "=== MOVE : NEW=" + fsDiffCopy.getCopyNewCount() + " UPDATE=" + fsDiffCopy.getCopyUpdateCount() + " SKIP=" + fsDiffCopy.getCopySkipCount() + " ERR=" + fsDiffCopy.getCopyErrorCount() + " / TOTAL=" + fsDiffCopy.getCopyTotalCount();
                    if (fsDiffCopy.getRmNgCount() > 0) {
                        message += " / DELETE FILE ERR=" + fsDiffCopy.getRmNgCount();
                    }
                    if (fsDiffCopy.getNotFoundCount() > 0) {
                        message += " / NOT FOUND=" + fsDiffCopy.getNotFoundCount();
                    }
                    logger.writeLine(MdlConst.LVL_NONE, message);
                } else if (prop.getVerbose() > -3) {
                    String message = "=== MOVE : MOVE=" + (fsDiffCopy.getCopyNewCount() + fsDiffCopy.getCopyUpdateCount()) + " SKIP=" + fsDiffCopy.getCopySkipCount() + " ERR=" + fsDiffCopy.getCopyErrorCount() + " / TOTAL=" + fsDiffCopy.getCopyTotalCount();
                    if (fsDiffCopy.getRmNgCount() > 0) {
                        message += " / DELETE FILE ERR=" + fsDiffCopy.getRmNgCount();
                    }
                    if (fsDiffCopy.getNotFoundCount() > 0) {
                        message += " / NOT FOUND=" + fsDiffCopy.getNotFoundCount();
                    }
                    logger.writeLine(MdlConst.LVL_NONE, message);
                }
                if (returnCode == MdlConst.LVL_I && fsDiffCopy.getCopyErrorCount() > 0) {
                    returnCode = MdlConst.LVL_E;
                }
                if (returnCode == MdlConst.LVL_I && fsDiffCopy.getMkdirNgCount() > 0) {
                    returnCode = MdlConst.LVL_W;
                }
                if (returnCode == MdlConst.LVL_I && fsDiffCopy.getNotFoundCount() > 0) {
                    returnCode = MdlConst.LVL_W;
                }
                prop.setFiles(fsDiffCopy.getCopyNewCount() + fsDiffCopy.getCopyUpdateCount());
                break;

            case ClsBaseDir.ACTION_SYNC:
                if (prop.getVerbose() > 1) {
                    logger.writeLine(MdlConst.LVL_NONE, "START : COPY ------------------------------------------------------------");
                }
                if (prop.isSyncRmOnly()) {
                    if (prop.getVerbose() > 1) {
                        logger.writeLine(MdlConst.LVL_NONE, " => SKIP (SYNC DELETE ONLY)");
                    }
                    isOk = true;
                } else {
                    isOk = find.execute(ClsBaseDir.TASK_CP);
                }
                if (prop.getVerbose() > 1) {
                    logger.writeLine(MdlConst.LVL_NONE, "E N D : COPY ------------------------------------------------------------");
                }
                if (isOk) {
                    if (prop.getVerbose() > 1) {
                        logger.writeLine(MdlConst.LVL_NONE, "START : SYNC DELETE -----------------------------------------------------");
                    }
                    if (prop.getVerbose() < -1) {
                        logger.writeLine(MdlConst.LVL_NONE, "");
                        logger.writeLine(MdlConst.LVL_NONE, "--- DELETE ---");
                    }
                    isOk = find.execute(ClsBaseDir.TASK_RM);
                    if (prop.getVerbose() > 1) {
                        logger.writeLine(MdlConst.LVL_NONE, "E N D : SYNC DELETE -----------------------------------------------------");
                    }
                }
                returnCode = isOk ? MdlConst.LVL_I : MdlConst.LVL_E;
                if (prop.getVerbose() > -1) {
                    String message = "=== COPY : NEW=" + fsDiffCopy.getCopyNewCount() + " UPDATE=" + fsDiffCopy.getCopyUpdateCount() + " SKIP=" + fsDiffCopy.getCopySkipCount() + " ERR=" + fsDiffCopy.getCopyErrorCount() + " / TOTAL=" + fsDiffCopy.getCopyTotalCount();
                    if (fsDiffCopy.getNotFoundCount() > 0) {
                        message += " / NOT FOUND=" + fsDiffCopy.getNotFoundCount();
                    }
                    logger.writeLine(MdlConst.LVL_NONE, message);
                } else if (prop.getVerbose() > -3) {
                    String message = "=== COPY : COPY=" + (fsDiffCopy.getCopyNewCount() + fsDiffCopy.getCopyUpdateCount()) + " SKIP=" + fsDiffCopy.getCopySkipCount() + " ERR=" + fsDiffCopy.getCopyErrorCount() + " / TOTAL=" + fsDiffCopy.getCopyTotalCount();
                    if (fsDiffCopy.getNotFoundCount() > 0) {
                        message += " / NOT FOUND=" + fsDiffCopy.getNotFoundCount();
                    }
                    logger.writeLine(MdlConst.LVL_NONE, message);
                }
                if (prop.getVerbose() > -3) {
                    logger.writeLine(MdlConst.LVL_NONE, "=== DEL  : DEL=" + fsDiffCopy.getRmOkCount() + " SKIP=" + fsDiffCopy.getRmSkipCount() + " ERR=" + fsDiffCopy.getRmNgCount() + " / TOTAL=" + fsDiffCopy.getRmTotalCount());
                }
                if (returnCode == MdlConst.LVL_I && fsDiffCopy.getCopyErrorCount() > 0) {
                    returnCode = MdlConst.LVL_E;
                }
                if (returnCode == MdlConst.LVL_I && fsDiffCopy.getMkdirNgCount() > 0) {
                    returnCode = MdlConst.LVL_W;
                }
                if (returnCode == MdlConst.LVL_I && fsDiffCopy.getRmNgCount() > 0) {
                    returnCode = MdlConst.LVL_W;
                }
                if (returnCode == MdlConst.LVL_I && fsDiffCopy.getNotFoundCount() > 0) {
                    returnCode = MdlConst.LVL_W;
                }
                prop.setFiles(fsDiffCopy.getCopyNewCount() + fsDiffCopy.getCopyUpdateCount());
                break;

            case ClsBaseDir.ACTION_MKDIR:
                returnCode = MdlFile.createDirectory(prop.getSourcePath());
                switch (returnCode) {
                    case MdlFile.OK_MKDIR_CREATE:
                        logger.writeLine(MdlConst.LVL_NONE, "OK : MKDIR " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_I;
                        break;
                    case MdlFile.OK_MKDIR_ALREADY_EXIST:
                        logger.writeLine(MdlConst.LVL_NONE, "OK : DIR ALREADY EXIST => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_I;
                        break;
                    case MdlFile.NG_MKDIR_FILE_EXIST:
                        logger.writeLine(MdlConst.LVL_NONE, "NG : SAME NAME FILE ALREADY EXIST => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                    case MdlFile.NG_MKDIR_WRONG_ARG:
                        logger.writeLine(MdlConst.LVL_NONE, "NG : INVALID ARGUMENT -f " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                    case MdlFile.NG_MKDIR:
                    default:
                        logger.writeLine(MdlConst.LVL_NONE, "NG : FAILED TO MKDIR " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                }
                break;

            case ClsBaseDir.ACTION_TOUCH:
                returnCode = MdlFile.createEmptyFile(prop.getSourcePath());
                switch (returnCode) {
                    case MdlFile.OK_TOUCH_CREATE:
                        logger.writeLine(MdlConst.LVL_NONE, "OK : TOUCH " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_I;
                        break;
                    case MdlFile.OK_TOUCH_ALREADY_EXIST:
                        logger.writeLine(MdlConst.LVL_NONE, "OK : FILE ALREADY EXIST => " + prop.getSourcePath());
                        try {
                            new File(prop.getSourcePath()).setLastModified(System.currentTimeMillis());
                            returnCode = MdlConst.LVL_I;
                        } catch (Exception e) {
                            returnCode = MdlConst.LVL_W;
                        }
                        break;
                    case MdlFile.NG_TOUCH_DIR_EXIST:
                        logger.writeLine(MdlConst.LVL_NONE, "NG : SAME NAME DIR ALREADY EXIST => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                    case MdlFile.NG_TOUCH_WRONG_ARG:
                        logger.writeLine(MdlConst.LVL_NONE, "NG : INVALID ARGUMENT -f " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                    case MdlFile.NG_TOUCH:
                    default:
                        logger.writeLine(MdlConst.LVL_NONE, "NG : FAILED TO TOUCH " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                }
                break;

            case ClsBaseDir.ACTION_DELETE:
                switch (MdlFile.getPathType(prop.getSourcePath())) {
                    case MdlFile.PATH_IS_DIRECTORY:
                        switch (prop.getTypeCode()) {
                            case MdlConst.INT_TYPE_ALL:
                            case MdlConst.INT_TYPE_DIRECTORY:
                                String typeText = prop.getTypeCode() == MdlConst.INT_TYPE_ALL ? "ALL" : "DIRECTORY";
                                if (fsDiffCopy.removeRecursive(prop.getSourcePath())) {
                                    logger.writeLine(MdlConst.LVL_NONE, "OK : DELETE " + typeText + " => " + prop.getSourcePath());
                                    returnCode = MdlConst.LVL_I;
                                } else {
                                    logger.writeLine(MdlConst.LVL_NONE, "NG : DELETE " + typeText + " => " + prop.getSourcePath());
                                    returnCode = MdlConst.LVL_E;
                                }
                                break;
                            default:
                                logger.writeLine(MdlConst.LVL_NONE, "NG : DELETE FILE => " + prop.getSourcePath() + "：NOT FILE, BUT DIRECTORY");
                                returnCode = MdlConst.LVL_E;
                                break;
                        }
                        break;
                    case MdlFile.PATH_IS_FILE:
                        switch (prop.getTypeCode()) {
                            case MdlConst.INT_TYPE_ALL:
                            case MdlConst.INT_TYPE_FILE:
                                String typeText = prop.getTypeCode() == MdlConst.INT_TYPE_ALL ? "ALL" : "FILE";
                                if (fsDiffCopy.removeRecursive(prop.getSourcePath())) {
                                    logger.writeLine(MdlConst.LVL_NONE, "OK : DELETE " + typeText + " => " + prop.getSourcePath());
                                    returnCode = MdlConst.LVL_I;
                                } else {
                                    logger.writeLine(MdlConst.LVL_NONE, "NG : DELETE " + typeText + " => " + prop.getSourcePath());
                                    returnCode = MdlConst.LVL_E;
                                }
                                break;
                            default:
                                logger.writeLine(MdlConst.LVL_NONE, "NG : DELETE DIRECTORY => " + prop.getSourcePath() + "：NOT DIRECTORY, BUT FILE");
                                returnCode = MdlConst.LVL_E;
                                break;
                        }
                        break;
                    default:
                        if (prop.isSourceCheck()) {
                            logger.writeLine(MdlConst.LVL_NONE, "NG : PATH NOT FOUND => " + prop.getSourcePath());
                            return MdlConst.LVL_E;
                        } else {
                            logger.writeLine(MdlConst.LVL_NONE, "OK : PATH NOT FOUND => " + prop.getSourcePath());
                            returnCode = MdlConst.LVL_I;
                        }
                        break;
                }
                break;

            case ClsBaseDir.ACTION_EXIST:
                if (MdlFile.pathExists(prop.getSourcePath())) {
                    if (prop.getCheckFileLock() > 0 && MdlFile.isFileLocked(prop.getSourcePath())) {
                        logger.writeLine(MdlConst.LVL_NONE, "NG : FILE LOCKED => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                    } else {
                        logger.writeLine(MdlConst.LVL_NONE, "OK : PATH FOUND => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_I;
                    }
                } else {
                    logger.writeLine(MdlConst.LVL_NONE, "NG : PATH NOT FOUND => " + prop.getSourcePath());
                    returnCode = MdlConst.LVL_E;
                }
                break;

            case ClsBaseDir.ACTION_EXIST_DIR:
                switch (MdlFile.getPathType(prop.getSourcePath())) {
                    case MdlFile.PATH_IS_DIRECTORY:
                        logger.writeLine(MdlConst.LVL_NONE, "OK : DIR FOUND => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_I;
                        break;
                    case MdlFile.PATH_IS_FILE:
                        logger.writeLine(MdlConst.LVL_NONE, "NG : SAME NAME FILE ALREADY EXIST => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                    default:
                        logger.writeLine(MdlConst.LVL_NONE, "NG : PATH NOT FOUND => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                }
                break;

            case ClsBaseDir.ACTION_EXIST_FILE:
                switch (MdlFile.getPathType(prop.getSourcePath())) {
                    case MdlFile.PATH_IS_DIRECTORY:
                        logger.writeLine(MdlConst.LVL_NONE, "NG : SAME NAME DIR ALREADY EXIST => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                    case MdlFile.PATH_IS_FILE:
                        logger.writeLine(MdlConst.LVL_NONE, "OK : FILE FOUND => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_I;
                        break;
                    default:
                        logger.writeLine(MdlConst.LVL_NONE, "NG : PATH NOT FOUND => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                }
                break;

            case ClsBaseDir.ACTION_WAIT:
                boolean isCheckFileLock = prop.getCheckFileLock() > 0;
                returnCode = fsUtil.waitUntilFileExists(prop.getSourcePath(), prop.getMaxLoop(), prop.getInterval(), isCheckFileLock) ? MdlConst.LVL_I : MdlConst.LVL_E;
                break;

            case ClsBaseDir.ACTION_FILE_LOCKED:
                if (MdlFile.pathExists(prop.getSourcePath())) {
                    if (MdlFile.isFileLocked(prop.getSourcePath())) {
                        logger.writeLine(MdlConst.LVL_NONE, "YES LOCKED(" + MdlConst.LVL_W + ") => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_W;
                    } else {
                        logger.writeLine(MdlConst.LVL_NONE, "NO LOCKED(" + MdlConst.LVL_I + ") => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_I;
                    }
                } else {
                    logger.writeLine(MdlConst.LVL_NONE, "NO SUCH A FILE OR DIRECTORY(" + MdlConst.LVL_E + ") => " + prop.getSourcePath());
                    returnCode = MdlConst.LVL_E;
                }
                break;

            case ClsBaseDir.ACTION_RENAME:
                if (!prop.getFileList().isEmpty()) {
                    if (prop.getVerbose() > 1) {
                        logger.writeLine(MdlConst.LVL_NONE, "START : RENAME ------------------------------------------------------------");
                    }
                    returnCode = find.execute(ClsBaseDir.TASK_RENAME) ? MdlConst.LVL_I : MdlConst.LVL_E;
                    if (prop.getVerbose() > 1) {
                        logger.writeLine(MdlConst.LVL_NONE, "E N D : RENAME ------------------------------------------------------------");
                    }
                    if (prop.getVerbose() > -1) {
                        String message = "=== RENAME : NEW=" + fsDiffCopy.getCopyNewCount() + " UPDATE=" + fsDiffCopy.getCopyUpdateCount() + " ERR=" + fsDiffCopy.getCopyErrorCount() + " / TOTAL=" + fsDiffCopy.getCopyTotalCount();
                        if (fsDiffCopy.getNotFoundCount() > 0) {
                            message += " / NOT FOUND=" + fsDiffCopy.getNotFoundCount();
                        }
                        logger.writeLine(MdlConst.LVL_NONE, message);
                    }
                    if (returnCode == MdlConst.LVL_I && fsDiffCopy.getCopyErrorCount() > 0) {
                        returnCode = MdlConst.LVL_E;
                    }
                    if (returnCode == MdlConst.LVL_I && fsDiffCopy.getMkdirNgCount() > 0) {
                        returnCode = MdlConst.LVL_W;
                    }
                    if (returnCode == MdlConst.LVL_I && fsDiffCopy.getNotFoundCount() > 0) {
                        returnCode = MdlConst.LVL_W;
                    }
                    prop.setFiles(fsDiffCopy.getCopyNewCount() + fsDiffCopy.getCopyUpdateCount());
                } else {
                    returnCode = fsUtil.rename(prop.getSourcePath(), prop.getDestinationPath()) ? MdlConst.LVL_I : MdlConst.LVL_E;
                }
                break;

            case ClsBaseDir.ACTION_ROTATE:
                returnCode = fsUtil.rotate(prop.getSourcePath(), prop.getMaxKeep());
                break;

            case ClsBaseDir.ACTION_MKLINK:
                switch (prop.getPathType()) {
                    case MdlFile.PATH_IS_DIRECTORY:
                    case MdlFile.PATH_IS_FILE:
                        symLink.setVerbose(3);
                        returnCode = symLink.createSymbolicLink(prop.getDestinationPath(), prop.getSourcePath(), prop.getPathType(), prop.getOverwriteLevel() > 0) ? MdlConst.LVL_I : MdlConst.LVL_E;
                        break;
                    default:
                        logger.writeLine(MdlConst.LVL_NONE, "NO SUCH A FILE OR DIRECTORY : -f " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                }
                break;

            case ClsBaseDir.ACTION_GET_REAL_PATH:
                isOk = true;
                String option = "";
                switch (prop.getPathType()) {
                    case MdlFile.PATH_IS_DIRECTORY:
                        option = "/D ";
                        break;
                    case MdlFile.PATH_IS_FILE:
                        break;
                    default:
                        if (prop.getVerbose() >= 0) {
                            logger.writeLine(MdlConst.LVL_NONE, "NO SUCH A FILE OR DIRECTORY : -f " + prop.getSourcePath());
                        }
                        isOk = false;
                        returnCode = MdlConst.LVL_E;
                        break;
                }
                if (isOk) {
                    symLink.setVerbose(0);
                    symLink.setSilent(true);
                    String targetPath = prop.getSourcePath();
                    String realPath;
                    if (MdlFile.isSymlink(targetPath)) {
                        realPath = symLink.getRealPath(targetPath, prop.isRelative());
                        if (realPath == null || realPath.isBlank()) {
                            if (prop.getVerbose() >= 0) {
                                logger.writeLine(MdlConst.LVL_NONE, "ERROR : UNABLE TO GET REAL PATH : " + targetPath);
                            }
                        } else {
                            if (prop.isDq()) {
                                realPath = "\"" + realPath + "\"";
                                targetPath = "\"" + targetPath + "\"";
                            }
                            if (prop.getVerbose() >= 0) {
                                boolean isWindows = System.getProperty("os.name", "").toLowerCase(java.util.Locale.ROOT).contains("win");
                                if (isWindows) {
                                    logger.writeLine(MdlConst.LVL_NONE, "mklink " + option + targetPath + " " + realPath);
                                } else {
                                    logger.writeLine(MdlConst.LVL_NONE, "ln -s " + realPath + " " + targetPath);
                                }
                            } else {
                                logger.writeLine(MdlConst.LVL_NONE, realPath);
                            }
                        }
                    } else {
                        realPath = MdlFile.getAbsolutePath(targetPath);
                        if (prop.isRelative()) {
                            realPath = MdlFile.getRelativePath(targetPath, realPath);
                        }
                        if (prop.isDq()) {
                            realPath = "\"" + realPath + "\"";
                        }
                        if (prop.getVerbose() >= 0) {
                            logger.writeLine(MdlConst.LVL_NONE, "Absolute path = " + realPath);
                        } else {
                            logger.writeLine(MdlConst.LVL_NONE, realPath);
                        }
                    }
                }
                break;

            case ClsBaseDir.ACTION_LS:
                if (MdlFile.pathExists(prop.getSourcePath())) {
                    if (prop.getTypeCode() == MdlConst.INT_TYPE_ALL || prop.getTypeCode() == MdlConst.INT_TYPE_DIRECTORY) {
                        for (String path : MdlFile.getSortedDirectories(prop.getSourcePath(), "*", false, prop.getSortType(), prop.isAscending(), prop.isShowDirList())) {
                            if (MdlFile.isValidDirDateTime(path, prop.isBefore(), prop.getBeforeTime(), prop.isAfter(), prop.getAfterTime())) {
                                String line = MdlFile.getDirInfoStr(path, prop.getVerbose(), prop.isDq());
                                logger.writeLine(MdlConst.LVL_NONE, line);
                                prop.setFiles(prop.getFiles() + 1);
                            }
                        }
                    }
                    if (prop.getTypeCode() == MdlConst.INT_TYPE_ALL || prop.getTypeCode() == MdlConst.INT_TYPE_FILE) {
                        for (String path : MdlFile.getSortedFiles(prop.getSourcePath(), "*", false, prop.getSortType(), prop.isAscending(), prop.isShowFileList())) {
                            if (MdlFile.isValidFileDateTime(path, prop.isBefore(), prop.getBeforeTime(), prop.isAfter(), prop.getAfterTime())) {
                                String line = MdlFile.getFileInfoString(path, prop.getVerbose(), prop.isDq());
                                boolean isHit = false;
                                String buff = "";
                                switch (prop.getCheckFileLock()) {
                                    case ClsBaseDir.CHECK_FILE_LOCK_SAMPLE:
                                        if (MdlFile.isFileLocked(path)) {
                                            isHit = true;
                                        } else {
                                            buff = "[NOLOCKED]";
                                        }
                                        break;
                                    case ClsBaseDir.CHECK_FILE_LOCK_SKIP:
                                        if (MdlFile.isFileLocked(path)) {
                                            buff = "[LOCKED]";
                                        } else {
                                            isHit = true;
                                        }
                                        break;
                                    default:
                                        isHit = true;
                                        break;
                                }
                                if (isHit) {
                                    logger.writeLine(MdlConst.LVL_NONE, line);
                                    prop.setFiles(prop.getFiles() + 1);
                                } else {
                                    if (prop.getVerbose() > 4) {
                                        logger.writeLine(MdlConst.LVL_NONE, buff + line);
                                    }
                                }
                            }
                        }
                    }
                    returnCode = MdlConst.LVL_I;
                } else {
                    logger.writeLine(MdlConst.LVL_NONE, "NG : PATH NOT FOUND => " + prop.getSourcePath());
                    returnCode = MdlConst.LVL_E;
                }
                break;

            case ClsBaseDir.ACTION_GET_SIZE:
                if (prop.isProgress()) {
                    fsAttrib.setProgressEnabled(prop.isProgress());
                    fsAttrib.setProgressIntervalDirectories(Math.max(0, prop.getProgressIntervalDirs()));
                    fsAttrib.setProgressIntervalFiles(Math.max(0, prop.getProgressIntervalFiles()));
                }
                fsAttrib.clearCounter();

                switch (MdlFile.getPathType(prop.getSourcePath())) {
                    case MdlFile.PATH_IS_DIRECTORY:
                        if (fsAttrib.calculateDirectorySize(prop.getSourcePath(), prop.isSymLink(), prop.getVerbose(), prop.isStackTrace())) {
                            returnCode = MdlConst.LVL_I;
                        } else {
                            returnCode = MdlConst.LVL_E;
                            logger.writeLine(MdlConst.LVL_NONE, "ERRORS : DIRS=" + fsAttrib.getErrorDirectoryCount() + " / FILES=" + fsAttrib.getErrorFileCount());
                        }
                        break;
                    case MdlFile.PATH_IS_FILE:
                        if (fsAttrib.calculateFileSize(prop.getSourcePath(), prop.isSymLink(), prop.getVerbose(), prop.isStackTrace())) {
                            returnCode = MdlConst.LVL_I;
                        } else {
                            returnCode = MdlConst.LVL_E;
                        }
                        break;
                    default:
                        logger.writeLine(MdlConst.LVL_NONE, "NG : PATH NOT FOUND => " + prop.getSourcePath());
                        returnCode = MdlConst.LVL_E;
                        break;
                }
                StringBuilder sizeLine = new StringBuilder();
                if (prop.isShowPath()) {
                    sizeLine.append(sizeLine.length() == 0 ? prop.getSourcePath() : "," + prop.getSourcePath());
                }
                if (prop.isShowDirNum()) {
                    sizeLine.append(sizeLine.length() == 0 ? fsAttrib.getDirectoryCount() : "," + fsAttrib.getDirectoryCount());
                }
                if (prop.isShowFileNum()) {
                    sizeLine.append(sizeLine.length() == 0 ? fsAttrib.getFileCount() : "," + fsAttrib.getFileCount());
                }
                if (prop.isShowSize()) {
                    sizeLine.append(sizeLine.length() == 0 ? fsAttrib.getTotalSize() : "," + fsAttrib.getTotalSize());
                }
                logger.writeLine(MdlConst.LVL_NONE, sizeLine.toString());
                break;

            case ClsBaseDir.ACTION_GET_PERM:
            case ClsBaseDir.ACTION_GET_OWNER:
                if (prop.isShowOwner() && !fsAttrib.outputDirectoryOwner(prop.getSourcePath(), prop.getVerbose(), prop.isShowPath(), prop.isStackTrace())) {
                    returnCode = MdlConst.LVL_E;
                }
                if (prop.isShowPerm() && !fsAttrib.outputDirectoryPermission(prop.getSourcePath(), prop.getVerbose(), prop.isShowPath(), prop.isStackTrace())) {
                    returnCode = MdlConst.LVL_E;
                }
                break;

            case ClsBaseDir.ACTION_EXEC:
                cmdExec.setShowCmd(prop.isShowCmd());
                cmdExec.setShowExitCode(prop.isShowExitCode());
                cmdExec.setShowOutput(prop.isShowOutput());
                cmdExec.setVerbose(prop.getVerbose());
                cmdExec.setStackTrace(prop.isStackTrace());
                cmdExec.setShowEmptyLine(false);
                if (prop.isCatRetWcl()) {
                    cmdExec.setNotShowExitCode(true);
                }
                cmdExec.setWarnThreshold(prop.getWarnThreshold());
                cmdExec.setErrorThreshold(prop.getErrorThreshold());
                cmdExec.setErrorAtNegativeValue(prop.isErrorAtNegativeValue());
                cmdExec.setAlwaysNormal(prop.isAlwaysNormal());
                cmdExec.setTimeout(prop.getTimeout());
                cmdExec.initialize();

                boolean isWindows = System.getProperty("os.name", "").toLowerCase(java.util.Locale.ROOT).contains("win");
                if (prop.isCat()) {
                    String defaultCatName = isWindows ? "cat.exe" : "cat";
                    cmdExec.setCmdPath(prop.getCmdPath() == null || prop.getCmdPath().isBlank() ? prop.getExeDir() + File.separator + defaultCatName : prop.getCmdPath());
                    StringBuilder sbArgs = new StringBuilder(" -f \"" + prop.getSourcePath() + "\"");
                    if (prop.getCatI() != null && !prop.getCatI().isBlank()) {
                        sbArgs.append(" -i \"").append(prop.getCatI()).append("\"");
                    }
                    if (prop.getCatX() != null && !prop.getCatX().isBlank()) {
                        sbArgs.append(" -x \"").append(prop.getCatX()).append("\"");
                    }
                    if (prop.getCatP() != null && !prop.getCatP().isBlank()) {
                        sbArgs.append(" -p ").append(prop.getCatP());
                    }
                    if (prop.getCatE() != null && !prop.getCatE().isBlank()) {
                        sbArgs.append(" -e ").append(prop.getCatE());
                    }
                    if (prop.getCatXmlNl() != null && !prop.getCatXmlNl().isBlank()) {
                        sbArgs.append(" -xml-nl \"").append(prop.getCatXmlNl()).append("\"");
                    }
                    if (prop.getCatOptions() != null && !prop.getCatOptions().isBlank()) {
                        sbArgs.append(" ").append(prop.getCatOptions());
                    }
                    if (prop.isCatRetWcl()) {
                        sbArgs.append(" -ret-wcl");
                    }
                    cmdExec.setCmdArgs(sbArgs.toString());

                    returnCode = cmdExec.executeThread(prop.getPriority());
                    if (returnCode != 0) {
                        if (prop.isCatRetWcl() && returnCode > 0) {
                            prop.setFiles(prop.getFiles() + 1);
                            prop.setLines(prop.getLines() + returnCode);
                        } else {
                            logger.writeLine(MdlConst.LVL_NONE, "[ERR] Cmd Return Code != 0 : " + cmdExec.getCmdPath() + " " + cmdExec.getCmdArgs());
                        }
                    } else {
                        if (!prop.isCatRetWcl()) {
                            prop.setFiles(prop.getFiles() + 1);
                        }
                    }
                    returnCode = cmdExec.getMethodExitStatus();
                } else {
                    String cmdArg = (prop.getCmdArgs() == null || prop.getCmdArgs().isBlank())
                            ? MdlFile.replacePathForCmd(prop.getCmdPath(), prop.getSourcePath(), prop.getSourcePath(), ".", prop.isDq(), prop.getVerbose())
                            : MdlFile.replacePathForCmd(prop.getCmdPath() + " " + prop.getCmdArgs(), prop.getSourcePath(), prop.getSourcePath(), ".", prop.isDq(), prop.getVerbose());

                    switch (prop.getExecModeCode()) {
                        case ClsBaseDir.EXEC_MODE_CMD:
                            if (isWindows) {
                                cmdExec.setCmdPath(System.getenv("ComSpec") != null ? System.getenv("ComSpec") : "cmd.exe");
                                cmdExec.setCmdArgs("/c " + cmdArg);
                            } else {
                                String shell = System.getenv("SHELL");
                                cmdExec.setCmdPath(shell != null && !shell.isBlank() ? shell : "/bin/sh");
                                cmdExec.setCmdArgs("-c \"" + cmdArg.replace("\"", "\\\"") + "\"");
                            }
                            break;
                        case ClsBaseDir.EXEC_MODE_PS:
                            String psCmd = isWindows ? "powershell" : "pwsh";
                            cmdExec.setCmdPath(psCmd);
                            cmdExec.setCmdArgs("-NoProfile -command \"" + cmdArg + "; exit $LASTEXITCODE\"");
                            break;
                        default:
                            cmdExec.setCmdPath(MdlUtil.getRegexTarget(cmdArg, "^(?<TARGET>\\S+)\\s+.*"));
                            cmdExec.setCmdArgs(MdlUtil.getRegexTarget(cmdArg, "^\\S+\\s+(?<TARGET>.*)"));
                            break;
                    }
                    returnCode = cmdExec.executeThread(prop.getPriority());
                    if (returnCode != 0) {
                        logger.writeLine(MdlConst.LVL_NONE, "[ERR] Cmd Return Code != 0 : " + cmdExec.getCmdPath() + " " + cmdExec.getCmdArgs());
                    } else {
                        prop.setFiles(prop.getFiles() + 1);
                    }
                    returnCode = cmdExec.getMethodExitStatus();
                }
                break;
            default:
                break;
        }
        return returnCode;
    }
}
