package tool;

import java.io.File;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;
import java.util.regex.Pattern;
import tool.cmnclslib.cls.ClsCmdExec;
import tool.cmnclslib.cls.ClsLogger;
import tool.cmnclslib.mdl.MdlConst;
import tool.cmnclslib.mdl.MdlFile;
import tool.cmnclslib.mdl.MdlUtil;

/**
 * ファイルおよびディレクトリの検索・複製・移動・削除・一覧表示などのファイルシステム操作を実行するクラスです。
 */
public class ClsFind {

    private final ClsLogger logger;
    private final ClsBaseDir prop;
    private final ClsFsDiffCopy fsDiffCopy;
    private final ClsCmdExec cmdExec;
    private final ClsFsUtil fsUtil;

    /**
     * ロガー、設定プロパティ、ファイルシステムユーティリティ、差分コピー処理インスタンスを指定して {@link ClsFind} の新しいインスタンスを初期化します。
     *
     * @param log ログ出力に使用するロガーインスタンス
     * @param prop アプリケーション設定プロパティ
     * @param fsUtil ファイルシステムユーティリティ
     * @param diffCopy 差分コピー処理インスタンス
     */
    public ClsFind(ClsLogger log, ClsBaseDir prop, ClsFsUtil fsUtil, ClsFsDiffCopy diffCopy) {
        this.logger = log != null ? log : new ClsLogger();
        this.prop = prop != null ? prop : new ClsBaseDir();
        this.fsUtil = fsUtil != null ? fsUtil : new ClsFsUtil(this.logger);
        this.fsDiffCopy = diffCopy != null ? diffCopy : new ClsFsDiffCopy(this.logger, this.prop, this.fsUtil, new ClsSymLinkWrapper(this.logger));
        this.cmdExec = new ClsCmdExec(this.logger);
    }

    /**
     * 指定されたタスク種別（コピー、移動、削除、一覧表示等）に基づいてファイル探索および操作を実行します。
     *
     * @param task タスク種別コード（TASK_CP, TASK_MV, TASK_RM, TASK_PRINT 等）
     * @return 処理がすべて成功した場合は true、エラーが発生した場合は false
     */
    public boolean execute(int task) {
        boolean isSuccess = false;
        prop.setTask(task);
        fsDiffCopy.getProperties().setTask(task);

        if ((prop.getCmdPath() != null && !prop.getCmdPath().isBlank()) || prop.isCat()) {
            prop.setExecCmd(true);
            cmdExec.setShowCmd(prop.isShowCmd());
            cmdExec.setShowExitCode(prop.isShowExitCode());
            cmdExec.setShowOutput(prop.isShowOutput());
            cmdExec.setVerbose(prop.getVerbose());
            cmdExec.setStackTrace(prop.isStackTrace());
            cmdExec.setShowEmptyLine(false);

            if (prop.isCatRetWcl()) {
                cmdExec.setNotShowExitCode(true);
            }
            if (prop.getWorkDir() != null && !prop.getWorkDir().isBlank()) {
                cmdExec.setWorkDir(prop.getWorkDir());
            }

            cmdExec.setWarnThreshold(prop.getWarnThreshold());
            cmdExec.setErrorThreshold(prop.getErrorThreshold());
            cmdExec.setErrorAtNegativeValue(prop.isErrorAtNegativeValue());
            cmdExec.setAlwaysNormal(prop.isAlwaysNormal());
            cmdExec.setTimeout(prop.getTimeout());
            cmdExec.initialize();
        }

        String sourcePath = "";
        String destinationPath = "";
        switch (task) {
            case ClsBaseDir.TASK_CP:
            case ClsBaseDir.TASK_MV:
                sourcePath = prop.getSourcePath();
                destinationPath = prop.getDestinationPath();
                break;
            case ClsBaseDir.TASK_RM:
                sourcePath = prop.getDestinationPath();
                destinationPath = prop.getSourcePath();
                break;
            case ClsBaseDir.TASK_PRINT:
                sourcePath = prop.getSourcePath();
                destinationPath = prop.getSourcePath();
                break;
            default:
                break;
        }

        try {
            if (sourcePath == null || sourcePath.isBlank()) {
                if (!prop.getFileList().isEmpty()) {
                    isSuccess = executeFileList();
                }
            } else {
                switch (prop.getPathType()) {
                    case MdlFile.PATH_IS_DIRECTORY:
                        if (!prop.getFileList().isEmpty()) {
                            isSuccess = executeFileList();
                        } else {
                            isSuccess = processDirectoryRecursive(sourcePath, destinationPath, "", 0, 0);
                        }
                        break;
                    case MdlFile.PATH_IS_FILE:
                        isSuccess = fsDiffCopy.copy(sourcePath, destinationPath, MdlFile.getFileName(sourcePath), MdlFile.PATH_IS_FILE, -1);
                        break;
                    default:
                        break;
                }
            }
        } catch (Exception ex) {
            isSuccess = false;
            logger.writeLine(MdlConst.LVL_NONE, "[ERR] ClsFind.Execute() 1 : " + ex.getMessage() + " : " + destinationPath);
            if (prop.isStackTrace()) {
                logger.writeLine(MdlConst.LVL_NONE, "");
                ex.printStackTrace();
                logger.writeLine(MdlConst.LVL_NONE, "");
            }
        }
        return isSuccess;
    }

    private boolean processDirectoryRecursive(String sourcePath, String destinationPath, String relativePath, long currentDepth, int previousEffective) {
        boolean isSuccess = true;
        int currentEffective = previousEffective;

        if (currentDepth >= prop.getMinDepth()) {
            if (currentDepth > prop.getMaxDepth()) {
                return true;
            }

            boolean isAvailable = true;
            if (prop.isDirTerm() && !MdlFile.isValidDirDateTime(sourcePath, prop.isBefore(), prop.getBeforeTime(), prop.isAfter(), prop.getAfterTime())) {
                isAvailable = false;
            }

            if (isAvailable) {
                try {
                    boolean isSymlinkDirectory = prop.isSymLink() && Files.isSymbolicLink(Path.of(sourcePath));

                    if (prop.getShowCurDir() > 0 && currentDepth <= prop.getShowCurDir()) {
                        int progressSize = 86;
                        String strMessage = "=====< [" + currentDepth + "] " + sourcePath + " >=====";
                        if (MdlUtil.getShiftJisByteCount(strMessage) < progressSize) {
                            strMessage = String.format("%-" + progressSize + "s", strMessage);
                            logger.setValueByKey(ClsLogger.IS_TRIM_CONSOLE, "false");
                        } else {
                            logger.setValueByKey(ClsLogger.IS_TRIM_CONSOLE, "true");
                        }
                        logger.writeLine(MdlConst.LVL_NONE, strMessage);
                    }

                    int filterResult = evaluatePathFilterCode(relativePath, prop.isRegIncBasename(), prop.isRegExcBasename(), prop.getIncDirsList(), prop.getExcDirsList(), prop.isDirFilterOr(), prop.getVerbose());
                    currentEffective = combineFilterFlags(currentEffective, filterResult, prop.isDirFilterOr(), prop.isIncHitRecursive(), prop.isExcHitRecursive());

                    if (currentDepth > 0 && currentEffective > 1 && prop.isExcHitRecursive()) {
                        return true;
                    }

                    if (currentEffective == 1 || prop.isXdOnlyFiles()) {
                        switch (prop.getTask()) {
                            case ClsBaseDir.TASK_PRINT:
                                if (currentEffective == 1 && (prop.getTypeCode() == MdlConst.INT_TYPE_ALL || prop.getTypeCode() == MdlConst.INT_TYPE_DIRECTORY)) {
                                    if (prop.isExecCmd()) {
                                        String cmdArg = (prop.getCmdArgs() == null || prop.getCmdArgs().isBlank())
                                                ? MdlFile.replacePathForCmd(prop.getCmdPath(), sourcePath, prop.getSourcePath(), relativePath, prop.isDq(), prop.getVerbose())
                                                : MdlFile.replacePathForCmd(prop.getCmdPath() + " " + prop.getCmdArgs(), sourcePath, prop.getSourcePath(), relativePath, prop.isDq(), prop.getVerbose());

                                        boolean isWindows = System.getProperty("os.name", "").toLowerCase(java.util.Locale.ROOT).contains("win");
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

                                        if (cmdExec.executeThread(prop.getPriority()) != 0) {
                                            logger.writeLine(MdlConst.LVL_NONE, "[ERR][ProcessDirectoryRecursive()-TASK_PRINT] Cmd Return Code != 0 : " + cmdExec.getCmdPath() + " " + cmdExec.getCmdArgs());
                                        } else {
                                            prop.setFiles(prop.getFiles() + 1);
                                        }
                                    } else {
                                        String line = MdlFile.getFileInfoString(sourcePath, prop.getVerbose(), prop.isDq());
                                        logger.writeLine(MdlConst.LVL_NONE, line);
                                        prop.setFiles(prop.getFiles() + 1);
                                    }
                                }
                                break;

                            case ClsBaseDir.TASK_CP:
                                if (currentEffective == 1 || prop.isXdOnlyFiles()) {
                                    if (!fsDiffCopy.copy(sourcePath, destinationPath, relativePath, MdlFile.PATH_IS_DIRECTORY, isSymlinkDirectory ? 1 : 0)) {
                                        isSuccess = false;
                                    }
                                }
                                break;

                            case ClsBaseDir.TASK_MV:
                                if (currentEffective == 1 || prop.isXdOnlyFiles()) {
                                    if (fsDiffCopy.copy(sourcePath, destinationPath, relativePath, MdlFile.PATH_IS_DIRECTORY, isSymlinkDirectory ? 1 : 0)) {
                                        File srcDir = new File(sourcePath);
                                        File[] list = srcDir.listFiles();
                                        if (list == null || list.length == 0) {
                                            return true;
                                        }
                                    } else {
                                        isSuccess = false;
                                    }
                                }
                                if (!MdlFile.pathExists(sourcePath)) {
                                    return isSuccess;
                                }
                                break;

                            case ClsBaseDir.TASK_RM:
                                if (currentDepth > 0 && !prop.isFlat()) {
                                    if (currentEffective == 1) {
                                        if (!MdlFile.pathExists(destinationPath) && fsDiffCopy.removeRecursive(sourcePath, relativePath, isSymlinkDirectory)) {
                                            return true;
                                        }
                                    } else if (prop.isRmNohit() && fsDiffCopy.removeRecursive(sourcePath, relativePath, isSymlinkDirectory)) {
                                        return true;
                                    }
                                }
                                break;
                            default:
                                break;
                        }

                        if (!isSymlinkDirectory && !processCurrentDirectoryFiles(sourcePath, destinationPath, relativePath, currentDepth)) {
                            isSuccess = false;
                        }
                    }

                    if (isSymlinkDirectory) {
                        return isSuccess;
                    }
                } catch (Exception exception) {
                    isSuccess = false;
                    logger.writeLine(MdlConst.LVL_NONE, "[ERR] ClsFind.ProcessDirectoryRecursive() 1 : " + exception.getMessage() + " : " + relativePath);
                    if (prop.isStackTrace()) {
                        logger.writeLine(MdlConst.LVL_NONE, "");
                        exception.printStackTrace();
                        logger.writeLine(MdlConst.LVL_NONE, "");
                    }
                }
            }
        }

        if (prop.getTask() == ClsBaseDir.TASK_PRINT && !MdlFile.pathExists(sourcePath)) {
            return true;
        }

        for (String directoryPath : MdlFile.getSortedDirectories(sourcePath, "*", false, prop.getSortType(), prop.isAscending(), prop.isShowDirList())) {
            try {
                String subDirName = MdlFile.getFileName(directoryPath);
                String relativePathNext = (currentDepth == 0)
                        ? subDirName
                        : relativePath + File.separator + subDirName;

                String sourcePathNext = sourcePath + File.separator + subDirName;
                String destinationPathNext = prop.isFlat() ? destinationPath : destinationPath + File.separator + subDirName;

                if (!processDirectoryRecursive(sourcePathNext, destinationPathNext, relativePathNext, currentDepth + 1, currentEffective)) {
                    isSuccess = false;
                }

                if (prop.getTask() == ClsBaseDir.TASK_MV) {
                    if (!MdlFile.pathExists(sourcePathNext)) {
                        continue;
                    }
                }
            } catch (Exception ex) {
                isSuccess = false;
                logger.writeLine(MdlConst.LVL_NONE, "[ERR] ClsFind.ProcessDirectoryRecursive() 2 : " + ex.getMessage() + " : " + relativePath);
            }
        }

        return isSuccess;
    }

    private boolean processCurrentDirectoryFiles(String sourcePath, String destinationPath, String relativePath, long currentDepth) {
        if (!prop.isFileCopy() && !prop.isSyncRmOnly()) {
            return true;
        }
        if (prop.getTask() == ClsBaseDir.TASK_PRINT && !MdlFile.pathExists(sourcePath)) {
            return true;
        }

        boolean isSuccess = true;
        for (String sourceFilePath : MdlFile.getSortedFiles(sourcePath, "*", false, prop.getSortType(), prop.isAscending(), prop.isShowFileList())) {
            String fileName = MdlFile.getFileName(sourceFilePath);
            String relativeFilePath = relativePath.isEmpty() ? fileName : relativePath + File.separator + fileName;
            String sourceFileFullPath = sourcePath + File.separator + fileName;
            String destFileFullPath = destinationPath + File.separator + fileName;

            if (!processFile(sourceFileFullPath, destFileFullPath, relativeFilePath)) {
                isSuccess = false;
            }
        }
        return isSuccess;
    }

    public boolean executeFileList() {
        if (!prop.isFileCopy() && !prop.isSyncRmOnly()) {
            return true;
        }

        boolean isSuccess = true;
        Pattern regex = Pattern.compile(prop.getFileListRegex() != null && !prop.getFileListRegex().isBlank() ? prop.getFileListRegex() : "\\s+");

        for (String fileElement : prop.getFileList()) {
            String sourceFilePath = "";
            String destinationFilePath = "";
            String relativeFilePath = fileElement;

            String[] filePaths = regex.split(fileElement);
            switch (prop.getFilesTypeCode()) {
                case ClsBaseDir.FILES_RELATIVE:
                    if (filePaths.length > 0) {
                        sourceFilePath = prop.getSourcePath() + File.separator + filePaths[0].trim();
                        destinationFilePath = prop.getDestinationPath() + File.separator + filePaths[0].trim();
                        relativeFilePath = filePaths[0].trim();
                    }
                    if (filePaths.length > 1) {
                        destinationFilePath = prop.getDestinationPath() + File.separator + filePaths[1].trim();
                    }
                    break;

                case ClsBaseDir.FILES_FULL:
                    if (filePaths.length > 0) {
                        sourceFilePath = filePaths[0].trim();
                        relativeFilePath = MdlFile.getFileName(sourceFilePath);
                    }
                    if (filePaths.length > 1) {
                        destinationFilePath = filePaths[1].trim();
                    }
                    break;
                default:
                    break;
            }

            switch (MdlFile.getPathType(sourceFilePath)) {
                case MdlFile.PATH_IS_DIRECTORY:
                    if (!processDirectoryRecursive(sourceFilePath, destinationFilePath, relativeFilePath, 0, 0)) {
                        isSuccess = false;
                    }
                    break;

                case MdlFile.PATH_IS_FILE:
                    if (!processFile(sourceFilePath, destinationFilePath, relativeFilePath)) {
                        isSuccess = false;
                    }
                    break;

                default:
                    if (prop.getTask() != ClsBaseDir.TASK_RM) {
                        fsDiffCopy.setNotFoundCount(fsDiffCopy.getNotFoundCount() + 1);
                        if (prop.getVerbose() > 1) {
                            fsDiffCopy.echoTitle("[ERR] NO SUCH A FILE OR DIRECTORY : " + sourceFilePath);
                        }
                    }
                    break;
            }
        }
        return isSuccess;
    }

    private boolean processFile(String sourceFilePath, String destinationFilePath, String relativePath) {
        if (!prop.isFileCopy() && !prop.isSyncRmOnly()) {
            return true;
        }

        boolean isSuccess = true;
        try {
            boolean isSymlinkFile = prop.isSymLink() && Files.isSymbolicLink(Path.of(sourceFilePath));
            boolean isDateValid = MdlFile.isValidFileDateTime(sourceFilePath, prop.isBefore(), prop.getBeforeTime(), prop.isAfter(), prop.getAfterTime());
            boolean isSizeValid = true;

            if (prop.getCompOpe() != ClsBaseDir.COMPARISON_NO) {
                File fileInfo = new File(sourceFilePath);
                switch (prop.getCompOpe()) {
                    case ClsBaseDir.COMPARISON_GE:
                        if (fileInfo.length() < prop.getCompSize()) {
                            isSizeValid = false;
                        }
                        break;
                    case ClsBaseDir.COMPARISON_LE:
                        if (fileInfo.length() > prop.getCompSize()) {
                            isSizeValid = false;
                        }
                        break;
                    default:
                        break;
                }
            }

            String fileName = MdlFile.getFileName(sourceFilePath);
            boolean isFilterValid = isPathFilterMatched(fileName, true, true, prop.getIncFilesList(), prop.getExcFilesList(), false, prop.getVerbose());
            boolean isFileNotLocked = true;

            switch (prop.getCheckFileLock()) {
                case ClsBaseDir.CHECK_FILE_LOCK_SAMPLE:
                    if (!MdlFile.isFileLocked(sourceFilePath)) {
                        isFileNotLocked = false;
                        if (prop.getVerbose() > 4) {
                            fsDiffCopy.echoTitle("[---] SKIP : FILE IS NOT LOCKED : " + sourceFilePath);
                        }
                    }
                    break;
                case ClsBaseDir.CHECK_FILE_LOCK_SKIP:
                    if (MdlFile.isFileLocked(sourceFilePath)) {
                        isFileNotLocked = false;
                        if (prop.getVerbose() > 4) {
                            fsDiffCopy.echoTitle("[---] SKIP : FILE IS LOCKED : " + sourceFilePath);
                        }
                    }
                    break;
                default:
                    break;
            }

            if (isDateValid && isFilterValid && isSizeValid && isFileNotLocked) {
                boolean isCopySuccess;
                switch (prop.getTask()) {
                    case ClsBaseDir.TASK_CP:
                    case ClsBaseDir.TASK_RENAME:
                        isCopySuccess = prop.isReverse()
                                ? fsDiffCopy.copy(destinationFilePath, sourceFilePath, relativePath, MdlFile.PATH_IS_FILE, -1)
                                : fsDiffCopy.copy(sourceFilePath, destinationFilePath, relativePath, MdlFile.PATH_IS_FILE, -1);
                        if (!isCopySuccess) {
                            isSuccess = false;
                        }
                        break;

                    case ClsBaseDir.TASK_MV:
                        isCopySuccess = prop.isReverse()
                                ? fsDiffCopy.copy(destinationFilePath, sourceFilePath, relativePath, MdlFile.PATH_IS_FILE, -1)
                                : fsDiffCopy.copy(sourceFilePath, destinationFilePath, relativePath, MdlFile.PATH_IS_FILE, -1);
                        if (isCopySuccess) {
                            fsDiffCopy.setRmTotalCount(fsDiffCopy.getRmTotalCount() + 1);
                            if (!fsDiffCopy.removeRecursive(sourceFilePath, relativePath, isSymlinkFile)) {
                                fsDiffCopy.setRmNgCount(fsDiffCopy.getRmNgCount() + 1);
                                isSuccess = false;
                            }
                        } else {
                            isSuccess = false;
                        }
                        break;

                    case ClsBaseDir.TASK_RM:
                        if (MdlFile.pathExists(destinationFilePath)) {
                            fsDiffCopy.setRmTotalCount(fsDiffCopy.getRmTotalCount() + 1);
                            fsDiffCopy.setRmSkipCount(fsDiffCopy.getRmSkipCount() + 1);
                        } else {
                            isCopySuccess = fsDiffCopy.removeRecursive(sourceFilePath, relativePath, isSymlinkFile);
                            if (!isCopySuccess) {
                                isSuccess = false;
                            }
                        }
                        break;

                    case ClsBaseDir.TASK_PRINT:
                        if (prop.getTypeCode() == MdlConst.INT_TYPE_ALL || prop.getTypeCode() == MdlConst.INT_TYPE_FILE) {
                            boolean isWindows = System.getProperty("os.name", "").toLowerCase(java.util.Locale.ROOT).contains("win");
                            if (prop.isCat()) {
                                String defaultCatName = isWindows ? "cat.exe" : "cat";
                                cmdExec.setCmdPath(prop.getCmdPath() == null || prop.getCmdPath().isBlank() ? prop.getExeDir() + File.separator + defaultCatName : prop.getCmdPath());
                                StringBuilder sbArgs = new StringBuilder(" -f \"" + sourceFilePath + "\"");
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

                                int cmdReturn = cmdExec.executeThread(prop.getPriority());
                                if (cmdReturn != 0) {
                                    if (prop.isCatRetWcl() && cmdReturn > 0) {
                                        if (prop.isRetFiles()) {
                                            prop.setFiles(prop.getFiles() + 1);
                                        } else {
                                            prop.setLines(prop.getLines() + cmdReturn);
                                        }
                                    } else {
                                        logger.writeLine(MdlConst.LVL_NONE, "[ERR][ProcessFile()-TASK_PRINT-CAT] Cmd Return Code != 0 : " + cmdExec.getCmdPath() + " " + cmdExec.getCmdArgs());
                                    }
                                }
                            } else if (prop.isExecCmd()) {
                                String cmdArg = (prop.getCmdArgs() == null || prop.getCmdArgs().isBlank())
                                        ? MdlFile.replacePathForCmd(prop.getCmdPath(), sourceFilePath, prop.getSourcePath(), relativePath, prop.isDq(), prop.getVerbose())
                                        : MdlFile.replacePathForCmd(prop.getCmdPath() + " " + prop.getCmdArgs(), sourceFilePath, prop.getSourcePath(), relativePath, prop.isDq(), prop.getVerbose());

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

                                if (cmdExec.executeThread(prop.getPriority()) != 0) {
                                    logger.writeLine(MdlConst.LVL_NONE, "[ERR][ProcessFile()-TASK_PRINT-EXE] Cmd Return Code != 0 : " + cmdExec.getCmdPath() + " " + cmdExec.getCmdArgs());
                                } else {
                                    prop.setFiles(prop.getFiles() + 1);
                                }
                            } else {
                                prop.setFiles(prop.getFiles() + 1);
                                String line = MdlFile.getFileInfoString(sourceFilePath, prop.getVerbose(), prop.isDq());
                                logger.writeLine(MdlConst.LVL_NONE, line);
                            }
                        }
                        break;
                    default:
                        break;
                }
            } else if (prop.getTask() == ClsBaseDir.TASK_RM && prop.isRmNohit()) {
                if (!fsDiffCopy.removeRecursive(sourceFilePath, relativePath, isSymlinkFile)) {
                    isSuccess = false;
                }
            }
        } catch (Exception ex) {
            logger.writeLine(MdlConst.LVL_NONE, "[ERR] ClsFind.ProcessFile() : " + ex.getMessage() + " : " + relativePath);
            if (prop.isStackTrace()) {
                logger.writeLine(MdlConst.LVL_NONE, "");
                ex.printStackTrace();
                logger.writeLine(MdlConst.LVL_NONE, "");
            }
        }
        return isSuccess;
    }

    private static int evaluatePathFilterCode(String path, boolean includeBaseName, boolean excludeBaseName, List<String> includePatterns, List<String> excludePatterns, boolean isOrCondition, int debugLevel) {
        String target = includeBaseName ? MdlFile.getFileName(path) : path;
        int result = 1;

        if (includePatterns != null && !includePatterns.isEmpty()) {
            boolean isHit = false;
            result = 0;
            for (String pattern : includePatterns) {
                if (Pattern.compile(pattern, Pattern.CASE_INSENSITIVE).matcher(target).find()) {
                    isHit = true;
                    break;
                }
            }
            if (isHit) {
                result = 1;
                if (isOrCondition) {
                    return result;
                }
            }
        }

        target = excludeBaseName ? MdlFile.getFileName(path) : path;
        if (excludePatterns != null && !excludePatterns.isEmpty()) {
            for (String pattern : excludePatterns) {
                if (Pattern.compile(pattern, Pattern.CASE_INSENSITIVE).matcher(target).find()) {
                    return 2;
                }
            }
        }
        return result;
    }

    private static int combineFilterFlags(int previousEffective, int currentEffective, boolean isOrCondition, boolean isIncludeHitRecursive, boolean isExcludeHitRecursive) {
        int result = currentEffective;
        switch (previousEffective) {
            case 0:
                result = currentEffective;
                break;
            case 1:
                if (isIncludeHitRecursive) {
                    result = currentEffective == 2 ? 3 : 1;
                }
                break;
            case 2:
                if (isExcludeHitRecursive) {
                    result = (currentEffective == 1 && isOrCondition) ? 1 : 2;
                }
                break;
            case 3:
                if (isExcludeHitRecursive) {
                    result = (currentEffective == 1 && isOrCondition) ? 1 : 3;
                }
                break;
            default:
                break;
        }
        return result;
    }

    private static boolean isPathFilterMatched(String path, boolean includeBaseName, boolean excludeBaseName, List<String> includePatterns, List<String> excludePatterns, boolean isOrCondition, int debugLevel) {
        return evaluatePathFilterCode(path, includeBaseName, excludeBaseName, includePatterns, excludePatterns, isOrCondition, debugLevel) == 1;
    }
}
