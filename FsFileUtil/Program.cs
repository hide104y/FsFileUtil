using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Module;
using CmnWinLib.Class;
using FsFileUtil.Class;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace FsFileUtil
{
    public class Program
    {
        /// <summary>
        /// アプリケーションのエントリーポイントです。コマンドライン引数を解析し、ファイルやディレクトリの操作、ネットワーク共有接続、認証ログオン等の処理を実行します。
        /// </summary>
        /// <param name="args">コマンドラインから渡された引数の配列。</param>
        /// <returns>プロセスの終了コード（MdlConst.LVL_I: 正常終了、MdlConst.LVL_W: 警告、MdlConst.LVL_E: エラー、または行数/ファイル数）。</returns>
        /// <example>
        /// <code>
        /// // 使用例: ファイルコピー処理の実行
        /// string[] args = ["-f", @"C:\data\input.txt", "-t", @"C:\data\output.txt", "-act", "copy", "-v", "1"];
        /// int result = Program.Main(args);
        /// </code>
        /// </example>
        public static int Main(string[] args)
        {
            DateTime startTime = DateTime.Now;
            ClsLogger logger = new();
            ClsProp prop = new();
            ClsAppArg argsParser = new(logger, prop);
            ClsLogon? logon = null;
            ClsNetUse? networkShare = null;
            int exitCode = MdlConst.LVL_I;
            bool isSuccess = argsParser.Parse(args);

            if (isSuccess)
            {
                if (prop.Verbose > 0)
                {
                    logger.WriteLine(MdlConst.LVL_NONE, $"===<<< [{argsParser.ExeBaseName}] START : {MdlDate.GetFormattedDate(startTime, "yyyy/MM/dd HH:mm:ss")}>===");
                }
                if (prop.Verbose > 1) argsParser.PrintDefinition();
                if (argsParser.IsUsage)
                {
                    exitCode = MdlConst.LVL_W;
                    argsParser.ShowUsage();
                }
                else
                {
                    // ネットワーク認証接続
                    if (OperatingSystem.IsWindows() && isSuccess && prop.IsMount)
                    {
                        networkShare = new ClsNetUse
                        {
                            NetworkPath = prop.NetSharePath,
                            DriveName = prop.DriveName,
                            Username = prop.Username,
                            Password = prop.Password,
                            IgnoreErrors = prop.IsLogonAlwaysOk,
                            AllowedErrorCodes = prop.NetUseOkErrNoList
                        };
                        if (prop.IsUmount) networkShare.Disconnect();
                        if (!networkShare.Connect())
                        {
                            isSuccess = false;
                            if (prop.IsLogonAlwaysOk) isSuccess = true;
                        }
                        logger.WriteLine(MdlConst.LVL_NONE, networkShare.Message);
                        logger.WriteLine(MdlConst.LVL_NONE, "");
                    }

                    // パスの文字列置換：-f
                    if (prop.IsNeedPathFr && !string.IsNullOrEmpty(prop.SourcePath))
                    {
                        DateTime timestamp = argsParser.GetTimestamp(prop.TsSource, "", MdlFile.PATH_IS_NULL);
                        prop.SourcePath = MdlDate.ReplaceStringWithDateTime(prop.SourcePath.Replace(@"%%", @"%"), timestamp);

                        // 親ディレクトリが存在しない場合：「C:\」など
                        switch (prop.ActionCode)
                        {
                            case ClsProp.ACTION_LS:
                                break;
                            default:
                                if (string.IsNullOrEmpty(MdlFile.GetDirectoryPath(prop.SourcePath))) prop.SourcePath += @"\.";
                                break;
                        }

                        int pathType = MdlFile.GetPathType(prop.SourcePath);
                        if (pathType is MdlFile.PATH_IS_DIRECTORY or MdlFile.PATH_IS_FILE)
                        {
                            prop.PathType = pathType;
                        }
                    }

                    // パスの文字列置換：-t
                    if (!string.IsNullOrEmpty(prop.DestinationPath))
                    {
                        DateTime timestamp = argsParser.GetTimestamp(prop.TsDestination, prop.SourcePath, prop.PathType);
                        prop.DestinationPath = MdlDate.ReplaceStringWithDateTime(prop.DestinationPath.Replace(@"%%", @"%"), timestamp);
                        // 親ディレクトリが存在しない場合：「C:\」など
                        if (string.IsNullOrEmpty(MdlFile.GetDirectoryPath(prop.DestinationPath))) prop.DestinationPath += @"\.";
                    }

                    // 上書きファイル退避先
                    if (prop.IsBackup && !string.IsNullOrEmpty(prop.BackupDir))
                    {
                        DateTime timestamp = argsParser.GetTimestamp(prop.TsBackup, prop.SourcePath, prop.PathType);
                        prop.BackupDir = MdlDate.ReplaceStringWithDateTime(prop.BackupDir.Replace(@"%%", @"%"), timestamp);
                        // 親ディレクトリが存在しない場合：「C:\」など
                        if (string.IsNullOrEmpty(MdlFile.GetDirectoryPath(prop.BackupDir)))
                        {
                            prop.BackupDir += @"\.";
                        }
                    }
                    // 処理の実行
                    if (isSuccess && ClsProp.ACTION_NONE != prop.ActionCode)
                    {
                        try
                        {
                            ClsActionCtrl actionController = new ClsActionCtrl(logger, prop);
                            if (OperatingSystem.IsWindows() && isSuccess && (prop.IsSwitchUser || prop.IsLogon))
                            {
                                logon = new ClsLogon();
                                logon.Verbose = prop.Verbose;
                                logon.DomainName = string.IsNullOrEmpty(prop.DomainName) ? Environment.UserDomainName.ToUpper() : prop.DomainName;
                                logon.Username = prop.UsernameWithoutDomain;
                                logon.Password = prop.Password;
                                try
                                {
                                    logon.Execute(actionController);
                                    exitCode = logon.ReturnCode;
                                }
                                catch (Exception ex)
                                {
                                    exitCode = MdlConst.LVL_E;
                                    logger.WriteLine(MdlConst.LVL_NONE, $"[ERR] CALL logon.Execute() -> actionController.Main(): {ex.Message}");
                                    if (prop.IsStackTrace)
                                    {
                                        logger.WriteLine(MdlConst.LVL_NONE, "");
                                        logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                                        logger.WriteLine(MdlConst.LVL_NONE, "");
                                    }
                                }
                                finally
                                {
                                    logon.Dispose();
                                }
                            }
                            else
                            {
                                exitCode = actionController.Execute();
                            }
                        }
                        catch (Exception ex)
                        {
                            exitCode = MdlConst.LVL_E;
                            logger.WriteLine(MdlConst.LVL_NONE, $"[ERR] CALL actionController.Main() : {ex.Message}");
                            if (prop.IsStackTrace)
                            {
                                logger.WriteLine(MdlConst.LVL_NONE, "");
                                logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                                logger.WriteLine(MdlConst.LVL_NONE, "");
                            }
                        }
                    }
                    // ネットワーク認証切断
                    if (OperatingSystem.IsWindows() && prop.IsUmount)
                    {
                        logger.WriteLine(MdlConst.LVL_NONE, "");
                        networkShare ??= new ClsNetUse();
                        networkShare.NetworkPath = string.IsNullOrEmpty(prop.DriveName) ? prop.NetSharePath : $"{prop.DriveName}:";
                        networkShare.IgnoreErrors = prop.IsLogonAlwaysOk;
                        networkShare.AllowedErrorCodes = prop.NetUseOkErrNoList;
                        if (!networkShare.Disconnect()) isSuccess = false;
                        logger.WriteLine(MdlConst.LVL_NONE, networkShare.Message);
                        logger.WriteLine(MdlConst.LVL_NONE, "");
                    }
                    // 戻り値の調整
                    if (ClsProp.ACTION_NONE == prop.ActionCode)
                    {
                        if (MdlConst.LVL_I == exitCode && !isSuccess) exitCode = MdlConst.LVL_E;
                    }
                    else
                    {
                        if (MdlConst.LVL_I == exitCode && !isSuccess) exitCode = MdlConst.LVL_W;
                    }
                }
                if (prop.IsCatRetWcl) exitCode = (int)Math.Min(prop.Lines, (ulong)MdlConst.INT_MAX);
                if (prop.IsRetFiles) exitCode = (int)Math.Min(prop.Files, (ulong)MdlConst.INT_MAX);
                if (prop.Verbose > 0)
                {
                    DateTime endTime = DateTime.Now;
                    double elapsedTime = (endTime - startTime).TotalSeconds;
                    logger.WriteLine(MdlConst.LVL_NONE, $"===<<< [{argsParser.ExeBaseName}] EXIT ({exitCode}) : {MdlDate.GetFormattedDate(endTime, "yyyy/MM/dd HH:mm:ss")} : {elapsedTime:F3} sec>>>===");
                }
            }
            else
            {
                exitCode = MdlConst.LVL_E;
                if (argsParser.IsUsage) argsParser.ShowUsage();
            }
            if (argsParser.IsEchoRetcode)
            {
                logger.WriteLine(MdlConst.LVL_NONE, exitCode.ToString());
            }
            return exitCode;
        }
    }
}
