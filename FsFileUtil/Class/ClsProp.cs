using System;
using System.Collections.Generic;
using CmnClsLib.Module;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace FsFileUtil.Class
{
    public class ClsProp
    {
        public const int RELATIVE = 0;
        public const int FROM = 1;
        public const int TO = 2;
        public const int BOTH = 3;

        public const int FILES_RELATIVE = 0;
        public const int FILES_FULL = 1;

        public const int COMPARISON_NO = 0;
        public const int COMPARISON_EQ = 1;
        public const int COMPARISON_GT = 2;
        public const int COMPARISON_GE = 3;
        public const int COMPARISON_LT = 4;
        public const int COMPARISON_LE = 5;

        public const int DATETIME_NOW = 0;
        public const int DATETIME_TODAY = 1;
        public const int DATETIME_YESTERDAY = 2;
        public const int DATETIME_FILEINFO = 3;

        public const int EXEC_MODE_NORMAL = 0;
        public const int EXEC_MODE_CMD = 1;
        public const int EXEC_MODE_PS = 2;
        public const int EXEC_MODE_PSC = 3;
        public const int EXEC_MODE_EXE = 4;

        public const int CHECK_FILE_LOCK_NONE = 0;
        public const int CHECK_FILE_LOCK_SAMPLE = 1;
        public const int CHECK_FILE_LOCK_SKIP = 2;

        public const int COPY_ASYNC = 0;
        public const int COPY_BINARY = 1;
        public const int COPY_OS_CMD = 2;

        public const int ACTION_NONE = -1;
        public const int ACTION_COPY = 0;
        public const int ACTION_MOVE = 1;
        public const int ACTION_SYNC = 2;
        public const int ACTION_MKDIR = 10;
        public const int ACTION_TOUCH = 11;
        public const int ACTION_DELETE = 12;
        public const int ACTION_MKLINK = 13;
        public const int ACTION_LS = 15;
        public const int ACTION_FIND = 16;
        public const int ACTION_GET_REAL_PATH = 17;
        public const int ACTION_LIST_LOCK_PROC = 18;
        public const int ACTION_EXIST = 20;
        public const int ACTION_EXIST_DIR = 21;
        public const int ACTION_EXIST_FILE = 22;
        public const int ACTION_WAIT = 23;
        public const int ACTION_FILE_LOCKED = 24;
        public const int ACTION_RENAME = 30;
        public const int ACTION_ROTATE = 31;
        public const int ACTION_GET_ATTRIB = 41;
        public const int ACTION_GET_SIZE = 42;
        public const int ACTION_GET_PERM = 43;
        public const int ACTION_GET_OWNER = 44;
        public const int ACTION_EXEC = 91;
        public const int ACTION_ETC = 99;

        public const int CHECK_NONE = 0;
        public const int CHECK_SIZE = 1;
        public const int CHECK_MTIME = 2;
        public const int CHECK_MTIME_NEW = 3;
        public const int CHECK_MTIME_OLD = 4;
        public const int CHECK_CKSUM = 5;
        public const int CHECK_SHA1 = 6;
        public const int CHECK_ADLER32 = 7;
        public const int CHECK_EXIST = 8;

        public const int TASK_CP = 0;
        public const int TASK_MV = 1;
        public const int TASK_RM = 2;
        public const int TASK_PRINT = 3;
        public const int TASK_RENAME = 4;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ClsProp()
        {
        }

        public string ExeBaseName { get; set; } = "";
        public string ExeDir { get; set; } = "";
        public string SourcePath { get; set; } = "";
        public string DestinationPath { get; set; } = "";
        public string WorkDir { get; set; } = "";
        public string Action { get; set; } = "copy";
        public string Mode { get; set; } = "";
        public int CopyCmdType { get; set; } = COPY_BINARY;
        public int ActionCode { get; set; } = ACTION_COPY;
        public int Task { get; set; } = TASK_CP;
        public int CheckLogic { get; set; } = CHECK_NONE;
        public int CompOpe { get; set; } = COMPARISON_NO;
        public int PathType { get; set; } = MdlFile.PATH_IS_NULL;
        public bool IsNeedPathFr { get; set; } = false;
        public bool IsNeedPathTo { get; set; } = false;
        public bool IsList { get; set; } = false;
        public bool IsReverse { get; set; } = false;
        public bool IsSizeCheck { get; set; } = true;
        public bool IsSyncRmOnly { get; set; } = false;
        public bool IsFlat { get; set; } = false;
        public bool IsDirTerm { get; set; } = false;
        public bool IsAlwaysMkDir { get; set; } = false;
        public bool IsFileCopy { get; set; } = true;
        public bool IsSkip { get; set; } = false;
        public int CheckFileLock { get; set; } = CHECK_FILE_LOCK_NONE;
        public bool IsSourceCheck { get; set; } = false;
        public bool IsFrPathCheck { get; set; } = true;
        public bool IsRetFiles { get; set; } = false;
        public long SkipSize { get; set; } = 0;
        public long CopySize { get; set; } = 0;
        public long CompSize { get; set; } = 0;
        public int Interval { get; set; } = 60;
        public int MaxLoop { get; set; } = 1;
        public bool IsBackup { get; set; } = false;
        public bool IsErrorIfBackupFailed { get; set; } = true;

        public string BackupDir { get; set; } = "";

        public System.IO.FileShare ObjFileShare { get; set; } = System.IO.FileShare.ReadWrite;
        public int WaitMSecForRetryCopy { get; set; } = 200;
        public int RetrySystemCopyMax { get; set; } = 0;
        public double SecRange { get; set; } = 0.0;
        public int Verbose { get; set; } = 0;
        public int IsShowCurDir { get; set; } = 0;
        public int OutputPathCode { get; set; } = RELATIVE;
        public int ProgressIntervalDirs { get; set; } = 0;
        public int ProgressIntervalFiles { get; set; } = 0;
        public bool IsRelative { get; set; } = false;
        public bool IsProgress { get; set; } = false;
        public bool IsStackTrace { get; set; } = false;
        public bool IsShowNewFile { get; set; } = true;
        public bool IsShowUpdatedFile { get; set; } = true;
        public bool IsShowSameFile { get; set; } = true;
        public string OutputPathPrefix { get; set; } = "";
        public bool IsShowPath { get; set; } = false;
        public bool IsShowSize { get; set; } = false;
        public bool IsShowDirNum { get; set; } = false;
        public bool IsShowFileNum { get; set; } = false;
        public bool IsShowPerm { get; set; } = false;
        public bool IsShowOwner { get; set; } = false;
        public bool IsSymLink { get; set; } = false;
        public int IntIsOverWrite { get; set; } = 0;
        public int MaxKeep { get; set; } = 7;
        public string CmdPath { get; set; } = "";
        public string CmdArgs { get; set; } = "";
        public bool IsDq { get; set; } = false;
        public int WarnThreshold { get; set; } = MdlConst.INT_NULL;
        public int ErrorThreshold { get; set; } = MdlConst.INT_NULL;
        public bool IsExecCmd { get; set; } = false;
        public int ExecModeCode { get; set; } = EXEC_MODE_EXE;
        public int Priority { get; set; } = 3;
        public int Timeout { get; set; } = 86400;
        public bool IsErrorAtNegativeValue { get; set; } = false;
        public bool IsAlwaysNormal { get; set; } = false;
        public bool IsShowCmd { get; set; } = false;
        public bool IsShowOutput { get; set; } = false;
        public bool IsShowExitCode { get; set; } = false;
        public bool IsCat { get; set; } = false;
        public bool IsCatRetWcl { get; set; } = false;
        public string CatI { get; set; } = "";
        public string CatX { get; set; } = "";
        public string CatP { get; set; } = "";
        public string CatE { get; set; } = "";
        public string CatXmlNl { get; set; } = "";
        public string CatOptions { get; set; } = "";
        public ulong Files { get; set; } = 0;
        public ulong Lines { get; set; } = 0;
        public bool IsLogonAlwaysOk { get; set; } = false;
        public bool IsMount { get; set; } = false;
        public bool IsUmount { get; set; } = false;
        public bool IsSwitchUser { get; set; } = false;
        public bool IsLogon { get; set; } = false;
        public bool IsLogoff { get; set; } = false;
        public string NetSharePath { get; set; } = "";
        public string DriveName { get; set; } = "";
        public string DomainName { get; set; } = "";
        public string Username { get; set; } = "";
        public string UsernameWithoutDomain { get; set; } = "";
        public string Password { get; set; } = "_THIS_IS_PASSWORD_";
        public List<int> NetUseOkErrNoList { get; set; } = [];
        public int TypeCode { get; set; } = MdlConst.INT_TYPE_ALL;
        public ulong MaxDepth { get; set; } = MdlConst.ULNG_MAX;
        public ulong MinDepth { get; set; } = 0;
        public bool IsBefore { get; set; } = false;
        public bool IsAfter { get; set; } = false;
        public DateTime BeforeTime { get; set; }
        public DateTime AfterTime { get; set; }
        public bool IsRegIncBasename { get; set; } = false;
        public bool IsRegExcBasename { get; set; } = false;
        public bool IsIncHitRecursive { get; set; } = false;
        public bool IsExcHitRecursive { get; set; } = false;
        public bool IsDirFilterOr { get; set; } = false;
        public bool IsXdOnlyFiles { get; set; } = false;
        public bool IsRmNohit { get; set; } = false;
        public List<string> IncFilesList { get; set; } = [];
        public List<string> ExcFilesList { get; set; } = [];
        public List<string> IncDirsList { get; set; } = [];
        public List<string> ExcDirsList { get; set; } = [];
        public int FilesTypeCode { get; set; } = FILES_RELATIVE;
        public string FileListPath { get; set; } = "";
        public string FileListType { get; set; } = "rel";
        public string FileListRegex { get; set; } = @"[,|]";
        public List<string> FileList { get; set; } = [];
        public int IsCpTimestamp { get; set; } = 0;
        public string TsSource { get; set; } = "";
        public string TsDestination { get; set; } = "";
        public string TsBackup { get; set; } = "";
        public int SortType { get; set; } = MdlFile.SORT_BY_NONE;
        public bool IsAscending { get; set; } = true;
        public bool IsShowDirList { get; set; } = false;
        public bool IsShowFileList { get; set; } = false;

        /// <summary>
        /// 指定された出力モードコード（整数値）に対応する文字列識別子（"rel", "fr", "to", "both"）を取得します。
        /// </summary>
        /// <param name="mode">出力モードコード（ClsProp.RELATIVE, FROM, TO, BOTH など）</param>
        /// <returns>対応する出力モードの文字列（"fr", "to", "both", デフォルトは "rel"）</returns>
        /// <example>
        /// <code>
        /// var prop = new ClsProp();
        /// string modeStr = prop.GetOutputModeStr(ClsProp.FROM); // "fr"
        /// </code>
        /// </example>
        public string GetOutputModeStr(int mode) => mode switch
        {
            FROM => "fr",
            TO => "to",
            BOTH => "both",
            _ => "rel"
        };

        /// <summary>
        /// 指定された出力モード文字列に対応する整数値コードを取得します。
        /// </summary>
        /// <param name="mode">出力モードを表す文字列（"from", "to", "both", "rel" 等、大文字小文字不問）</param>
        /// <returns>対応する出力モードの整数値（ClsProp.FROM, TO, BOTH, デフォルトは RELATIVE）</returns>
        /// <example>
        /// <code>
        /// var prop = new ClsProp();
        /// int modeCode = prop.GetOutputModeCode("from"); // ClsProp.FROM (1)
        /// </code>
        /// </example>
        public int GetOutputModeCode(string mode)
        {
            return mode?.ToLowerInvariant() switch
            {
                "from" or "fr" or "f" => FROM,
                "to" or "t" => TO,
                "both" or "b" => BOTH,
                _ => RELATIVE
            };
        }

        /// <summary>
        /// 指定されたファイルロック確認モードコードに対応する文字列を取得します。
        /// </summary>
        /// <param name="mode">ファイルロック確認モードコード（ClsProp.CHECK_FILE_LOCK_NONE, CHECK_FILE_LOCK_SAMPLE など）</param>
        /// <returns>対応するファイルロック確認モードの文字列（"false", "sample", デフォルトは "skip"）</returns>
        /// <example>
        /// <code>
        /// var prop = new ClsProp();
        /// string lockStr = prop.GetCheckLockFileModeStr(ClsProp.CHECK_FILE_LOCK_NONE); // "false"
        /// </code>
        /// </example>
        public string GetCheckLockFileModeStr(int mode) => mode switch
        {
            CHECK_FILE_LOCK_NONE => "false",
            CHECK_FILE_LOCK_SAMPLE => "sample",
            _ => "skip"
        };

        /// <summary>
        /// 指定された実行モードコードに対応する文字列識別子を取得します。
        /// </summary>
        /// <param name="mode">実行モードコード（ClsProp.EXEC_MODE_CMD, EXEC_MODE_PS, EXEC_MODE_EXE など）</param>
        /// <returns>対応する実行モードの文字列（"cmd", "ps", "psc", "exe", デフォルトは "normal"）</returns>
        /// <example>
        /// <code>
        /// var prop = new ClsProp();
        /// string execStr = prop.GetExecModeStr(ClsProp.EXEC_MODE_CMD); // "cmd"
        /// </code>
        /// </example>
        public string GetExecModeStr(int mode) => mode switch
        {
            EXEC_MODE_CMD => "cmd",
            EXEC_MODE_PS => "ps",
            EXEC_MODE_PSC => "psc",
            EXEC_MODE_EXE => "exe",
            _ => "normal"
        };

    }
}
