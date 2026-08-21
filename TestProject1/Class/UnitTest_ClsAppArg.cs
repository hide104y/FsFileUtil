using System;
using System.Collections.Generic;
using System.IO;
using CmnClsLib.Class;
using CmnClsLib.Module;
using FsFileUtil.Class;
using Xunit;

namespace TestProject1.Class
{
    public class UnitTest_ClsAppArg : IDisposable
    {
        private readonly string _testRoot;
        private readonly ClsLogger _logger;

        public UnitTest_ClsAppArg()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), @"UnitTest", @"FsFileUtil", @"ClsAppArg", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);
            _logger = new ClsLogger();
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testRoot))
                {
                    Directory.Delete(_testRoot, true);
                }
            }
            catch
            {
                // クリーンアップエラーは無視
            }
        }

        private ClsAppArg CreateAppArg(out ClsProp prop)
        {
            prop = new ClsProp();
            return new ClsAppArg(_logger, prop);
        }

        // =====================================================================
        // 1. コンストラクタおよびプロパティテスト
        // =====================================================================
        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var appArg = CreateAppArg(out var prop);

            // Assert
            Assert.NotNull(appArg.Properties);
            Assert.Same(prop, appArg.Properties);
            Assert.False(string.IsNullOrEmpty(appArg.ExeDir));
            Assert.False(string.IsNullOrEmpty(appArg.ExeBaseName));
            Assert.False(appArg.IsUsage);
            Assert.False(appArg.IsEchoRetcode);
        }

        [Fact]
        public void Properties_GetSet_WorksCorrectly()
        {
            // Arrange
            var appArg = CreateAppArg(out _);
            var newProp = new ClsProp { Verbose = 3 };

            // Act
            appArg.Properties = newProp;
            appArg.ExeBaseName = "CustomExe";
            appArg.ExeDir = @"C:\CustomDir";
            appArg.IsEchoRetcode = true;

            // Assert
            Assert.Same(newProp, appArg.Properties);
            Assert.Equal("CustomExe", appArg.ExeBaseName);
            Assert.Equal(@"C:\CustomDir", appArg.ExeDir);
            Assert.True(appArg.IsEchoRetcode);
        }

        // =====================================================================
        // 2. Action解析テスト (-a)
        // =====================================================================
        [Fact]
        public void Parse_DefaultAction_SetsCopy()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal("copy", prop.Action);
            Assert.Equal(ClsProp.ACTION_COPY, prop.ActionCode);
            Assert.True(prop.IsNeedPathFr);
            Assert.True(prop.IsNeedPathTo);
            Assert.True(prop.IsAlwaysMkDir);
        }

        [Theory]
        [InlineData("move", ClsProp.ACTION_MOVE, true, true, true)]
        [InlineData("sync", ClsProp.ACTION_SYNC, true, true, true)]
        [InlineData("ls", ClsProp.ACTION_LS, true, false, false)]
        [InlineData("mkdir", ClsProp.ACTION_MKDIR, true, false, false)]
        [InlineData("touch", ClsProp.ACTION_TOUCH, true, false, false)]
        [InlineData("exist", ClsProp.ACTION_EXIST, true, false, false)]
        [InlineData("isdir", ClsProp.ACTION_EXIST_DIR, true, false, false)]
        [InlineData("isfile", ClsProp.ACTION_EXIST_FILE, true, false, false)]
        [InlineData("wait", ClsProp.ACTION_WAIT, true, false, false)]
        [InlineData("rename", ClsProp.ACTION_RENAME, true, true, false)]
        [InlineData("rotate", ClsProp.ACTION_ROTATE, true, false, false)]
        [InlineData("realpath", ClsProp.ACTION_GET_REAL_PATH, true, false, false)]
        [InlineData("size", ClsProp.ACTION_GET_SIZE, true, false, false)]
        [InlineData("perm", ClsProp.ACTION_GET_PERM, true, false, false)]
        [InlineData("owner", ClsProp.ACTION_GET_OWNER, true, false, false)]
        public void Parse_VariousActions_SetsExpectedProperties(
            string action,
            int expectedActionCode,
            bool expectedNeedFr,
            bool expectedNeedTo,
            bool expectedAlwaysMkDir)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            List<string> argsList = ["-a", action, "-f", @"C:\src"];
            if (expectedNeedTo)
            {
                argsList.AddRange(["-t", @"C:\dest"]);
            }

            // Act
            bool result = appArg.Parse(argsList.ToArray());

            // Assert
            Assert.True(result);
            Assert.Equal(action, prop.Action);
            Assert.Equal(expectedActionCode, prop.ActionCode);
            Assert.Equal(expectedNeedFr, prop.IsNeedPathFr);
            Assert.Equal(expectedNeedTo, prop.IsNeedPathTo);
            if (expectedAlwaysMkDir)
            {
                Assert.True(prop.IsAlwaysMkDir);
            }
        }

        [Fact]
        public void Parse_Action_SyncRm_SetsExpectedFlags()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "syncrm", "-f", @"C:\src", "-t", @"C:\dest"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(ClsProp.ACTION_SYNC, prop.ActionCode);
            Assert.True(prop.IsSyncRmOnly);
            Assert.False(prop.IsFileCopy);
        }

        [Fact]
        public void Parse_Action_Find_SetsExpectedFlags()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "find", "-f", @"C:\src"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(ClsProp.ACTION_FIND, prop.ActionCode);
            Assert.Equal(MdlConst.INT_TYPE_FILE, prop.TypeCode);
            Assert.True(prop.IsShowOutput);
        }

        [Theory]
        [InlineData("delete", MdlConst.INT_TYPE_ALL)]
        [InlineData("delete-dir", MdlConst.INT_TYPE_DIRECTORY)]
        [InlineData("delete-file", MdlConst.INT_TYPE_FILE)]
        public void Parse_Action_DeleteVariants_SetsExpectedTypeCode(string action, int expectedTypeCode)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", action, "-f", @"C:\src"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(ClsProp.ACTION_DELETE, prop.ActionCode);
            Assert.Equal(expectedTypeCode, prop.TypeCode);
        }

        [Fact]
        public void Parse_Action_IsLocked_SetsSampleCheck()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "islocked", "-f", @"C:\src\file.txt"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(ClsProp.ACTION_FILE_LOCKED, prop.ActionCode);
            Assert.Equal(ClsProp.CHECK_FILE_LOCK_SAMPLE, prop.CheckFileLock);
        }

        [Fact]
        public void Parse_Action_Reverse_SetsExpectedFlags()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "reverse", "-f", @"C:\src", "-t", @"C:\dest"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(ClsProp.ACTION_COPY, prop.ActionCode);
            Assert.True(prop.IsReverse);
            Assert.False(prop.IsAlwaysMkDir);
        }

        [Fact]
        public void Parse_Action_FlatCopy_SetsExpectedFlags()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "flatcopy", "-f", @"C:\src", "-t", @"C:\dest"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(ClsProp.ACTION_COPY, prop.ActionCode);
            Assert.True(prop.IsFlat);
            Assert.False(prop.IsAlwaysMkDir);
        }

        [Fact]
        public void Parse_Action_DirCopy_SetsExpectedFlags()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "dircopy", "-f", @"C:\src", "-t", @"C:\dest"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(ClsProp.ACTION_COPY, prop.ActionCode);
            Assert.False(prop.IsFileCopy);
            Assert.True(prop.IsAlwaysMkDir);
        }

        [Theory]
        [InlineData("lock-proc")]
        [InlineData("logon")]
        [InlineData("logoff")]
        public void Parse_DeprecatedActions_ReturnsFalse(string deprecatedAction)
        {
            // Arrange
            var appArg = CreateAppArg(out _);
            string[] args = ["-a", deprecatedAction, "-f", @"C:\src"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Parse_Action_MountAndUmount_SetsExpectedFlags()
        {
            // Arrange & Act (mount)
            var appArg1 = CreateAppArg(out var prop1);
            bool resMount = appArg1.Parse(["-a", "mount", "-f", @"\\server\share"]);

            // Arrange & Act (umount)
            var appArg2 = CreateAppArg(out var prop2);
            bool resUmount = appArg2.Parse(["-a", "umount", "-f", @"\\server\share"]);

            // Assert
            Assert.True(resMount);
            Assert.True(prop1.IsMount);
            Assert.False(prop1.IsUmount);
            Assert.Equal(@"\\server\share", prop1.NetSharePath);

            Assert.True(resUmount);
            Assert.False(prop2.IsMount);
            Assert.True(prop2.IsUmount);
            Assert.Equal(@"\\server\share", prop2.NetSharePath);
        }

        [Fact]
        public void Parse_Action_Exec_SetsShowOutput()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "exec", "-exec", "echo hello"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(ClsProp.ACTION_EXEC, prop.ActionCode);
            Assert.True(prop.IsShowOutput);
        }

        // =====================================================================
        // 3. 差分判定モード (-m) およびコピー先 (-t) 解析テスト
        // =====================================================================
        [Theory]
        [InlineData("size", ClsProp.CHECK_SIZE, true)]
        [InlineData("mtime", ClsProp.CHECK_MTIME, true)]
        [InlineData("new", ClsProp.CHECK_MTIME_NEW, true)]
        [InlineData("old", ClsProp.CHECK_MTIME_OLD, true)]
        [InlineData("cksum", ClsProp.CHECK_CKSUM, true)]
        [InlineData("adler32", ClsProp.CHECK_ADLER32, true)]
        [InlineData("sha1", ClsProp.CHECK_SHA1, true)]
        [InlineData("date", ClsProp.CHECK_MTIME, false)]
        [InlineData("newer", ClsProp.CHECK_MTIME_NEW, false)]
        [InlineData("older", ClsProp.CHECK_MTIME_OLD, false)]
        [InlineData("exist", ClsProp.CHECK_EXIST, false)]
        [InlineData("unknown_mode", ClsProp.CHECK_NONE, true)]
        public void Parse_CheckLogicModes_SetsExpectedLogic(string mode, int expectedLogic, bool expectedSizeCheck)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest", "-m", mode];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedLogic, prop.CheckLogic);
            Assert.Equal(expectedSizeCheck, prop.IsSizeCheck);
        }

        [Fact]
        public void Parse_DestinationPath_WithDot_AppendsSourceFileName()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\source_folder\my_file.txt", "-t", @"C:\dest_folder\."];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(@"C:\dest_folder\my_file.txt", prop.DestinationPath);
        }

        [Fact]
        public void Parse_CopyOptions_SetsFlagsCorrectly()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = [
                "-f", @"C:\src",
                "-t", @"C:\dest",
                "-list",
                "-tsc",
                "-fchk",
                "-rmnohit",
                "-no-emptydir",
                "-async",
                "-skipsize", "10",
                "-copysize", "20",
                "-wait-retry-copy", "500",
                "-retry-syscopy", "3",
                "-rel"
            ];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.True(prop.IsList);
            Assert.Equal(1, prop.IsCpTimestamp);
            Assert.True(prop.IsSourceCheck);
            Assert.True(prop.IsRmNohit);
            Assert.False(prop.IsAlwaysMkDir);
            Assert.Equal(ClsProp.COPY_ASYNC, prop.CopyCmdType);
            Assert.Equal(10 * 1024 * 1024, prop.SkipSize);
            Assert.Equal(20 * 1024 * 1024, prop.CopySize);
            Assert.Equal(500, prop.WaitMSecForRetryCopy);
            Assert.Equal(3, prop.RetrySystemCopyMax);
            Assert.True(prop.IsRelative);
        }

        [Theory]
        [InlineData("tsm", 2)]
        [InlineData("ts", 3)]
        public void Parse_TimestampSyncOptions_SetsTimestampCode(string opt, int expectedCode)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest", "-" + opt];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedCode, prop.IsCpTimestamp);
        }

        [Fact]
        public void Parse_CopyCmdType_Os_SetsExpectedFlags()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest", "-os"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(ClsProp.COPY_OS_CMD, prop.CopyCmdType);
            Assert.False(prop.IsProgress);
        }

        // =====================================================================
        // 4. ファイル共有モード (-fileshare) 解析テスト
        // =====================================================================
        [Theory]
        [InlineData("none", FileShare.None)]
        [InlineData("0", FileShare.None)]
        [InlineData("read", FileShare.Read)]
        [InlineData("1", FileShare.Read)]
        [InlineData("write", FileShare.Write)]
        [InlineData("2", FileShare.Write)]
        [InlineData("readwrite", FileShare.ReadWrite)]
        [InlineData("3", FileShare.ReadWrite)]
        [InlineData("delete", FileShare.Delete)]
        [InlineData("4", FileShare.Delete)]
        [InlineData("5", FileShare.Read | FileShare.Delete)]
        [InlineData("write|delete", FileShare.Write | FileShare.Delete)]
        [InlineData("6", FileShare.Write | FileShare.Delete)]
        [InlineData("readwrite|delete", FileShare.ReadWrite | FileShare.Delete)]
        [InlineData("7", FileShare.ReadWrite | FileShare.Delete)]
        [InlineData("inheritable", FileShare.Inheritable)]
        [InlineData("16", FileShare.Inheritable)]
        [InlineData("17", FileShare.Read | FileShare.Inheritable)]
        [InlineData("write|inheritable", FileShare.Write | FileShare.Inheritable)]
        [InlineData("18", FileShare.Write | FileShare.Inheritable)]
        [InlineData("readwrite|inheritable", FileShare.ReadWrite | FileShare.Inheritable)]
        [InlineData("19", FileShare.ReadWrite | FileShare.Inheritable)]
        [InlineData("delete|inheritable", FileShare.Delete | FileShare.Inheritable)]
        [InlineData("20", FileShare.Delete | FileShare.Inheritable)]
        [InlineData("read|delete|inheritable", FileShare.Read | FileShare.Delete | FileShare.Inheritable)]
        [InlineData("21", FileShare.Read | FileShare.Delete | FileShare.Inheritable)]
        [InlineData("write|delete|inheritable", FileShare.Write | FileShare.Delete | FileShare.Inheritable)]
        [InlineData("22", FileShare.Write | FileShare.Delete | FileShare.Inheritable)]
        [InlineData("readwrite|delete|inheritable", FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable)]
        [InlineData("23", FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable)]
        public void Parse_FileShare_SetsObjFileShareCorrectly(string shareArg, FileShare expectedShare)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest", "-fileshare", shareArg];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedShare, prop.ObjFileShare);
            Assert.Equal(ClsProp.CHECK_FILE_LOCK_SKIP, prop.CheckFileLock);
        }

        // =====================================================================
        // 5. バックアップオプション (-backup, -force, -ts-*) 解析テスト
        // =====================================================================
        [Fact]
        public void Parse_Backup_ExplicitAndDefault()
        {
            // Arrange & Act (明示指定)
            var appArg1 = CreateAppArg(out var prop1);
            bool res1 = appArg1.Parse(["-f", @"C:\src", "-t", @"C:\dest", "-backup", @"C:\my_backup", "-force"]);

            // Arrange & Act (引数なしデフォルト)
            var appArg2 = CreateAppArg(out var prop2);
            bool res2 = appArg2.Parse(["-f", @"C:\src", "-t", @"C:\dest", "-backup"]);

            // Assert
            Assert.True(res1);
            Assert.Equal(@"C:\my_backup", prop1.BackupDir);
            Assert.True(prop1.IsErrorIfBackupFailed);

            Assert.True(res2);
            Assert.Contains(".%Y%m%d.%H%M%S.", prop2.BackupDir);
        }

        [Fact]
        public void Parse_TimestampMacroOptions_SetsTsProperties()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = [
                "-f", @"C:\src",
                "-t", @"C:\dest",
                "-ts-f", "today",
                "-ts-t", "yesterday",
                "-ts-b", "now"
            ];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal("today", prop.TsSource);
            Assert.Equal("yesterday", prop.TsDestination);
            Assert.Equal("now", prop.TsBackup);
        }

        // =====================================================================
        // 6. フィルター・期間・日付・サイズ解析テスト
        // =====================================================================
        [Fact]
        public void Parse_MinMaxDepth_ValidRange_SetsProperties()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest", "-min", "2", "-max", "5"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(2UL, prop.MinDepth);
            Assert.Equal(5UL, prop.MaxDepth);
        }

        [Fact]
        public void Parse_MinMaxDepth_MinGreaterThanMax_ReturnsFalse()
        {
            // Arrange
            var appArg = CreateAppArg(out _);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest", "-min", "5", "-max", "2"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("d", 2.0, true, true)]
        [InlineData("h", 5.0, true, true)]
        [InlineData("m", 30.0, true, true)]
        [InlineData("s", 120.0, true, true)]
        [InlineData("d", 3.0, false, false)]
        public void Parse_PeriodAndTerm_SetsCorrectTime(string unit, double term, bool isNew, bool useTerm)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            List<string> argsList = ["-f", @"C:\src", "-t", @"C:\dest", "-period", unit, useTerm ? "-term" : "-days", term.ToString()];
            if (isNew)
            {
                argsList.Add("-new");
            }

            // Act
            bool result = appArg.Parse(argsList.ToArray());

            // Assert
            Assert.True(result);
            if (isNew)
            {
                Assert.True(prop.IsAfter);
                Assert.True(prop.AfterTime <= DateTime.Now);
            }
            else
            {
                Assert.True(prop.IsBefore);
                Assert.True(prop.BeforeTime <= DateTime.Now.AddDays(1));
            }
        }

        [Theory]
        [InlineData("now")]
        [InlineData("today")]
        [InlineData("yesterday")]
        [InlineData("tomorrow")]
        [InlineData("20260101")]
        [InlineData("-3")]
        public void Parse_BeforeAndAfterOptions_ParsesCorrectly(string dateArg)
        {
            // Arrange & Act (Before)
            var appArg1 = CreateAppArg(out var prop1);
            bool res1 = appArg1.Parse(["-f", @"C:\src", "-t", @"C:\dest", "-before", dateArg]);

            // Arrange & Act (After)
            var appArg2 = CreateAppArg(out var prop2);
            bool res2 = appArg2.Parse(["-f", @"C:\src", "-t", @"C:\dest", "-after", dateArg]);

            // Assert
            Assert.True(res1);
            Assert.True(prop1.IsBefore);

            Assert.True(res2);
            Assert.True(prop2.IsAfter);
        }

        [Fact]
        public void Parse_DirTerm_SetsFlag()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest", "-dirterm"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.True(prop.IsDirTerm);
        }

        [Theory]
        [InlineData("+100", 100L, ClsProp.COMPARISON_GE)]
        [InlineData("-200", 200L, ClsProp.COMPARISON_LE)]
        [InlineData("+1KB", 1024L, ClsProp.COMPARISON_GE)]
        [InlineData(@"\-2MB", 2L * 1024 * 1024, ClsProp.COMPARISON_LE)]
        [InlineData("+3GB", 3L * 1024 * 1024 * 1024, ClsProp.COMPARISON_GE)]
        [InlineData(@"\-4TB", 4L * 1024 * 1024 * 1024 * 1024, ClsProp.COMPARISON_LE)]
        public void Parse_SizeOption_ParsesSignAndUnits(string sizeArg, long expectedBytes, int expectedOpe)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest", "-size", sizeArg];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedBytes, prop.CompSize);
            Assert.Equal(expectedOpe, prop.CompOpe);
        }

        [Fact]
        public void Parse_FilterOptions_SetsRegexListsAndFlags()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = [
                "-f", @"C:\src",
                "-t", @"C:\dest",
                "-if", @"\.log$,\.txt$",
                "-id", @"debug,temp",
                "-xf", @"\.bak$",
                "-xd", @"obj,bin",
                "-idorxd",
                "-no-id-rec",
                "-no-xd-rec",
                "-xd-exc-p-dir",
                "-locked", "sample"
            ];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Contains(@"\.log$", prop.IncFilesList);
            Assert.Contains(@"\.txt$", prop.IncFilesList);
            Assert.Contains("debug", prop.IncDirsList);
            Assert.Contains("temp", prop.IncDirsList);
            Assert.Contains(@"\.bak$", prop.ExcFilesList);
            Assert.Contains("obj", prop.ExcDirsList);
            Assert.Contains("bin", prop.ExcDirsList);
            Assert.True(prop.IsDirFilterOr);
            Assert.False(prop.IsIncHitRecursive);
            Assert.False(prop.IsExcHitRecursive);
            Assert.True(prop.IsXdOnlyFiles);
            Assert.Equal(ClsProp.CHECK_FILE_LOCK_SAMPLE, prop.CheckFileLock);
        }

        // =====================================================================
        // 7. ファイルリスト解析テスト (-files, -files-type, -files-regex)
        // =====================================================================
        [Fact]
        public void Parse_FilesList_ValidFile_LoadsList()
        {
            // Arrange
            string listFilePath = Path.Combine(_testRoot, "filelist.txt");
            File.WriteAllLines(listFilePath, ["file1.txt", "file2.txt", "subdir/file3.txt"]);

            var appArg = CreateAppArg(out var prop);
            string[] args = [
                "-f", @"C:\src",
                "-t", @"C:\dest",
                "-files", listFilePath,
                "-files-type", "full",
                "-files-regex", @"[,\t]"
            ];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(listFilePath, prop.FileListPath);
            Assert.Equal(3, prop.FileList.Count);
            Assert.Equal("full", prop.FileListType);
            Assert.Equal(ClsProp.FILES_FULL, prop.FilesTypeCode);
        }

        [Fact]
        public void Parse_FilesList_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_testRoot, "non_existent_filelist.txt");
            var appArg = CreateAppArg(out _);
            string[] args = ["-f", @"C:\src", "-files", nonExistentPath];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.False(result);
        }

        // =====================================================================
        // 8. Find / Exec / Cat / Wait / Rotate オプション解析テスト
        // =====================================================================
        [Theory]
        [InlineData("f", MdlConst.INT_TYPE_FILE)]
        [InlineData("d", MdlConst.INT_TYPE_DIRECTORY)]
        [InlineData("a", MdlConst.INT_TYPE_ALL)]
        [InlineData("b", MdlConst.INT_TYPE_ALL)]
        public void Parse_TypeOption_SetsTypeCode(string typeStr, int expectedTypeCode)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "find", "-f", @"C:\src", "-type", typeStr, "-dq"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedTypeCode, prop.TypeCode);
            Assert.True(prop.IsDq);
        }

        [Fact]
        public void Parse_ExecOptions_SetsCommandProperties()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = [
                "-a", "exec",
                "-exec", "mytool.exe {}",
                "-exec-args", "arg1 arg2",
                "-exec-mode", "cmd",
                "-cwd", @"C:\workdir"
            ];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal("mytool.exe {}", prop.CmdPath);
            Assert.Equal("arg1 arg2", prop.CmdArgs);
            Assert.Equal(ClsProp.EXEC_MODE_CMD, prop.ExecModeCode);
            Assert.Equal(@"C:\workdir", prop.WorkDir);
        }

        [Fact]
        public void Parse_PsOption_SetsPowerShellMode()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "exec", "-ps", "Get-ChildItem"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal("Get-ChildItem", prop.CmdPath);
            Assert.Equal(ClsProp.EXEC_MODE_PS, prop.ExecModeCode);
        }

        [Fact]
        public void Parse_CatOptions_SetsCatFlagsAndOptions()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = [
                "-a", "find",
                "-f", @"C:\src",
                "-cat",
                "-cat-i", "pattern_i",
                "-cat-x", "pattern_x",
                "-cat-p", "pattern_p",
                "-cat-e", "utf-8",
                "-cat-a",
                "-cat-wcl",
                "-cat-ret-wcl",
                "-cat-n",
                "-cat-h",
                "-cat-options", "o1,o2"
            ];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.True(prop.IsCat);
            Assert.Equal("pattern_i", prop.CatI);
            Assert.Equal("pattern_x", prop.CatX);
            Assert.Equal("pattern_p", prop.CatP);
            Assert.Equal("utf-8", prop.CatE);
            Assert.True(prop.IsCatRetWcl);
            Assert.Contains("-o1", prop.CatOptions);
            Assert.Contains("-o2", prop.CatOptions);
            Assert.Contains("-a", prop.CatOptions);
            Assert.Contains("-wcl", prop.CatOptions);
            Assert.Contains("-n", prop.CatOptions);
            Assert.Contains("-h", prop.CatOptions);
        }

        [Fact]
        public void Parse_CatXmlNl_SetsXmlMode()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "find", "-f", @"C:\src", "-cat-xml-nl", "record"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.True(prop.IsCat);
            Assert.Equal("xml", prop.CatP);
            Assert.Equal("record", prop.CatXmlNl);
        }

        [Fact]
        public void Parse_ThresholdsAndProcessControl_SetsProperties()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = [
                "-f", @"C:\src",
                "-t", @"C:\dest",
                "-w", "5",
                "-e", "10",
                "-normal",
                "-negative",
                "-show-cmd", "no",
                "-show-output", "no",
                "-show-retcd", "no",
                "-priority", "2",
                "-n",
                "-c", "20",
                "-i", "2",
                "-k", "7",
                "-sec-range", "3"
            ];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(5, prop.WarnThreshold);
            Assert.Equal(10, prop.ErrorThreshold);
            Assert.True(prop.IsAlwaysNormal);
            Assert.True(prop.IsErrorAtNegativeValue);
            Assert.False(prop.IsShowCmd);
            Assert.False(prop.IsShowOutput);
            Assert.False(prop.IsShowExitCode);
            Assert.Equal(2, prop.Priority);
            Assert.True(prop.IsShowPath);
            Assert.Equal(20, prop.MaxLoop);
            Assert.Equal(2, prop.Interval);
            Assert.Equal(7, prop.MaxKeep);
            Assert.Equal(3, prop.SecRange);
        }

        // =====================================================================
        // 9. 出力制御・進捗・共通オプション解析テスト
        // =====================================================================
        [Theory]
        [InlineData("100,5000", 100, 5000)]
        [InlineData("", 1000, 100000)]
        public void Parse_ProgressOption_SetsIntervals(string progressArg, int expectedDirs, int expectedFiles)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            List<string> argsList = ["-f", @"C:\src", "-t", @"C:\dest", "-progress"];
            if (!string.IsNullOrEmpty(progressArg))
            {
                argsList.Add(progressArg);
            }

            // Act
            bool result = appArg.Parse(argsList.ToArray());

            // Assert
            Assert.True(result);
            Assert.True(prop.IsProgress);
            Assert.Equal(expectedDirs, prop.ProgressIntervalDirs);
            Assert.Equal(expectedFiles, prop.ProgressIntervalFiles);
        }

        [Theory]
        [InlineData("new", true, false, false)]
        [InlineData("updated", false, false, true)]
        [InlineData("diff", true, false, true)]
        public void Parse_ShowOption_InCopyAction_SetsVisibility(string showArg, bool expectedShowNew, bool expectedShowSame, bool expectedShowUpdated)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest", "-show", showArg];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedShowNew, prop.IsShowNewFile);
            Assert.Equal(expectedShowSame, prop.IsShowSameFile);
            Assert.Equal(expectedShowUpdated, prop.IsShowUpdatedFile);
        }

        [Fact]
        public void Parse_ShowOption_InSizeAction_SetsFlags()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "size", "-f", @"C:\src", "-show", "pdf"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.True(prop.IsShowPath);
            Assert.True(prop.IsShowDirNum);
            Assert.True(prop.IsShowFileNum);
        }

        [Fact]
        public void Parse_CommonAndMiscellaneousOptions_SetsFlags()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = [
                "-f", @"C:\src",
                "-t", @"C:\dest",
                "-h",
                "-v", "2",
                "-diff",
                "-timeout", "45",
                "-op-path", "rel",
                "-op-prefix", "[PREFIX]",
                "-show-dir", "10",
                "-echo-retcd"
            ];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.True(appArg.IsUsage);
            Assert.Equal(2, prop.Verbose);
            Assert.False(prop.IsShowSameFile);
            Assert.Equal(45, prop.Timeout);
            Assert.Equal("[PREFIX]", prop.OutputPathPrefix);
            Assert.Equal(10, prop.IsShowCurDir);
            Assert.True(appArg.IsEchoRetcode);
        }

        [Theory]
        [InlineData("1", 1)]
        [InlineData("2", 2)]
        public void Parse_SymOption_SetsOverwriteFlag(string symVal, int expectedVal)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest", "-sym", symVal];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedVal, prop.IntIsOverWrite);
        }

        [Fact]
        public void Parse_ReplaceDictionary_ReplacesPaths()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = [
                "-splitby", ",",
                "-replace", "SRC_KEY:NEW_SRC,DST_KEY:NEW_DST,BAK_KEY:NEW_BAK",
                "-f", @"C:\folder\SRC_KEY.txt",
                "-t", @"C:\folder\DST_KEY.txt",
                "-backup", @"C:\folder\BAK_KEY"
            ];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(@"C:\folder\NEW_SRC.txt", prop.SourcePath);
            Assert.Equal(@"C:\folder\NEW_DST.txt", prop.DestinationPath);
            Assert.Equal(@"C:\folder\NEW_BAK", prop.BackupDir);
        }

        [Fact]
        public void Parse_ShowPermAndOwner_SetsFlags()
        {
            // Arrange & Act (Perm)
            var appArg1 = CreateAppArg(out var prop1);
            bool res1 = appArg1.Parse(["-a", "perm", "-f", @"C:\src", "-show", "all"]);

            // Arrange & Act (Owner)
            var appArg2 = CreateAppArg(out var prop2);
            bool res2 = appArg2.Parse(["-a", "owner", "-f", @"C:\src", "-show", "all"]);

            // Assert
            Assert.True(res1);
            Assert.True(prop1.IsShowPath);
            Assert.True(prop1.IsShowOwner);
            Assert.True(prop1.IsShowPerm);

            Assert.True(res2);
            Assert.True(prop2.IsShowPath);
            Assert.True(prop2.IsShowOwner);
        }

        [Theory]
        [InlineData("cmd", ClsProp.EXEC_MODE_CMD)]
        [InlineData("c", ClsProp.EXEC_MODE_EXE)]
        [InlineData("exe", ClsProp.EXEC_MODE_EXE)]
        [InlineData("ps", ClsProp.EXEC_MODE_PS)]
        public void Parse_ExecModeVariants_SetsExecModeCode(string modeStr, int expectedCode)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-a", "exec", "-exec-mode", modeStr];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedCode, prop.ExecModeCode);
        }

        [Theory]
        [InlineData("r")]
        [InlineData("f")]
        [InlineData("t")]
        [InlineData("b")]
        public void Parse_OutputModeOptions_SetsOutputPathCode(string opPathMode)
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = ["-f", @"C:\src", "-t", @"C:\dest", "-op-path", opPathMode];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal(prop.GetOutputModeCode(opPathMode), prop.OutputPathCode);
        }

        [Fact]
        public void Parse_NetworkAuthOptions_SetsProperties()
        {
            // Arrange
            var appArg = CreateAppArg(out var prop);
            string[] args = [
                "-f", @"C:\src",
                "-t", @"C:\dest",
                "-domain", "MYDOMAIN",
                "-u", "myuser",
                "-p", "mypass",
                "-ignore-fail",
                "-su"
            ];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.True(result);
            Assert.Equal("MYDOMAIN", prop.DomainName);
            Assert.Equal("myuser", prop.UsernameWithoutDomain);
            Assert.Equal("mypass", prop.Password);
            Assert.True(prop.IsLogonAlwaysOk);
            Assert.True(prop.IsSwitchUser);
        }

        // =====================================================================
        // 10. エラーハンドリング（必須引数欠落など）
        // =====================================================================
        [Fact]
        public void Parse_MissingSourcePath_ForRequiredAction_ReturnsFalse()
        {
            // Arrange
            var appArg = CreateAppArg(out _);
            string[] args = ["-a", "copy", "-t", @"C:\dest"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Parse_MissingDestinationPath_ForRequiredAction_ReturnsFalse()
        {
            // Arrange
            var appArg = CreateAppArg(out _);
            string[] args = ["-a", "copy", "-f", @"C:\src"];

            // Act
            bool result = appArg.Parse(args);

            // Assert
            Assert.False(result);
        }

        // =====================================================================
        // 11. ShowUsage および PrintDefinition の実行テスト
        // =====================================================================
        [Fact]
        public void ShowUsage_ExecutesWithoutException()
        {
            // Arrange
            var appArg = CreateAppArg(out _);

            // Act & Assert
            var ex = Record.Exception(() => appArg.ShowUsage());
            Assert.Null(ex);
        }

        [Fact]
        public void PrintDefinition_ExecutesWithoutException_ForVariousActions()
        {
            // Arrange
            var appArg1 = CreateAppArg(out _);
            appArg1.Parse(["-a", "copy", "-f", @"C:\src", "-t", @"C:\dest", "-m", "cksum", "-if", @"\.txt$", "-xf", @"\.bak$"]);

            var appArg2 = CreateAppArg(out _);
            appArg2.Parse(["-a", "wait", "-f", @"C:\src", "-c", "5", "-i", "1"]);

            var appArg3 = CreateAppArg(out _);
            appArg3.Parse(["-a", "rotate", "-f", @"C:\src", "-k", "3"]);

            // Act & Assert
            var ex1 = Record.Exception(() => appArg1.PrintDefinition());
            var ex2 = Record.Exception(() => appArg2.PrintDefinition());
            var ex3 = Record.Exception(() => appArg3.PrintDefinition());

            Assert.Null(ex1);
            Assert.Null(ex2);
            Assert.Null(ex3);
        }

        // =====================================================================
        // 12. GetTimestamp メソッドテスト
        // =====================================================================
        [Theory]
        [InlineData("today")]
        [InlineData("t")]
        public void GetTimestamp_Today_ReturnsStartOfToday(string mode)
        {
            // Arrange
            var appArg = CreateAppArg(out _);

            // Act
            DateTime result = appArg.GetTimestamp(mode, "", 0);

            // Assert
            Assert.Equal(DateTime.Today, result);
        }

        [Theory]
        [InlineData("yesterday")]
        [InlineData("y")]
        public void GetTimestamp_Yesterday_ReturnsYesterday(string mode)
        {
            // Arrange
            var appArg = CreateAppArg(out _);

            // Act
            DateTime result = appArg.GetTimestamp(mode, "", 0);

            // Assert
            Assert.Equal(DateTime.Today.AddDays(-1), result);
        }

        [Fact]
        public void GetTimestamp_NextDay_ReturnsTomorrow()
        {
            // Arrange
            var appArg = CreateAppArg(out _);

            // Act
            DateTime result = appArg.GetTimestamp("nextday", "", 0);

            // Assert
            Assert.Equal(DateTime.Today.AddDays(1), result);
        }

        [Theory]
        [InlineData("fotm")]
        [InlineData("firstofthismonth")]
        public void GetTimestamp_FirstOfThisMonth_ReturnsFirstDay(string mode)
        {
            // Arrange
            var appArg = CreateAppArg(out _);
            DateTime expected = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            // Act
            DateTime result = appArg.GetTimestamp(mode, "", 0);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("eolm")]
        [InlineData("endoflastmonth")]
        public void GetTimestamp_EndOfLastMonth_ReturnsLastDayOfPrevMonth(string mode)
        {
            // Arrange
            var appArg = CreateAppArg(out _);
            DateTime expected = (new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).AddDays(-1);

            // Act
            DateTime result = appArg.GetTimestamp(mode, "", 0);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTimestamp_FileMode_ReturnsFileCreationTime()
        {
            // Arrange
            var appArg = CreateAppArg(out _);
            string testFile = Path.Combine(_testRoot, "timestamp_test.txt");
            File.WriteAllText(testFile, "test data");
            DateTime expectedTime = File.GetCreationTime(testFile);

            // Act
            DateTime fileResult = appArg.GetTimestamp("file", testFile, MdlFile.PATH_IS_FILE);
            DateTime fResult = appArg.GetTimestamp("f", testFile, MdlFile.PATH_IS_FILE);

            // Assert
            Assert.Equal(expectedTime, fileResult);
            Assert.Equal(expectedTime, fResult);
        }

        [Fact]
        public void GetTimestamp_DirectoryMode_ReturnsDirCreationTime()
        {
            // Arrange
            var appArg = CreateAppArg(out _);
            string testDir = Path.Combine(_testRoot, "timestamp_dir");
            Directory.CreateDirectory(testDir);
            DateTime expectedTime = Directory.GetCreationTime(testDir);

            // Act
            DateTime result = appArg.GetTimestamp("file", testDir, MdlFile.PATH_IS_DIRECTORY);

            // Assert
            Assert.Equal(expectedTime, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("unknown_timestamp_mode")]
        public void GetTimestamp_UnknownOrEmpty_ReturnsCurrentTime(string? mode)
        {
            // Arrange
            var appArg = CreateAppArg(out _);
            DateTime before = DateTime.Now.AddSeconds(-1);

            // Act
            DateTime result = appArg.GetTimestamp(mode!, "", 0);
            DateTime after = DateTime.Now.AddSeconds(1);

            // Assert
            Assert.InRange(result, before, after);
        }
    }
}
