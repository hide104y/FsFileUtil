using System;
using System.Collections.Generic;
using System.IO;
using CmnClsLib.Module;
using FsFileUtil.Class;
using Xunit;

namespace TestProject1.Class
{
    /// <summary>
    /// <see cref="ClsProp"/> クラスの単体テストを提供します。
    /// </summary>
    public class UnitTest_ClsProp
    {
        #region 1. 定数値の検証テスト

        [Fact]
        public void Constants_OutputMode_ValuesAreCorrect()
        {
            Assert.Equal(0, ClsProp.RELATIVE);
            Assert.Equal(1, ClsProp.FROM);
            Assert.Equal(2, ClsProp.TO);
            Assert.Equal(3, ClsProp.BOTH);
        }

        [Fact]
        public void Constants_FilesType_ValuesAreCorrect()
        {
            Assert.Equal(0, ClsProp.FILES_RELATIVE);
            Assert.Equal(1, ClsProp.FILES_FULL);
        }

        [Fact]
        public void Constants_Comparison_ValuesAreCorrect()
        {
            Assert.Equal(0, ClsProp.COMPARISON_NO);
            Assert.Equal(1, ClsProp.COMPARISON_EQ);
            Assert.Equal(2, ClsProp.COMPARISON_GT);
            Assert.Equal(3, ClsProp.COMPARISON_GE);
            Assert.Equal(4, ClsProp.COMPARISON_LT);
            Assert.Equal(5, ClsProp.COMPARISON_LE);
        }

        [Fact]
        public void Constants_DateTime_ValuesAreCorrect()
        {
            Assert.Equal(0, ClsProp.DATETIME_NOW);
            Assert.Equal(1, ClsProp.DATETIME_TODAY);
            Assert.Equal(2, ClsProp.DATETIME_YESTERDAY);
            Assert.Equal(3, ClsProp.DATETIME_FILEINFO);
        }

        [Fact]
        public void Constants_ExecMode_ValuesAreCorrect()
        {
            Assert.Equal(0, ClsProp.EXEC_MODE_NORMAL);
            Assert.Equal(1, ClsProp.EXEC_MODE_CMD);
            Assert.Equal(2, ClsProp.EXEC_MODE_PS);
            Assert.Equal(3, ClsProp.EXEC_MODE_PSC);
            Assert.Equal(4, ClsProp.EXEC_MODE_EXE);
        }

        [Fact]
        public void Constants_CheckFileLock_ValuesAreCorrect()
        {
            Assert.Equal(0, ClsProp.CHECK_FILE_LOCK_NONE);
            Assert.Equal(1, ClsProp.CHECK_FILE_LOCK_SAMPLE);
            Assert.Equal(2, ClsProp.CHECK_FILE_LOCK_SKIP);
        }

        [Fact]
        public void Constants_CopyMode_ValuesAreCorrect()
        {
            Assert.Equal(0, ClsProp.COPY_ASYNC);
            Assert.Equal(1, ClsProp.COPY_BINARY);
            Assert.Equal(2, ClsProp.COPY_OS_CMD);
        }

        [Fact]
        public void Constants_Action_ValuesAreCorrect()
        {
            Assert.Equal(-1, ClsProp.ACTION_NONE);
            Assert.Equal(0, ClsProp.ACTION_COPY);
            Assert.Equal(1, ClsProp.ACTION_MOVE);
            Assert.Equal(2, ClsProp.ACTION_SYNC);
            Assert.Equal(10, ClsProp.ACTION_MKDIR);
            Assert.Equal(11, ClsProp.ACTION_TOUCH);
            Assert.Equal(12, ClsProp.ACTION_DELETE);
            Assert.Equal(13, ClsProp.ACTION_MKLINK);
            Assert.Equal(15, ClsProp.ACTION_LS);
            Assert.Equal(16, ClsProp.ACTION_FIND);
            Assert.Equal(17, ClsProp.ACTION_GET_REAL_PATH);
            Assert.Equal(18, ClsProp.ACTION_LIST_LOCK_PROC);
            Assert.Equal(20, ClsProp.ACTION_EXIST);
            Assert.Equal(21, ClsProp.ACTION_EXIST_DIR);
            Assert.Equal(22, ClsProp.ACTION_EXIST_FILE);
            Assert.Equal(23, ClsProp.ACTION_WAIT);
            Assert.Equal(24, ClsProp.ACTION_FILE_LOCKED);
            Assert.Equal(30, ClsProp.ACTION_RENAME);
            Assert.Equal(31, ClsProp.ACTION_ROTATE);
            Assert.Equal(41, ClsProp.ACTION_GET_ATTRIB);
            Assert.Equal(42, ClsProp.ACTION_GET_SIZE);
            Assert.Equal(43, ClsProp.ACTION_GET_PERM);
            Assert.Equal(44, ClsProp.ACTION_GET_OWNER);
            Assert.Equal(91, ClsProp.ACTION_EXEC);
            Assert.Equal(99, ClsProp.ACTION_ETC);
        }

        [Fact]
        public void Constants_Check_ValuesAreCorrect()
        {
            Assert.Equal(0, ClsProp.CHECK_NONE);
            Assert.Equal(1, ClsProp.CHECK_SIZE);
            Assert.Equal(2, ClsProp.CHECK_MTIME);
            Assert.Equal(3, ClsProp.CHECK_MTIME_NEW);
            Assert.Equal(4, ClsProp.CHECK_MTIME_OLD);
            Assert.Equal(5, ClsProp.CHECK_CKSUM);
            Assert.Equal(6, ClsProp.CHECK_SHA1);
            Assert.Equal(7, ClsProp.CHECK_ADLER32);
            Assert.Equal(8, ClsProp.CHECK_EXIST);
        }

        [Fact]
        public void Constants_Task_ValuesAreCorrect()
        {
            Assert.Equal(0, ClsProp.TASK_CP);
            Assert.Equal(1, ClsProp.TASK_MV);
            Assert.Equal(2, ClsProp.TASK_RM);
            Assert.Equal(3, ClsProp.TASK_PRINT);
            Assert.Equal(4, ClsProp.TASK_RENAME);
        }

        #endregion

        #region 2. コンストラクタおよびプロパティ初期値のテスト

        [Fact]
        public void Constructor_InitializesDefaultValuesCorrectly()
        {
            // Arrange & Act
            var prop = new ClsProp();

            // Assert - 文字列プロパティ
            Assert.Equal("", prop.ExeBaseName);
            Assert.Equal("", prop.ExeDir);
            Assert.Equal("", prop.SourcePath);
            Assert.Equal("", prop.DestinationPath);
            Assert.Equal("", prop.WorkDir);
            Assert.Equal("copy", prop.Action);
            Assert.Equal("", prop.Mode);
            Assert.Equal("", prop.BackupDir);
            Assert.Equal("", prop.OutputPathPrefix);
            Assert.Equal("", prop.CmdPath);
            Assert.Equal("", prop.CmdArgs);
            Assert.Equal("", prop.CatI);
            Assert.Equal("", prop.CatX);
            Assert.Equal("", prop.CatP);
            Assert.Equal("", prop.CatE);
            Assert.Equal("", prop.CatXmlNl);
            Assert.Equal("", prop.CatOptions);
            Assert.Equal("", prop.NetSharePath);
            Assert.Equal("", prop.DriveName);
            Assert.Equal("", prop.DomainName);
            Assert.Equal("", prop.Username);
            Assert.Equal("", prop.UsernameWithoutDomain);
            Assert.Equal("_THIS_IS_PASSWORD_", prop.Password);
            Assert.Equal("", prop.FileListPath);
            Assert.Equal("rel", prop.FileListType);
            Assert.Equal(@"[,|]", prop.FileListRegex);
            Assert.Equal("", prop.TsSource);
            Assert.Equal("", prop.TsDestination);
            Assert.Equal("", prop.TsBackup);

            // Assert - 数値・列挙型プロパティ
            Assert.Equal(ClsProp.COPY_BINARY, prop.CopyCmdType);
            Assert.Equal(ClsProp.ACTION_COPY, prop.ActionCode);
            Assert.Equal(ClsProp.TASK_CP, prop.Task);
            Assert.Equal(ClsProp.CHECK_NONE, prop.CheckLogic);
            Assert.Equal(ClsProp.COMPARISON_NO, prop.CompOpe);
            Assert.Equal(MdlFile.PATH_IS_NULL, prop.PathType);
            Assert.Equal(ClsProp.CHECK_FILE_LOCK_NONE, prop.CheckFileLock);
            Assert.Equal(0L, prop.SkipSize);
            Assert.Equal(0L, prop.CopySize);
            Assert.Equal(0L, prop.CompSize);
            Assert.Equal(60, prop.Interval);
            Assert.Equal(1, prop.MaxLoop);
            Assert.Equal(FileShare.ReadWrite, prop.ObjFileShare);
            Assert.Equal(200, prop.WaitMSecForRetryCopy);
            Assert.Equal(0, prop.RetrySystemCopyMax);
            Assert.Equal(0.0, prop.SecRange);
            Assert.Equal(0, prop.Verbose);
            Assert.Equal(0, prop.IsShowCurDir);
            Assert.Equal(ClsProp.RELATIVE, prop.OutputPathCode);
            Assert.Equal(0, prop.ProgressIntervalDirs);
            Assert.Equal(0, prop.ProgressIntervalFiles);
            Assert.Equal(0, prop.IntIsOverWrite);
            Assert.Equal(7, prop.MaxKeep);
            Assert.Equal(MdlConst.INT_NULL, prop.WarnThreshold);
            Assert.Equal(MdlConst.INT_NULL, prop.ErrorThreshold);
            Assert.Equal(ClsProp.EXEC_MODE_EXE, prop.ExecModeCode);
            Assert.Equal(3, prop.Priority);
            Assert.Equal(86400, prop.Timeout);
            Assert.Equal((ulong)0, prop.Files);
            Assert.Equal((ulong)0, prop.Lines);
            Assert.Equal(MdlConst.INT_TYPE_ALL, prop.TypeCode);
            Assert.Equal(MdlConst.ULNG_MAX, prop.MaxDepth);
            Assert.Equal((ulong)0, prop.MinDepth);
            Assert.Equal(ClsProp.FILES_RELATIVE, prop.FilesTypeCode);
            Assert.Equal(0, prop.IsCpTimestamp);
            Assert.Equal(MdlFile.SORT_BY_NONE, prop.SortType);

            // Assert - 日時プロパティ
            Assert.Equal(default, prop.BeforeTime);
            Assert.Equal(default, prop.AfterTime);

            // Assert - 真偽値プロパティ
            Assert.False(prop.IsNeedPathFr);
            Assert.False(prop.IsNeedPathTo);
            Assert.False(prop.IsList);
            Assert.False(prop.IsReverse);
            Assert.True(prop.IsSizeCheck);
            Assert.False(prop.IsSyncRmOnly);
            Assert.False(prop.IsFlat);
            Assert.False(prop.IsDirTerm);
            Assert.False(prop.IsAlwaysMkDir);
            Assert.True(prop.IsFileCopy);
            Assert.False(prop.IsSkip);
            Assert.False(prop.IsSourceCheck);
            Assert.True(prop.IsFrPathCheck);
            Assert.False(prop.IsRetFiles);
            Assert.False(prop.IsBackup);
            Assert.True(prop.IsErrorIfBackupFailed);
            Assert.False(prop.IsRelative);
            Assert.False(prop.IsProgress);
            Assert.False(prop.IsStackTrace);
            Assert.True(prop.IsShowNewFile);
            Assert.True(prop.IsShowUpdatedFile);
            Assert.True(prop.IsShowSameFile);
            Assert.False(prop.IsShowPath);
            Assert.False(prop.IsShowSize);
            Assert.False(prop.IsShowDirNum);
            Assert.False(prop.IsShowFileNum);
            Assert.False(prop.IsShowPerm);
            Assert.False(prop.IsShowOwner);
            Assert.False(prop.IsSymLink);
            Assert.False(prop.IsDq);
            Assert.False(prop.IsExecCmd);
            Assert.False(prop.IsErrorAtNegativeValue);
            Assert.False(prop.IsAlwaysNormal);
            Assert.False(prop.IsShowCmd);
            Assert.False(prop.IsShowOutput);
            Assert.False(prop.IsShowExitCode);
            Assert.False(prop.IsCat);
            Assert.False(prop.IsCatRetWcl);
            Assert.False(prop.IsLogonAlwaysOk);
            Assert.False(prop.IsMount);
            Assert.False(prop.IsUmount);
            Assert.False(prop.IsSwitchUser);
            Assert.False(prop.IsLogon);
            Assert.False(prop.IsLogoff);
            Assert.False(prop.IsBefore);
            Assert.False(prop.IsAfter);
            Assert.False(prop.IsRegIncBasename);
            Assert.False(prop.IsRegExcBasename);
            Assert.False(prop.IsIncHitRecursive);
            Assert.False(prop.IsExcHitRecursive);
            Assert.False(prop.IsDirFilterOr);
            Assert.False(prop.IsXdOnlyFiles);
            Assert.False(prop.IsRmNohit);
            Assert.True(prop.IsAscending);
            Assert.False(prop.IsShowDirList);
            Assert.False(prop.IsShowFileList);

            // Assert - コレクションプロパティ
            Assert.NotNull(prop.NetUseOkErrNoList);
            Assert.Empty(prop.NetUseOkErrNoList);
            Assert.NotNull(prop.IncFilesList);
            Assert.Empty(prop.IncFilesList);
            Assert.NotNull(prop.ExcFilesList);
            Assert.Empty(prop.ExcFilesList);
            Assert.NotNull(prop.IncDirsList);
            Assert.Empty(prop.IncDirsList);
            Assert.NotNull(prop.ExcDirsList);
            Assert.Empty(prop.ExcDirsList);
            Assert.NotNull(prop.FileList);
            Assert.Empty(prop.FileList);
        }

        #endregion

        #region 3. プロパティの読み書き（Getter/Setter）テスト

        [Fact]
        public void Properties_CanGetAndSetAllValues()
        {
            // Arrange
            var prop = new ClsProp();
            var testTime = new DateTime(2026, 8, 15, 12, 0, 0);

            // Act - 各種プロパティに値を設定
            prop.ExeBaseName = "test_exe";
            prop.ExeDir = @"C:\app";
            prop.SourcePath = @"C:\src";
            prop.DestinationPath = @"C:\dst";
            prop.WorkDir = @"C:\work";
            prop.Action = "sync";
            prop.Mode = "fast";
            prop.CopyCmdType = ClsProp.COPY_OS_CMD;
            prop.ActionCode = ClsProp.ACTION_SYNC;
            prop.Task = ClsProp.TASK_MV;
            prop.CheckLogic = ClsProp.CHECK_SHA1;
            prop.CompOpe = ClsProp.COMPARISON_GT;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IsNeedPathFr = true;
            prop.IsNeedPathTo = true;
            prop.IsList = true;
            prop.IsReverse = true;
            prop.IsSizeCheck = false;
            prop.IsSyncRmOnly = true;
            prop.IsFlat = true;
            prop.IsDirTerm = true;
            prop.IsAlwaysMkDir = true;
            prop.IsFileCopy = false;
            prop.IsSkip = true;
            prop.CheckFileLock = ClsProp.CHECK_FILE_LOCK_SAMPLE;
            prop.IsSourceCheck = true;
            prop.IsFrPathCheck = false;
            prop.IsRetFiles = true;
            prop.SkipSize = 1000L;
            prop.CopySize = 2000L;
            prop.CompSize = 3000L;
            prop.Interval = 120;
            prop.MaxLoop = 5;
            prop.IsBackup = true;
            prop.IsErrorIfBackupFailed = false;
            prop.BackupDir = @"C:\backup";
            prop.ObjFileShare = FileShare.None;
            prop.WaitMSecForRetryCopy = 500;
            prop.RetrySystemCopyMax = 3;
            prop.SecRange = 1.5;
            prop.Verbose = 2;
            prop.IsShowCurDir = 1;
            prop.OutputPathCode = ClsProp.BOTH;
            prop.ProgressIntervalDirs = 10;
            prop.ProgressIntervalFiles = 50;
            prop.IsRelative = true;
            prop.IsProgress = true;
            prop.IsStackTrace = true;
            prop.IsShowNewFile = false;
            prop.IsShowUpdatedFile = false;
            prop.IsShowSameFile = false;
            prop.OutputPathPrefix = ">> ";
            prop.IsShowPath = true;
            prop.IsShowSize = true;
            prop.IsShowDirNum = true;
            prop.IsShowFileNum = true;
            prop.IsShowPerm = true;
            prop.IsShowOwner = true;
            prop.IsSymLink = true;
            prop.IntIsOverWrite = 1;
            prop.MaxKeep = 14;
            prop.CmdPath = @"C:\cmd.exe";
            prop.CmdArgs = "/c dir";
            prop.IsDq = true;
            prop.WarnThreshold = 10;
            prop.ErrorThreshold = 20;
            prop.IsExecCmd = true;
            prop.ExecModeCode = ClsProp.EXEC_MODE_PS;
            prop.Priority = 1;
            prop.Timeout = 3600;
            prop.IsErrorAtNegativeValue = true;
            prop.IsAlwaysNormal = true;
            prop.IsShowCmd = true;
            prop.IsShowOutput = true;
            prop.IsShowExitCode = true;
            prop.IsCat = true;
            prop.IsCatRetWcl = true;
            prop.CatI = "include";
            prop.CatX = "exclude";
            prop.CatP = "pattern";
            prop.CatE = "encoding";
            prop.CatXmlNl = "xmlnl";
            prop.CatOptions = "-v";
            prop.Files = 100UL;
            prop.Lines = 5000UL;
            prop.IsLogonAlwaysOk = true;
            prop.IsMount = true;
            prop.IsUmount = true;
            prop.IsSwitchUser = true;
            prop.IsLogon = true;
            prop.IsLogoff = true;
            prop.NetSharePath = @"\\server\share";
            prop.DriveName = "Z:";
            prop.DomainName = "DOMAIN";
            prop.Username = "user";
            prop.UsernameWithoutDomain = "user_nodom";
            prop.Password = "secret";
            prop.NetUseOkErrNoList = [0, 85];
            prop.TypeCode = MdlConst.INT_TYPE_FILE;
            prop.MaxDepth = 3UL;
            prop.MinDepth = 1UL;
            prop.IsBefore = true;
            prop.IsAfter = true;
            prop.BeforeTime = testTime;
            prop.AfterTime = testTime.AddDays(-1);
            prop.IsRegIncBasename = true;
            prop.IsRegExcBasename = true;
            prop.IsIncHitRecursive = true;
            prop.IsExcHitRecursive = true;
            prop.IsDirFilterOr = true;
            prop.IsXdOnlyFiles = true;
            prop.IsRmNohit = true;
            prop.IncFilesList = ["*.txt", "*.log"];
            prop.ExcFilesList = ["*.tmp"];
            prop.IncDirsList = ["dir1", "dir2"];
            prop.ExcDirsList = ["obj", "bin"];
            prop.FilesTypeCode = ClsProp.FILES_FULL;
            prop.FileListPath = @"C:\list.txt";
            prop.FileListType = "full";
            prop.FileListRegex = @"\t";
            prop.FileList = ["item1", "item2"];
            prop.IsCpTimestamp = 1;
            prop.TsSource = "ts_src";
            prop.TsDestination = "ts_dst";
            prop.TsBackup = "ts_bak";
            prop.SortType = MdlFile.SORT_BY_NAME;
            prop.IsAscending = false;
            prop.IsShowDirList = true;
            prop.IsShowFileList = true;

            // Assert - 設定した値が取得できることの検証
            Assert.Equal("test_exe", prop.ExeBaseName);
            Assert.Equal(@"C:\app", prop.ExeDir);
            Assert.Equal(@"C:\src", prop.SourcePath);
            Assert.Equal(@"C:\dst", prop.DestinationPath);
            Assert.Equal(@"C:\work", prop.WorkDir);
            Assert.Equal("sync", prop.Action);
            Assert.Equal("fast", prop.Mode);
            Assert.Equal(ClsProp.COPY_OS_CMD, prop.CopyCmdType);
            Assert.Equal(ClsProp.ACTION_SYNC, prop.ActionCode);
            Assert.Equal(ClsProp.TASK_MV, prop.Task);
            Assert.Equal(ClsProp.CHECK_SHA1, prop.CheckLogic);
            Assert.Equal(ClsProp.COMPARISON_GT, prop.CompOpe);
            Assert.Equal(MdlFile.PATH_IS_DIRECTORY, prop.PathType);
            Assert.True(prop.IsNeedPathFr);
            Assert.True(prop.IsNeedPathTo);
            Assert.True(prop.IsList);
            Assert.True(prop.IsReverse);
            Assert.False(prop.IsSizeCheck);
            Assert.True(prop.IsSyncRmOnly);
            Assert.True(prop.IsFlat);
            Assert.True(prop.IsDirTerm);
            Assert.True(prop.IsAlwaysMkDir);
            Assert.False(prop.IsFileCopy);
            Assert.True(prop.IsSkip);
            Assert.Equal(ClsProp.CHECK_FILE_LOCK_SAMPLE, prop.CheckFileLock);
            Assert.True(prop.IsSourceCheck);
            Assert.False(prop.IsFrPathCheck);
            Assert.True(prop.IsRetFiles);
            Assert.Equal(1000L, prop.SkipSize);
            Assert.Equal(2000L, prop.CopySize);
            Assert.Equal(3000L, prop.CompSize);
            Assert.Equal(120, prop.Interval);
            Assert.Equal(5, prop.MaxLoop);
            Assert.True(prop.IsBackup);
            Assert.False(prop.IsErrorIfBackupFailed);
            Assert.Equal(@"C:\backup", prop.BackupDir);
            Assert.Equal(FileShare.None, prop.ObjFileShare);
            Assert.Equal(500, prop.WaitMSecForRetryCopy);
            Assert.Equal(3, prop.RetrySystemCopyMax);
            Assert.Equal(1.5, prop.SecRange);
            Assert.Equal(2, prop.Verbose);
            Assert.Equal(1, prop.IsShowCurDir);
            Assert.Equal(ClsProp.BOTH, prop.OutputPathCode);
            Assert.Equal(10, prop.ProgressIntervalDirs);
            Assert.Equal(50, prop.ProgressIntervalFiles);
            Assert.True(prop.IsRelative);
            Assert.True(prop.IsProgress);
            Assert.True(prop.IsStackTrace);
            Assert.False(prop.IsShowNewFile);
            Assert.False(prop.IsShowUpdatedFile);
            Assert.False(prop.IsShowSameFile);
            Assert.Equal(">> ", prop.OutputPathPrefix);
            Assert.True(prop.IsShowPath);
            Assert.True(prop.IsShowSize);
            Assert.True(prop.IsShowDirNum);
            Assert.True(prop.IsShowFileNum);
            Assert.True(prop.IsShowPerm);
            Assert.True(prop.IsShowOwner);
            Assert.True(prop.IsSymLink);
            Assert.Equal(1, prop.IntIsOverWrite);
            Assert.Equal(14, prop.MaxKeep);
            Assert.Equal(@"C:\cmd.exe", prop.CmdPath);
            Assert.Equal("/c dir", prop.CmdArgs);
            Assert.True(prop.IsDq);
            Assert.Equal(10, prop.WarnThreshold);
            Assert.Equal(20, prop.ErrorThreshold);
            Assert.True(prop.IsExecCmd);
            Assert.Equal(ClsProp.EXEC_MODE_PS, prop.ExecModeCode);
            Assert.Equal(1, prop.Priority);
            Assert.Equal(3600, prop.Timeout);
            Assert.True(prop.IsErrorAtNegativeValue);
            Assert.True(prop.IsAlwaysNormal);
            Assert.True(prop.IsShowCmd);
            Assert.True(prop.IsShowOutput);
            Assert.True(prop.IsShowExitCode);
            Assert.True(prop.IsCat);
            Assert.True(prop.IsCatRetWcl);
            Assert.Equal("include", prop.CatI);
            Assert.Equal("exclude", prop.CatX);
            Assert.Equal("pattern", prop.CatP);
            Assert.Equal("encoding", prop.CatE);
            Assert.Equal("xmlnl", prop.CatXmlNl);
            Assert.Equal("-v", prop.CatOptions);
            Assert.Equal(100UL, prop.Files);
            Assert.Equal(5000UL, prop.Lines);
            Assert.True(prop.IsLogonAlwaysOk);
            Assert.True(prop.IsMount);
            Assert.True(prop.IsUmount);
            Assert.True(prop.IsSwitchUser);
            Assert.True(prop.IsLogon);
            Assert.True(prop.IsLogoff);
            Assert.Equal(@"\\server\share", prop.NetSharePath);
            Assert.Equal("Z:", prop.DriveName);
            Assert.Equal("DOMAIN", prop.DomainName);
            Assert.Equal("user", prop.Username);
            Assert.Equal("user_nodom", prop.UsernameWithoutDomain);
            Assert.Equal("secret", prop.Password);
            Assert.Equal([0, 85], prop.NetUseOkErrNoList);
            Assert.Equal(MdlConst.INT_TYPE_FILE, prop.TypeCode);
            Assert.Equal(3UL, prop.MaxDepth);
            Assert.Equal(1UL, prop.MinDepth);
            Assert.True(prop.IsBefore);
            Assert.True(prop.IsAfter);
            Assert.Equal(testTime, prop.BeforeTime);
            Assert.Equal(testTime.AddDays(-1), prop.AfterTime);
            Assert.True(prop.IsRegIncBasename);
            Assert.True(prop.IsRegExcBasename);
            Assert.True(prop.IsIncHitRecursive);
            Assert.True(prop.IsExcHitRecursive);
            Assert.True(prop.IsDirFilterOr);
            Assert.True(prop.IsXdOnlyFiles);
            Assert.True(prop.IsRmNohit);
            Assert.Equal(["*.txt", "*.log"], prop.IncFilesList);
            Assert.Equal(["*.tmp"], prop.ExcFilesList);
            Assert.Equal(["dir1", "dir2"], prop.IncDirsList);
            Assert.Equal(["obj", "bin"], prop.ExcDirsList);
            Assert.Equal(ClsProp.FILES_FULL, prop.FilesTypeCode);
            Assert.Equal(@"C:\list.txt", prop.FileListPath);
            Assert.Equal("full", prop.FileListType);
            Assert.Equal(@"\t", prop.FileListRegex);
            Assert.Equal(["item1", "item2"], prop.FileList);
            Assert.Equal(1, prop.IsCpTimestamp);
            Assert.Equal("ts_src", prop.TsSource);
            Assert.Equal("ts_dst", prop.TsDestination);
            Assert.Equal("ts_bak", prop.TsBackup);
            Assert.Equal(MdlFile.SORT_BY_NAME, prop.SortType);
            Assert.False(prop.IsAscending);
            Assert.True(prop.IsShowDirList);
            Assert.True(prop.IsShowFileList);
        }

        #endregion

        #region 4. GetOutputModeStr メソッドのテスト

        [Theory]
        [InlineData(ClsProp.FROM, "fr")]
        [InlineData(ClsProp.TO, "to")]
        [InlineData(ClsProp.BOTH, "both")]
        [InlineData(ClsProp.RELATIVE, "rel")]
        [InlineData(-1, "rel")]
        [InlineData(999, "rel")]
        public void GetOutputModeStr_ReturnsExpectedString(int mode, string expected)
        {
            // Arrange
            var prop = new ClsProp();

            // Act
            string actual = prop.GetOutputModeStr(mode);

            // Assert
            Assert.Equal(expected, actual);
        }

        #endregion

        #region 5. GetOutputModeCode メソッドのテスト

        [Theory]
        [InlineData("from", ClsProp.FROM)]
        [InlineData("FROM", ClsProp.FROM)]
        [InlineData("fr", ClsProp.FROM)]
        [InlineData("FR", ClsProp.FROM)]
        [InlineData("f", ClsProp.FROM)]
        [InlineData("F", ClsProp.FROM)]
        [InlineData("to", ClsProp.TO)]
        [InlineData("TO", ClsProp.TO)]
        [InlineData("t", ClsProp.TO)]
        [InlineData("T", ClsProp.TO)]
        [InlineData("both", ClsProp.BOTH)]
        [InlineData("BOTH", ClsProp.BOTH)]
        [InlineData("b", ClsProp.BOTH)]
        [InlineData("B", ClsProp.BOTH)]
        [InlineData("rel", ClsProp.RELATIVE)]
        [InlineData("REL", ClsProp.RELATIVE)]
        [InlineData("", ClsProp.RELATIVE)]
        [InlineData("unknown", ClsProp.RELATIVE)]
        [InlineData(null, ClsProp.RELATIVE)]
        public void GetOutputModeCode_ReturnsExpectedCode(string? mode, int expected)
        {
            // Arrange
            var prop = new ClsProp();

            // Act
            int actual = prop.GetOutputModeCode(mode!);

            // Assert
            Assert.Equal(expected, actual);
        }

        #endregion

        #region 6. GetCheckLockFileModeStr メソッドのテスト

        [Theory]
        [InlineData(ClsProp.CHECK_FILE_LOCK_NONE, "false")]
        [InlineData(ClsProp.CHECK_FILE_LOCK_SAMPLE, "sample")]
        [InlineData(ClsProp.CHECK_FILE_LOCK_SKIP, "skip")]
        [InlineData(-1, "skip")]
        [InlineData(100, "skip")]
        public void GetCheckLockFileModeStr_ReturnsExpectedString(int mode, string expected)
        {
            // Arrange
            var prop = new ClsProp();

            // Act
            string actual = prop.GetCheckLockFileModeStr(mode);

            // Assert
            Assert.Equal(expected, actual);
        }

        #endregion

        #region 7. GetExecModeStr メソッドのテスト

        [Theory]
        [InlineData(ClsProp.EXEC_MODE_CMD, "cmd")]
        [InlineData(ClsProp.EXEC_MODE_PS, "ps")]
        [InlineData(ClsProp.EXEC_MODE_PSC, "psc")]
        [InlineData(ClsProp.EXEC_MODE_EXE, "exe")]
        [InlineData(ClsProp.EXEC_MODE_NORMAL, "normal")]
        [InlineData(-1, "normal")]
        [InlineData(999, "normal")]
        public void GetExecModeStr_ReturnsExpectedString(int mode, string expected)
        {
            // Arrange
            var prop = new ClsProp();

            // Act
            string actual = prop.GetExecModeStr(mode);

            // Assert
            Assert.Equal(expected, actual);
        }

        #endregion
    }
}
