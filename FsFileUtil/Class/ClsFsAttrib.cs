
using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Module;
using CmnWinLib.Class;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace FsFileUtil.Class
{
    /// <summary>
    /// ファイルシステム上のファイルおよびディレクトリの属性・サイズ・所有者・アクセス権限の取得およびログ出力を管理するクラスです。
    /// </summary>
    /// <param name="logger">ログ出力を行う ClsLogger インスタンス。</param>
    public class ClsFsAttrib(ClsLogger logger)
    {
        private readonly ClsLogger _logger = logger;

        /// <summary>
        /// 処理したディレクトリの合計件数を取得または設定します。
        /// </summary>
        public ulong DirectoryCount { get; set; }

        /// <summary>
        /// 処理したファイルの合計件数を取得または設定します。
        /// </summary>
        public ulong FileCount { get; set; }

        /// <summary>
        /// 処理したファイルの合計サイズ（バイト単位）を取得または設定します。
        /// </summary>
        public ulong TotalSize { get; set; }

        /// <summary>
        /// エラーが発生したディレクトリの件数を取得または設定します。
        /// </summary>
        public ulong ErrorDirectoryCount { get; set; }

        /// <summary>
        /// エラーが発生したファイルの件数を取得または設定します。
        /// </summary>
        public ulong ErrorFileCount { get; set; }

        /// <summary>
        /// 進捗状況ログ出力機能が有効かどうかを取得または設定します。
        /// </summary>
        public bool IsProgressEnabled { get; set; }

        /// <summary>
        /// 進捗メッセージを出力するディレクトリ件数の間隔を取得または設定します。
        /// </summary>
        public int ProgressIntervalDirectories { get; set; }

        /// <summary>
        /// 進捗メッセージを出力するファイル件数の間隔を取得または設定します。
        /// </summary>
        public int ProgressIntervalFiles { get; set; }

        /// <summary>
        /// ディレクトリ数、ファイル数、合計サイズ、および各エラーカウンターを 0 にリセットします。
        /// </summary>
        /// <example>
        /// <code>
        /// var attrib = new ClsFsAttrib(logger);
        /// attrib.ClearCounter();
        /// </code>
        /// </example>
        public void ClearCounter()
        {
            DirectoryCount = 0;
            FileCount = 0;
            TotalSize = 0;
            ErrorDirectoryCount = 0;
            ErrorFileCount = 0;
        }

        /// <summary>
        /// 指定されたパスのディレクトリおよび配下ファイルのサイズと件数を再帰的に取得・計算します。
        /// </summary>
        /// <param name="targetPath">対象のディレクトリパス。</param>
        /// <param name="checkSymlink">true の場合、シンボリックリンクを除外・判定対象とします。</param>
        /// <param name="verboseLevel">詳細ログのレベル (0 より大きい場合に出力)。</param>
        /// <param name="isStackTrace">例外発生時にスタックトレースを出力するかどうか。</param>
        /// <returns>処理が正常に完了した場合は true。アクセスエラー等の問題が発生した場合は false。</returns>
        /// <example>
        /// <code>
        /// var attrib = new ClsFsAttrib(logger);
        /// bool result = attrib.CalculateDirectorySize(@"C:\Data\Logs", checkSymlink: true, verboseLevel: 1, isStackTrace: true);
        /// Console.WriteLine($"Total Size: {attrib.TotalSize} bytes");
        /// </code>
        /// </example>
        public bool CalculateDirectorySize(string targetPath, bool checkSymlink, int verboseLevel, bool isStackTrace)
        {
            return CalculateDirectorySize(new DirectoryInfo(targetPath), checkSymlink, verboseLevel, isStackTrace);
        }

        /// <summary>
        /// 指定された DirectoryInfo オブジェクトに基づいてディレクトリおよび配下ファイルのサイズと件数を再帰的に取得・計算します。
        /// </summary>
        /// <param name="targetDirectory">対象のディレクトリ情報。</param>
        /// <param name="checkSymlink">true の場合、シンボリックリンクを除外・判定対象とします。</param>
        /// <param name="verboseLevel">詳細ログのレベル (0 より大きい場合に出力)。</param>
        /// <param name="isStackTrace">例外発生時にスタックトレースを出力するかどうか。</param>
        /// <returns>処理が正常に完了した場合は true。例外等が発生した場合は false。</returns>
        /// <example>
        /// <code>
        /// var attrib = new ClsFsAttrib(logger);
        /// var dirInfo = new DirectoryInfo(@"C:\Data\Logs");
        /// bool result = attrib.CalculateDirectorySize(dirInfo, checkSymlink: false, verboseLevel: 0, isStackTrace: false);
        /// </code>
        /// </example>
        public bool CalculateDirectorySize(DirectoryInfo targetDirectory, bool checkSymlink, int verboseLevel, bool isStackTrace)
        {
            bool isSuccess = true;
            try
            {
                DirectoryCount++;
                if (IsProgressEnabled)
                {
                    if (ProgressIntervalDirectories > 0 && DirectoryCount > 0 && 0 == (DirectoryCount % (ulong)ProgressIntervalDirectories))
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, $" => CURRENT STATUS : {MdlDate.GetFormattedDate(DateTime.Now, "yyyy/MM/dd HH:mm:ss")} : DIRS={DirectoryCount} / FILES={FileCount} / SIZE={MdlUtil.GetHumanReadableBytes(TotalSize)}");
                    }
                    else if (ProgressIntervalFiles > 0 && FileCount > 0 && 0 == (FileCount % (ulong)ProgressIntervalFiles))
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, $" => CURRENT STATUS : {MdlDate.GetFormattedDate(DateTime.Now, "yyyy/MM/dd HH:mm:ss")} : DIRS={DirectoryCount} / FILES={FileCount} / SIZE={MdlUtil.GetHumanReadableBytes(TotalSize)}");
                    }
                }

                if (checkSymlink && MdlFile.IsSymlink(targetDirectory.FullName))
                {
                    return true;
                }

                foreach (FileInfo fileInfo in targetDirectory.EnumerateFiles())
                {
                    FileCount++;
                    try
                    {
                        if (!checkSymlink || !MdlFile.IsSymlink(fileInfo.FullName))
                        {
                            TotalSize += (ulong)fileInfo.Length;
                        }
                    }
                    catch (Exception fileException)
                    {
                        isSuccess = false;
                        ErrorFileCount++;
                        if (verboseLevel > 0)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $" => SKIP FILE : {fileInfo.FullName} : {fileException.Message}");
                        }
                        if (isStackTrace)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, "");
                            _logger.WriteLine(MdlConst.LVL_NONE, fileException.StackTrace ?? "");
                            _logger.WriteLine(MdlConst.LVL_NONE, "");
                        }
                    }
                }

                foreach (DirectoryInfo directoryInfo in targetDirectory.EnumerateDirectories())
                {
                    if (!CalculateDirectorySize(directoryInfo, checkSymlink, verboseLevel, isStackTrace))
                    {
                        isSuccess = false;
                    }
                }
            }
            catch (Exception directoryException)
            {
                isSuccess = false;
                ErrorDirectoryCount++;
                if (verboseLevel > 0)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, $" => SKIP DIR  : {targetDirectory.FullName} : {directoryException.Message}");
                }
                if (isStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, directoryException.StackTrace ?? "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 指定されたパスの単一ファイルサイズを取得し、合計サイズおよび件数に加算します。
        /// </summary>
        /// <param name="targetPath">対象のファイルパス。</param>
        /// <param name="checkSymlink">true の場合、シンボリックリンク判定を行い除外します。</param>
        /// <param name="verboseLevel">詳細ログのレベル。</param>
        /// <param name="isStackTrace">スタックトレースを出力するかどうか。</param>
        /// <returns>処理が成功した場合は true。例外が発生した場合は false。</returns>
        /// <example>
        /// <code>
        /// var attrib = new ClsFsAttrib(logger);
        /// bool result = attrib.CalculateFileSize(@"C:\Data\test.txt", checkSymlink: true, verboseLevel: 1, isStackTrace: false);
        /// </code>
        /// </example>
        public bool CalculateFileSize(string targetPath, bool checkSymlink, int verboseLevel, bool isStackTrace)
        {
            return CalculateFileSize(new FileInfo(targetPath), checkSymlink, verboseLevel, isStackTrace);
        }

        /// <summary>
        /// 指定された FileInfo オブジェクトのファイルサイズを取得し、合計サイズおよび件数に加算します。
        /// </summary>
        /// <param name="targetFile">対象の FileInfo オブジェクト。</param>
        /// <param name="checkSymlink">true の場合、シンボリックリンク判定を行い除外します。</param>
        /// <param name="verboseLevel">詳細ログのレベル。</param>
        /// <param name="isStackTrace">スタックトレースを出力するかどうか。</param>
        /// <returns>処理が成功した場合は true。例外が発生した場合は false。</returns>
        /// <example>
        /// <code>
        /// var attrib = new ClsFsAttrib(logger);
        /// var fileInfo = new FileInfo(@"C:\Data\test.txt");
        /// bool result = attrib.CalculateFileSize(fileInfo, checkSymlink: true, verboseLevel: 1, isStackTrace: false);
        /// </code>
        /// </example>
        public bool CalculateFileSize(FileInfo targetFile, bool checkSymlink, int verboseLevel, bool isStackTrace)
        {
            bool isSuccess = true;
            try
            {
                if (!checkSymlink || !MdlFile.IsSymlink(targetFile.FullName))
                {
                    TotalSize += (ulong)targetFile.Length;
                    FileCount++;
                }
            }
            catch (Exception fileException)
            {
                isSuccess = false;
                ErrorFileCount++;
                if (verboseLevel > 0)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, $" => SKIP FILE : {targetFile.FullName} : {fileException.Message}");
                }
                if (isStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, fileException.StackTrace ?? "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 指定されたパスのディレクトリ所有者アカウントを取得し、ログに出力します（Windows 環境のみ実行）。
        /// </summary>
        /// <param name="targetPath">対象のディレクトリパス。</param>
        /// <param name="verboseLevel">詳細ログのレベル。</param>
        /// <param name="showPath">ログにディレクトリパスを含めるかどうか。</param>
        /// <param name="isStackTrace">スタックトレースを出力するかどうか。</param>
        /// <returns>処理が成功した場合は true。取得失敗または例外時は false。</returns>
        /// <example>
        /// <code>
        /// var attrib = new ClsFsAttrib(logger);
        /// bool result = attrib.OutputDirectoryOwner(@"C:\Data", verboseLevel: 1, showPath: true, isStackTrace: true);
        /// </code>
        /// </example>
        public bool OutputDirectoryOwner(string targetPath, int verboseLevel, bool showPath, bool isStackTrace)
        {
            bool isSuccess = true;
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    DirectoryInfo directoryInfo = new(targetPath);
                    System.Security.AccessControl.DirectorySecurity security = directoryInfo.GetAccessControl();
                    string owner = $"{security.GetOwner(typeof(System.Security.Principal.NTAccount))}";
                    if (showPath)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, $"{targetPath},OWNER,OWNER,{owner}");
                    }
                    else
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, owner);
                    }
                }
                catch (Exception exception)
                {
                    isSuccess = false;
                    if (verboseLevel > 0)
                    {
                        if (showPath)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $"{targetPath},FAILED TO GET OWNER({targetPath})：EXCEPTION：{exception.Message}");
                        }
                        else
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $" => FAILED TO GET OWNER({targetPath})：EXCEPTION：{exception.Message}");
                        }
                    }
                    if (isStackTrace)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                        _logger.WriteLine(MdlConst.LVL_NONE, exception.StackTrace ?? "");
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                    }
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 指定されたパスのディレクトリのアクセス許可 (ACL) を取得・分類し、ログに出力します（Windows 環境のみ実行）。
        /// </summary>
        /// <param name="targetPath">対象のディレクトリパス。</param>
        /// <param name="verboseLevel">詳細ログのレベル。</param>
        /// <param name="showPath">ログにディレクトリパスを含めるかどうか。</param>
        /// <param name="isStackTrace">スタックトレースを出力するかどうか。</param>
        /// <returns>処理が成功した場合は true。権限取得失敗または例外時は false。</returns>
        /// <example>
        /// <code>
        /// var attrib = new ClsFsAttrib(logger);
        /// bool result = attrib.OutputDirectoryPermission(@"C:\Data", verboseLevel: 1, showPath: true, isStackTrace: true);
        /// </code>
        /// </example>
        public bool OutputDirectoryPermission(string targetPath, int verboseLevel, bool showPath, bool isStackTrace)
        {
            bool isSuccess = true;
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    List<string> aclFullControl = [];
                    List<string> aclModify = [];
                    List<string> aclReadAndExecute = [];
                    List<string> aclGenericFullControl = [];
                    List<string> aclGenericModify = [];
                    List<string> aclGenericReadAndExecute = [];
                    List<string> aclOtherPermissions = [];

                    DirectoryInfo directoryInfo = new(targetPath);
                    System.Security.AccessControl.DirectorySecurity security = directoryInfo.GetAccessControl();
                    foreach (System.Security.AccessControl.FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                    {
                        string accessControlType;
                        try
                        {
                            accessControlType = rule.AccessControlType.ToString();
                        }
                        catch
                        {
                            accessControlType = "ERROR";
                        }

                        string identityReference;
                        try
                        {
                            identityReference = (rule.IdentityReference as System.Security.Principal.NTAccount)?.Value ?? rule.IdentityReference.ToString();
                        }
                        catch
                        {
                            identityReference = "ERROR";
                        }

                        string fileSystemRights;
                        try
                        {
                            string rawRights = rule.FileSystemRights.ToString();
                            fileSystemRights = rawRights switch
                            {
                                "268435456" => "Generic_All (268435456)",
                                "536870912" => "Generic_Execute (536870912)",
                                "1073741824" => "Generic_Write (1073741824)",
                                "2147483648" => "Generic_Read (2147483648)",
                                "-536805376" => "Generic_Modify, Synchronize (-536805376)",
                                "-1610612736" => "Generic_ReadAndExecute, Synchronize (-1610612736)",
                                _ => rawRights
                            };
                        }
                        catch
                        {
                            fileSystemRights = "ERROR";
                        }

                        string isInherited;
                        try
                        {
                            isInherited = rule.IsInherited ? "継承" : "<継承なし>";
                        }
                        catch
                        {
                            isInherited = "ERROR";
                        }

                        string inheritanceFlags;
                        try
                        {
                            inheritanceFlags = rule.InheritanceFlags.ToString()
                                .Replace("None", "このフォルダーのみ")
                                .Replace("ContainerInherit", "サブフォルダー")
                                .Replace("ObjectInherit", "ファイル")
                                .Replace(",", "と")
                                .Replace(" ", "");
                        }
                        catch
                        {
                            inheritanceFlags = "ERROR";
                        }

                        string propagationFlags;
                        try
                        {
                            propagationFlags = rule.PropagationFlags.ToString().ToLower() switch
                            {
                                "none" => "このフォルダー以下すべてに適用",
                                "inheritonly" => "このフォルダーに適用せず、子階層以下に継承",
                                "nopropagateinherit" => "このフォルダーと子階層には適用するが、孫階層以下には継承しない",
                                var other => other
                            };
                        }
                        catch
                        {
                            propagationFlags = "ERROR";
                        }

                        string line = $"{accessControlType},\"{fileSystemRights}\",{identityReference},{isInherited},\"{inheritanceFlags}\",{propagationFlags}";
                        string check = fileSystemRights.ToUpperInvariant();

                        if (check.Contains("FULLCONTROL"))
                        {
                            aclFullControl.Add(line);
                        }
                        else if (check.Contains("GENERIC_ALL"))
                        {
                            aclGenericFullControl.Add(line);
                        }
                        else if (check.Contains("GENERIC_MODIFY"))
                        {
                            aclGenericModify.Add(line);
                        }
                        else if (check.Contains("MODIFY"))
                        {
                            aclModify.Add(line);
                        }
                        else if (check.Contains("GENERIC_READANDEXECUTE"))
                        {
                            aclGenericReadAndExecute.Add(line);
                        }
                        else if (check.Contains("READANDEXECUTE"))
                        {
                            aclReadAndExecute.Add(line);
                        }
                        else
                        {
                            aclOtherPermissions.Add(line);
                        }
                    }

                    aclFullControl.Sort();
                    aclModify.Sort();
                    aclReadAndExecute.Sort();
                    aclGenericFullControl.Sort();
                    aclGenericModify.Sort();
                    aclGenericReadAndExecute.Sort();
                    aclOtherPermissions.Sort();

                    List<string>[] allCategories = [
                        aclFullControl,
                        aclGenericFullControl,
                        aclModify,
                        aclGenericModify,
                        aclReadAndExecute,
                        aclGenericReadAndExecute,
                        aclOtherPermissions
                    ];

                    foreach (List<string> category in allCategories)
                    {
                        foreach (string line in category)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, showPath ? $"{targetPath},{line}" : line);
                        }
                    }
                }
                catch (Exception exception)
                {
                    isSuccess = false;
                    if (verboseLevel > 0)
                    {
                        if (showPath)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $"{targetPath},FAILED TO GET PERMISSION({targetPath})：EXCEPTION 1：{exception.Message}");
                        }
                        else
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, $" => FAILED TO GET PERMISSION({targetPath})：EXCEPTION 1：{exception.Message}");
                        }
                    }
                    if (isStackTrace)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                        _logger.WriteLine(MdlConst.LVL_NONE, exception.StackTrace ?? "");
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                    }
                }
            }
            return isSuccess;
        }
    }
}
