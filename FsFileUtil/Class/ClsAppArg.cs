using System;
using System.Text.RegularExpressions;
using CmnClsLib.Class;
using CmnClsLib.Module;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace FsFileUtil.Class
{
    public partial class ClsAppArg
    {
        [GeneratedRegex(@"[,\/|]")]
        private static partial Regex DelimiterRegex();

        [GeneratedRegex(@"^(?<SIGN>[+-]*)(?<VALUE>[\.\d]+)(?<UNIT>\D*)$")]
        private static partial Regex SizePatternRegex();

        private ClsLogger _logger;
        private ClsCmmnArgs _cmmnArgs;
        private ClsProp _prop;
        private string _exeDir = "";
        private string _exeBaseName = "";
        private int _pid = 0;
        private bool _isUsage = false;
        private bool _isEchoRetcode = false;
        private string _showMessageMode = "all";
        private bool _isAjsJob = false;
        private string _showTypeMode = "a";
        private string _periodUnit = "day";
        private double _periodTerm = 0.0;
        private bool _isNew = false;
        private string _fileShareMode = "3:ReadWrite";
        private string _formattedCompSize = "";

        /// <summary>
        /// <see cref="ClsAppArg"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="logger">ログ出力を行うためのロガーオブジェクト</param>
        /// <param name="prop">アプリケーションプロパティを保持するオブジェクト</param>
        /// <example>
        /// <code>
        /// ClsLogger logger = new ClsLogger();
        /// ClsProp prop = new ClsProp();
        /// ClsAppArg appArg = new ClsAppArg(logger, prop);
        /// </code>
        /// </example>
        public ClsAppArg(ClsLogger logger, ClsProp prop)
        {
            _logger = logger;
            _prop = prop;
            _cmmnArgs = new(_logger);
            _cmmnArgs.GetModuleInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");
            _exeDir = _cmmnArgs.ExeDir;
            _exeBaseName = _cmmnArgs.ExeBaseName;
            _pid = _cmmnArgs.Pid;
        }

        /// <summary>
        /// アプリケーションのプロパティ設定オブジェクトを取得または設定します。
        /// </summary>
        /// <example>
        /// <code>
        /// ClsProp prop = appArg.Properties;
        /// </code>
        /// </example>
        public ClsProp Properties { get { return _prop; } set { _prop = value; } }

        /// <summary>
        /// 実行ファイルベース名（拡張子なし）を取得または設定します。
        /// </summary>
        /// <example>
        /// <code>
        /// string exeName = appArg.ExeBaseName;
        /// </code>
        /// </example>
        public string ExeBaseName { get { return _exeBaseName; } set { _exeBaseName = value; } }

        /// <summary>
        /// 実行ファイルの格納ディレクトリパスを取得または設定します。
        /// </summary>
        /// <example>
        /// <code>
        /// string exeDir = appArg.ExeDir;
        /// </code>
        /// </example>
        public string ExeDir { get { return _exeDir; } set { _exeDir = value; } }

        /// <summary>
        /// 使用方法（Usage）ヘルプの表示要求フラグを取得します。
        /// </summary>
        /// <example>
        /// <code>
        /// if (appArg.IsUsage) { appArg.ShowUsage(); }
        /// </code>
        /// </example>
        public bool IsUsage { get { return _isUsage; } }

        /// <summary>
        /// 終了コードのエコー表示要求フラグを取得または設定します。
        /// </summary>
        /// <example>
        /// <code>
        /// appArg.IsEchoRetcode = true;
        /// </code>
        /// </example>
        public bool IsEchoRetcode { get { return _isEchoRetcode; } set { _isEchoRetcode = value; } }

        /// <summary>
        /// コマンドライン引数の配列を解析し、内部のプロパティや状態を設定します。
        /// </summary>
        /// <param name="args">コマンドライン引数の配列</param>
        /// <returns>引数の解析および事前チェックが正常に完了した場合は <c>true</c>。不正な引数が存在する場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// string[] args = ["-f", @"C:\Source", "-t", @"C:\Dest", "-a", "copy"];
        /// bool success = appArg.Parse(args);
        /// </code>
        /// </example>
        public bool Parse(string[] args)
        {
            Dictionary<string, string> namedArgs = [];
            bool isOk = true;
            string paramValue = "";
            bool isParamFound = false;

            // -----------------------------------------------------------------
            // ClsCmmnParams処理
            // -----------------------------------------------------------------
            namedArgs = MdlArg.GetNamedArgs(args);
            _cmmnArgs.NamedArgs = namedArgs;
            isOk = _cmmnArgs.GetCommonArgs();

            // -----------------------------------------------------------------
            // ClsCmmnParams引数取得：ETC
            // -----------------------------------------------------------------
            // -h|--help ：使用方法
            _isUsage = _cmmnArgs.IsUsage;
            // -v|-vv|-brief      ：冗長表示|簡素表示
            _prop.Verbose = _cmmnArgs.Verbose;
            // -stacktrace        ：例外時スタックトレース表示
            _prop.IsStackTrace = _cmmnArgs.IsStackTrace;
            // -nojp1              ：AJSJOBNAME参照フラグ
            _isAjsJob = _cmmnArgs.IsAjsJob;
            // -diff               ：差分表示フラグ
            if (_cmmnArgs.IsDiff) _prop.IsShowSameFile = false;
            // -timeout            ：タイムアウト（秒）
            _prop.Timeout = _cmmnArgs.Timeout;

            // -----------------------------------------------------------------
            // ClsCmmnParams引数取得：認証情報
            // -----------------------------------------------------------------
            isOk = _cmmnArgs.GetArgsForAuth();
            // -domain             ：ドメイン名
            _prop.DomainName = _cmmnArgs.DomainName;
            // -u|-user|-username n：ユーザ名
            _prop.Username = _cmmnArgs.Username;
            _prop.UsernameWithoutDomain = _cmmnArgs.UsernameWithoutDomain;
            // -p|-pass|-password p：パスワード
            _prop.Password = _cmmnArgs.Password;
            // -ignore-fail        ：認証エラー無視フラグ
            _prop.IsLogonAlwaysOk = _cmmnArgs.IsLogonAlwaysOk;
            // -su                 ：ユーザー認証実行フラグ
            _prop.IsSwitchUser = _cmmnArgs.IsSwitchUser;
            // -logon              ：ログオンフラグ
            _prop.IsLogon = _cmmnArgs.IsLogon;

            // -----------------------------------------------------------------
            // ClsCmmnParams引数取得：Net Use
            // -----------------------------------------------------------------
            if (isOk)
            {
                _cmmnArgs.GetNetUseArgs();
                _prop.NetSharePath = _cmmnArgs.NetSharePath;
                _prop.DriveName = _cmmnArgs.DriveName;
                _prop.IsMount = _cmmnArgs.IsMount;
                _prop.IsUmount = _cmmnArgs.IsUmount;
                _prop.NetUseOkErrNoList = _cmmnArgs.NetUseOkErrNoList;
            }

            // -----------------------------------------------------------------
            // Basic Option：
            // -----------------------------------------------------------------
            // -a    action       ：操作内容
            isParamFound = false;
            foreach (string key in (ReadOnlySpan<string>)["a"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        isParamFound = true;
                        _prop.Action = paramValue.ToLower();
                        switch (_prop.Action)
                        {
                            case "move":
                                _prop.ActionCode = ClsProp.ACTION_MOVE;
                                _prop.IsAlwaysMkDir = true;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = true;
                                break;
                            case "sync":
                                _prop.ActionCode = ClsProp.ACTION_SYNC;
                                _prop.IsAlwaysMkDir = true;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = true;
                                break;
                            case "syncrm":
                                _prop.ActionCode = ClsProp.ACTION_SYNC;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = true;
                                _prop.IsSyncRmOnly = true;
                                _prop.IsFileCopy = false;
                                break;
                            case "ls":
                                _prop.ActionCode = ClsProp.ACTION_LS;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                break;
                            case "find":
                                _prop.ActionCode = ClsProp.ACTION_FIND;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                _prop.TypeCode = MdlConst.INT_TYPE_FILE;
                                _prop.IsShowOutput = true;
                                break;
                            case "mkdir":
                                _prop.ActionCode = ClsProp.ACTION_MKDIR;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                break;
                            case "touch":
                                _prop.ActionCode = ClsProp.ACTION_TOUCH;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                break;
                            case "delete":
                                _prop.ActionCode = ClsProp.ACTION_DELETE;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                _prop.TypeCode = MdlConst.INT_TYPE_ALL;
                                break;
                            case "delete-dir":
                                _prop.ActionCode = ClsProp.ACTION_DELETE;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                _prop.TypeCode = MdlConst.INT_TYPE_DIRECTORY;
                                break;
                            case "delete-file":
                                _prop.ActionCode = ClsProp.ACTION_DELETE;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                _prop.TypeCode = MdlConst.INT_TYPE_FILE;
                                break;
                            case "exist":
                                _prop.ActionCode = ClsProp.ACTION_EXIST;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                break;
                            case "lock-proc":
                                _prop.ActionCode = ClsProp.ACTION_LIST_LOCK_PROC;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                _prop.CheckFileLock = ClsProp.CHECK_FILE_LOCK_SAMPLE;
                                // 2025/03/01 廃止
                                _logger.WriteLine(MdlConst.LVL_E, "INVALID ARGUMENT -a lock-proc : This feature is no longer supported.");
                                isOk = false;
                                break;
                            case "isdir":
                                _prop.ActionCode = ClsProp.ACTION_EXIST_DIR;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                break;
                            case "isfile":
                                _prop.ActionCode = ClsProp.ACTION_EXIST_FILE;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                break;
                            case "islocked":
                                _prop.ActionCode = ClsProp.ACTION_FILE_LOCKED;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                _prop.CheckFileLock = ClsProp.CHECK_FILE_LOCK_SAMPLE;
                                break;
                            case "wait":
                                _prop.ActionCode = ClsProp.ACTION_WAIT;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                break;
                            case "rename":
                                _prop.ActionCode = ClsProp.ACTION_RENAME;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = true;
                                break;
                            case "rotate":
                                _prop.ActionCode = ClsProp.ACTION_ROTATE;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                break;
                            case "reverse":
                                _prop.ActionCode = ClsProp.ACTION_COPY;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = true;
                                _prop.IsReverse = true;
                                _prop.IsAlwaysMkDir = false;
                                break;
                            case "flatcopy":
                                _prop.ActionCode = ClsProp.ACTION_COPY;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = true;
                                _prop.IsAlwaysMkDir = false;
                                _prop.IsFlat = true;
                                break;
                            case "dircopy":
                                _prop.ActionCode = ClsProp.ACTION_COPY;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = true;
                                _prop.IsFileCopy = false;
                                _prop.IsAlwaysMkDir = true;
                                break;
                            case "logon":
                                _prop.ActionCode = ClsProp.ACTION_NONE;
                                _prop.IsLogon = true;
                                _prop.IsLogoff = false;
                                // 2025/03/01 廃止
                                _logger.WriteLine(MdlConst.LVL_E, "INVALID ARGUMENT -a logon : This feature is no longer supported.");
                                isOk = false;
                                break;
                            case "logoff":
                                _prop.ActionCode = ClsProp.ACTION_NONE;
                                _prop.IsLogon = false;
                                _prop.IsLogoff = true;
                                // 2025/03/01 廃止
                                _logger.WriteLine(MdlConst.LVL_E, "INVALID ARGUMENT -a logoff : This feature is no longer supported.");
                                isOk = false;
                                break;
                            case "mount":
                                _prop.ActionCode = ClsProp.ACTION_NONE;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                _prop.IsMount = true;
                                _prop.IsUmount = false;
                                break;
                            case "umount":
                                _prop.ActionCode = ClsProp.ACTION_NONE;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                _prop.IsMount = false;
                                _prop.IsUmount = true;
                                break;
                            case "mklink":
                                _prop.ActionCode = ClsProp.ACTION_MKLINK;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = true;
                                _prop.IsSymLink = true;
                                break;
                            case "realpath":
                                _prop.ActionCode = ClsProp.ACTION_GET_REAL_PATH;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = false;
                                _prop.IsSymLink = true;
                                break;
                            case "exec":
                                _prop.ActionCode = ClsProp.ACTION_EXEC;
                                _prop.IsShowOutput = true;
                                _prop.IsNeedPathFr = true;
                                break;
                            case "size":
                                _prop.ActionCode = ClsProp.ACTION_GET_SIZE;
                                _prop.IsNeedPathFr = true;
                                break;
                            case "perm":
                                _prop.ActionCode = ClsProp.ACTION_GET_PERM;
                                _prop.IsNeedPathFr = true;
                                break;
                            case "owner":
                                _prop.ActionCode = ClsProp.ACTION_GET_OWNER;
                                _prop.IsNeedPathFr = true;
                                break;
                            default:
                                _prop.Action = "copy";
                                _prop.ActionCode = ClsProp.ACTION_COPY;
                                _prop.IsNeedPathFr = true;
                                _prop.IsNeedPathTo = true;
                                _prop.IsAlwaysMkDir = true;
                                break;
                        }
                        break;
                    }
                }
            }
            if (!isParamFound)
            {
                _prop.Action = "copy";
                _prop.ActionCode = ClsProp.ACTION_COPY;
                _prop.IsNeedPathFr = true;
                _prop.IsNeedPathTo = true;
                _prop.IsAlwaysMkDir = true;
            }

            // -path|-f path      ：操作対象パス
            if (_prop.IsNeedPathFr)
            {
                isParamFound = false;
                foreach (string key in (ReadOnlySpan<string>)["f", "path"])
                {
                    if (MdlArg.ContainsKey(namedArgs, key))
                    {
                        paramValue = MdlArg.GetValue(namedArgs, key);
                        if (!String.IsNullOrEmpty(paramValue))
                        {
                            isParamFound = true;
                            switch (_prop.Action)
                            {
                                case "mount":
                                case "umount":
                                    _prop.SourcePath = paramValue;
                                    _prop.IsNeedPathFr = false;
                                    break;
                                default:
                                    _prop.SourcePath = MdlFile.RemoveTrailingPathSeparator(MdlFile.GetAbsolutePath(paramValue));
                                    break;
                            }
                            break;
                        }
                    }
                }
                if (!isParamFound)
                {
                    switch (_prop.Action)
                    {
                        case "exec":
                        case "mount":
                        case "umount":
                            break;
                        default:
                            _logger.WriteLine(MdlConst.LVL_E, "INVALID ARGUMENT -path|-f");
                            isOk = false;
                            break;
                    }
                }
            }
            // パスの文字列置換：-f
            if (_prop.IsNeedPathFr && !string.IsNullOrEmpty(_prop.SourcePath))
            {
                if (_cmmnArgs.ReplaceDic.Count > 0)
                {
                    if (!string.IsNullOrEmpty(_prop.SourcePath)) _prop.SourcePath = _cmmnArgs.ReplaceByDictionary(_prop.SourcePath);
                }
            }

            // -----------------------------------------------------------------
            // Copy Option：
            // -----------------------------------------------------------------
            // -m    check        ：差分更新モード
            foreach (string key in new string[] { "m" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.Mode = paramValue.ToLower();
                        switch (_prop.Mode)
                        {
                            // サイズチェック有
                            case "size": _prop.CheckLogic = ClsProp.CHECK_SIZE; break;
                            case "mtime": _prop.CheckLogic = ClsProp.CHECK_MTIME; break;
                            case "new": _prop.CheckLogic = ClsProp.CHECK_MTIME_NEW; break;
                            case "old": _prop.CheckLogic = ClsProp.CHECK_MTIME_OLD; break;
                            case "cksum": _prop.CheckLogic = ClsProp.CHECK_CKSUM; break;
                            case "adler32": _prop.CheckLogic = ClsProp.CHECK_ADLER32; break;
                            case "sha1": _prop.CheckLogic = ClsProp.CHECK_SHA1; break;
                            // サイズチェック無
                            case "date":
                                _prop.CheckLogic = ClsProp.CHECK_MTIME;
                                _prop.IsSizeCheck = false;
                                break;
                            case "newer":
                                _prop.CheckLogic = ClsProp.CHECK_MTIME_NEW;
                                _prop.IsSizeCheck = false;
                                break;
                            case "older":
                                _prop.CheckLogic = ClsProp.CHECK_MTIME_OLD;
                                _prop.IsSizeCheck = false;
                                break;
                            case "exist":
                                _prop.CheckLogic = ClsProp.CHECK_EXIST;
                                _prop.IsSizeCheck = false;
                                break;
                            // デフォルト
                            default:
                                _prop.CheckLogic = ClsProp.CHECK_NONE;
                                _prop.Mode = "none";
                                break;
                        }
                        break;
                    }
                }
            }

            // -t    path         ：コピー・移動先パス
            if (_prop.IsNeedPathTo)
            {
                isParamFound = false;
                foreach (string key in new string[] { "t" })
                {
                    if (MdlArg.ContainsKey(namedArgs, key))
                    {
                        paramValue = MdlArg.GetValue(namedArgs, key);
                        if (!String.IsNullOrEmpty(paramValue))
                        {
                            isParamFound = true;
                            if (MdlFile.GetFileName(paramValue).Equals(@"."))
                            {
                                paramValue = MdlFile.GetDirectoryPath(paramValue) + @"\" + MdlFile.GetFileName(_prop.SourcePath);
                            }
                            _prop.DestinationPath = MdlFile.RemoveTrailingPathSeparator(MdlFile.GetAbsolutePath(paramValue));
                            break;
                        }
                    }
                }
                if (!isParamFound)
                {
                    _logger.WriteLine(MdlConst.LVL_E, "INVALID ARGUMENT -t");
                    isOk = false;
                }
            }

            // パスの文字列置換：-t
            if (!string.IsNullOrEmpty(_prop.DestinationPath))
            {
                if (_cmmnArgs.ReplaceDic.Count > 0)
                {
                    if (!string.IsNullOrEmpty(_prop.DestinationPath)) _prop.DestinationPath = _cmmnArgs.ReplaceByDictionary(_prop.DestinationPath);
                }
            }

            // -list              ：対象リストの表示
            foreach (string key in new string[] { "list" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsList = true;
                    break;
                }
            }

            // -tsc|-tsm|-ts      ：タイムスタンプ同期(1:作成日のみ|2:修正日のみ|3:全部)
            foreach (string key in new string[] { "tsc" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsCpTimestamp = 1;
                    break;
                }
            }
            foreach (string key in new string[] { "tsm" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsCpTimestamp = 2;
                    break;
                }
            }
            foreach (string key in new string[] { "ts" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsCpTimestamp = 3;
                    break;
                }
            }

            // -fchk              ：コピー元が存在しなければ異常終了
            foreach (string key in new string[] { "fchk" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsSourceCheck = true;
                    break;
                }
            }

            // -rmnohit           ：同期削除時の除外設定無効化フラグ
            foreach (string key in new string[] { "rmnohit" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsRmNohit = true;
                    break;
                }
            }

            // -no-emptydir       ：空ディレクトリ非コピーフラグ
            foreach (string key in new string[] { "no-emptydir" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsAlwaysMkDir = false;
                    break;
                }
            }

            // コピーコマンド区分
            foreach (string key in new string[] { "async" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.CopyCmdType = ClsProp.COPY_ASYNC;
                    break;
                }
            }
            foreach (string key in new string[] { "os" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.CopyCmdType = ClsProp.COPY_OS_CMD;
                    _prop.IsProgress = false;
                    break;
                }
            }

            // -skipsize|-copysize：cksum計算除外サイズ(MB)
            foreach (string key in new string[] { "skipsize" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.SkipSize = parsedInt * 1024 * 1024;
                        break;
                    }
                }
            }

            // -skipsize|-copysize：cksum計算除外サイズ(MB)
            foreach (string key in new string[] { "copysize" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.CopySize = parsedInt * 1024 * 1024;
                        break;
                    }
                }
            }

            // -fileshare mode    ：3:ReadWrite、7:ReadWrite|Delete 
            foreach (string key in new string[] { "fileshare" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.CheckFileLock = ClsProp.CHECK_FILE_LOCK_SKIP;
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        switch (paramValue.ToLower())
                        {
                            case "none":
                            case "0":
                                _fileShareMode = "0:None";
                                _prop.ObjFileShare = System.IO.FileShare.None;
                                break;
                            case "read":
                            case "1":
                                _fileShareMode = "1:Read";
                                _prop.ObjFileShare = System.IO.FileShare.Read;
                                break;
                            case "write":
                            case "2":
                                _fileShareMode = "2:Write";
                                _prop.ObjFileShare = System.IO.FileShare.Write;
                                break;
                            case "readwrite":
                            case "3":
                                _fileShareMode = "3:ReadWrite";
                                _prop.ObjFileShare = System.IO.FileShare.ReadWrite;
                                break;
                            case "delete":
                            case "4":
                                _fileShareMode = "4:Delete";
                                _prop.ObjFileShare = System.IO.FileShare.Delete;
                                break;
                            case "Read|delete":
                            case "5":
                                _fileShareMode = "5:Read|Delete";
                                _prop.ObjFileShare = System.IO.FileShare.Read | System.IO.FileShare.Delete;
                                break;
                            case "write|delete":
                            case "6":
                                _fileShareMode = "6:Write|Delete";
                                _prop.ObjFileShare = System.IO.FileShare.Write | System.IO.FileShare.Delete;
                                break;
                            case "readwrite|delete":
                            case "7":
                                _fileShareMode = "7:ReadWrite|Delete";
                                _prop.ObjFileShare = System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete;
                                break;
                            case "inheritable":
                            case "16":
                                _fileShareMode = "16:Inheritable";
                                _prop.ObjFileShare = System.IO.FileShare.Inheritable;
                                break;
                            case "Read|inheritable":
                            case "17":
                                _fileShareMode = "17:Read|Inheritable";
                                _prop.ObjFileShare = System.IO.FileShare.Read | System.IO.FileShare.Inheritable;
                                break;
                            case "write|inheritable":
                            case "18":
                                _fileShareMode = "18:Write|Inheritable";
                                _prop.ObjFileShare = System.IO.FileShare.Write | System.IO.FileShare.Inheritable;
                                break;
                            case "readwrite|inheritable":
                            case "19":
                                _fileShareMode = "19:ReadWrite|Inheritable";
                                _prop.ObjFileShare = System.IO.FileShare.ReadWrite | System.IO.FileShare.Inheritable;
                                break;
                            case "delete|inheritable":
                            case "20":
                                _fileShareMode = "20:Delete|Inheritable";
                                _prop.ObjFileShare = System.IO.FileShare.Delete | System.IO.FileShare.Inheritable;
                                break;
                            case "read|delete|inheritable":
                            case "21":
                                _fileShareMode = "21:Read|Delete|Inheritable";
                                _prop.ObjFileShare = System.IO.FileShare.Read | System.IO.FileShare.Delete | System.IO.FileShare.Inheritable;
                                break;
                            case "write|delete|inheritable":
                            case "22":
                                _fileShareMode = "22:Write|Delete|Inheritable";
                                _prop.ObjFileShare = System.IO.FileShare.Write | System.IO.FileShare.Delete | System.IO.FileShare.Inheritable;
                                break;
                            case "readwrite|delete|inheritable":
                            case "23":
                                _fileShareMode = "23:ReadWrite|Delete|Inheritable";
                                _prop.ObjFileShare = System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete | System.IO.FileShare.Inheritable;
                                break;
                            default:
                                _fileShareMode = "3:ReadWrite";
                                break;
                        }
                        break;
                    }
                }
            }

            // wait-retry-copy n ：Wait msec before retry copy
            foreach (string key in new string[] { "wait-retry-copy" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.WaitMSecForRetryCopy = parsedInt;
                        break;
                    }
                }
            }

            // -retry-syscopy n   ：例外時system copyリトライ回数
            foreach (string key in new string[] { "retry-syscopy" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.RetrySystemCopyMax = parsedInt;
                        break;
                    }
                }
            }

            // -----------------------------------------------------------------
            // Symbolic Link Option：
            // -----------------------------------------------------------------
            // -sym [0|1|2]       ：シンボリックリンク判定有効化
            foreach (string key in new string[] { "sym" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.IntIsOverWrite = parsedInt;
                        break;
                    }
                }
            }

            // -rel               ：シンボリックリンク相対パス化
            foreach (string key in new string[] { "rel" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsRelative = true;
                    break;
                }
            }

            // -----------------------------------------------------------------
            // Backup Option：
            // -----------------------------------------------------------------
            // -backup <path>     ：上書きファイル退避フラグ
            if (_prop.IsNeedPathTo)
            {
                isParamFound = false;
                foreach (string key in new string[] { "backup" })
                {
                    if (MdlArg.ContainsKey(namedArgs, key))
                    {
                        paramValue = MdlArg.GetValue(namedArgs, key);
                        if (!String.IsNullOrEmpty(paramValue))
                        {
                            isParamFound = true;
                            _prop.BackupDir = MdlFile.RemoveTrailingPathSeparator(MdlFile.GetAbsolutePath(paramValue));
                            if (_cmmnArgs.ReplaceDic.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(_prop.BackupDir)) _prop.BackupDir = _cmmnArgs.ReplaceByDictionary(_prop.BackupDir);
                            }
                        }
                    }
                    if (!isParamFound)
                    {
                        string strPathDBk = MdlFile.GetDirectoryPath(_prop.DestinationPath);
                        string strNameFBk = MdlFile.GetFileName(_prop.DestinationPath);
                        _prop.BackupDir = strPathDBk + @"\" + strNameFBk + ".%Y%m%d.%H%M%S." + _pid.ToString();
                    }
                }
            }

            // -force             ：退避失敗時処理強行フラグ
            _prop.IsErrorIfBackupFailed = _cmmnArgs.IsForce;

            // -----------------------------------------------------------------
            // Replace to path string Option：
            // -----------------------------------------------------------------

            // -replace a:b,c:d   ：-f|-tの文字列置換CSVリスト");
            // ClsCmmnParams.GetCommonArgs()

            // -ts-f|-ts-t|-ts-b n：-f|-t|-backup日付変換マクロ置換日付：n:now|t:today|y:yesterday|nextday:nextday|fotm:firstofthismonth|eolm:endoflastmonth|f:file"
            foreach (string key in new string[] { "ts-f" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.TsSource = paramValue;
                        break;
                    }
                }
            }
            foreach (string key in new string[] { "ts-t" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.TsDestination = paramValue;
                        break;
                    }
                }
            }
            foreach (string key in new string[] { "ts-b" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.TsBackup = paramValue;
                        break;
                    }
                }
            }

            // -----------------------------------------------------------------
            // Filter Option：
            // -----------------------------------------------------------------
            // 最大ディレクトリ階層
            foreach (string key in new string[] { "max" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.MaxDepth = (ulong)parsedInt;
                        break;
                    }
                }
            }

            // 最小ディレクトリ階層
            foreach (string key in new string[] { "min" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.MinDepth = (ulong)parsedInt;
                        break;
                    }
                }
            }

            if (_prop.MinDepth > _prop.MaxDepth)
            {
                isOk = false;
                _logger.WriteLine(MdlConst.LVL_E, "INVALID ARGUMENT : -min " + _prop.MinDepth + " > -max : " + _prop.MaxDepth);
            }

            // -period d|h|m|s    ：期間単位
            foreach (string key in new string[] { "period" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _periodUnit = paramValue.ToLower();
                        break;
                    }
                }
            }

            // -term|-days value  ：更新経過期間
            // -new               ：経過日数(-term)以内の場合
            foreach (string key in new string[] { "term", "days" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    bool isBaseTimeNow = false;
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        double parsedDbl = MdlUtil.ParseDouble(paramValue, MdlConst.DBL_NULL);
                        if (parsedDbl != MdlConst.DBL_NULL)
                        {
                            _periodTerm = parsedDbl;
                            switch (key)
                            {
                                case "term":
                                    isBaseTimeNow = true;
                                    break;
                            }
                        }
                        if (MdlArg.ContainsKey(namedArgs, "new"))
                        {
                            _prop.IsAfter = true;
                            _isNew = true;
                            if (isBaseTimeNow)
                            {
                                switch (_periodUnit)
                                {
                                    case "h":
                                        _prop.AfterTime = DateTime.Now.AddHours(-1.0 * parsedDbl);
                                        break;
                                    case "m":
                                        _prop.AfterTime = DateTime.Now.AddMinutes(-1.0 * parsedDbl);
                                        break;
                                    case "s":
                                        _prop.AfterTime = DateTime.Now.AddSeconds(-1.0 * parsedDbl);
                                        break;
                                    default:
                                        _prop.AfterTime = DateTime.Now.AddDays(-1.0 * parsedDbl);
                                        break;
                                }
                            }
                            else
                            {
                                switch (_periodUnit)
                                {
                                    case "h":
                                        _prop.AfterTime = DateTime.Today.AddHours(-1.0 * parsedDbl);
                                        break;
                                    case "m":
                                        _prop.AfterTime = DateTime.Today.AddMinutes(-1.0 * parsedDbl);
                                        break;
                                    case "s":
                                        _prop.AfterTime = DateTime.Today.AddSeconds(-1.0 * parsedDbl);
                                        break;
                                    default:
                                        _prop.AfterTime = DateTime.Today.AddDays(-1.0 * parsedDbl);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            _prop.IsBefore = true;
                            if (isBaseTimeNow)
                            {
                                switch (_periodUnit)
                                {
                                    case "h":
                                        _prop.BeforeTime = DateTime.Now.AddHours(-1.0 * parsedDbl);
                                        break;
                                    case "m":
                                        _prop.BeforeTime = DateTime.Now.AddMinutes(-1.0 * parsedDbl);
                                        break;
                                    case "s":
                                        _prop.BeforeTime = DateTime.Now.AddSeconds(-1.0 * parsedDbl);
                                        break;
                                    default:
                                        _prop.BeforeTime = DateTime.Now.AddDays(-1.0 * parsedDbl);
                                        break;
                                }
                            }
                            else
                            {
                                switch (_periodUnit)
                                {
                                    case "h":
                                        _prop.BeforeTime = DateTime.Today.AddHours(-1.0 * parsedDbl);
                                        break;
                                    case "m":
                                        _prop.BeforeTime = DateTime.Today.AddMinutes(-1.0 * parsedDbl);
                                        break;
                                    case "s":
                                        _prop.BeforeTime = DateTime.Today.AddSeconds(-1.0 * parsedDbl);
                                        break;
                                    default:
                                        _prop.BeforeTime = DateTime.Today.AddDays(-1.0 * parsedDbl);
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            // -before yyyyMMdd   ：更新日付が指定日以前
            foreach (string key in new string[] { "before" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        switch (paramValue)
                        {
                            case "now":
                                _prop.BeforeTime = DateTime.Now;
                                _prop.IsBefore = true;
                                break;
                            case "today":
                                _prop.BeforeTime = DateTime.Today;
                                _prop.IsBefore = true;
                                break;
                            case "lastday":
                            case "yesterday":
                                _prop.BeforeTime = DateTime.Today.AddDays(-1.0);
                                _prop.IsBefore = true;
                                break;
                            case "tomorrow":
                            case "nextday":
                                _prop.BeforeTime = DateTime.Today.AddDays(1.0);
                                _prop.IsBefore = true;
                                break;
                            default:
                                double parsedDbl = MdlUtil.ParseDouble(paramValue, MdlConst.DBL_NULL);
                                if (parsedDbl != MdlConst.DBL_NULL)
                                {
                                    if (parsedDbl < 19700101.0)
                                    {
                                        if (parsedDbl < 0.0)
                                        {
                                            _prop.BeforeTime = DateTime.Today.AddDays(parsedDbl);
                                            _prop.IsBefore = true;
                                        }
                                        else
                                        {
                                            _prop.BeforeTime = DateTime.Today.AddDays(parsedDbl);
                                            _prop.IsBefore = true;
                                        }
                                    }
                                    else
                                    {
                                        if (MdlDate.TryParseDateTime(paramValue, out DateTime dtTmp))
                                        {
                                            _prop.BeforeTime = dtTmp;
                                            _prop.IsBefore = true;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            // -after  yyyyMMdd   ：更新日付が指定日以降
            foreach (string key in new string[] { "after" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        switch (paramValue)
                        {
                            case "now":
                                _prop.AfterTime = DateTime.Now;
                                _prop.IsAfter = true;
                                break;
                            case "today":
                                _prop.AfterTime = DateTime.Today;
                                _prop.IsAfter = true;
                                break;
                            case "lastday":
                            case "yesterday":
                                _prop.AfterTime = DateTime.Today.AddDays(-1.0);
                                _prop.IsAfter = true;
                                break;
                            case "tomorrow":
                            case "nextday":
                                _prop.AfterTime = DateTime.Today.AddDays(1.0);
                                _prop.IsAfter = true;
                                break;
                            default:
                                double parsedDbl = MdlUtil.ParseDouble(paramValue, MdlConst.DBL_NULL);
                                if (parsedDbl != MdlConst.DBL_NULL)
                                {
                                    if (parsedDbl < 10101.0)
                                    {
                                        if (parsedDbl < 0.0)
                                        {
                                            _prop.AfterTime = DateTime.Today.AddDays(parsedDbl);
                                            _prop.IsAfter = true;
                                        }
                                        else
                                        {
                                            _prop.AfterTime = DateTime.Today.AddDays(parsedDbl);
                                            _prop.IsAfter = true;
                                        }
                                    }
                                    else
                                    {
                                        if (MdlDate.TryParseDateTime(paramValue, out DateTime dtTmp))
                                        {
                                            _prop.AfterTime = dtTmp;
                                            _prop.IsAfter = true;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            // -dirterm           ：ディレクトリ日付判定フラグ
            foreach (string key in new string[] { "dirterm" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsDirTerm = true;
                    break;
                }
            }

            // -size value        ：サイズ比較 >= +val | <= -val
            foreach (string key in new string[] { "size" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        Match matchSize = SizePatternRegex().Match(paramValue);
                        if (matchSize.Success)
                        {
                            string sign = MdlUtil.TrimQuotes(matchSize.Groups["SIGN"].Value);
                            string valueStr = MdlUtil.TrimQuotes(matchSize.Groups["VALUE"].Value);
                            string unit = MdlUtil.TrimQuotes(matchSize.Groups["UNIT"].Value);
                            Double dblVal = MdlUtil.ParseDouble(valueStr, 0.0);
                            _prop.CompSize = MdlUtil.ParseLong(valueStr, 0);
                            switch (unit.ToUpper())
                            {
                                case "KB":
                                    dblVal = dblVal * 1024.0;
                                    break;
                                case "MB":
                                    dblVal = dblVal * 1024.0 * 1024.0;
                                    break;
                                case "GB":
                                    dblVal = dblVal * 1024.0 * 1024.0 * 1024.0;
                                    break;
                                case "TB":
                                    dblVal = dblVal * 1024.0 * 1024.0 * 1024.0 * 1024.0;
                                    break;
                            }
                            _prop.CompSize = (long)dblVal;
                            switch (sign)
                            {
                                case "-":
                                    _prop.CompOpe = ClsProp.COMPARISON_LE;
                                    _formattedCompSize = "-" + MdlUtil.GetHumanReadableBytes(_prop.CompSize);
                                    break;
                                default:
                                    _prop.CompOpe = ClsProp.COMPARISON_GE;
                                    _formattedCompSize = MdlUtil.GetHumanReadableBytes(_prop.CompSize);
                                    break;
                            }
                        }
                    }
                }
            }

            // フィルター引数取得
            _cmmnArgs.GetFilterLists();
            // -if 正規表現       ：絞込ファイル名(,|/区切り)        (例：\\.log$,\\.dat$）
            _prop.IncFilesList = _cmmnArgs.IncFilesList;
            // -id 正規表現       ：絞込ディレクトリ名(,|/区切り)    (例：log,dat)
            _prop.IncDirsList = _cmmnArgs.IncDirsList;
            // -xf 正規表現       ：除外ファイル名(,|/区切り)        (例：\\.log$,\\.dat$）
            _prop.ExcFilesList = _cmmnArgs.ExcFilesList;
            // -xd 正規表現       ：除外ディレクトリ名(,|/区切り)    (例：log,dat)
            _prop.ExcDirsList = _cmmnArgs.ExcDirsList;
            // 「-idf 正規表現リスト」が指定された場合はフルパスで評価(eXclude Directory with Fullpath)
            _prop.IsRegIncBasename = _cmmnArgs.IsRegIncBasename;
            // 「-xdf 正規表現リスト」が指定された場合はフルパスで評価(eXclude Directory with Fullpath)
            _prop.IsRegExcBasename = _cmmnArgs.IsRegExcBasename;
            // -idorxd            ：-id or -xdフラグ
            _prop.IsDirFilterOr = _cmmnArgs.IsDirFilterOr;
            // -no-id-rec         ：-id結果の階層下への非適用フラグ
            _prop.IsIncHitRecursive = _cmmnArgs.IsIncHitRecursive;
            // -no-xd-rec         ：-xd結果の階層下への非適用フラグ
            _prop.IsExcHitRecursive = _cmmnArgs.IsExcHitRecursive;

            // -xd-exc-p-dir      ：-xd該当時親DIRコピーフラグ   _prop.IsXdOnlyFiles
            foreach (string key in new string[] { "xd-exc-p-dir" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsXdOnlyFiles = true;
                    break;
                }
            }

            // -locked [sample]   ：ロックファイル除外、又は抽出
            foreach (string key in new string[] { "locked" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.CheckFileLock = ClsProp.CHECK_FILE_LOCK_SKIP;
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        switch (paramValue.ToLower())
                        {
                            case "sample":
                                _prop.CheckFileLock = ClsProp.CHECK_FILE_LOCK_SAMPLE;
                                break;
                        }
                        break;
                    }
                }
            }

            // -----------------------------------------------------------------
            // Copy With List File Option：
            // -----------------------------------------------------------------
            // -files path        ：コピー対象相対パスリスト
            foreach (string key in new string[] { "files" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.FileListPath = paramValue;
                        ClsConfigFile objConfigFile = new(_logger)
                        {
                            ConfigList = _prop.FileList
                        };
                        if (objConfigFile.LoadToList(_prop.FileListPath, true) < 1)
                        {
                            isOk = false;
                            _logger.WriteLine(MdlConst.LVL_E, "INVALID ARGUMENT : -files " + _prop.FileListPath);
                        }
                        break;
                    }
                }
            }

            // -files-type type   ：パスリスト形式(rel|full)
            foreach (string key in new string[] { "files-type" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        switch (paramValue)
                        {
                            case "full":
                                _prop.FileListType = "full";
                                _prop.FilesTypeCode = ClsProp.FILES_FULL;
                                _prop.IsNeedPathFr = false;
                                _prop.IsNeedPathTo = false;
                                _prop.IsFrPathCheck = false;
                                break;
                        }
                        break;
                    }
                }
            }

            // -files-Regex regex ：パスリストデリミタ正規表現
            foreach (string key in new string[] { "files-regex" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.FileListRegex = paramValue;
                    break;
                }
            }

            // -----------------------------------------------------------------
            // Find Or Commnad Exec Cmd Option：
            // -----------------------------------------------------------------
            // -dq                ：-a find|ls時のDQ囲み有無
            foreach (string key in new string[] { "dq" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsDq = true;
                    break;
                }
            }

            // -type f|d|a        ：-a find|ls時の表示対象
            foreach (string key in new string[] { "type" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        if ("f".Equals(paramValue))
                        {
                            _showTypeMode = paramValue;
                            _prop.TypeCode = MdlConst.INT_TYPE_FILE;
                        }
                        else if ("d".Equals(paramValue))
                        {
                            _showTypeMode = paramValue;
                            _prop.TypeCode = MdlConst.INT_TYPE_DIRECTORY;
                            switch (_prop.ActionCode)
                            {
                                case ClsProp.ACTION_COPY:
                                    _prop.IsFileCopy = false;
                                    break;
                            }
                        }
                        else if ("a".Equals(paramValue))
                        {
                            _showTypeMode = paramValue;
                            _prop.TypeCode = MdlConst.INT_TYPE_ALL;
                        }
                        else if ("b".Equals(paramValue))
                        {
                            _showTypeMode = paramValue;
                            _prop.TypeCode = MdlConst.INT_TYPE_ALL;
                        }
                        break;
                    }
                }
            }

            // -exec|-ps cmd {}   ：実行コマンド
            // C:\Tool\Infra\bin.cur\FsFileUtil.exe -f G:\HostInfo -type f -a find -if .cs$ -exec "C:\Tool\Infra\bin.cur\cat.exe -f {} -i objParam -n -e SJIS"
            // C:\Tool\Infra\bin.cur\FsFileUtil.exe -f G:\HostInfo -type f -a find -if .cs$ -exec "C:\Tool\Infra\bin.cur\FsFileUtil.exe -f G:\backup\_RELPATH_ -a mkdir && C:\Progra~1\7-Zip\7z.exe a G:\backup\_RELPATH_\_FILENAME_.zip {}"
            // C:\Tool\Infra\bin.cur\FsFileUtil.exe -f G:\HostInfo -type f -a find -if .cs$ -exec "C:\Tool\Infra\bin.cur\FsFileUtil.exe -f G:\backup -a mkdir && C:\Progra~1\7-Zip\7z.exe a G:\backup\_RELFLAT___FILENAME_.zip {}"
            foreach (string key in new string[] { "exec" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.CmdPath = paramValue;
                        _prop.CmdPath = _prop.CmdPath.Replace("_FSFILEUTIL_", _exeDir + Path.DirectorySeparatorChar + _exeBaseName + ".exe");
                        break;
                    }
                }
            }
            foreach (string key in new string[] { "ps" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.CmdPath = paramValue;
                        _prop.ExecModeCode = ClsProp.EXEC_MODE_PS;
                        break;
                    }
                }
            }

            // -exec-args args    ：実行コマンド引数
            // C:\Tool\Infra\bin.cur\FsFileUtil.exe -f G:\HostInfo -type f -a find -if .cs$ -exec "C:\Tool\Infra\bin.cur\cat.exe" -exec-args "\-f {} -i objParam -n -e SJIS"
            foreach (string key in new string[] { "exec-args" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.CmdArgs = paramValue;
                        break;
                    }
                }
            }

            // -exec-mode mode    ：cmd|exe|ps
            foreach (string key in new string[] { "exec-mode", "cnd-mode" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        switch(paramValue.ToLower())
                        {
                            case "cmd":
                                _prop.ExecModeCode = ClsProp.EXEC_MODE_CMD;
                                break;
                            case "c":
                                _prop.ExecModeCode = ClsProp.EXEC_MODE_EXE;
                                break;
                            case "exe":
                                _prop.ExecModeCode = ClsProp.EXEC_MODE_EXE;
                                break;
                            case "ps":
                                _prop.ExecModeCode = ClsProp.EXEC_MODE_PS;
                                break;
                        }
                        break;
                    }
                }
            }

            // -cwd [path]        ：ワーキングディレクトリ
            foreach (string key in new string[] { "cwd" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    isParamFound = false;
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        isParamFound = true;
                        _prop.WorkDir = paramValue;
                    }
                    if (!isParamFound)
                    {
                        _prop.WorkDir = _prop.SourcePath;
                    }
                }
            }

            // -w int             ：警告閾値                   _prop.WarnThreshold
            foreach (string key in new string[] { "w" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.WarnThreshold = parsedInt;
                        break;
                    }
                }
            }

            // -e int             ：異常閾値                   _prop.ErrorThreshold
            foreach (string key in new string[] { "e" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.ErrorThreshold = parsedInt;
                        break;
                    }
                }
            }

            // -normal            ：常に正常終了               _prop.IsAlwaysNormal
            foreach (string key in new string[] { "normal" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsAlwaysNormal = true;
                    break;
                }
            }

            // -negative          ：負値のエラー判定有無       _prop.IsErrorAtNegativeValue
            foreach (string key in new string[] { "negative" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsErrorAtNegativeValue = true;
                    break;
                }
            }

            // -show-cmd y|n      ：実行コマンド表示
            foreach (string key in new string[] { "show-cmd" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        switch (paramValue.ToLower())
                        {
                            case "false":
                            case "no":
                            case "n":
                                _prop.IsShowCmd = false;
                                break;
                        }
                        break;
                    }
                }
            }

            // -show-output y|n   ：実行コマンド出力表示
            foreach (string key in new string[] { "show-output" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        switch (paramValue.ToLower())
                        {
                            case "false":
                            case "no":
                            case "n":
                                _prop.IsShowOutput = false;
                                break;
                        }
                        break;
                    }
                }
            }

            // -show-retcd y|n    ：実行コマンド結果表示
            foreach (string key in new string[] { "show-retcd" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        switch (paramValue.ToLower())
                        {
                            case "false":
                            case "no":
                            case "n":
                                _prop.IsShowExitCode = false;
                                break;
                        }
                        break;
                    }
                }
            }

            // -cat-options o1,o2 ：cat.exe実行オプションリスト
            List<string> _listOptons = [];
            foreach (string key in new string[] { "cat-options" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.IsCat = true;
                        _listOptons = MdlUtil.ParseCsvToList(_listOptons, paramValue, @"[,\/|]", 0, false);
                        break;
                    }
                }
            }

            // -cat-i|x|p|e|xml-nl：cat.exe実行オプション
            // C:\Tool\Infra\bin.cur\FsFileUtil.exe -f G:\HostInfo -type f -a find -if .cs$ -cat-i "objParam" -cat-e SJIS -cat-options "n|wcl"
            foreach (string key in new string[] { "cat" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsCat = true;
                    break;
                }
            }

            foreach (string key in new string[] { "cat-i" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.IsCat = true;
                        _prop.CatI = paramValue;
                        break;
                    }
                }
            }
            foreach (string key in new string[] { "cat-x" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.IsCat = true;
                        _prop.CatX = paramValue;
                        break;
                    }
                }
            }
            foreach (string key in new string[] { "cat-p" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.IsCat = true;
                        _prop.CatP = paramValue;
                        break;
                    }
                }
            }
            foreach (string key in new string[] { "cat-xml-nl" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.IsCat = true;
                        _prop.CatP = "xml";
                        _prop.CatXmlNl = paramValue;
                        break;
                    }
                }
            }
            foreach (string key in new string[] { "cat-e" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.IsCat = true;
                        _prop.CatE = paramValue;
                        break;
                    }
                }
            }
            foreach (string key in new string[] { "cat-a" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsCat = true;
                    if (!_listOptons.Contains("a")) _listOptons.Add("a");
                    break;
                }
            }
            foreach (string key in new string[] { "cat-wcl" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsCat = true;
                    if (!_listOptons.Contains("wcl")) _listOptons.Add("wcl");
                    break;
                }
            }
            foreach (string key in new string[] { "cat-ret-wcl" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsCat = true;
                    _prop.IsCatRetWcl = true;
                    break;
                }
            }
            foreach (string key in new string[] { "cat-n" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsCat = true;
                    if (!_listOptons.Contains("n")) _listOptons.Add("n");
                    break;
                }
            }
            foreach (string key in new string[] { "cat-h" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsCat = true;
                    if (!_listOptons.Contains("h")) _listOptons.Add("h");
                    break;
                }
            }
            foreach (String strOption in _listOptons)
            {
                if (string.IsNullOrEmpty(_prop.CatOptions))
                {
                    _prop.CatOptions = "-" + strOption;
                }
                else
                {
                    _prop.CatOptions += " -" + strOption;
                }
            }
            if (_prop.IsCat)
            {
                if (string.IsNullOrEmpty(_prop.CmdPath))
                {
                    _prop.CmdPath = _exeDir + @"\cat.exe";
                }
            }

            // -nice int          ：プロセス優先度(0:RealTime - 5:Idle)
            foreach (string key in new string[] { "priority", "nice" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.Priority = parsedInt;
                        break;
                    }
                }
            }

            // -n                 ：パス表示フラグ
            foreach (string key in new string[] { "n" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsShowPath = true;
                    if (!_listOptons.Contains("n")) _listOptons.Add("n");
                    break;
                }
            }

            // -----------------------------------------------------------------
            // Wait Option：
            // -----------------------------------------------------------------
            // -c count           ：確認回数(回)
            foreach (string key in new string[] { "c" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.MaxLoop = parsedInt;
                        break;
                    }
                }
            }

            // -i interval        ：確認間隔(秒) 
            foreach (string key in new string[] { "i" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.Interval = parsedInt;
                        break;
                    }
                }
            }

            // -----------------------------------------------------------------
            // Rotate Option：
            // -----------------------------------------------------------------
            // 最大保存世代数
            foreach (string key in new string[] { "k" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.MaxKeep = parsedInt;
                        break;
                    }
                }
            }

            // -----------------------------------------------------------------
            // Network Option：
            // -----------------------------------------------------------------
            // -sec-range
            foreach (string key in new string[] { "sec-range" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.SecRange = parsedInt;
                        break;
                    }
                }
            }

            // ※CmmnParams Option
            // -su                ：ユーザー認証実行フラグ          _prop.IsSwitchUser
            // -mount path        ：ネットワーク共有パス            _prop.NetSharePath
            // -drive [A-Z]       ：マウントドライブ文字            _prop.DriveName
            // -def path          ：アカウント設定ファイルパス      _cmmnArgs.AuthDefFilePath
            // -u username        ：ドメイン名\\ユーザー名           _prop.Username
            // -p password        ：パスワード                      _prop.Password
            // -ignore-fail       ：認証エラー無視フラグ            _prop.IsLogonAlwaysOk
            // -mount-ok-no i     ：net useの戻り値で正常と見なすエラー番号リスト(,|/区切り)")
            // -no-mount          ：NET USE 非接続フラグ            _prop.IsMount
            // -no-umount         ：NET USE 非切断フラグ            _prop.IsUmount

            // 操作内容別引数チェック
            if (!string.IsNullOrEmpty(_prop.Action))
            {
                switch (_prop.Action)
                {
                    case "mount":
                    case "umount":
                        if (string.IsNullOrEmpty(_prop.NetSharePath))
                        {
                            if (!string.IsNullOrEmpty(_prop.SourcePath))
                            {
                                _prop.NetSharePath = _prop.SourcePath;
                            }
                        }
                        break;
                }
            }

            // -----------------------------------------------------------------
            // Output Option：
            // -----------------------------------------------------------------
            // -v|-vv|-brief      ：冗長表示|簡素表示

            // -progress          ：進捗表示
            foreach (string key in new string[] { "progress" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _prop.IsProgress = true;
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        List<String> listTmp = [];
                        listTmp = MdlUtil.ParseCsvToList(listTmp, paramValue, @"[,\/\.\-|]", 0, false);
                        if (listTmp.Count > 0 && MdlUtil.IsNumeric(listTmp[0])) _prop.ProgressIntervalDirs = MdlUtil.ParseInt(listTmp[0], 0);
                        if (listTmp.Count > 1 && MdlUtil.IsNumeric(listTmp[1])) _prop.ProgressIntervalFiles = MdlUtil.ParseInt(listTmp[1], 0);
                    }
                    if (0 == _prop.ProgressIntervalDirs && 0 == _prop.ProgressIntervalFiles)
                    {
                        _prop.ProgressIntervalDirs = 1000;
                        _prop.ProgressIntervalFiles = 100000;
                    }
                }
            }

            // -show show         ：表示内容：new|updated|diff
            isParamFound = false;
            foreach (string key in new string[] { "show" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        isParamFound = true;
                        switch (_prop.ActionCode)
                        {
                            case ClsProp.ACTION_GET_SIZE:
                                switch (paramValue.ToLower())
                                {
                                    case "":
                                        _prop.IsShowSize = true;
                                        break;
                                    case "all":
                                        _prop.IsShowPath = true;
                                        _prop.IsShowDirNum = true;
                                        _prop.IsShowFileNum = true;
                                        _prop.IsShowSize = true;
                                        break;
                                    default:
                                        if (paramValue.ToLower().Contains("p")) _prop.IsShowPath = true;
                                        if (paramValue.ToLower().Contains("d")) _prop.IsShowDirNum = true;
                                        if (paramValue.ToLower().Contains("f")) _prop.IsShowFileNum = true;
                                        if (paramValue.ToLower().Contains("s")) _prop.IsShowSize = true;
                                        break;
                                }
                                break;
                            case ClsProp.ACTION_GET_PERM:
                                switch (paramValue.ToLower())
                                {
                                    case "":
                                        _prop.IsShowPerm = true;
                                        break;
                                    case "all":
                                        _prop.IsShowPath = true;
                                        _prop.IsShowOwner = true;
                                        _prop.IsShowPerm = true;
                                        break;
                                    default:
                                        if (paramValue.ToLower().Contains("p")) _prop.IsShowPath = true;
                                        if (paramValue.ToLower().Contains("o")) _prop.IsShowOwner = true;
                                        if (paramValue.ToLower().Contains("r")) _prop.IsShowPerm = true;
                                        break;
                                }
                                break;
                            case ClsProp.ACTION_GET_OWNER:
                                switch (paramValue.ToLower())
                                {
                                    case "":
                                        _prop.IsShowOwner = true;
                                        break;
                                    case "all":
                                        _prop.IsShowPath = true;
                                        _prop.IsShowOwner = true;
                                        break;
                                    default:
                                        if (paramValue.ToLower().Contains("p")) _prop.IsShowPath = true;
                                        if (paramValue.ToLower().Contains("o")) _prop.IsShowOwner = true;
                                        if (paramValue.ToLower().Contains("r")) _prop.IsShowPerm = true;
                                        break;
                                }
                                break;
                            default:
                                switch (paramValue.ToLower())
                                {
                                    case "new":
                                    case "n":
                                        _showMessageMode = "new";
                                        _prop.IsShowUpdatedFile = false;
                                        _prop.IsShowSameFile = false;
                                        break;
                                    case "updated":
                                    case "u":
                                        _showMessageMode = "updated";
                                        _prop.IsShowNewFile = false;
                                        _prop.IsShowSameFile = false;
                                        break;
                                    case "diff":
                                    case "modified":
                                    case "m":
                                    case "nu":
                                    case "un":
                                        _showMessageMode = "diff";
                                        _prop.IsShowSameFile = false;
                                        break;
                                    default:
                                        _showMessageMode = "all";
                                        break;
                                }
                                break;
                        }
                    }
                    if (!isParamFound)
                    {
                        switch (_prop.ActionCode)
                        {
                            case ClsProp.ACTION_GET_SIZE:
                                _prop.IsShowSize = true;
                                break;
                            case ClsProp.ACTION_GET_PERM:
                                _prop.IsShowPerm = true;
                                break;
                            case ClsProp.ACTION_GET_OWNER:
                                _prop.IsShowOwner = true;
                                break;
                        }
                    }
                }
            }

            // -op-path r|f|t|b   ：画面表示パス種別
            foreach (string key in new string[] { "op-path" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.OutputPathCode = _prop.GetOutputModeCode(paramValue);
                        break;
                    }
                }
            }

            // -op-prefix         ：画面表示相対パス付加PREFIX
            foreach (string key in new string[] { "op-prefix" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        _prop.OutputPathPrefix = paramValue;
                        break;
                    }
                }
            }

            // ※CmmnParams Option
            // -stacktrace        ：例外時スタックトレース表示

            // -show-dir max      ：処理中ディレクトリの表示
            foreach (string key in new string[] { "show-dir" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    paramValue = MdlArg.GetValue(namedArgs, key);
                    if (!String.IsNullOrEmpty(paramValue))
                    {
                        int parsedInt = MdlUtil.ParseInt(paramValue, MdlConst.INT_NULL);
                        if (parsedInt != MdlConst.INT_NULL) _prop.IsShowCurDir = parsedInt;
                        break;
                    }
                }
            }

            // -echo-retcd        ：終了コード表示フラグ
            foreach (string key in new string[] { "echo-retcd" })
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _isEchoRetcode = true;
                    break;
                }
            }

            if (OperatingSystem.IsWindows()) _prop.IsSymLink = false;

            // -----------------------------------------------------------------
            // 後処理
            // -----------------------------------------------------------------
            namedArgs.Clear();

            // -----------------------------------------------------------------
            // END
            // -----------------------------------------------------------------
            return isOk;
        }

        /// <summary>
        /// コマンドライン引数の使用方法（Usage）および指定可能な各種オプション一覧をログに出力します。
        /// </summary>
        /// <example>
        /// <code>
        /// appArg.ShowUsage();
        /// </code>
        /// </example>
        public void ShowUsage()
        {
            _logger.WriteLine(MdlConst.LVL_NONE, "");
            _logger.WriteLine(MdlConst.LVL_NONE, "Usage : " + _exeDir + Path.DirectorySeparatorChar + _exeBaseName + ".exe -f <path> -t <path> [Option] [Option]...");
            _logger.WriteLine(MdlConst.LVL_NONE, "");
            _logger.WriteLine(MdlConst.LVL_NONE, "Basic Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -path|-f path      ：操作対象パス                    （現在値=" + _prop.SourcePath + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -a    action       ：操作内容                        （現在値=" + _prop.Action + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "                        コピー      ：copy(初期値)|sync|move|reverse|dircopy|syncrm|flatcopy");
            _logger.WriteLine(MdlConst.LVL_NONE, "                        ファイル操作：ls|find|mkdir|touch|delete|delete-dir|delete-file|rename|rotate|isLocked");
            _logger.WriteLine(MdlConst.LVL_NONE, "                        コマンド実行：exec");
            _logger.WriteLine(MdlConst.LVL_NONE, "                        SYMLINK     ：mklink|realpath");
            _logger.WriteLine(MdlConst.LVL_NONE, "                        存在確認    ：exist|isdir|isfile|wait");
            _logger.WriteLine(MdlConst.LVL_NONE, "                        属性表示    ：size|perm|owner");
            _logger.WriteLine(MdlConst.LVL_NONE, "                        NETWORK認証 ：mount|umount");
            _logger.WriteLine(MdlConst.LVL_NONE, "                        ※廃止：.NET8  ：lock-proc|logon|logoff");
            _logger.WriteLine(MdlConst.LVL_NONE, "Copy Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -t    path         ：コピー・移動先パス              （現在値=" + _prop.DestinationPath + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -m    check        ：差分更新モード                  （現在値=" + _prop.Mode + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "                        サイズチェック有：size | new | old | mtime | cksum | adler32 | sha1");
            _logger.WriteLine(MdlConst.LVL_NONE, "                        サイズチェック無：date | newer | older | exist");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -list              ：対象リストの表示                （現在値=" + _prop.IsList + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -tsc|-tsm|-ts      ：タイムスタンプ同期(1:作成日のみ|2:修正日のみ|3:全部)（現在値=" + _prop.IsCpTimestamp + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -fchk              ：コピー元が存在しなければ異常終了（現在値=" + _prop.IsSourceCheck + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -rmnohit           ：同期削除時の除外設定無効化フラグ（現在値=" + _prop.IsRmNohit + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -no-emptydir       ：空ディレクトリ非コピーフラグ    （現在値=" + (_prop.IsAlwaysMkDir ? "False" : "True") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -async             ：非同期コピーフラグ              （現在値=" + (_prop.CopyCmdType == ClsProp.COPY_ASYNC ? true : false) + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -os                ：OSコピー/移動フラグ             （現在値=" + (_prop.CopyCmdType == ClsProp.COPY_OS_CMD ? true : false) + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -skipsize|-copysize：cksum計算除外サイズ(MB)         （現在値=" + (_prop.SkipSize / 1024 / 1024) + " / " + (_prop.CopySize / 1024 / 1024) + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -fileshare mode    ：3:ReadWrite、7:ReadWrite|Delete （現在値=" + _fileShareMode + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -wait-retry-copy n ：Wait msec before retry copy     （現在値=" + _prop.WaitMSecForRetryCopy + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -retry-syscopy n   ：例外時system copyリトライ回数   （現在値=" + _prop.RetrySystemCopyMax + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Symbolic Link Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -sym [0|1|2]       ：シンボリックリンク判定有効化    （現在値=" + _prop.IsSymLink + " OverWrite=" + _prop.IntIsOverWrite + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -rel               ：シンボリックリンク相対パス化    （現在値=" + _prop.IsRelative + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Backup Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -backup <path>     ：上書きファイル退避フラグ        （現在値=" + (_prop.IsBackup ? _prop.BackupDir : "False") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -force             ：退避失敗時処理強行フラグ        （現在値=" + (_prop.IsErrorIfBackupFailed ? "False" : "True") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Replace to path string Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -replace a:b,c:d   ：-f|-tの文字列置換CSVリスト");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -ts-f|-ts-t|-ts-b n：-f|-t|-backup日付変換マクロ置換日付：n:now|t:today|y:yesterday|nextday:nextday|fotm:firstofthismonth|eolm:endoflastmonth|f:file");
            _logger.WriteLine(MdlConst.LVL_NONE, "Filter Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -max               ：最大ディレクトリ階層            （現在値=" + _prop.MaxDepth + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -min               ：最小ディレクトリ階層            （現在値=" + _prop.MinDepth + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -term|-days value  ：更新経過期間                    （現在値=" + _periodTerm + ")");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -period d|h|m|s    ：期間単位                        （現在値=" + _periodUnit + ")");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -new               ：経過日数(-term)以内の場合       （現在値=" + _isNew + ")");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -before yyyyMMdd   ：更新日付が指定日以前            （現在値=" + (_prop.IsBefore ? MdlDate.GetFormattedDate(_prop.BeforeTime, "yyyyMMdd") + "：" + MdlDate.GetFormattedDate(_prop.BeforeTime, "yyyy/MM/dd HH:mm:ss") : "") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -after  yyyyMMdd   ：更新日付が指定日以降            （現在値=" + (_prop.IsAfter ? MdlDate.GetFormattedDate(_prop.AfterTime, "yyyyMMdd") + "：" + MdlDate.GetFormattedDate(_prop.AfterTime, "yyyy/MM/dd HH:mm:ss") : "") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -dirterm           ：ディレクトリ日付判定フラグ      （現在値=" + _prop.IsDirTerm + ")");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -size value        ：サイズ比較 >= +val | <= -val    （現在値=" + _formattedCompSize + ")");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -id|-idf 正規表現  ：絞込ディレクトリ名(,|/区切り）  （現在値=[" + string.Join("|", _prop.IncDirsList.ToArray()) + "] FullPath=" + !_prop.IsRegIncBasename + ")");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -xd|-xdf 正規表現  ：除外ディレクトリ名(,|/区切り）  （現在値=[" + string.Join("|", _prop.ExcDirsList.ToArray()) + "] FullPath=" + !_prop.IsRegExcBasename + ")");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -if 正規表現       ：絞込ファイル名(,|/区切り)     (例：\\.log$,\\.dat$）（現在値=[" + string.Join("|", _prop.IncFilesList.ToArray()) + "])");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -xf 正規表現       ：除外ファイル名(,|/区切り)     (例：\\.exe$,\\.dll$）（現在値=[" + string.Join("|", _prop.ExcFilesList.ToArray()) + "])");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -idorxd            ：-id or -xdフラグ                （現在値=" + _prop.IsDirFilterOr + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -no-id-rec         ：-id結果の階層下への非適用フラグ （現在値=" + !_prop.IsIncHitRecursive + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -no-xd-rec         ：-xd結果の階層下への非適用フラグ （現在値=" + !_prop.IsExcHitRecursive + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -xd-exc-p-dir      ：-xd該当時親DIRコピーフラグ      （現在値=" + _prop.IsXdOnlyFiles + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -locked [sample]   ：ロックファイル除外、又は抽出    （現在値=" + _prop.GetCheckLockFileModeStr(_prop.CheckFileLock) + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Copy With List File Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -files path        ：コピー対象相対パスリスト        （現在値=" + _prop.FileListPath + " / ファイル数=" + _prop.FileList.Count + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -files-type type   ：パスリスト形式(rel|full)        （現在値=" + _prop.FileListType + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -files-Regex regex ：パスリストデリミタ正規表現      （現在値=" + _prop.FileListRegex + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Find Or Commnad Exec Cmd Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -dq                ：-a find|ls時のDQ囲み有無        （現在値=" + _prop.IsDq + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -type f|d|a        ：-a find|ls時の表示対象          （現在値=" + _showTypeMode + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -exec|-ps cmd {}   ：実行コマンド                    （現在値=" + (_prop.CmdPath).Trim() + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -exec-args args    ：実行コマンド引数                （現在値=" + (_prop.CmdArgs).Trim() + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -exec-mode mode    ：cmd|exe|ps                      （現在値=" + _prop.GetExecModeStr(_prop.ExecModeCode) + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -cwd [path]        ：ワーキングディレクトリ          （現在値=" + _prop.WorkDir + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -w int             ：警告閾値                        （現在値=" + _prop.WarnThreshold + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -e int             ：異常閾値                        （現在値=" + _prop.ErrorThreshold + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -normal            ：常に正常終了                    （現在値=" + _prop.IsAlwaysNormal + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -negative          ：負値のエラー判定有無            （現在値=" + _prop.IsErrorAtNegativeValue + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -show-cmd y|n      ：実行コマンド表示                （現在値=" + _prop.IsShowCmd + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -show-output y|n   ：実行コマンド出力表示            （現在値=" + _prop.IsShowOutput + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -show-retcd y|n    ：実行コマンド結果表示            （現在値=" + _prop.IsShowExitCode + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -cat-i|x|p|e|xml-nl：cat.exe実行オプション           （現在値=" + _prop.IsCat + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -cat-options o1,o2 ：cat.exe実行オプションリスト     （現在値=" + _prop.CatOptions + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -cat-ret-wcl       ：cat.exe -ret-wcl行数戻値フラグ  （現在値=" + _prop.IsCatRetWcl + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -nice int          ：プロセス優先度（0:RealTime - 5:Idle：現在値=" + _prop.Priority + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -n                 ：パス表示フラグ                  （現在値=" + _prop.IsShowPath + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -timeout           ：TIMEOUT(秒)                     （現在値=" + _prop.Timeout + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Wait Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -i interval        ：確認間隔(秒)                    （現在値=" + _prop.Interval + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -c count           ：確認回数(回)                    （現在値=" + _prop.MaxLoop + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Rotate Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -k keep max        ：最大保存世代数(個)              （現在値=" + _prop.MaxKeep + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Network Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -sec-range         ：タイムスタンプずれ許容範囲（秒）（現在値=" + _prop.SecRange + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -su                ：ユーザー認証実行フラグ          （現在値=" + _prop.IsSwitchUser + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -mount path        ：ネットワーク共有パス            （現在値=" + _prop.NetSharePath + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -drive [A-Z]       ：マウントドライブ文字            （現在値=" + _prop.DriveName + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -def path          ：アカウント設定ファイルパス      （現在値=" + _cmmnArgs.AuthDefFilePath + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -u username        ：ドメイン名\\ユーザー名           （現在値=" + _prop.DomainName + "\\" + _prop.UsernameWithoutDomain + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -p password        ：パスワード                      （現在値=" + _prop.Password + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -ignore-fail       ：認証エラー無視フラグ            （現在値=" + _prop.IsLogonAlwaysOk + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -mount-ok-no i     ：net useの戻り値で正常と見なすエラー番号リスト(,|/区切り)");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -no-mount          ：NET USE 非接続フラグ            （現在値=" + (_prop.IsMount ? "False" : "True") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -no-umount         ：NET USE 非切断フラグ            （現在値=" + (_prop.IsUmount ? "False" : "True") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Subfolder Sorting Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -sort type         ：ソート=none|name|ctime|mtime    （現在値=" + MdlFile.GetSortTypeName(_prop.SortType) + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -desc              ：降順フラグ                      （現在値=" + !_prop.IsAscending + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Output Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -v|-vv|-brief      ：冗長表示|簡素表示               （現在値=" + _prop.Verbose + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -progress          ：進捗表示                        （現在値=" + _prop.IsProgress + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -diff              ：差分表示フラグ                  （現在値=" + !_prop.IsShowSameFile + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -show show         ：表示内容：new|updated|diff      （現在値=" + _showMessageMode + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -op-path r|f|t|b   ：画面表示パス種別                （現在値=" + _prop.GetOutputModeStr(_prop.OutputPathCode) + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -op-prefix         ：画面表示相対パス付加PREFIX      （現在値=" + _prop.OutputPathPrefix + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -stacktrace        ：例外時スタックトレース表示      （現在値=" + _prop.IsStackTrace + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -show-dir max      ：処理中ディレクトリの表示        （現在値=" + _prop.IsShowCurDir + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -echo-retcd        ：終了コード表示フラグ            （現在値=" + _isEchoRetcode + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -console mode      ：メッセージ表示 off|stdout|stderr");
            _logger.WriteLine(MdlConst.LVL_NONE, "Other Option：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -ldir path         ：ログ出力先ディレクトリパス(日付付ファイル名で出力)");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -log  path         ：ログ出力ファイルパス(-ldirより優先)");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -dumpargs          ：引数の表示");
            _logger.WriteLine(MdlConst.LVL_NONE, "   -ret-files         ：ファイル数戻値フラグ            （現在値=" + _prop.IsRetFiles + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Format specifier conversion：");
            _logger.WriteLine(MdlConst.LVL_NONE, "   ファイルパス       ：{}、_PATH_、_RELPATH_、_RELFLAT_");
            _logger.WriteLine(MdlConst.LVL_NONE, "   ディレクトリパス   ：_BASEDIR_、_DIR_、_RELDIR_、_RELDIRFLAT_");
            _logger.WriteLine(MdlConst.LVL_NONE, "   ファイル・他       ：_FILENAME_、_BASENAME_、_COMPUTERNAME_、_USERNAME_");
            _logger.WriteLine(MdlConst.LVL_NONE, "   日時               ：%Y、%m、%d、%H、%M、%S、%w、%pid");
            _logger.WriteLine(MdlConst.LVL_NONE, "");
            _logger.WriteLine(MdlConst.LVL_NONE, "Return Code           ：" + MdlConst.LVL_I + ":SUCCESS / " + MdlConst.LVL_W + ":WARN / " + MdlConst.LVL_E + ":ERROR");
            _logger.WriteLine(MdlConst.LVL_NONE, "");
        }

        /// <summary>
        /// 解析された現在の設定情報および対象パス一覧をログに出力します。
        /// </summary>
        /// <example>
        /// <code>
        /// appArg.PrintDefinition();
        /// </code>
        /// </example>
        public void PrintDefinition()
        {
            string temp = "";
            _logger.WriteLine(MdlConst.LVL_NONE, "------------------------------------------------------------");
            if (ClsProp.ACTION_NONE != _prop.ActionCode) _logger.WriteLine(MdlConst.LVL_NONE, "TARGET PATH : " + _prop.SourcePath);
            if (_prop.IsNeedPathTo) _logger.WriteLine(MdlConst.LVL_NONE, "TO PATH     : " + _prop.DestinationPath);
            if (_prop.IsSwitchUser || _prop.IsLogon)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "SU USERNAME : " + _prop.Username);
                if (_prop.Verbose > 3 || _prop.Password.Equals("_THIS_IS_PASSWORD_")) _logger.WriteLine(MdlConst.LVL_NONE, "SU PASSWORD : " + _prop.Password);
            }
            if (_prop.IsMount && !string.IsNullOrEmpty(_prop.NetSharePath))
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "MOUNT PATH  : " + (string.IsNullOrEmpty(_prop.DriveName) ? _prop.NetSharePath : _prop.NetSharePath + " => " + _prop.DriveName + ":"));
                _logger.WriteLine(MdlConst.LVL_NONE, "USERNAME    : " + _prop.Username);
                if (_prop.Verbose > 3 || (!string.IsNullOrEmpty(_prop.Password) && _prop.Password.Equals("_THIS_IS_PASSWORD_"))) _logger.WriteLine(MdlConst.LVL_NONE, "PASSWORD    : " + _prop.Password);
            }
            if (_prop.IsUmount)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "UMOUNT PATH  : " + (string.IsNullOrEmpty(_prop.DriveName) ? _prop.NetSharePath : _prop.DriveName + ":"));
            }
            switch (_prop.ActionCode)
            {
                case ClsProp.ACTION_SYNC: temp = "sync (-rmnohit=" + _prop.IsRmNohit.ToString() + ")"; break;
                default: temp = _prop.Action; break;
            }
            _logger.WriteLine(MdlConst.LVL_NONE, "ACTION      : " + temp);
            if (_prop.IsNeedPathTo)
            {
                switch (_prop.CheckLogic)
                {
                    case ClsProp.CHECK_SIZE: temp = "CHECK : FILE SIZE"; break;
                    case ClsProp.CHECK_MTIME_NEW: temp = (_prop.IsSizeCheck ? "CHECK : FILE SIZE | MTIME(NEW)" : "CHECK : MTIME(NEW)"); break;
                    case ClsProp.CHECK_MTIME_OLD: temp = (_prop.IsSizeCheck ? "CHECK : FILE SIZE | MTIME(OLD)" : "CHECK : MTIME(OLD)"); break;
                    case ClsProp.CHECK_MTIME: temp = (_prop.IsSizeCheck ? "CHECK : FILE SIZE | MTIME" : "CHECK : MTIME"); break;
                    case ClsProp.CHECK_CKSUM: temp = "CHECK : FILE SIZE | cksum"; break;
                    case ClsProp.CHECK_SHA1: temp = "CHECK : FILE SIZE | sha1"; break;
                    case ClsProp.CHECK_ADLER32: temp = "CHECK : FILE SIZE | adler32"; break;
                    case ClsProp.CHECK_EXIST: temp = "CHECK : FILE EXIST OR NOT"; break;
                    default: temp = "NONE"; break;
                }
                _logger.WriteLine(MdlConst.LVL_NONE, "DIFF MODE   : " + temp);
                _logger.WriteLine(MdlConst.LVL_NONE, "FILTER INC  : DIR = [" + string.Join("|", _prop.IncDirsList.ToArray()) + "] / FILE = [" + string.Join("|", _prop.IncFilesList.ToArray()) + "]");
                _logger.WriteLine(MdlConst.LVL_NONE, "FILTER EXC  : DIR = [" + string.Join("|", _prop.ExcDirsList.ToArray()) + "] / FILE = [" + string.Join("|", _prop.ExcFilesList.ToArray()) + "]");
            }
            if (_prop.ActionCode == ClsProp.ACTION_WAIT)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "MAX COUNT   : " + _prop.MaxLoop);
                _logger.WriteLine(MdlConst.LVL_NONE, "INTERVAL    : " + _prop.Interval);
                _logger.WriteLine(MdlConst.LVL_NONE, "SKIP LOCKED : " + (ClsProp.CHECK_FILE_LOCK_SKIP == _prop.CheckFileLock ? "True" : "False"));
            }
            if (_prop.ActionCode == ClsProp.ACTION_ROTATE)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "MAX KEEP    : " + _prop.MaxKeep);

            }
            if (_prop.IsList) _logger.WriteLine(MdlConst.LVL_NONE, "LIST ONLY   : TRUE");
            _logger.WriteLine(MdlConst.LVL_NONE, "------------------------------------------------------------\n");
        }

        /// <summary>
        /// 指定されたタイムスタンプモード（"today", "yesterday", "file"等）に基づいて対象の <see cref="DateTime"/> を取得します。
        /// </summary>
        /// <param name="timestampMode">タイムスタンプの取得モード指定文字列（"t", "today", "y", "yesterday", "fotm", "eolm", "f", "file"）</param>
        /// <param name="path">ファイルまたはディレクトリのパス（モードが "file" / "f" の場合に使用）</param>
        /// <param name="pathType">パスの種類（ファイル: <c>MdlFile.PATH_IS_FILE</c>、ディレクトリ: <c>MdlFile.PATH_IS_DIR</c>）</param>
        /// <returns>計算またはファイル/ディレクトリから取得された <see cref="DateTime"/> オブジェクト。</returns>
        /// <example>
        /// <code>
        /// DateTime today = appArg.GetTimestamp("today", "", 0);
        /// DateTime fileTime = appArg.GetTimestamp("file", @"C:\temp\test.txt", MdlFile.PATH_IS_FILE);
        /// </code>
        /// </example>
        public DateTime GetTimestamp(string timestampMode, string path, int pathType)
        {
            DateTime ts = DateTime.Now;
            if (!string.IsNullOrEmpty(timestampMode))
            {
                switch (timestampMode.ToLower())
                {
                    case "t":
                    case "today":
                        ts = DateTime.Today;
                        break;
                    case "y":
                    case "yesterday":
                        ts = DateTime.Today.AddDays(-1);
                        break;
                    case "nextday":
                        ts = DateTime.Today.AddDays(+1);
                        break;
                    case "fotm":
                    case "firstofthismonth":
                        ts = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        break;
                    case "eolm":
                    case "endoflastmonth":
                        ts = (new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).AddDays(-1);
                        break;
                    case "f":
                    case "file":
                        if (MdlFile.PathExists(path))
                        {
                            ts = (MdlFile.PATH_IS_FILE == pathType ? System.IO.File.GetCreationTime(path) : System.IO.Directory.GetCreationTime(path));
                        }
                        break;
                }
            }
            return ts;
        }

    }
}
