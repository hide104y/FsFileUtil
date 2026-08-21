using System;
using System.IO;
using CmnClsLib.Class;
using CmnClsLib.Module;
using CmnWinLib.Class;
using FsFileUtil.Class;
using Xunit;

namespace TestProject1.Class
{
    public class UnitTest_ClsFsDiffCopy : IDisposable
    {
        private readonly string _testRoot;
        private readonly ClsLogger _logger;

        public UnitTest_ClsFsDiffCopy()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "UnitTest", "FsFileUtil", "ClsFsDiffCopy", Guid.NewGuid().ToString("N"));
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
                // クリーンアップ時の例外は無視
            }
        }

        private ClsProp CreateBaseProp()
        {
            return new ClsProp
            {
                Verbose = 0,
                IsStackTrace = false,
                IsFileCopy = true,
                IsAlwaysMkDir = true,
                Task = ClsProp.TASK_CP,
                CheckLogic = ClsProp.CHECK_MTIME,
                SecRange = 2,
                OutputPathCode = ClsProp.RELATIVE
            };
        }

        private (ClsFsDiffCopy diffCopy, ClsFsUtil fsUtil, ClsSymLinkWrapper symLink) CreateDiffCopy(ClsProp prop)
        {
            var symLink = new ClsSymLinkWrapper(_logger);
            var fsUtil = new ClsFsUtil(_logger);
            var diffCopy = new ClsFsDiffCopy(_logger, prop, fsUtil, symLink);
            return (diffCopy, fsUtil, symLink);
        }

        private string CreateTestFile(string dirPath, string fileName, string content = "test content")
        {
            Directory.CreateDirectory(dirPath);
            string filePath = Path.Combine(dirPath, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        private string CreateTestFileWithBytes(string dirPath, string fileName, byte[] bytes)
        {
            Directory.CreateDirectory(dirPath);
            string filePath = Path.Combine(dirPath, fileName);
            File.WriteAllBytes(filePath, bytes);
            return filePath;
        }

        #region 1. コンストラクタおよびプロパティ・カウンタのテスト

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, fsUtil, symLink) = CreateDiffCopy(prop);

            // Assert
            Assert.NotNull(diffCopy);
            Assert.Same(prop, diffCopy.Properties);
            Assert.Equal((ulong)0, diffCopy.CopyNewCount);
            Assert.Equal((ulong)0, diffCopy.CopyUpdateCount);
            Assert.Equal((ulong)0, diffCopy.CopySkipCount);
            Assert.Equal((ulong)0, diffCopy.CopyErrorCount);
            Assert.Equal((ulong)0, diffCopy.CopyTotalCount);
            Assert.Equal((ulong)0, diffCopy.RmOkCount);
            Assert.Equal((ulong)0, diffCopy.RmNgCount);
            Assert.Equal((ulong)0, diffCopy.RmSkipCount);
            Assert.Equal((ulong)0, diffCopy.RmTotalCount);
            Assert.Equal((ulong)0, diffCopy.MkdirOkCount);
            Assert.Equal((ulong)0, diffCopy.MkdirNgCount);
            Assert.Equal((ulong)0, diffCopy.NotFoundCount);
        }

        [Fact]
        public void Property_Counters_GetAndSetWorkProperly()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            // Act
            diffCopy.CopyNewCount = 1;
            diffCopy.CopyUpdateCount = 2;
            diffCopy.CopySkipCount = 3;
            diffCopy.CopyErrorCount = 4;
            diffCopy.CopyTotalCount = 5;
            diffCopy.RmOkCount = 6;
            diffCopy.RmNgCount = 7;
            diffCopy.RmSkipCount = 8;
            diffCopy.RmTotalCount = 9;
            diffCopy.MkdirOkCount = 10;
            diffCopy.MkdirNgCount = 11;
            diffCopy.NotFoundCount = 12;

            var newProp = new ClsProp();
            diffCopy.Properties = newProp;

            // Assert
            Assert.Equal((ulong)1, diffCopy.CopyNewCount);
            Assert.Equal((ulong)2, diffCopy.CopyUpdateCount);
            Assert.Equal((ulong)3, diffCopy.CopySkipCount);
            Assert.Equal((ulong)4, diffCopy.CopyErrorCount);
            Assert.Equal((ulong)5, diffCopy.CopyTotalCount);
            Assert.Equal((ulong)6, diffCopy.RmOkCount);
            Assert.Equal((ulong)7, diffCopy.RmNgCount);
            Assert.Equal((ulong)8, diffCopy.RmSkipCount);
            Assert.Equal((ulong)9, diffCopy.RmTotalCount);
            Assert.Equal((ulong)10, diffCopy.MkdirOkCount);
            Assert.Equal((ulong)11, diffCopy.MkdirNgCount);
            Assert.Equal((ulong)12, diffCopy.NotFoundCount);
            Assert.Same(newProp, diffCopy.Properties);
        }

        #endregion

        #region 2. 補助メソッドのテスト (CheckIsSkipBySize, GetOutputRelativePath, EchoTitle)

        [Theory]
        [InlineData(0, 0, 1000, 0)]          // 制限なし -> 0
        [InlineData(1000, 0, 500, 0)]        // SkipSize=1000, File=500 -> 0
        [InlineData(1000, 0, 1500, 10)]      // SkipSize=1000, File=1500 -> 10 (スキップ)
        [InlineData(0, 1000, 500, 0)]        // CopySize=1000, File=500 -> 0
        [InlineData(0, 1000, 1500, 1)]       // CopySize=1000, File=1500 -> 1 (強制コピー)
        [InlineData(2000, 1000, 500, 0)]     // Skip=2000, Copy=1000, File=500 -> 0
        [InlineData(2000, 1000, 1500, 1)]    // Skip=2000, Copy=1000, File=1500 -> 1 (CopySize超え)
        [InlineData(2000, 1000, 2500, 10)]   // Skip=2000, Copy=1000, File=2500 -> 10 (両方超え -> スキップ優先)
        [InlineData(1000, 2000, 500, 0)]     // Skip=1000, Copy=2000, File=500 -> 0
        [InlineData(1000, 2000, 1500, 10)]   // Skip=1000, Copy=2000, File=1500 -> 10 (SkipSize超え)
        [InlineData(1000, 2000, 2500, 1)]    // Skip=1000, Copy=2000, File=2500 -> 1 (SkipSize < CopySizeのときは両方成立で1)
        public void CheckIsSkipBySize_EvaluatesCorrectly(long skipSize, long copySize, long fileSize, int expected)
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.SkipSize = skipSize;
            prop.CopySize = copySize;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            // Act
            int result = diffCopy.CheckIsSkipBySize(fileSize);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetOutputRelativePath_ReturnsCorrectPath()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            // プレフィックスなし
            prop.OutputPathPrefix = "";
            Assert.Equal(@"sub\file.txt", diffCopy.GetOutputRelativePath(@"sub\file.txt"));

            // プレフィックスあり
            prop.OutputPathPrefix = "[PRE] ";
            Assert.Equal(@"[PRE] sub\file.txt", diffCopy.GetOutputRelativePath(@"sub\file.txt"));
        }

        [Fact]
        public void EchoTitle_ExecutesWithoutError_InVariousModes()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, fsUtil, _) = CreateDiffCopy(prop);

            // TASK_CP with Progress
            prop.Task = ClsProp.TASK_CP;
            prop.IsProgress = true;
            fsUtil.Result = "Progress status 50%";

            // Act & Assert (例外が出ないこと)
            diffCopy.EchoTitle("Processing file 1");

            // Verbose levels
            prop.Verbose = -1;
            diffCopy.EchoTitle("Processing file 2");

            prop.Verbose = -2;
            diffCopy.EchoTitle("Processing file 3");
        }

        #endregion

        #region 3. ディレクトリ作成 (Mkdir, MkParentDir) のテスト

        [Fact]
        public void MkParentDir_CreatesParentDirectoryWhenNotExists()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);
            string targetDir = Path.Combine(_testRoot, "parent_test", "sub_parent");
            string targetFile = Path.Combine(targetDir, "file.txt");

            // Act
            bool result = diffCopy.MkParentDir(targetFile, true);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(targetDir));
            Assert.Equal((ulong)1, diffCopy.MkdirOkCount);
        }

        [Fact]
        public void MkParentDir_DoesNotIncrementCount_WhenCountIsFalse()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);
            string targetDir = Path.Combine(_testRoot, "parent_no_count", "sub_dir");
            string targetFile = Path.Combine(targetDir, "file.txt");

            // Act
            bool result = diffCopy.MkParentDir(targetFile, false);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(targetDir));
            Assert.Equal((ulong)0, diffCopy.MkdirOkCount);
        }

        [Fact]
        public void Mkdir_CreatesNewDirectory_AndIncrementsOkCount()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);
            string srcDir = Path.Combine(_testRoot, "src_mkdir");
            string dstDir = Path.Combine(_testRoot, "dst_mkdir");
            Directory.CreateDirectory(srcDir);

            // Act
            bool result = diffCopy.Mkdir(srcDir, dstDir, "dst_mkdir");

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(dstDir));
            Assert.Equal((ulong)1, diffCopy.MkdirOkCount);
        }

        [Fact]
        public void Mkdir_ExistingDirectory_DoesNotIncrementOkCount()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);
            string srcDir = Path.Combine(_testRoot, "src_mkdir_exist");
            string dstDir = Path.Combine(_testRoot, "dst_mkdir_exist");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(dstDir);

            // Act
            bool result = diffCopy.Mkdir(srcDir, dstDir, "dst_mkdir_exist");

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)0, diffCopy.MkdirOkCount);
        }

        [Fact]
        public void Mkdir_IsListMode_DoesNotCreateDirectory()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsList = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);
            string srcDir = Path.Combine(_testRoot, "src_mkdir_list");
            string dstDir = Path.Combine(_testRoot, "dst_mkdir_list");
            Directory.CreateDirectory(srcDir);

            // Act
            bool result = diffCopy.Mkdir(srcDir, dstDir, "dst_mkdir_list");

            // Assert
            Assert.True(result);
            Assert.False(Directory.Exists(dstDir));
            Assert.Equal((ulong)0, diffCopy.MkdirOkCount);
        }

        #endregion

        #region 4. ファイルコピー処理 (CopyFile) のテスト

        [Fact]
        public void CopyFile_TaskCp_DefaultCopy_CopiesSuccessfully()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.Task = ClsProp.TASK_CP;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "src_cp_def");
            string dstDir = Path.Combine(_testRoot, "dst_cp_def");
            string srcFile = CreateTestFile(srcDir, "test.txt", "Hello Copy Default");
            string dstFile = Path.Combine(dstDir, "test.txt");

            // Act
            bool result = diffCopy.CopyFile(srcFile, dstFile, "test.txt", true);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(dstFile));
            Assert.Equal("Hello Copy Default", File.ReadAllText(dstFile));
            Assert.Equal((ulong)0, diffCopy.CopyErrorCount);
        }

        [Fact]
        public void CopyFile_TaskCp_BinaryCopy_CopiesSuccessfully()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.Task = ClsProp.TASK_CP;
            prop.CopyCmdType = ClsProp.COPY_BINARY;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "src_cp_bin");
            string dstDir = Path.Combine(_testRoot, "dst_cp_bin");
            byte[] bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            string srcFile = CreateTestFileWithBytes(srcDir, "bin.dat", bytes);
            string dstFile = Path.Combine(dstDir, "bin.dat");

            // Act
            bool result = diffCopy.CopyFile(srcFile, dstFile, "bin.dat", true);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(dstFile));
            Assert.Equal(bytes, File.ReadAllBytes(dstFile));
        }

        [Fact]
        public void CopyFile_TaskCp_AsyncCopy_CopiesSuccessfully()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.Task = ClsProp.TASK_CP;
            prop.CopyCmdType = ClsProp.COPY_ASYNC;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "src_cp_async");
            string dstDir = Path.Combine(_testRoot, "dst_cp_async");
            byte[] bytes = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
            string srcFile = CreateTestFileWithBytes(srcDir, "async.dat", bytes);
            string dstFile = Path.Combine(dstDir, "async.dat");

            // Act
            bool result = diffCopy.CopyFile(srcFile, dstFile, "async.dat", true);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(dstFile));
            Assert.Equal(bytes, File.ReadAllBytes(dstFile));
        }

        [Fact]
        public void CopyFile_TaskMv_WithoutProgress_MovesSuccessfully()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.Task = ClsProp.TASK_MV;
            prop.IsProgress = false;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "src_mv_noprog");
            string dstDir = Path.Combine(_testRoot, "dst_mv_noprog");
            string srcFile = CreateTestFile(srcDir, "mv_noprog.txt", "Move Content");
            string dstFile = Path.Combine(dstDir, "mv_noprog.txt");

            // Act
            bool result = diffCopy.CopyFile(srcFile, dstFile, "mv_noprog.txt", true);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(srcFile));
            Assert.True(File.Exists(dstFile));
            Assert.Equal("Move Content", File.ReadAllText(dstFile));
        }

        [Fact]
        public void CopyFile_TaskMv_WithProgress_MovesSuccessfully()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.Task = ClsProp.TASK_MV;
            prop.IsProgress = true;
            prop.CopyCmdType = ClsProp.COPY_BINARY;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "src_mv_prog");
            string dstDir = Path.Combine(_testRoot, "dst_mv_prog");
            string srcFile = CreateTestFile(srcDir, "mv_prog.txt", "Move Content Progress");
            string dstFile = Path.Combine(dstDir, "mv_prog.txt");

            // Act
            bool result = diffCopy.CopyFile(srcFile, dstFile, "mv_prog.txt", true);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(srcFile));
            Assert.True(File.Exists(dstFile));
            Assert.Equal("Move Content Progress", File.ReadAllText(dstFile));
        }

        [Fact]
        public void CopyFile_TaskRename_RenamesSuccessfully()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.Task = ClsProp.TASK_RENAME;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "src_rename");
            string dstDir = Path.Combine(_testRoot, "dst_rename");
            string srcFile = CreateTestFile(srcDir, "old_name.txt", "Rename Content");
            string dstFile = Path.Combine(dstDir, "new_name.txt");

            // Act
            bool result = diffCopy.CopyFile(srcFile, dstFile, "new_name.txt", true);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(srcFile));
            Assert.True(File.Exists(dstFile));
            Assert.Equal("Rename Content", File.ReadAllText(dstFile));
        }

        [Fact]
        public void CopyFile_WithBackup_BacksUpExistingDestination()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.Task = ClsProp.TASK_CP;
            prop.IsBackup = true;
            string backupDir = Path.Combine(_testRoot, "backup_dir");
            prop.BackupDir = backupDir;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "src_backup");
            string dstDir = Path.Combine(_testRoot, "dst_backup");
            string srcFile = CreateTestFile(srcDir, "data.txt", "New Version Content");
            string dstFile = CreateTestFile(dstDir, "data.txt", "Original Dest Content");

            // Act
            bool result = diffCopy.CopyFile(srcFile, dstFile, "data.txt", false);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(dstFile));
            Assert.Equal("New Version Content", File.ReadAllText(dstFile));

            string expectedBackupFile = Path.Combine(backupDir, "data.txt");
            Assert.True(File.Exists(expectedBackupFile));
            Assert.Equal("Original Dest Content", File.ReadAllText(expectedBackupFile));
        }

        [Fact]
        public void CopyFile_IsListMode_DoesNotPerformActualCopy()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsList = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "src_cp_list");
            string dstDir = Path.Combine(_testRoot, "dst_cp_list");
            string srcFile = CreateTestFile(srcDir, "list.txt", "List Only");
            string dstFile = Path.Combine(dstDir, "list.txt");

            // Act
            bool result = diffCopy.CopyFile(srcFile, dstFile, "list.txt", true);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(dstFile));
        }

        [Fact]
        public void CopyFile_NonExistentSource_ReturnsFalseAndIncrementsErrorCount()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcFile = Path.Combine(_testRoot, "non_existent_source.txt");
            string dstFile = Path.Combine(_testRoot, "dst_error", "dst.txt");

            // Act
            bool result = diffCopy.CopyFile(srcFile, dstFile, "dst.txt", true);

            // Assert
            Assert.False(result);
            Assert.Equal((ulong)1, diffCopy.CopyErrorCount);
        }

        #endregion

        #region 5. 差分判定およびメインコピー処理 (DiffCopyFileMain) のテスト

        [Fact]
        public void DiffCopyFileMain_NewFile_CopiesAndIncrementsCopyNewCount()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsShowNewFile = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_new");
            string dstDir = Path.Combine(_testRoot, "diff_dst_new");
            string srcFile = CreateTestFile(srcDir, "new_file.txt", "Content New");
            string dstFile = Path.Combine(dstDir, "new_file.txt");

            // Act
            bool result = diffCopy.DiffCopyFileMain(srcFile, dstFile, "new_file.txt");

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(dstFile));
            Assert.Equal((ulong)1, diffCopy.CopyNewCount);
            Assert.Equal((ulong)0, diffCopy.CopyUpdateCount);
            Assert.Equal((ulong)0, diffCopy.CopySkipCount);
        }

        [Fact]
        public void DiffCopyFileMain_DestinationIsDirectory_RemovesDirAndCopiesFile()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_dirover");
            string dstPath = Path.Combine(_testRoot, "diff_dst_dirover");
            string srcFile = CreateTestFile(srcDir, "item", "File replacing folder");
            // 宛先を同名のディレクトリとして作成
            Directory.CreateDirectory(dstPath);
            CreateTestFile(dstPath, "inner.txt", "Inner file");

            // Act
            bool result = diffCopy.DiffCopyFileMain(srcFile, dstPath, "item");

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(dstPath));
            Assert.False(Directory.Exists(dstPath));
            Assert.Equal((ulong)1, diffCopy.CopyNewCount);
        }

        [Fact]
        public void DiffCopyFileMain_CheckLogic_None_AlwaysCopies()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.CheckLogic = ClsProp.CHECK_NONE;
            prop.IsShowUpdatedFile = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_none");
            string dstDir = Path.Combine(_testRoot, "diff_dst_none");
            string srcFile = CreateTestFile(srcDir, "same.txt", "Same Content");
            string dstFile = CreateTestFile(dstDir, "same.txt", "Same Content");

            // Act
            bool result = diffCopy.DiffCopyFileMain(srcFile, dstFile, "same.txt");

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, diffCopy.CopyUpdateCount);
            Assert.Equal((ulong)0, diffCopy.CopySkipCount);
        }

        [Fact]
        public void DiffCopyFileMain_CheckLogic_Exist_SkipsIfDestinationExists()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.CheckLogic = ClsProp.CHECK_EXIST;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_exist");
            string dstDir = Path.Combine(_testRoot, "diff_dst_exist");
            string srcFile = CreateTestFile(srcDir, "file.txt", "New Source Content");
            string dstFile = CreateTestFile(dstDir, "file.txt", "Old Dest Content");

            // Act
            bool result = diffCopy.DiffCopyFileMain(srcFile, dstFile, "file.txt");

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)0, diffCopy.CopyUpdateCount);
            Assert.Equal((ulong)1, diffCopy.CopySkipCount);
            Assert.Equal("Old Dest Content", File.ReadAllText(dstFile)); // コピーされず保持
        }

        [Fact]
        public void DiffCopyFileMain_CheckLogic_MTime_CopiesWhenDateDiffers()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.CheckLogic = ClsProp.CHECK_MTIME;
            prop.SecRange = 2;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_mtime");
            string dstDir = Path.Combine(_testRoot, "diff_dst_mtime");
            string srcFile = CreateTestFile(srcDir, "date.txt", "Content");
            string dstFile = CreateTestFile(dstDir, "date.txt", "Content");

            // タイムスタンプに意図的に差をつける（10分差）
            File.SetLastWriteTime(srcFile, DateTime.Now);
            File.SetLastWriteTime(dstFile, DateTime.Now.AddMinutes(-10));

            // Act
            bool result = diffCopy.DiffCopyFileMain(srcFile, dstFile, "date.txt");

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, diffCopy.CopyUpdateCount);
        }

        [Fact]
        public void DiffCopyFileMain_CheckLogic_MTimeNew_CopiesOnlyWhenSourceIsNewer()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.CheckLogic = ClsProp.CHECK_MTIME_NEW;
            prop.SecRange = 2;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_mtnew");
            string dstDir = Path.Combine(_testRoot, "diff_dst_mtnew");
            string srcFile = CreateTestFile(srcDir, "mtnew.txt", "Content");
            string dstFile = CreateTestFile(dstDir, "mtnew.txt", "Content");

            // 宛先の方が新しい場合 -> スキップされるべき
            File.SetLastWriteTime(srcFile, DateTime.Now.AddMinutes(-10));
            File.SetLastWriteTime(dstFile, DateTime.Now);

            // Act 1
            bool res1 = diffCopy.DiffCopyFileMain(srcFile, dstFile, "mtnew.txt");

            // Assert 1
            Assert.True(res1);
            Assert.Equal((ulong)1, diffCopy.CopySkipCount);
            Assert.Equal((ulong)0, diffCopy.CopyUpdateCount);

            // ソースの方が新しい場合 -> コピーされるべき
            File.SetLastWriteTime(srcFile, DateTime.Now.AddMinutes(10));

            // Act 2
            bool res2 = diffCopy.DiffCopyFileMain(srcFile, dstFile, "mtnew.txt");

            // Assert 2
            Assert.True(res2);
            Assert.Equal((ulong)1, diffCopy.CopyUpdateCount);
        }

        [Fact]
        public void DiffCopyFileMain_CheckLogic_MTimeOld_CopiesOnlyWhenDestIsNewer()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.CheckLogic = ClsProp.CHECK_MTIME_OLD;
            prop.SecRange = 2;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_mtold");
            string dstDir = Path.Combine(_testRoot, "diff_dst_mtold");
            string srcFile = CreateTestFile(srcDir, "mtold.txt", "Content");
            string dstFile = CreateTestFile(dstDir, "mtold.txt", "Content");

            // 宛先の方が新しい場合 -> コピーされるべき
            File.SetLastWriteTime(srcFile, DateTime.Now.AddMinutes(-10));
            File.SetLastWriteTime(dstFile, DateTime.Now);

            // Act
            bool result = diffCopy.DiffCopyFileMain(srcFile, dstFile, "mtold.txt");

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, diffCopy.CopyUpdateCount);
        }

        [Fact]
        public void DiffCopyFileMain_IsSizeCheck_CopiesWhenSizeDiffers()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsSizeCheck = true;
            prop.Verbose = 2;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_size");
            string dstDir = Path.Combine(_testRoot, "diff_dst_size");
            string srcFile = CreateTestFile(srcDir, "size.txt", "Short");
            string dstFile = CreateTestFile(dstDir, "size.txt", "Much Longer Content");

            // Act
            bool result = diffCopy.DiffCopyFileMain(srcFile, dstFile, "size.txt");

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, diffCopy.CopyUpdateCount);
        }

        [Fact]
        public void DiffCopyFileMain_CheckLogic_Adler32_ComparesChecksum()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.CheckLogic = ClsProp.CHECK_ADLER32;
            prop.Verbose = 2;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_adler");
            string dstDir = Path.Combine(_testRoot, "diff_dst_adler");
            string srcFile = CreateTestFile(srcDir, "data.txt", "Content AAAAA");
            string dstFile = CreateTestFile(dstDir, "data.txt", "Content BBBBB");

            // Act - 内容が異なるためコピー
            bool res1 = diffCopy.DiffCopyFileMain(srcFile, dstFile, "data.txt");
            Assert.True(res1);
            Assert.Equal((ulong)1, diffCopy.CopyUpdateCount);

            // Act - 同一内容で再度実行 -> スキップ
            bool res2 = diffCopy.DiffCopyFileMain(srcFile, dstFile, "data.txt");
            Assert.True(res2);
            Assert.Equal((ulong)1, diffCopy.CopySkipCount);
        }

        [Fact]
        public void DiffCopyFileMain_CheckLogic_Cksum_ComparesChecksum()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.CheckLogic = ClsProp.CHECK_CKSUM;
            prop.Verbose = 2;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_cksum");
            string dstDir = Path.Combine(_testRoot, "diff_dst_cksum");
            string srcFile = CreateTestFile(srcDir, "cksum.txt", "Alpha Beta Gamma");
            string dstFile = CreateTestFile(dstDir, "cksum.txt", "Delta Epsilon Zeta");

            // Act - 差分あり
            bool res1 = diffCopy.DiffCopyFileMain(srcFile, dstFile, "cksum.txt");
            Assert.True(res1);
            Assert.Equal((ulong)1, diffCopy.CopyUpdateCount);

            // Act - 差分なし
            bool res2 = diffCopy.DiffCopyFileMain(srcFile, dstFile, "cksum.txt");
            Assert.True(res2);
            Assert.Equal((ulong)1, diffCopy.CopySkipCount);
        }

        [Fact]
        public void DiffCopyFileMain_CheckLogic_Sha1_ComparesHash()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.CheckLogic = ClsProp.CHECK_SHA1;
            prop.Verbose = 2;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_sha1");
            string dstDir = Path.Combine(_testRoot, "diff_dst_sha1");
            string srcFile = CreateTestFile(srcDir, "sha1.txt", "Hash Test 1");
            string dstFile = CreateTestFile(dstDir, "sha1.txt", "Hash Test 2");

            // Act - 差分あり
            bool res1 = diffCopy.DiffCopyFileMain(srcFile, dstFile, "sha1.txt");
            Assert.True(res1);
            Assert.Equal((ulong)1, diffCopy.CopyUpdateCount);

            // Act - 差分なし
            bool res2 = diffCopy.DiffCopyFileMain(srcFile, dstFile, "sha1.txt");
            Assert.True(res2);
            Assert.Equal((ulong)1, diffCopy.CopySkipCount);
        }

        [Fact]
        public void DiffCopyFileMain_IsProgress_NullFsUtil_ReturnsFalse()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsProgress = true;
            var symLink = new ClsSymLinkWrapper(_logger);
            var diffCopy = new ClsFsDiffCopy(_logger, prop, null!, symLink);

            string srcDir = Path.Combine(_testRoot, "diff_null_fs");
            string srcFile = CreateTestFile(srcDir, "file.txt");
            string dstFile = Path.Combine(_testRoot, "diff_null_fs_dst", "file.txt");

            // Act
            bool result = diffCopy.DiffCopyFileMain(srcFile, dstFile, "file.txt");

            // Assert
            Assert.False(result);
            Assert.Equal((ulong)1, diffCopy.CopyErrorCount);
        }

        [Fact]
        public void DiffCopyFileMain_IsListMode_DoesNotCopyFile()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsList = true;
            prop.IsShowNewFile = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_src_list");
            string dstDir = Path.Combine(_testRoot, "diff_dst_list");
            string srcFile = CreateTestFile(srcDir, "list.txt", "List Test");
            string dstFile = Path.Combine(dstDir, "list.txt");

            // Act
            bool result = diffCopy.DiffCopyFileMain(srcFile, dstFile, "list.txt");

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(dstFile));
            Assert.Equal((ulong)1, diffCopy.CopyNewCount);
        }

        #endregion

        #region 6. 再帰削除処理 (RemoveRecursive) のテスト

        [Fact]
        public void RemoveRecursive_SingleFile_DeletesSuccessfully()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);
            string targetDir = Path.Combine(_testRoot, "rm_single");
            string targetFile = CreateTestFile(targetDir, "del.txt", "Delete Me");

            // Act
            bool result = diffCopy.RemoveRecursive(targetFile, "del.txt", false);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(targetFile));
            Assert.Equal((ulong)1, diffCopy.RmOkCount);
            Assert.Equal((ulong)1, diffCopy.RmTotalCount);
            Assert.Equal((ulong)0, diffCopy.RmNgCount);
        }

        [Fact]
        public void RemoveRecursive_ReadOnlyFile_ClearsAttributesAndDeletes()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);
            string targetDir = Path.Combine(_testRoot, "rm_readonly");
            string targetFile = CreateTestFile(targetDir, "readonly.txt", "Readonly Content");
            File.SetAttributes(targetFile, FileAttributes.ReadOnly);

            // Act
            bool result = diffCopy.RemoveRecursive(targetFile, "readonly.txt", false);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(targetFile));
            Assert.Equal((ulong)1, diffCopy.RmOkCount);
        }

        [Fact]
        public void RemoveRecursive_DirectoryWithChildren_DeletesRecursively()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);
            string targetDir = Path.Combine(_testRoot, "rm_tree");
            string subDir1 = Path.Combine(targetDir, "sub1");
            string subDir2 = Path.Combine(targetDir, "sub2");
            CreateTestFile(targetDir, "file1.txt");
            CreateTestFile(subDir1, "file2.txt");
            CreateTestFile(subDir2, "file3.txt");

            // Act
            bool result = diffCopy.RemoveRecursive(new DirectoryInfo(targetDir), "rm_tree", false);

            // Assert
            Assert.True(result);
            Assert.False(Directory.Exists(targetDir));
            Assert.True(diffCopy.RmOkCount >= 3);
            Assert.Equal((ulong)0, diffCopy.RmNgCount);
        }

        [Fact]
        public void RemoveRecursive_IsListMode_DoesNotDelete()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsList = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);
            string targetDir = Path.Combine(_testRoot, "rm_list");
            string targetFile = CreateTestFile(targetDir, "keep.txt", "Do Not Delete");

            // Act
            bool result = diffCopy.RemoveRecursive(targetFile, "keep.txt", false);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(targetFile));
            Assert.Equal((ulong)0, diffCopy.RmOkCount);
            Assert.Equal((ulong)1, diffCopy.RmTotalCount);
        }

        [Fact]
        public void RemoveRecursive_SinglePathOverload_DeletesSuccessfully()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);
            string targetDir = Path.Combine(_testRoot, "rm_overload");
            string targetFile = CreateTestFile(targetDir, "single_arg.txt");

            // Act
            bool result = diffCopy.RemoveRecursive(targetFile);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(targetFile));
        }

        #endregion

        #region 7. タイムスタンプ同期 (SetDateToDir, SetDateToFile) のテスト

        [Theory]
        [InlineData(0, false)] // 同期無効
        [InlineData(1, true)]  // 作成日時同期
        [InlineData(2, true)]  // 更新日時同期
        [InlineData(3, true)]  // 作成・更新日時同期
        public void SetDateToFile_SyncsTimestampsAccordingToProp(int isCpTimestamp, bool shouldSync)
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsCpTimestamp = isCpTimestamp;
            prop.SecRange = 1;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, $"ts_src_file_{isCpTimestamp}");
            string dstDir = Path.Combine(_testRoot, $"ts_dst_file_{isCpTimestamp}");
            string srcFile = CreateTestFile(srcDir, "ts.txt");
            string dstFile = CreateTestFile(dstDir, "ts.txt");

            DateTime fixedTime = new DateTime(2025, 1, 1, 12, 0, 0);
            File.SetCreationTime(srcFile, fixedTime);
            File.SetLastWriteTime(srcFile, fixedTime);
            File.SetCreationTime(dstFile, fixedTime.AddHours(-10));
            File.SetLastWriteTime(dstFile, fixedTime.AddHours(-10));

            // Act
            diffCopy.SetDateToFile(srcFile, dstFile, "ts.txt", "UPD");

            // Assert
            if (shouldSync)
            {
                if (isCpTimestamp == 1 || isCpTimestamp == 3)
                {
                    Assert.True(Math.Abs((File.GetCreationTime(dstFile) - fixedTime).TotalSeconds) <= 2);
                }
                if (isCpTimestamp == 2 || isCpTimestamp == 3)
                {
                    Assert.True(Math.Abs((File.GetLastWriteTime(dstFile) - fixedTime).TotalSeconds) <= 2);
                }
            }
            else
            {
                Assert.True((File.GetLastWriteTime(dstFile) - fixedTime).TotalHours < -5);
            }
        }

        [Fact]
        public void SetDateToDir_SyncsTimestampsAccordingToProp()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsCpTimestamp = 3;
            prop.SecRange = 1;
            prop.IsFileCopy = false;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "ts_src_dir");
            string dstDir = Path.Combine(_testRoot, "ts_dst_dir");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(dstDir);

            DateTime fixedTime = new DateTime(2025, 5, 5, 10, 30, 0);
            Directory.SetCreationTime(srcDir, fixedTime);
            Directory.SetLastWriteTime(srcDir, fixedTime);
            Directory.SetCreationTime(dstDir, fixedTime.AddHours(-5));
            Directory.SetLastWriteTime(dstDir, fixedTime.AddHours(-5));

            // Act
            diffCopy.SetDateToDir(srcDir, dstDir, "dir", "UPD");

            // Assert
            Assert.True(Math.Abs((Directory.GetLastWriteTime(dstDir) - fixedTime).TotalSeconds) <= 2);
        }

        #endregion

        #region 8. シンボリックリンク処理 (MkLink) のテスト

        [Fact]
        public void MkLink_ListMode_IncrementsNewCount_WithoutCreatingLink()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsList = true;
            prop.IsShowNewFile = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "symlink_src_list");
            string srcFile = CreateTestFile(srcDir, "target.txt", "Symlink target");
            string dstFile = Path.Combine(_testRoot, "symlink_dst_list", "link.txt");

            // Act
            bool result = diffCopy.MkLink(srcFile, dstFile, "link.txt", MdlFile.PATH_IS_FILE);

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, diffCopy.CopyNewCount);
        }

        [Fact]
        public void MkLink_OutputPathCode_FormatsProperly()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsList = true;
            prop.IsShowNewFile = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "symlink_src_fmt");
            string srcFile = CreateTestFile(srcDir, "target.txt", "Symlink target");
            string dstFile = Path.Combine(_testRoot, "symlink_dst_fmt", "link.txt");

            // Test OutputPathCode = FROM
            prop.OutputPathCode = ClsProp.FROM;
            Assert.True(diffCopy.MkLink(srcFile, dstFile, "link.txt", MdlFile.PATH_IS_FILE));

            // Test OutputPathCode = TO
            prop.OutputPathCode = ClsProp.TO;
            Assert.True(diffCopy.MkLink(srcFile, dstFile, "link.txt", MdlFile.PATH_IS_FILE));

            // Test OutputPathCode = BOTH
            prop.OutputPathCode = ClsProp.BOTH;
            Assert.True(diffCopy.MkLink(srcFile, dstFile, "link.txt", MdlFile.PATH_IS_FILE));

            // Test Directory pathType
            Assert.True(diffCopy.MkLink(srcDir, Path.Combine(_testRoot, "symlink_dst_dir"), "dir", MdlFile.PATH_IS_DIRECTORY));
        }

        #endregion

        #region 9. 総合エントリポイント (Copy) のテスト

        [Fact]
        public void Copy_Directory_WhenAlwaysMkDirIsTrue_CreatesDirectory()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsAlwaysMkDir = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "entry_src_dir");
            string dstDir = Path.Combine(_testRoot, "entry_dst_dir");
            Directory.CreateDirectory(srcDir);

            // Act
            bool result = diffCopy.Copy(srcDir, dstDir, "entry_dst_dir", MdlFile.PATH_IS_DIRECTORY, 0);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(dstDir));
            Assert.Equal((ulong)1, diffCopy.MkdirOkCount);
        }

        [Fact]
        public void Copy_Directory_WhenAlwaysMkDirIsFalse_DoesNotCreateDirectory()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsAlwaysMkDir = false;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "entry_src_dir_skip");
            string dstDir = Path.Combine(_testRoot, "entry_dst_dir_skip");
            Directory.CreateDirectory(srcDir);

            // Act
            bool result = diffCopy.Copy(srcDir, dstDir, "entry_dst_dir_skip", MdlFile.PATH_IS_DIRECTORY, 0);

            // Assert
            Assert.True(result);
            Assert.False(Directory.Exists(dstDir));
            Assert.Equal((ulong)0, diffCopy.MkdirOkCount);
        }

        [Fact]
        public void Copy_File_WhenIsFileCopyIsTrue_CopiesFile()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsFileCopy = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "entry_src_file");
            string dstDir = Path.Combine(_testRoot, "entry_dst_file");
            string srcFile = CreateTestFile(srcDir, "copy.txt", "Entry point copy test");
            string dstFile = Path.Combine(dstDir, "copy.txt");

            // Act
            bool result = diffCopy.Copy(srcFile, dstFile, "copy.txt", MdlFile.PATH_IS_FILE, 0);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(dstFile));
            Assert.Equal((ulong)1, diffCopy.CopyTotalCount);
            Assert.Equal((ulong)1, diffCopy.CopyNewCount);
        }

        [Fact]
        public void Copy_File_WhenIsFileCopyIsFalse_DoesNotCopyFile()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsFileCopy = false;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "entry_src_file_skip");
            string dstDir = Path.Combine(_testRoot, "entry_dst_file_skip");
            string srcFile = CreateTestFile(srcDir, "skip.txt", "Do not copy");
            string dstFile = Path.Combine(dstDir, "skip.txt");

            // Act
            bool result = diffCopy.Copy(srcFile, dstFile, "skip.txt", MdlFile.PATH_IS_FILE, 0);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(dstFile));
            Assert.Equal((ulong)0, diffCopy.CopyTotalCount);
        }

        [Fact]
        public void Copy_SymlinkFlags_BranchCoverage()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsSymLink = true;
            prop.IsList = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "entry_symlink_src");
            string dstDir = Path.Combine(_testRoot, "entry_symlink_dst");
            string srcFile = CreateTestFile(srcDir, "sym_flag.txt", "Symlink Flag Test");
            string dstFile = Path.Combine(dstDir, "sym_flag.txt");

            // Act 1: isSymLink = 1 (強制)
            bool res1 = diffCopy.Copy(srcFile, dstFile, "sym_flag.txt", MdlFile.PATH_IS_FILE, 1);
            Assert.True(res1);

            // Act 2: isSymLink = -1 (自動判定)
            bool res2 = diffCopy.Copy(srcFile, dstFile, "sym_flag.txt", MdlFile.PATH_IS_FILE, -1);
            Assert.True(res2);

            // Act 3: ディレクトリかつ isSymLink = 1
            bool res3 = diffCopy.Copy(srcDir, dstDir, "dir", MdlFile.PATH_IS_DIRECTORY, 1);
            Assert.True(res3);
        }

        #endregion

        #region 10. 異常系・エラーハンドリングのテスト

        [Fact]
        public void DiffCopyFileMain_ThrowsAndCatchesException_IncrementsErrorCount()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.Task = ClsProp.TASK_CP;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            // 存在しないパス
            string srcFile = Path.Combine(_testRoot, "not_exist_source.txt");
            string dstFile = Path.Combine(_testRoot, "dst_invalid", "file.txt");

            // Act
            bool result = diffCopy.DiffCopyFileMain(srcFile, dstFile, "file.txt");

            // Assert
            Assert.False(result);
            Assert.True(diffCopy.CopyErrorCount > 0);
        }

        [Fact]
        public void RemoveRecursive_InvalidPath_ReturnsFalseAndIncrementsNgCount()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            // 存在しないパス
            string nonExistentPath = Path.Combine(_testRoot, "non_existent_folder", "file.txt");

            // Act
            bool result = diffCopy.RemoveRecursive(nonExistentPath, "file.txt", false);

            // Assert
            Assert.True(result); // 存在しないパスは PathType が 0 で何もしないため true
        }

        [Fact]
        public void DiffCopyFileMain_OutputPathCode_AllFormats()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.IsList = true;
            prop.IsShowNewFile = true;
            var (diffCopy, _, _) = CreateDiffCopy(prop);

            string srcDir = Path.Combine(_testRoot, "diff_fmt_src");
            string dstDir = Path.Combine(_testRoot, "diff_fmt_dst");
            string srcFile = CreateTestFile(srcDir, "fmt.txt", "Format test");
            string dstFile = Path.Combine(dstDir, "fmt.txt");

            // FROM
            prop.OutputPathCode = ClsProp.FROM;
            Assert.True(diffCopy.DiffCopyFileMain(srcFile, dstFile, "fmt.txt"));

            // TO
            prop.OutputPathCode = ClsProp.TO;
            Assert.True(diffCopy.DiffCopyFileMain(srcFile, dstFile, "fmt.txt"));

            // BOTH
            prop.OutputPathCode = ClsProp.BOTH;
            Assert.True(diffCopy.DiffCopyFileMain(srcFile, dstFile, "fmt.txt"));

            // RELATIVE
            prop.OutputPathCode = ClsProp.RELATIVE;
            Assert.True(diffCopy.DiffCopyFileMain(srcFile, dstFile, "fmt.txt"));
        }

        #endregion
    }
}
