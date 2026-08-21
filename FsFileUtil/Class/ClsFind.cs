using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Module;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace FsFileUtil.Class
{
    /// <summary>
    /// ファイルおよびディレクトリの検索・複製・移動・削除・一覧表示などのファイルシステム操作を実行するクラス。
    /// </summary>
    public class ClsFind
    {
        private readonly ClsLogger _logger;
        private readonly ClsProp _prop;
        private readonly ClsFsDiffCopy _fsDiffCopy;
        private readonly ClsCmdExec _cmdExec;
        private readonly ClsFsUtil _fsUtil;

        /// <summary>
        /// <see cref="ClsFind"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="log">ログ出力管理オブジェクト</param>
        /// <param name="prop">設定プロパティ管理オブジェクト</param>
        /// <param name="fsUtil">ファイルシステムユーティリティオブジェクト</param>
        /// <param name="diffCopy">ファイルシステム差分コピー実行オブジェクト</param>
        /// <example>
        /// <code>
        /// var logger = new ClsLogger();
        /// var prop = new ClsProp();
        /// var fsUtil = new ClsFsUtil();
        /// var diffCopy = new ClsFsDiffCopy(logger, prop);
        /// var finder = new ClsFind(logger, prop, fsUtil, diffCopy);
        /// </code>
        /// </example>
        public ClsFind(ClsLogger log, ClsProp prop, ClsFsUtil fsUtil, ClsFsDiffCopy diffCopy)
        {
            _logger = log;
            _prop = prop;
            _fsUtil = fsUtil;
            _fsDiffCopy = diffCopy;
            _cmdExec = new(_logger);
        }

        /// <summary>
        /// 指定されたタスク種別に基づいてメインのファイル・ディレクトリ処理を実行します。
        /// </summary>
        /// <param name="task">実行するタスク種別（例: <see cref="ClsProp.TASK_CP"/>, <see cref="ClsProp.TASK_MV"/>, <see cref="ClsProp.TASK_RM"/>, <see cref="ClsProp.TASK_PRINT"/>）</param>
        /// <returns>全処理が正常に完了した場合は <c>true</c>。失敗またはエラーが発生した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// prop.SourcePath = @"C:\Source";
        /// prop.DestinationPath = @"D:\Destination";
        /// bool result = finder.Execute(ClsProp.TASK_CP);
        /// </code>
        /// </example>
        public bool Execute(int task)
        {
            bool isSuccess = false;
            _prop.Task = task;
            _fsDiffCopy.Properties.Task = task;

            if (!string.IsNullOrEmpty(_prop.CmdPath) || _prop.IsCat)
            {
                _prop.IsExecCmd = true;
                _cmdExec.IsShowCmd = _prop.IsShowCmd;
                _cmdExec.IsShowExitCode = _prop.IsShowExitCode;
                _cmdExec.IsShowOutput = _prop.IsShowOutput;
                _cmdExec.Verbose = _prop.Verbose;
                _cmdExec.IsStackTrace = _prop.IsStackTrace;
                _cmdExec.IsShowEmptyLine = false;

                if (_prop.IsCatRetWcl) _cmdExec.IsNotShowExitCode = true;
                if (!string.IsNullOrEmpty(_prop.WorkDir)) _cmdExec.WorkDir = _prop.WorkDir;

                _cmdExec.WarnThreshold = _prop.WarnThreshold;
                _cmdExec.ErrorThreshold = _prop.ErrorThreshold;
                _cmdExec.IsErrorAtNegativeValue = _prop.IsErrorAtNegativeValue;
                _cmdExec.IsAlwaysNormal = _prop.IsAlwaysNormal;
                _cmdExec.Timeout = _prop.Timeout;
                _cmdExec.Initialize();
            }

            (string sourcePath, string destinationPath) = task switch
            {
                ClsProp.TASK_CP or ClsProp.TASK_MV => (_prop.SourcePath, _prop.DestinationPath),
                ClsProp.TASK_RM => (_prop.DestinationPath, _prop.SourcePath),
                ClsProp.TASK_PRINT => (_prop.SourcePath, _prop.SourcePath),
                _ => (string.Empty, string.Empty)
            };

            try
            {
                if (string.IsNullOrEmpty(sourcePath))
                {
                    if (_prop.FileList.Count > 0)
                    {
                        isSuccess = ExecuteFileList();
                    }
                }
                else
                {
                    switch (_prop.PathType)
                    {
                        case MdlFile.PATH_IS_DIRECTORY:
                            if (_prop.FileList.Count > 0)
                            {
                                isSuccess = ExecuteFileList();
                            }
                            else
                            {
                                isSuccess = ProcessDirectoryRecursive(sourcePath, destinationPath, string.Empty, 0, 0);
                            }
                            break;

                        case MdlFile.PATH_IS_FILE:
                            isSuccess = _fsDiffCopy.Copy(sourcePath, destinationPath, Path.GetFileName(sourcePath), MdlFile.PATH_IS_FILE, -1);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                _logger.WriteLine(MdlConst.LVL_NONE, $"[ERR] ClsFind.Execute() 1 : {ex.Message} : {destinationPath}");
                if (_prop.IsStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, string.Empty);
                    _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? string.Empty);
                    _logger.WriteLine(MdlConst.LVL_NONE, string.Empty);
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 指定されたソースディレクトリ配下を再帰的にトラバースし、設定条件（深度・フィルタ・日時等）に従ってディレクトリ・ファイルの処理を実行します。
        /// </summary>
        /// <param name="sourcePath">処理対象となるコピー/移動/削除元のルートディレクトリパス</param>
        /// <param name="destinationPath">処理結果の出力先となるディレクトリパス</param>
        /// <param name="relativePath">ルートからの相対ディレクトリパス</param>
        /// <param name="currentDepth">現在のディレクトリ階層の深さ（ルート階層は0）</param>
        /// <param name="previousEffective">親ディレクトリ階層から引き継がれたフィルタ評価有効フラグ値</param>
        /// <returns>ディレクトリ配下の全処理が成功した場合は <c>true</c>。いずれかの処理でエラーが発生した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool isOk = ProcessDirectoryRecursive(@"C:\SrcDir", @"D:\DstDir", "", 0, 0);
        /// </code>
        /// </example>
        private bool ProcessDirectoryRecursive(string sourcePath, string destinationPath, string relativePath, ulong currentDepth, int previousEffective)
        {
            bool isSuccess = true;
            bool isSymlinkDirectory = false;
            bool isAvailable = true;
            int currentEffective = previousEffective;

            if (currentDepth >= _prop.MinDepth)
            {
                if (currentDepth > _prop.MaxDepth) return true;

                if (_prop.IsDirTerm && !MdlFile.IsValidDirectoryDateTime(sourcePath, _prop.IsBefore, _prop.BeforeTime, _prop.IsAfter, _prop.AfterTime))
                {
                    isAvailable = false;
                }

                if (isAvailable)
                {
                    try
                    {
                        if (_prop.IsSymLink) isSymlinkDirectory = MdlFile.IsSymlink(sourcePath);

                        if (_prop.IsShowCurDir > 0 && currentDepth <= (ulong)_prop.IsShowCurDir)
                        {
                            const int progressSize = 86;
                            string strMessage = $"=====< [{currentDepth}] {sourcePath} >=====";
                            if (MdlUtil.GetShiftJisByteCount(strMessage) < progressSize)
                            {
                                strMessage = strMessage.PadRight(progressSize);
                                _logger.SetValueByKey(ClsLogger.IS_TRIM_CONSOLE, "false");
                            }
                            else
                            {
                                _logger.SetValueByKey(ClsLogger.IS_TRIM_CONSOLE, "true");
                            }
                            _logger.WriteLine(MdlConst.LVL_NONE, strMessage);
                        }

                        if (_prop.Verbose > 6)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $"■■■[ProcessDirectoryRecursive()][ParentDir][{currentDepth}] PATH={relativePath} ■■■");
                            _logger.WriteLine(MdlConst.LVL_NONE, $"sourcePath           = {sourcePath}");
                            _logger.WriteLine(MdlConst.LVL_NONE, $"destinationPath      = {destinationPath}");
                            _logger.WriteLine(MdlConst.LVL_NONE, $"isSymlinkDirectory   = {isSymlinkDirectory}");
                            _logger.WriteLine(MdlConst.LVL_NONE, $"previousEffective    = {previousEffective}");
                            _logger.WriteLine(MdlConst.LVL_NONE, $"IsIncHitRecursive = {_prop.IsIncHitRecursive}");
                            _logger.WriteLine(MdlConst.LVL_NONE, $"IsExcHitRecursive = {_prop.IsExcHitRecursive}");
                            _logger.WriteLine(MdlConst.LVL_NONE, $"IsDirFilterOr     = {_prop.IsDirFilterOr}");
                        }

                        int filterResult = MdlFile.EvaluatePathFilterCode(relativePath, _prop.IsRegIncBasename, _prop.IsRegExcBasename, _prop.IncDirsList, _prop.ExcDirsList, _prop.IsDirFilterOr, _prop.Verbose);
                        currentEffective = MdlFile.CombineFilterFlags(currentEffective, filterResult, _prop.IsDirFilterOr, _prop.IsIncHitRecursive, _prop.IsExcHitRecursive);

                        if (_prop.Verbose > 6)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $"filterResult      = {filterResult}");
                            _logger.WriteLine(MdlConst.LVL_NONE, $"currentEffective  = {currentEffective}");
                        }

                        if (currentDepth > 0 && currentEffective > 1 && _prop.IsExcHitRecursive)
                        {
                            return true;
                        }

                        if (currentEffective == 1 || _prop.IsXdOnlyFiles)
                        {
                            switch (_prop.Task)
                            {
                                case ClsProp.TASK_PRINT:
                                    if (currentEffective == 1 && (_prop.TypeCode == MdlConst.INT_TYPE_ALL || _prop.TypeCode == MdlConst.INT_TYPE_DIRECTORY))
                                    {
                                        if (_prop.IsExecCmd)
                                        {
                                            string cmdArg = string.IsNullOrEmpty(_prop.CmdArgs)
                                                ? MdlFile.ReplacePathForCmdExec(_prop.CmdPath, sourcePath, _prop.SourcePath, relativePath, _prop.IsDq, _prop.Verbose)
                                                : MdlFile.ReplacePathForCmdExec($"{_prop.CmdPath} {_prop.CmdArgs}", sourcePath, _prop.SourcePath, relativePath, _prop.IsDq, _prop.Verbose);

                                            switch (_prop.ExecModeCode)
                                            {
                                                case ClsProp.EXEC_MODE_CMD:
                                                    _cmdExec.CmdPath = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
                                                    _cmdExec.CmdArgs = $"/c {cmdArg}";
                                                    break;
                                                case ClsProp.EXEC_MODE_PS:
                                                    _cmdExec.CmdPath = "powershell";
                                                    _cmdExec.CmdArgs = $"-NoProfile -command \"{cmdArg}; exit $LASTEXITCODE\"";
                                                    break;
                                                default:
                                                    _cmdExec.CmdPath = MdlUtil.GetRegexTarget(cmdArg, @"^(?<TARGET>\S+)\s+.*");
                                                    _cmdExec.CmdArgs = MdlUtil.GetRegexTarget(cmdArg, @"^\S+\s+(?<TARGET>.*)");
                                                    break;
                                            }

                                            if (_cmdExec.ExecuteThread(_prop.Priority) != 0)
                                            {
                                                _logger.WriteLine(MdlConst.LVL_NONE, $"[ERR][ProcessDirectoryRecursive()-TASK_PRINT] Cmd Return Code != 0 : {_cmdExec.CmdPath} {_cmdExec.CmdArgs}");
                                            }
                                            else
                                            {
                                                _prop.Files++;
                                            }
                                        }
                                        else
                                        {
                                            string line = MdlFile.GetFileInfoString(sourcePath, _prop.Verbose, _prop.IsDq);
                                            _logger.WriteLine(MdlConst.LVL_NONE, line);
                                            _prop.Files++;
                                        }
                                    }
                                    break;

                                case ClsProp.TASK_CP:
                                    if (currentEffective == 1 || _prop.IsXdOnlyFiles)
                                    {
                                        if (!_fsDiffCopy.Copy(sourcePath, destinationPath, relativePath, MdlFile.PATH_IS_DIRECTORY, isSymlinkDirectory ? 1 : 0)) isSuccess = false;
                                    }
                                    break;

                                case ClsProp.TASK_MV:
                                    if (currentEffective == 1 || _prop.IsXdOnlyFiles)
                                    {
                                        if (_fsDiffCopy.Copy(sourcePath, destinationPath, relativePath, MdlFile.PATH_IS_DIRECTORY, isSymlinkDirectory ? 1 : 0))
                                        {
                                            if (_prop.TypeCode != MdlConst.INT_TYPE_FILE && _prop.IncFilesList.Count == 0 && _prop.ExcFilesList.Count == 0)
                                            {
                                                if (MdlFile.IsEmptyDirectory(sourcePath)) return true;
                                            }
                                        }
                                        else
                                        {
                                            isSuccess = false;
                                        }
                                    }
                                    if (!MdlFile.PathExists(sourcePath)) return isSuccess;
                                    break;

                                case ClsProp.TASK_RM:
                                    if (currentDepth > 0 && !_prop.IsFlat)
                                    {
                                        if (currentEffective == 1)
                                        {
                                            if (!MdlFile.PathExists(destinationPath) && _fsDiffCopy.RemoveRecursive(sourcePath, relativePath, isSymlinkDirectory))
                                            {
                                                return true;
                                            }
                                        }
                                        else if (_prop.IsRmNohit && _fsDiffCopy.RemoveRecursive(sourcePath, relativePath, isSymlinkDirectory))
                                        {
                                            return true;
                                        }
                                    }
                                    break;
                            }

                            if (!isSymlinkDirectory && !ProcessCurrentDirectoryFiles(sourcePath, destinationPath, relativePath, currentDepth))
                            {
                                isSuccess = false;
                            }
                        }

                        if (isSymlinkDirectory) return isSuccess;
                    }
                    catch (Exception objExcptn)
                    {
                        isSuccess = false;
                        _logger.WriteLine(MdlConst.LVL_NONE, $"[ERR] ClsFind.ProcessDirectoryRecursive() 1 : {objExcptn.Message} : {relativePath}");
                        if (_prop.IsStackTrace)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, string.Empty);
                            _logger.WriteLine(MdlConst.LVL_NONE, objExcptn.StackTrace ?? string.Empty);
                            _logger.WriteLine(MdlConst.LVL_NONE, string.Empty);
                        }
                    }
                }
            }

            if (_prop.Task == ClsProp.TASK_PRINT && !MdlFile.PathExists(sourcePath)) return true;

            foreach (string directoryPath in MdlFile.GetSortedDirectories(sourcePath, "*", SearchOption.TopDirectoryOnly, _prop.SortType, _prop.IsAscending, _prop.IsShowDirList))
            {
                try
                {
                    string subDirName = Path.GetFileName(directoryPath);
                    string relativePathNext = (currentDepth == 0)
                        ? subDirName
                        : Path.Combine(relativePath, subDirName);

                    string sourcePathNext = Path.Combine(sourcePath, subDirName);
                    string destinationPathNext = _prop.IsFlat ? destinationPath : Path.Combine(destinationPath, subDirName);

                    if (!ProcessDirectoryRecursive(sourcePathNext, destinationPathNext, relativePathNext, currentDepth + 1, currentEffective))
                    {
                        isSuccess = false;
                    }

                    if (_prop.Task == ClsProp.TASK_MV)
                    {
                        if (MdlFile.PathExists(sourcePathNext))
                        {
                            if (_prop.TypeCode != MdlConst.INT_TYPE_FILE && _prop.IncFilesList.Count == 0 && _prop.ExcFilesList.Count == 0)
                            {
                                if (!MdlFile.DeleteEmptyDirectories(sourcePathNext, 1)) isSuccess = false;
                            }
                        }
                        if (!MdlFile.PathExists(sourcePathNext)) continue;
                    }
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    _logger.WriteLine(MdlConst.LVL_NONE, $"[ERR] ClsFind.ProcessDirectoryRecursive() 2 : {ex.Message} : {relativePath}");
                    if (_prop.IsStackTrace)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, string.Empty);
                        _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? string.Empty);
                        _logger.WriteLine(MdlConst.LVL_NONE, string.Empty);
                    }
                }
            }

            return isSuccess;
        }

        /// <summary>
        /// 指定されたディレクトリ直下に存在する全ファイルに対してフィルタ判定を行い、個々のファイル処理を呼び出します。
        /// </summary>
        /// <param name="sourcePath">ファイルの検索元ディレクトリパス</param>
        /// <param name="destinationPath">ファイルの出力先ディレクトリパス</param>
        /// <param name="relativePath">ルート階層からの相対ディレクトリパス</param>
        /// <param name="currentDepth">現在のディレクトリ階層の深さ</param>
        /// <returns>配下のファイル処理がすべて成功した場合は <c>true</c>。失敗があった場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool result = ProcessCurrentDirectoryFiles(@"C:\SrcDir", @"D:\DstDir", "SubFolder", 1);
        /// </code>
        /// </example>
        private bool ProcessCurrentDirectoryFiles(string sourcePath, string destinationPath, string relativePath, ulong currentDepth)
        {
            if (!_prop.IsFileCopy && !_prop.IsSyncRmOnly) return true;
            if (_prop.Task == ClsProp.TASK_PRINT && !MdlFile.PathExists(sourcePath)) return true;

            bool isSuccess = true;
            foreach (string sourceFilePath in MdlFile.GetSortedFiles(sourcePath, "*", SearchOption.TopDirectoryOnly, _prop.SortType, _prop.IsAscending, _prop.IsShowFileList))
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string relativeFilePath = Path.Combine(relativePath, fileName);
                string sourceFileFullPath = Path.Combine(sourcePath, fileName);
                string destFileFullPath = Path.Combine(destinationPath, fileName);

                if (!ProcessFile(sourceFileFullPath, destFileFullPath, relativeFilePath)) isSuccess = false;
            }
            return isSuccess;
        }

        /// <summary>
        /// 設定オブジェクトのファイルリスト (<see cref="ClsProp.FileList"/>) に定義されたファイル・ディレクトリ一覧を順次処理します。
        /// </summary>
        /// <returns>全ファイル要素の処理が正常に完了した場合は <c>true</c>。失敗があった場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// prop.FileList.Add(@"C:\file1.txt");
        /// bool ok = finder.ExecuteFileList();
        /// </code>
        /// </example>
        public bool ExecuteFileList()
        {
            if (!_prop.IsFileCopy && !_prop.IsSyncRmOnly) return true;

            bool isSuccess = true;
            var regex = new System.Text.RegularExpressions.Regex(_prop.FileListRegex);

            foreach (string fileElement in _prop.FileList)
            {
                string sourceFilePath = string.Empty;
                string destinationFilePath = string.Empty;
                string relativeFilePath = fileElement;

                string[] filePaths = regex.Split(fileElement);
                switch (_prop.FilesTypeCode)
                {
                    case ClsProp.FILES_RELATIVE:
                        if (filePaths.Length > 0)
                        {
                            sourceFilePath = Path.Combine(_prop.SourcePath, filePaths[0].Trim());
                            destinationFilePath = Path.Combine(_prop.DestinationPath, filePaths[0].Trim());
                            relativeFilePath = filePaths[0].Trim();
                        }
                        if (filePaths.Length > 1) destinationFilePath = Path.Combine(_prop.DestinationPath, filePaths[1].Trim());
                        break;

                    case ClsProp.FILES_FULL:
                        if (filePaths.Length > 0)
                        {
                            sourceFilePath = filePaths[0].Trim();
                            relativeFilePath = MdlFile.GetFileName(sourceFilePath);
                        }
                        if (filePaths.Length > 1) destinationFilePath = filePaths[1].Trim();
                        break;
                }

                switch (MdlFile.GetPathType(sourceFilePath))
                {
                    case MdlFile.PATH_IS_DIRECTORY:
                        if (!ProcessDirectoryRecursive(sourceFilePath, destinationFilePath, relativeFilePath, 0, 0)) isSuccess = false;
                        break;

                    case MdlFile.PATH_IS_FILE:
                        if (!ProcessFile(sourceFilePath, destinationFilePath, relativeFilePath)) isSuccess = false;
                        break;

                    default:
                        if (_prop.Task != ClsProp.TASK_RM)
                        {
                            _fsDiffCopy.NotFoundCount++;
                            if (_prop.Verbose > 1)
                            {
                                _fsDiffCopy.EchoTitle($"[ERR] NO SUCH A FILE OR DIRECTORY : {sourceFilePath}");
                            }
                        }
                        break;
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 単一の指定ファイルに対して、更新日時・サイズ・ファイルロック・名称フィルタなどの検証を行い、コピー/移動/削除/印刷等の処理を実行します。
        /// </summary>
        /// <param name="sourceFilePath">処理対象ファイルのフルパス</param>
        /// <param name="destinationFilePath">出力先ファイルのフルパス</param>
        /// <param name="relativePath">ルートからの相対ファイルパス</param>
        /// <returns>ファイル処理が成功した場合（スキップ含む）は <c>true</c>。処理失敗時は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool result = ProcessFile(@"C:\SrcDir\data.txt", @"D:\DstDir\data.txt", "data.txt");
        /// </code>
        /// </example>
        private bool ProcessFile(string sourceFilePath, string destinationFilePath, string relativePath)
        {
            if (!_prop.IsFileCopy && !_prop.IsSyncRmOnly) return true;

            bool isSuccess = true;
            try
            {
                bool isSymlinkFile = _prop.IsSymLink && MdlFile.IsSymlink(sourceFilePath);
                bool isDateValid = MdlFile.IsValidFileDateTime(sourceFilePath, _prop.IsBefore, _prop.BeforeTime, _prop.IsAfter, _prop.AfterTime);
                bool isSizeValid = true;

                if (_prop.CompOpe != ClsProp.COMPARISON_NO)
                {
                    var fileInfo = new FileInfo(sourceFilePath);
                    switch (_prop.CompOpe)
                    {
                        case ClsProp.COMPARISON_GE:
                            if (fileInfo.Length < _prop.CompSize) isSizeValid = false;
                            break;
                        case ClsProp.COMPARISON_LE:
                            if (fileInfo.Length > _prop.CompSize) isSizeValid = false;
                            break;
                    }
                }

                string fileName = Path.GetFileName(sourceFilePath);
                bool isFilterValid = MdlFile.IsPathFilterMatched(fileName, true, true, _prop.IncFilesList, _prop.ExcFilesList, _prop.Verbose);
                bool isFileNotLocked = true;

                switch (_prop.CheckFileLock)
                {
                    case ClsProp.CHECK_FILE_LOCK_SAMPLE:
                        if (!MdlFile.IsFileLocked(sourceFilePath))
                        {
                            isFileNotLocked = false;
                            if (_prop.Verbose > 4) _fsDiffCopy.EchoTitle($"[---] SKIP : FILE IS NOT LOCKED : {sourceFilePath}");
                        }
                        break;
                    case ClsProp.CHECK_FILE_LOCK_SKIP:
                        if (MdlFile.IsFileLocked(sourceFilePath))
                        {
                            isFileNotLocked = false;
                            if (_prop.Verbose > 4) _fsDiffCopy.EchoTitle($"[---] SKIP : FILE IS LOCKED : {sourceFilePath}");
                        }
                        break;
                }

                if (isDateValid && isFilterValid && isSizeValid && isFileNotLocked)
                {
                    bool isCopySuccess = false;
                    switch (_prop.Task)
                    {
                        case ClsProp.TASK_CP:
                        case ClsProp.TASK_RENAME:
                            isCopySuccess = _prop.IsReverse
                                ? _fsDiffCopy.Copy(destinationFilePath, sourceFilePath, relativePath, MdlFile.PATH_IS_FILE, -1)
                                : _fsDiffCopy.Copy(sourceFilePath, destinationFilePath, relativePath, MdlFile.PATH_IS_FILE, -1);

                            if (!isCopySuccess) isSuccess = false;
                            break;

                        case ClsProp.TASK_MV:
                            isCopySuccess = _prop.IsReverse
                                ? _fsDiffCopy.Copy(destinationFilePath, sourceFilePath, relativePath, MdlFile.PATH_IS_FILE, -1)
                                : _fsDiffCopy.Copy(sourceFilePath, destinationFilePath, relativePath, MdlFile.PATH_IS_FILE, -1);

                            if (isCopySuccess)
                            {
                                _fsDiffCopy.RmTotalCount++;
                                if (!_fsDiffCopy.RemoveRecursive(sourceFilePath, relativePath, isSymlinkFile))
                                {
                                    _fsDiffCopy.RmNgCount++;
                                    isSuccess = false;
                                }
                            }
                            else
                            {
                                isSuccess = false;
                            }
                            break;

                        case ClsProp.TASK_RM:
                            if (MdlFile.PathExists(destinationFilePath))
                            {
                                _fsDiffCopy.RmTotalCount++;
                                _fsDiffCopy.RmSkipCount++;
                            }
                            else
                            {
                                isCopySuccess = _fsDiffCopy.RemoveRecursive(sourceFilePath, relativePath, isSymlinkFile);
                                if (!isCopySuccess) isSuccess = false;
                            }
                            break;

                        case ClsProp.TASK_PRINT:
                            if (_prop.TypeCode == MdlConst.INT_TYPE_ALL || _prop.TypeCode == MdlConst.INT_TYPE_FILE)
                            {
                                if (_prop.IsCat)
                                {
                                    _cmdExec.CmdPath = string.IsNullOrEmpty(_prop.CmdPath)
                                        ? Path.Combine(_prop.ExeDir, "cat.exe")
                                        : _prop.CmdPath;

                                    _cmdExec.CmdArgs = $" -f \"{sourceFilePath}\"";
                                    if (!string.IsNullOrEmpty(_prop.CatI)) _cmdExec.CmdArgs += $" -i \"{_prop.CatI}\"";
                                    if (!string.IsNullOrEmpty(_prop.CatX)) _cmdExec.CmdArgs += $" -x \"{_prop.CatX}\"";
                                    if (!string.IsNullOrEmpty(_prop.CatP)) _cmdExec.CmdArgs += $" -p {_prop.CatP}";
                                    if (!string.IsNullOrEmpty(_prop.CatE)) _cmdExec.CmdArgs += $" -e {_prop.CatE}";
                                    if (!string.IsNullOrEmpty(_prop.CatXmlNl)) _cmdExec.CmdArgs += $" -xml-nl \"{_prop.CatXmlNl}\"";

                                    if (!string.IsNullOrEmpty(_prop.CatOptions))
                                    {
                                        _cmdExec.CmdArgs = string.IsNullOrEmpty(_cmdExec.CmdArgs)
                                            ? _prop.CatOptions
                                            : $"{_cmdExec.CmdArgs} {_prop.CatOptions}";
                                    }
                                    if (_prop.IsCatRetWcl) _cmdExec.CmdArgs += " -ret-wcl";

                                    string showCmd = _cmdExec.CmdPath + _cmdExec.CmdArgs;
                                    if (_prop.IsSwitchUser || _prop.IsLogon)
                                    {
                                        _cmdExec.CmdArgs += $" -su -u {_prop.Username} -p {_prop.Password}";
                                        showCmd += $" -su -u {_prop.Username} -p ****";
                                    }

                                    int cmdReturn = _cmdExec.ExecuteThread(_prop.Priority);
                                    if (cmdReturn != 0)
                                    {
                                        if (_prop.IsCatRetWcl && cmdReturn > 0)
                                        {
                                            if (_prop.IsRetFiles)
                                            {
                                                _prop.Files++;
                                            }
                                            else
                                            {
                                                _prop.Lines += (ulong)cmdReturn;
                                            }
                                        }
                                        else
                                        {
                                            _logger.WriteLine(MdlConst.LVL_NONE, $"[ERR][ProcessFile()-TASK_PRINT-CAT] Cmd Return Code != 0 : {showCmd}");
                                        }
                                    }
                                }
                                else if (_prop.IsExecCmd)
                                {
                                    string cmdArg = string.IsNullOrEmpty(_prop.CmdArgs)
                                        ? MdlFile.ReplacePathForCmdExec(_prop.CmdPath, sourceFilePath, _prop.SourcePath, relativePath, _prop.IsDq, _prop.Verbose)
                                        : MdlFile.ReplacePathForCmdExec($"{_prop.CmdPath} {_prop.CmdArgs}", sourceFilePath, _prop.SourcePath, relativePath, _prop.IsDq, _prop.Verbose);

                                    switch (_prop.ExecModeCode)
                                    {
                                        case ClsProp.EXEC_MODE_CMD:
                                            _cmdExec.CmdPath = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
                                            _cmdExec.CmdArgs = $"/c {cmdArg}";
                                            break;
                                        case ClsProp.EXEC_MODE_PS:
                                            _cmdExec.CmdPath = "powershell";
                                            _cmdExec.CmdArgs = $"-NoProfile -command \"{cmdArg}; exit $LASTEXITCODE\"";
                                            break;
                                        default:
                                            _cmdExec.CmdPath = MdlUtil.GetRegexTarget(cmdArg, @"^(?<TARGET>\S+)\s+.*");
                                            _cmdExec.CmdArgs = MdlUtil.GetRegexTarget(cmdArg, @"^\S+\s+(?<TARGET>.*)");
                                            break;
                                    }

                                    if (_cmdExec.ExecuteThread(_prop.Priority) != 0)
                                    {
                                        _logger.WriteLine(MdlConst.LVL_NONE, $"[ERR][ProcessFile()-TASK_PRINT-EXE] Cmd Return Code != 0 : {_cmdExec.CmdPath} {_cmdExec.CmdArgs}");
                                    }
                                    else
                                    {
                                        _prop.Files++;
                                    }
                                }
                                else
                                {
                                    _prop.Files++;
                                    string line = MdlFile.GetFileInfoString(sourceFilePath, _prop.Verbose, _prop.IsDq);
                                    _logger.WriteLine(MdlConst.LVL_NONE, line);
                                }
                            }
                            break;
                    }
                }
                else if (_prop.Task == ClsProp.TASK_RM && _prop.IsRmNohit)
                {
                    if (!_fsDiffCopy.RemoveRecursive(sourceFilePath, relativePath, isSymlinkFile)) isSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, $"[ERR] ClsFind.ProcessFile() : {ex.Message} : {relativePath}");
                if (_prop.IsStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, string.Empty);
                    _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? string.Empty);
                    _logger.WriteLine(MdlConst.LVL_NONE, string.Empty);
                }
            }
            return isSuccess;
        }

    }
}
