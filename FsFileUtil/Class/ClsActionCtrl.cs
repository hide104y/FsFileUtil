using System;
using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Module;
using CmnWinLib.Class;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace FsFileUtil.Class
{
    public class ClsActionCtrl
    {
        private ClsLogger _logger;
        private ClsProp _prop;
        private ClsFind _find;
        private ClsFsDiffCopy _fsDiffCopy;
        private ClsFsUtil _fsUtil;
        private ClsSymLinkWrapper _symLink;
        private ClsCmdExec _cmdExec;
        private ClsFsAttrib _fsAttrib;

        /// <summary>
        /// <c>ClsActionCtrl</c> クラスの新しいインスタンスを初期化し、各種ユーティリティの依存関係を設定します。
        /// </summary>
        /// <param name="logger">ログ出力を行うための <see cref="ClsLogger"/> オブジェクト。</param>
        /// <param name="prop">動作条件やオプション設定を保持する <see cref="ClsProp"/> オブジェクト。</param>
        /// <example>
        /// <code>
        /// var logger = new ClsLogger();
        /// var prop = new ClsProp();
        /// var actionCtrl = new ClsActionCtrl(logger, prop);
        /// </code>
        /// </example>
        public ClsActionCtrl(ClsLogger logger, ClsProp prop)
        {
            _logger = logger;
            _prop = prop;
            _symLink = new(_logger);
            _cmdExec = new(_logger);
            _fsUtil = new(_logger);
            _fsAttrib = new(_logger);
            _fsDiffCopy = new(_logger, _prop, _fsUtil, _symLink);
            _find = new(_logger, _prop, _fsUtil, _fsDiffCopy);
            _fsUtil.Verbose = _prop.Verbose;
            _fsUtil.IsStackTrace = _prop.IsStackTrace;
            _fsUtil.WaitMSecForRetryCopy = _prop.WaitMSecForRetryCopy;
            _fsUtil.RetryMax = _prop.RetrySystemCopyMax;
        }

        /// <summary>
        /// 設定されたアクションコード（検索、コピー、移動、同期、ディレクトリ作成、削除、コマンド実行など）に基づき、該当する処理を実行します。
        /// </summary>
        /// <returns>処理結果を示す整数値（<c>MdlConst.LVL_I</c>: 正常終了, <c>MdlConst.LVL_W</c>: 警告, <c>MdlConst.LVL_E</c>: エラー）。</returns>
        /// <example>
        /// <code>
        /// var actionCtrl = new ClsActionCtrl(logger, prop);
        /// int returnCode = actionCtrl.Execute();
        /// </code>
        /// </example>
        public int Execute()
        {
            int returnCode = MdlConst.LVL_I;
            bool isOk = false;
            if (_prop.IsSymLink)
            {
                _symLink.Verbose = _prop.Verbose;
            }
            bool isSymLinkSource = false;
            if (_prop.IsSymLink) isSymLinkSource = MdlFile.IsSymlink(_prop.SourcePath);
            switch (_prop.ActionCode)
            {
                case ClsProp.ACTION_FIND:
                case ClsProp.ACTION_COPY:
                case ClsProp.ACTION_MOVE:
                case ClsProp.ACTION_SYNC:
                    if (_prop.IsFrPathCheck && !MdlFile.PathExists(_prop.SourcePath))
                    {
                        if (_prop.IsSourceCheck)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : NOT FOUND {_prop.SourcePath}");
                            return MdlConst.LVL_E;
                        }
                        else
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $"OK : NOT FOUND {_prop.SourcePath}");
                            return MdlConst.LVL_I;
                        }
                    }
                    break;
            }
            switch (_prop.ActionCode)
            {
                case ClsProp.ACTION_FIND:
                    if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "START : FIND ------------------------------------------------------------");
                    returnCode = _find.Execute(ClsProp.TASK_PRINT) ? MdlConst.LVL_I : MdlConst.LVL_E;
                    if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "E N D : FIND ------------------------------------------------------------");
                    break;
                case ClsProp.ACTION_COPY:
                    if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "START : COPY ------------------------------------------------------------");
                    returnCode = _find.Execute(ClsProp.TASK_CP) ? MdlConst.LVL_I : MdlConst.LVL_E;
                    if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "E N D : COPY ------------------------------------------------------------");
                    if (_prop.Verbose > -1)
                    {
                        string message = $"=== COPY : NEW={_fsDiffCopy.CopyNewCount} UPDATE={_fsDiffCopy.CopyUpdateCount} SKIP={_fsDiffCopy.CopySkipCount} ERR={_fsDiffCopy.CopyErrorCount} / TOTAL={_fsDiffCopy.CopyTotalCount}";
                        if (_fsDiffCopy.NotFoundCount > 0) message += $" / NOT FOUND={_fsDiffCopy.NotFoundCount}";
                        _logger.WriteLine(MdlConst.LVL_NONE, message);
                    }
                    else if (_prop.Verbose > -3)
                    {
                        string message = $"=== COPY : COPY={_fsDiffCopy.CopyNewCount + _fsDiffCopy.CopyUpdateCount} SKIP={_fsDiffCopy.CopySkipCount} ERR={_fsDiffCopy.CopyErrorCount} / TOTAL={_fsDiffCopy.CopyTotalCount}";
                        if (_fsDiffCopy.NotFoundCount > 0) message += $" / NOT FOUND={_fsDiffCopy.NotFoundCount}";
                        _logger.WriteLine(MdlConst.LVL_NONE, message);
                    }
                    if (returnCode == MdlConst.LVL_I && _fsDiffCopy.CopyErrorCount > 0) returnCode = MdlConst.LVL_E;
                    if (returnCode == MdlConst.LVL_I && _fsDiffCopy.MkdirNgCount > 0) returnCode = MdlConst.LVL_W;
                    if (returnCode == MdlConst.LVL_I && _fsDiffCopy.NotFoundCount > 0) returnCode = MdlConst.LVL_W;
                    _prop.Files = _fsDiffCopy.CopyNewCount + _fsDiffCopy.CopyUpdateCount;
                    break;
                case ClsProp.ACTION_MOVE:
                    if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "START : MOVE ------------------------------------------------------------");
                    isOk = _find.Execute(ClsProp.TASK_MV);
                    if (MdlConst.INT_TYPE_FILE != _prop.TypeCode && 0 == _prop.IncFilesList.Count && 0 == _prop.ExcFilesList.Count)
                    {
                        if (MdlFile.PathExists(_prop.SourcePath))
                        {
                            if (!MdlFile.DeleteEmptyDirectories(_prop.SourcePath, 1)) isOk = false;
                        }
                    }
                    if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "E N D : MOVE ------------------------------------------------------------");
                    returnCode = isOk ? MdlConst.LVL_I : MdlConst.LVL_E;
                    if (_prop.Verbose > -1)
                    {
                        string message = $"=== MOVE : NEW={_fsDiffCopy.CopyNewCount} UPDATE={_fsDiffCopy.CopyUpdateCount} SKIP={_fsDiffCopy.CopySkipCount} ERR={_fsDiffCopy.CopyErrorCount} / TOTAL={_fsDiffCopy.CopyTotalCount}";
                        if (_fsDiffCopy.RmNgCount > 0) message += $" / DELETE FILE ERR={_fsDiffCopy.RmNgCount}";
                        if (_fsDiffCopy.NotFoundCount > 0) message += $" / NOT FOUND={_fsDiffCopy.NotFoundCount}";
                        _logger.WriteLine(MdlConst.LVL_NONE, message);
                    }
                    else if (_prop.Verbose > -3)
                    {
                        string message = $"=== MOVE : MOVE={_fsDiffCopy.CopyNewCount + _fsDiffCopy.CopyUpdateCount} SKIP={_fsDiffCopy.CopySkipCount} ERR={_fsDiffCopy.CopyErrorCount} / TOTAL={_fsDiffCopy.CopyTotalCount}";
                        if (_fsDiffCopy.RmNgCount > 0) message += $" / DELETE FILE ERR={_fsDiffCopy.RmNgCount}";
                        if (_fsDiffCopy.NotFoundCount > 0) message += $" / NOT FOUND={_fsDiffCopy.NotFoundCount}";
                        _logger.WriteLine(MdlConst.LVL_NONE, message);
                    }
                    if (returnCode == MdlConst.LVL_I && _fsDiffCopy.CopyErrorCount > 0) returnCode = MdlConst.LVL_E;
                    if (returnCode == MdlConst.LVL_I && _fsDiffCopy.MkdirNgCount > 0) returnCode = MdlConst.LVL_W;
                    if (returnCode == MdlConst.LVL_I && _fsDiffCopy.NotFoundCount > 0) returnCode = MdlConst.LVL_W;
                    _prop.Files = _fsDiffCopy.CopyNewCount + _fsDiffCopy.CopyUpdateCount;
                    break;
                case ClsProp.ACTION_SYNC:
                    if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "START : COPY ------------------------------------------------------------");
                    if (_prop.IsSyncRmOnly)
                    {
                        if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, " => SKIP (SYNC DELETE ONLY)");
                        isOk = true;
                    }
                    else
                    {
                        isOk = _find.Execute(ClsProp.TASK_CP);
                    }
                    if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "E N D : COPY ------------------------------------------------------------");
                    if (isOk)
                    {
                        if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "START : SYNC DELETE -----------------------------------------------------");
                        if (_prop.Verbose < -1)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, "");
                            _logger.WriteLine(MdlConst.LVL_NONE, "--- DELETE ---");
                        }
                        isOk = _find.Execute(ClsProp.TASK_RM);
                        if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "E N D : SYNC DELETE -----------------------------------------------------");
                    }
                    returnCode = isOk ? MdlConst.LVL_I : MdlConst.LVL_E;
                    if (_prop.Verbose > -1)
                    {
                        string message = $"=== COPY : NEW={_fsDiffCopy.CopyNewCount} UPDATE={_fsDiffCopy.CopyUpdateCount} SKIP={_fsDiffCopy.CopySkipCount} ERR={_fsDiffCopy.CopyErrorCount} / TOTAL={_fsDiffCopy.CopyTotalCount}";
                        if (_fsDiffCopy.NotFoundCount > 0) message += $" / NOT FOUND={_fsDiffCopy.NotFoundCount}";
                        _logger.WriteLine(MdlConst.LVL_NONE, message);
                    }
                    else if (_prop.Verbose > -3)
                    {
                        string message = $"=== COPY : COPY={_fsDiffCopy.CopyNewCount + _fsDiffCopy.CopyUpdateCount} SKIP={_fsDiffCopy.CopySkipCount} ERR={_fsDiffCopy.CopyErrorCount} / TOTAL={_fsDiffCopy.CopyTotalCount}";
                        if (_fsDiffCopy.NotFoundCount > 0) message += $" / NOT FOUND={_fsDiffCopy.NotFoundCount}";
                        _logger.WriteLine(MdlConst.LVL_NONE, message);
                    }
                    if (_prop.Verbose > -3)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, $"=== DEL  : DEL={_fsDiffCopy.RmOkCount} SKIP={_fsDiffCopy.RmSkipCount} ERR={_fsDiffCopy.RmNgCount} / TOTAL={_fsDiffCopy.RmTotalCount}");
                    }
                    if (returnCode == MdlConst.LVL_I && _fsDiffCopy.CopyErrorCount > 0) returnCode = MdlConst.LVL_E;
                    if (returnCode == MdlConst.LVL_I && _fsDiffCopy.MkdirNgCount > 0) returnCode = MdlConst.LVL_W;
                    if (returnCode == MdlConst.LVL_I && _fsDiffCopy.RmNgCount > 0) returnCode = MdlConst.LVL_W;
                    if (returnCode == MdlConst.LVL_I && _fsDiffCopy.NotFoundCount > 0) returnCode = MdlConst.LVL_W;
                    _prop.Files = _fsDiffCopy.CopyNewCount + _fsDiffCopy.CopyUpdateCount;
                    break;
                case ClsProp.ACTION_MKDIR:
                    returnCode = MdlFile.CreateDirectory(_prop.SourcePath);
                    switch (returnCode)
                    {
                        case MdlFile.OK_MKDIR_CREATE:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"OK : MKDIR {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_I;
                            break;
                        case MdlFile.OK_MKDIR_ALREADY_EXIST:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"OK : DIR ALREADY EXIST => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_I;
                            break;
                        case MdlFile.NG_MKDIR_FILE_EXIST:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : SAME NAME FILE ALREADY EXIST => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                        case MdlFile.NG_MKDIR_WRONG_ARG:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : INVALID ARGUMENT -f {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                        case MdlFile.NG_MKDIR:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : FAILD TO MKDIR {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                        default:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : FAILD({returnCode}) TO MKDIR {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                    }
                    break;
                case ClsProp.ACTION_TOUCH:
                    returnCode = MdlFile.CreateEmptyFile(_prop.SourcePath);
                    switch (returnCode)
                    {
                        case MdlFile.OK_TOUCH_CREATE:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"OK : TOUCH {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_I;
                            break;
                        case MdlFile.OK_TOUCH_ALREADY_EXIST:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"OK : FILE ALREADY EXIST => {_prop.SourcePath}");
                            try
                            {
                                returnCode = MdlFile.SetDateToFile(_prop.SourcePath, DateTime.Now, 0) ? MdlConst.LVL_I : MdlConst.LVL_W;
                            }
                            catch
                            {
                                returnCode = MdlConst.LVL_W;
                            }
                            break;
                        case MdlFile.NG_TOUCH_DIR_EXIST:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : SAME NAME DIR ALREADY EXIST => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                        case MdlFile.NG_TOUCH_WRONG_ARG:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : INVALID ARGUMENT -f {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                        case MdlFile.NG_TOUCH:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : FAILD TO TOUCH {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                        default:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : FAILD({returnCode}) TO TOUCH {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                    }
                    break;
                case ClsProp.ACTION_DELETE:
                    switch (MdlFile.GetPathType(_prop.SourcePath))
                    {
                        case MdlFile.PATH_IS_DIRECTORY:
                            switch (_prop.TypeCode)
                            {
                                case MdlConst.INT_TYPE_ALL:
                                case MdlConst.INT_TYPE_DIRECTORY:
                                    string typeText = _prop.TypeCode == MdlConst.INT_TYPE_ALL ? "ALL" : "DIRECTORY";
                                    if (_fsDiffCopy.RemoveRecursive(_prop.SourcePath))
                                    {
                                        _logger.WriteLine(MdlConst.LVL_NONE, $"OK : DELETE {typeText} => {_prop.SourcePath}");
                                        returnCode = MdlConst.LVL_I;
                                    }
                                    else
                                    {
                                        _logger.WriteLine(MdlConst.LVL_NONE, $"NG : DELETE {typeText} => {_prop.SourcePath}");
                                        returnCode = MdlConst.LVL_E;
                                    }
                                    break;
                                default:
                                    _logger.WriteLine(MdlConst.LVL_NONE, $"NG : DELETE FILE => {_prop.SourcePath}：NOT FILE, BUT DIRECTORY");
                                    break;
                            }
                            break;
                        case MdlFile.PATH_IS_FILE:
                            switch (_prop.TypeCode)
                            {
                                case MdlConst.INT_TYPE_ALL:
                                case MdlConst.INT_TYPE_FILE:
                                    string typeText = _prop.TypeCode == MdlConst.INT_TYPE_ALL ? "ALL" : "FILE";
                                    if (_fsDiffCopy.RemoveRecursive(_prop.SourcePath))
                                    {
                                        _logger.WriteLine(MdlConst.LVL_NONE, $"OK : DELETE {typeText} => {_prop.SourcePath}");
                                        returnCode = MdlConst.LVL_I;
                                    }
                                    else
                                    {
                                        _logger.WriteLine(MdlConst.LVL_NONE, $"NG : DELETE {typeText} => {_prop.SourcePath}");
                                        returnCode = MdlConst.LVL_E;
                                    }
                                    break;
                                default:
                                    _logger.WriteLine(MdlConst.LVL_NONE, $"NG : DELETE DIRECTORY => {_prop.SourcePath}：NOT DIRECTORY, BUT FILE");
                                    break;
                            }
                            break;
                        default:
                            if (_prop.IsSourceCheck)
                            {
                                _logger.WriteLine(MdlConst.LVL_NONE, $"NG : PATH NOT FOUND => {_prop.SourcePath}");
                                return MdlConst.LVL_E;
                            }
                            else
                            {
                                _logger.WriteLine(MdlConst.LVL_NONE, $"OK : PATH NOT FOUND => {_prop.SourcePath}");
                                returnCode = MdlConst.LVL_I;
                            }
                            break;
                    }
                    break;
                case ClsProp.ACTION_EXIST:
                    if (MdlFile.PathExists(_prop.SourcePath))
                    {
                        if (_prop.CheckFileLock > 0 && MdlFile.IsFileLocked(_prop.SourcePath))
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : FILE LOCKED => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                        }
                        else
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $"OK : PATH FOUND => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_I;
                        }
                    }
                    else
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, $"NG : PATH NOT FOUND => {_prop.SourcePath}");
                        returnCode = MdlConst.LVL_E;
                    }
                    break;
                case ClsProp.ACTION_EXIST_DIR:
                    switch (_prop.PathType)
                    {
                        case MdlFile.PATH_IS_DIRECTORY:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"OK : DIR FOUND => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_I;
                            break;
                        case MdlFile.PATH_IS_FILE:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : SAME NAME FILE ALREADY EXIST => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                        default:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : PATH NOT FOUND => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                    }
                    break;
                case ClsProp.ACTION_EXIST_FILE:
                    switch (_prop.PathType)
                    {
                        case MdlFile.PATH_IS_DIRECTORY:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : SAME NAME DIR ALREADY EXIST => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                        case MdlFile.PATH_IS_FILE:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"OK : FILE FOUND => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_I;
                            break;
                        default:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : PATH NOT FOUND => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                    }
                    break;
                case ClsProp.ACTION_WAIT:
                    bool isCheckFileLock = _prop.CheckFileLock > 0;
                    returnCode = _fsUtil.WaitUntilFileExists(_prop.SourcePath, _prop.MaxLoop, _prop.Interval, isCheckFileLock) ? MdlConst.LVL_I : MdlConst.LVL_E;
                    break;
                case ClsProp.ACTION_FILE_LOCKED:
                    if (MdlFile.PathExists(_prop.SourcePath))
                    {
                        if (MdlFile.IsFileLocked(_prop.SourcePath))
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $"YES LOCKED({MdlConst.LVL_W}) => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_W;
                        }
                        else
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NO LOCKED({MdlConst.LVL_I}) => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_I;
                        }
                    }
                    else
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, $"NO SUCH A FILE OR DIRECTORY({MdlConst.LVL_E}) => {_prop.SourcePath}");
                        returnCode = MdlConst.LVL_E;
                    }
                    break;
                case ClsProp.ACTION_LIST_LOCK_PROC:
                    _logger.WriteLine(MdlConst.LVL_E, "This feature is no longer supported.");
                    returnCode = MdlConst.LVL_E;
                    break;
                case ClsProp.ACTION_RENAME:
                    if (_prop.FileList.Count > 0)
                    {
                        if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "START : RENAME ------------------------------------------------------------");
                        returnCode = _find.Execute(ClsProp.TASK_RENAME) ? MdlConst.LVL_I : MdlConst.LVL_E;
                        if (_prop.Verbose > 1) _logger.WriteLine(MdlConst.LVL_NONE, "E N D : RENAME ------------------------------------------------------------");
                        if (_prop.Verbose > -1)
                        {
                            String message = "=== RENAME : NEW=" + _fsDiffCopy.CopyNewCount + " UPDATE=" + _fsDiffCopy.CopyUpdateCount + " ERR=" + _fsDiffCopy.CopyErrorCount + " / TOTAL=" + _fsDiffCopy.CopyTotalCount;
                            if (_fsDiffCopy.NotFoundCount > 0) message += " / NOT FOUND=" + _fsDiffCopy.NotFoundCount;
                            _logger.WriteLine(MdlConst.LVL_NONE, message);
                        }
                        if (returnCode == MdlConst.LVL_I && _fsDiffCopy.CopyErrorCount > 0) returnCode = MdlConst.LVL_E;
                        if (returnCode == MdlConst.LVL_I && _fsDiffCopy.MkdirNgCount > 0) returnCode = MdlConst.LVL_W;
                        if (returnCode == MdlConst.LVL_I && _fsDiffCopy.NotFoundCount > 0) returnCode = MdlConst.LVL_W;
                        _prop.Files = _fsDiffCopy.CopyNewCount + _fsDiffCopy.CopyUpdateCount;
                    }
                    else
                    {
                        returnCode = _fsUtil.Rename(_prop.SourcePath, _prop.DestinationPath) ? MdlConst.LVL_I : MdlConst.LVL_E;
                    }
                    break;
                case ClsProp.ACTION_ROTATE:
                    returnCode = _fsUtil.Rotate(_prop.SourcePath, _prop.MaxKeep);
                    break;
                case ClsProp.ACTION_MKLINK:
                    switch (_prop.PathType)
                    {
                        case MdlFile.PATH_IS_DIRECTORY:
                            _symLink.Verbose = 3;
                            returnCode = _symLink.CreateSymbolicLink(_prop.DestinationPath, _prop.SourcePath, MdlFile.PATH_IS_DIRECTORY, _prop.IntIsOverWrite > 0) ? 0 : 20;
                            break;
                        case MdlFile.PATH_IS_FILE:
                            _symLink.Verbose = 3;
                            returnCode = _symLink.CreateSymbolicLink(_prop.DestinationPath, _prop.SourcePath, MdlFile.PATH_IS_FILE, _prop.IntIsOverWrite > 0) ? 0 : 20;
                            break;
                        default:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NO SUCH A FILE OR DIRECTORY : -f {_prop.SourcePath}");
                            returnCode = 20;
                            break;
                    }
                    break;
                case ClsProp.ACTION_GET_REAL_PATH:
                    string realPath = "";
                    string option = "";
                    isOk = true;
                    switch (_prop.PathType)
                    {
                        case MdlFile.PATH_IS_DIRECTORY:
                            option = "/D ";
                            break;
                        case MdlFile.PATH_IS_FILE:
                            break;
                        default:
                            if (_prop.Verbose >= 0)
                            {
                                _logger.WriteLine(MdlConst.LVL_NONE, $"NO SUCH A FILE OR DIRECTORY : -f {_prop.SourcePath}");
                            }
                            isOk = false;
                            returnCode = MdlConst.LVL_E;
                            break;
                    }
                    if (isOk)
                    {
                        _symLink.Verbose = 0;
                        _symLink.IsSilent = true;
                        string targetPath = _prop.SourcePath;
                        if (MdlFile.IsSymlink(targetPath))
                        {
                            realPath = _symLink.GetRealPath(targetPath, _prop.IsRelative);
                            if (string.IsNullOrEmpty(realPath))
                            {
                                if (_prop.Verbose >= 0)
                                {
                                    _logger.WriteLine(MdlConst.LVL_NONE, $"ERROR : UNABLE TO GET REAL PATH : {targetPath}");
                                }
                            }
                            else
                            {
                                if (_prop.IsDq)
                                {
                                    realPath = $"\"{realPath}\"";
                                    targetPath = $"\"{targetPath}\"";
                                }
                                if (_prop.Verbose >= 0)
                                {
                                    _logger.WriteLine(MdlConst.LVL_NONE, $"mklink {option}{targetPath} {realPath}");
                                }
                                else
                                {
                                    _logger.WriteLine(MdlConst.LVL_NONE, realPath);
                                }
                            }
                        }
                        else
                        {
                            realPath = MdlFile.GetAbsolutePath(targetPath);
                            if (_prop.IsRelative) realPath = MdlFile.GetRelativePath(targetPath, realPath);
                            if (_prop.IsDq) realPath = $"\"{realPath}\"";
                            if (_prop.Verbose >= 0)
                            {
                                _logger.WriteLine(MdlConst.LVL_NONE, $"Absolute path = {realPath}");
                            }
                            else
                            {
                                _logger.WriteLine(MdlConst.LVL_NONE, realPath);
                            }
                        }
                    }
                    break;
                case ClsProp.ACTION_LS:
                    if (MdlFile.PathExists(_prop.SourcePath))
                    {
                        if (_prop.TypeCode is MdlConst.INT_TYPE_ALL or MdlConst.INT_TYPE_DIRECTORY)
                        {
                            foreach (string path in MdlFile.GetSortedDirectories(_prop.SourcePath, "*", System.IO.SearchOption.TopDirectoryOnly, _prop.SortType, _prop.IsAscending, _prop.IsShowDirList))
                            {
                                if (MdlFile.IsValidDateTime(path, _prop.IsBefore, _prop.BeforeTime, _prop.IsAfter, _prop.AfterTime))
                                {
                                    string line = MdlFile.GetDirectoryInfoString(path, _prop.Verbose, _prop.IsDq);
                                    _logger.WriteLine(MdlConst.LVL_NONE, line);
                                    _prop.Files++;
                                }
                            }
                        }
                        if (_prop.TypeCode is MdlConst.INT_TYPE_ALL or MdlConst.INT_TYPE_FILE)
                        {
                            foreach (string path in MdlFile.GetSortedFiles(_prop.SourcePath, "*", System.IO.SearchOption.TopDirectoryOnly, _prop.SortType, _prop.IsAscending, _prop.IsShowFileList))
                            {
                                if (MdlFile.IsValidDateTime(path, _prop.IsBefore, _prop.BeforeTime, _prop.IsAfter, _prop.AfterTime))
                                {
                                    string line = MdlFile.GetFileInfoString(path, _prop.Verbose, _prop.IsDq);
                                    bool isHit = false;
                                    string buff = "";
                                    switch(_prop.CheckFileLock)
                                    {
                                        case ClsProp.CHECK_FILE_LOCK_SAMPLE:
                                            if (MdlFile.IsFileLocked(path))
                                            {
                                                isHit = true;
                                            }
                                            else
                                            {
                                                buff = "[NOLOCKED]";
                                            }
                                            break;
                                        case ClsProp.CHECK_FILE_LOCK_SKIP:
                                            if (MdlFile.IsFileLocked(path))
                                            {
                                                buff = "[LOCKED]";
                                            }
                                            else
                                            {
                                                isHit = true;
                                            }
                                            break;
                                        default:
                                            isHit = true;
                                            break;
                                    }
                                    if (isHit)
                                    {
                                        _logger.WriteLine(MdlConst.LVL_NONE, line);
                                        _prop.Files++;
                                    }
                                    else
                                    {
                                        if (_prop.Verbose > 4) _logger.WriteLine(MdlConst.LVL_NONE, buff + line);
                                    }
                                }
                            }
                        }
                        returnCode = MdlConst.LVL_I;
                    }
                    else
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, $"NG : PATH NOT FOUND => {_prop.SourcePath}");
                        returnCode = MdlConst.LVL_E;
                    }
                    break;
                case ClsProp.ACTION_GET_SIZE:
                    if (_prop.IsProgress)
                    {
                        _fsAttrib.IsProgressEnabled = _prop.IsProgress;
                        _fsAttrib.ProgressIntervalDirectories = Math.Max(0, _prop.ProgressIntervalDirs);
                        _fsAttrib.ProgressIntervalFiles = Math.Max(0, _prop.ProgressIntervalFiles);
                    }
                    _fsAttrib.ClearCounter();

                    switch(MdlFile.GetPathType(_prop.SourcePath))
                    {
                        case MdlFile.PATH_IS_DIRECTORY:
                            if (_fsAttrib.CalculateDirectorySize(_prop.SourcePath, _prop.IsSymLink, _prop.Verbose, _prop.IsStackTrace))
                            {
                                returnCode = MdlConst.LVL_I;
                            }
                            else
                            {
                                returnCode = MdlConst.LVL_E;
                                _logger.WriteLine(MdlConst.LVL_NONE, $"ERRORS : DIRS={_fsAttrib.ErrorDirectoryCount} / FILES={_fsAttrib.ErrorFileCount}");
                            }
                            break;
                        case MdlFile.PATH_IS_FILE:
                            if (_fsAttrib.CalculateFileSize(_prop.SourcePath, _prop.IsSymLink, _prop.Verbose, _prop.IsStackTrace))
                            {
                                returnCode = MdlConst.LVL_I;
                            }
                            else
                            {
                                returnCode = MdlConst.LVL_E;
                            }
                            break;
                        default:
                            _logger.WriteLine(MdlConst.LVL_NONE, $"NG : PATH NOT FOUND => {_prop.SourcePath}");
                            returnCode = MdlConst.LVL_E;
                            break;
                    }
                    string sizeLine = "";
                    if (_prop.IsShowPath) sizeLine += string.IsNullOrEmpty(sizeLine) ? _prop.SourcePath : $",{_prop.SourcePath}";
                    if (_prop.IsShowDirNum) sizeLine += string.IsNullOrEmpty(sizeLine) ? _fsAttrib.DirectoryCount.ToString() : $",{_fsAttrib.DirectoryCount}";
                    if (_prop.IsShowFileNum) sizeLine += string.IsNullOrEmpty(sizeLine) ? _fsAttrib.FileCount.ToString() : $",{_fsAttrib.FileCount}";
                    if (_prop.IsShowSize) sizeLine += string.IsNullOrEmpty(sizeLine) ? _fsAttrib.TotalSize.ToString() : $",{_fsAttrib.TotalSize}";
                    _logger.WriteLine(MdlConst.LVL_NONE, sizeLine);
                    break;
                case ClsProp.ACTION_GET_PERM:
                case ClsProp.ACTION_GET_OWNER:
                    if (_prop.IsShowOwner && !_fsAttrib.OutputDirectoryOwner(_prop.SourcePath, _prop.Verbose, _prop.IsShowPath, _prop.IsStackTrace)) returnCode = MdlConst.LVL_E;
                    if (_prop.IsShowPerm && !_fsAttrib.OutputDirectoryPermission(_prop.SourcePath, _prop.Verbose, _prop.IsShowPath, _prop.IsStackTrace)) returnCode = MdlConst.LVL_E;
                    break;
                case ClsProp.ACTION_EXEC:
                    _cmdExec.IsShowCmd = _prop.IsShowCmd;
                    _cmdExec.IsShowExitCode = _prop.IsShowExitCode;
                    _cmdExec.IsShowOutput = _prop.IsShowOutput;
                    _cmdExec.Verbose = _prop.Verbose;
                    _cmdExec.IsStackTrace = _prop.IsStackTrace;
                    _cmdExec.IsShowEmptyLine = false;
                    if (_prop.IsCatRetWcl) _cmdExec.IsNotShowExitCode = true;
                    _cmdExec.WarnThreshold = _prop.WarnThreshold;
                    _cmdExec.ErrorThreshold = _prop.ErrorThreshold;
                    _cmdExec.IsErrorAtNegativeValue = _prop.IsErrorAtNegativeValue;
                    _cmdExec.IsAlwaysNormal = _prop.IsAlwaysNormal;
                    _cmdExec.Timeout = _prop.Timeout;
                    _cmdExec.Initialize();
                    if (_prop.IsCat)
                    {
                        _cmdExec.CmdPath = string.IsNullOrEmpty(_prop.CmdPath) ? $@"{_prop.ExeDir}\cat.exe" : _prop.CmdPath;
                        _cmdExec.CmdArgs = $" -f \"{_prop.SourcePath}\"";
                        if (!string.IsNullOrEmpty(_prop.CatI)) _cmdExec.CmdArgs += $" -i \"{_prop.CatI}\"";
                        if (!string.IsNullOrEmpty(_prop.CatX)) _cmdExec.CmdArgs += $" -x \"{_prop.CatX}\"";
                        if (!string.IsNullOrEmpty(_prop.CatP)) _cmdExec.CmdArgs += $" -p {_prop.CatP}";
                        if (!string.IsNullOrEmpty(_prop.CatE)) _cmdExec.CmdArgs += $" -e {_prop.CatE}";
                        if (!string.IsNullOrEmpty(_prop.CatXmlNl)) _cmdExec.CmdArgs += $" -xml-nl \"{_prop.CatXmlNl}\"";
                        if (!string.IsNullOrEmpty(_prop.CatOptions))
                        {
                            if (string.IsNullOrEmpty(_cmdExec.CmdArgs))
                            {
                                _cmdExec.CmdArgs = _prop.CatOptions;
                            }
                            else
                            {
                                _cmdExec.CmdArgs += $" {_prop.CatOptions}";
                            }
                        }
                        if (_prop.IsCatRetWcl) _cmdExec.CmdArgs += " -ret-wcl";
                        string showCmd = $"{_cmdExec.CmdPath} {_cmdExec.CmdArgs}";
                        if (_prop.IsSwitchUser || _prop.IsLogon)
                        {
                            _cmdExec.CmdArgs += $" -su -u {_prop.Username} -p {_prop.Password}";
                            showCmd += $" -su -u {_prop.Username} -p ****";
                        }
                        returnCode = _cmdExec.ExecuteThread(_prop.Priority);
                        if (0 != returnCode)
                        {
                            if (_prop.IsCatRetWcl && returnCode > 0)
                            {
                                _prop.Files++;
                                _prop.Lines += (ulong)returnCode;
                            }
                            else
                            {
                                _logger.WriteLine(MdlConst.LVL_NONE, $"[ERR] Cmd Return Code != 0 : {showCmd}");
                            }
                        }
                        else
                        {
                            if (!_prop.IsCatRetWcl) _prop.Files++;
                        }
                        returnCode = _cmdExec.MethodExitStatus;
                    }
                    else
                    {
                        string cmdArg = string.IsNullOrEmpty(_prop.CmdArgs)
                            ? MdlFile.ReplacePathForCmdExec(_prop.CmdPath, _prop.SourcePath, _prop.SourcePath, ".", _prop.IsDq, _prop.Verbose)
                            : MdlFile.ReplacePathForCmdExec($"{_prop.CmdPath} {_prop.CmdArgs}", _prop.SourcePath, _prop.SourcePath, ".", _prop.IsDq, _prop.Verbose);

                        switch (_prop.ExecModeCode)
                        {
                            case ClsProp.EXEC_MODE_CMD:
                                _cmdExec.CmdPath = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd";
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
                        returnCode = _cmdExec.ExecuteThread(_prop.Priority);
                        if (0 != returnCode)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $"[ERR] Cmd Return Code != 0 : cmd.exe {_cmdExec.CmdArgs}");
                        }
                        else
                        {
                            _prop.Files++;
                        }
                        returnCode = _cmdExec.MethodExitStatus;
                    }
                    break;
            }
            return returnCode;
        }

    }
}
