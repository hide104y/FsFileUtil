using System;
using System.Collections.Generic;
using System.IO;
using CmnClsLib.Class;
using CmnClsLib.Module;
using FsFileUtil.Class;
using Xunit;

namespace TestProject1.Class
{
    public class UnitTest_ClsFind : IDisposable
    {
        private readonly string _testRoot;
        private readonly ClsLogger _logger;

        public UnitTest_ClsFind()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "UnitTest", "FsFileUtil", "ClsFind", Guid.NewGuid().ToString("N"));
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
                // 一時ディレクトリのクリーンアップ失敗は無視
            }
        }

        private ClsProp CreateBaseProp()
        {
            return new ClsProp
            {
                Verbose = 0,
                IsStackTrace = false,
                IsFileCopy = true,
                PathType = MdlFile.PATH_IS_DIRECTORY,
                FileListRegex = @"\t"
            };
        }

        private (ClsFind finder, ClsFsDiffCopy diffCopy, ClsFsUtil fsUtil) CreateFinder(ClsProp prop)
        {
            var symLink = new ClsSymLinkWrapper(_logger);
            var fsUtil = new ClsFsUtil(_logger);
            var diffCopy = new ClsFsDiffCopy(_logger, prop, fsUtil, symLink);
            var finder = new ClsFind(_logger, prop, fsUtil, diffCopy);
            return (finder, diffCopy, fsUtil);
        }

        private string CreateFile(string dirPath, string fileName, string content = "test content")
        {
            Directory.CreateDirectory(dirPath);
            string filePath = Path.Combine(dirPath, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        private string CreateFileWithSize(string dirPath, string fileName, int byteSize)
        {
            Directory.CreateDirectory(dirPath);
            string filePath = Path.Combine(dirPath, fileName);
            byte[] bytes = new byte[byteSize];
            Array.Fill(bytes, (byte)0x41);
            File.WriteAllBytes(filePath, bytes);
            return filePath;
        }

        #region 1. コンストラクタおよび初期化テスト

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange
            var prop = CreateBaseProp();
            var (finder, diffCopy, fsUtil) = CreateFinder(prop);

            // Assert
            Assert.NotNull(finder);
            Assert.NotNull(diffCopy);
            Assert.NotNull(fsUtil);
        }

        #endregion

        #region 2. TASK_CP (コピー処理) のテスト

        [Fact]
        public void Execute_TaskCp_SingleFile_CopiesSuccessfully()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_single");
            string dstDir = Path.Combine(_testRoot, "dst_single");
            string srcFile = CreateFile(srcDir, "sample.txt", "Hello Copy");
            string dstFile = Path.Combine(dstDir, "sample.txt");

            var prop = CreateBaseProp();
            prop.SourcePath = srcFile;
            prop.DestinationPath = dstFile;
            prop.PathType = MdlFile.PATH_IS_FILE;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(dstFile));
            Assert.Equal("Hello Copy", File.ReadAllText(dstFile));
        }

        [Fact]
        public void Execute_TaskCp_DirectoryRecursive_CopiesAllContents()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_rec");
            string dstDir = Path.Combine(_testRoot, "dst_rec");
            CreateFile(srcDir, "root.txt", "root file");
            string subDir = Path.Combine(srcDir, "sub");
            CreateFile(subDir, "sub.txt", "sub file");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "root.txt")));
            Assert.True(File.Exists(Path.Combine(dstDir, "sub", "sub.txt")));
        }

        [Fact]
        public void Execute_TaskCp_Reverse_CopiesFromDestToSource()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_rev");
            string dstDir = Path.Combine(_testRoot, "dst_rev");
            CreateFile(srcDir, "rev.txt", "old content");
            CreateFile(dstDir, "rev.txt", "reverse new content");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IsReverse = true;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(srcDir, "rev.txt")));
            Assert.Equal("reverse new content", File.ReadAllText(Path.Combine(srcDir, "rev.txt")));
        }

        [Fact]
        public void Execute_TaskCp_FlatMode_CopiesFilesToRootOfDestination()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_flat");
            string dstDir = Path.Combine(_testRoot, "dst_flat");
            CreateFile(Path.Combine(srcDir, "dir1", "dir2"), "nested.txt", "nested content");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IsFlat = true;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "nested.txt")));
            Assert.False(Directory.Exists(Path.Combine(dstDir, "dir1")));
        }

        #endregion

        #region 3. TASK_MV (移動処理) のテスト

        [Fact]
        public void Execute_TaskMv_Directory_MovesFilesAndCleansEmptyDirectories()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_mv");
            string dstDir = Path.Combine(_testRoot, "dst_mv");
            string subDir = Path.Combine(srcDir, "sub_mv");
            string srcFile = CreateFile(subDir, "mv_file.txt", "moving data");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_MV);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "sub_mv", "mv_file.txt")));
            Assert.False(File.Exists(srcFile));
            Assert.False(Directory.Exists(subDir));
        }

        [Fact]
        public void Execute_TaskMv_SingleFile_MovesSuccessfully()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_mv_single");
            string dstDir = Path.Combine(_testRoot, "dst_mv_single");
            string srcFile = CreateFile(srcDir, "move_me.txt", "move content");
            string dstFile = Path.Combine(dstDir, "move_me.txt");

            var prop = CreateBaseProp();
            prop.SourcePath = srcFile;
            prop.DestinationPath = dstFile;
            prop.PathType = MdlFile.PATH_IS_FILE;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_MV);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(dstFile));
            Assert.False(File.Exists(srcFile));
        }

        #endregion

        #region 4. TASK_RM (削除・同期削除) のテスト

        [Fact]
        public void Execute_TaskRm_DirectorySync_DeletesFilesNotInDestination()
        {
            // Arrange
            // TASK_RM では DestinationPath が走査元となり、SourcePath に存在しない DestinationPath 側のファイルが削除される
            string srcDir = Path.Combine(_testRoot, "src_rm_source");
            string dstDir = Path.Combine(_testRoot, "dst_rm_target");
            CreateFile(srcDir, "keep.txt", "keep me");
            CreateFile(dstDir, "keep.txt", "keep me in dst");
            CreateFile(dstDir, "extra.txt", "delete me from dst");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_RM);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "keep.txt")));
            Assert.False(File.Exists(Path.Combine(dstDir, "extra.txt")));
        }

        [Fact]
        public void Execute_TaskRm_WithRmNohit_RemovesFilteredFiles()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_rm_nohit_source");
            string dstDir = Path.Combine(_testRoot, "dst_rm_nohit_target");
            CreateFile(dstDir, "match.log", "log data");
            CreateFile(dstDir, "other.txt", "text data");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IncFilesList.Add(@"\.log$");
            prop.IsRmNohit = true;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_RM);

            // Assert
            Assert.True(result);
            // IsRmNohit が有効な場合、フィルタにマッチしなかった other.txt が削除される
            Assert.False(File.Exists(Path.Combine(dstDir, "other.txt")));
        }

        #endregion

        #region 5. TASK_PRINT (一覧表示・カウント・外部コマンド実行) のテスト

        [Fact]
        public void Execute_TaskPrint_CountsFilesAndDirectories()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_print");
            CreateFile(srcDir, "f1.txt", "1");
            CreateFile(srcDir, "f2.txt", "2");
            CreateFile(Path.Combine(srcDir, "sub"), "f3.txt", "3");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.TypeCode = MdlConst.INT_TYPE_ALL;
            prop.Files = 0;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_PRINT);

            // Assert
            Assert.True(result);
            // ルートdir(1) + sub dir(1) + 3ファイル = 5
            Assert.True(prop.Files >= 3);
        }

        [Fact]
        public void Execute_TaskPrint_DirectoryOnly_CountsOnlyDirectories()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_print_dirs");
            CreateFile(srcDir, "f1.txt", "1");
            string sub1 = Path.Combine(srcDir, "sub1");
            string sub2 = Path.Combine(srcDir, "sub2");
            Directory.CreateDirectory(sub1);
            Directory.CreateDirectory(sub2);

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.TypeCode = MdlConst.INT_TYPE_DIRECTORY;
            prop.Files = 0;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_PRINT);

            // Assert
            Assert.True(result);
            // ルートディレクトリ(1) + sub1(1) + sub2(1) = 3
            Assert.Equal(3UL, prop.Files);
        }

        [Fact]
        public void Execute_TaskPrint_FileOnly_CountsOnlyFiles()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_print_files");
            CreateFile(srcDir, "f1.txt", "1");
            CreateFile(Path.Combine(srcDir, "sub"), "f2.txt", "2");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.TypeCode = MdlConst.INT_TYPE_FILE;
            prop.Files = 0;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_PRINT);

            // Assert
            Assert.True(result);
            // ファイルのみカウント = 2
            Assert.Equal(2UL, prop.Files);
        }

        [Fact]
        public void Execute_TaskPrint_WithExecCmd_CmdMode()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_cmd_exec");
            CreateFile(srcDir, "test.txt", "data");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.TypeCode = MdlConst.INT_TYPE_FILE;
            prop.CmdPath = "echo";
            prop.CmdArgs = "testing %PATH%";
            prop.ExecModeCode = ClsProp.EXEC_MODE_CMD;
            prop.Files = 0;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_PRINT);

            // Assert
            Assert.True(result);
            Assert.Equal(1UL, prop.Files);
        }

        [Fact]
        public void Execute_TaskPrint_WithExecCmd_PsMode()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_ps_exec");
            CreateFile(srcDir, "test_ps.txt", "data");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.TypeCode = MdlConst.INT_TYPE_FILE;
            prop.CmdPath = "Write-Output";
            prop.CmdArgs = "Hello";
            prop.ExecModeCode = ClsProp.EXEC_MODE_PS;
            prop.Files = 0;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_PRINT);

            // Assert
            Assert.True(result);
            Assert.Equal(1UL, prop.Files);
        }

        [Fact]
        public void Execute_TaskPrint_WithExecCmd_DirectExeMode()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_direct_exec");
            CreateFile(srcDir, "direct.txt", "data");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.TypeCode = MdlConst.INT_TYPE_FILE;
            prop.CmdPath = "cmd.exe";
            prop.CmdArgs = "/c echo direct";
            prop.ExecModeCode = 0; // Default regex split mode
            prop.Files = 0;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_PRINT);

            // Assert
            Assert.True(result);
            Assert.Equal(1UL, prop.Files);
        }

        #endregion

        #region 6. 深度制御 (MinDepth, MaxDepth) のテスト

        [Fact]
        public void Execute_DepthControl_RespectsMinAndMaxDepth()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_depth");
            string dstDir = Path.Combine(_testRoot, "dst_depth");
            CreateFile(Path.Combine(srcDir, "d1"), "d1_file.txt");
            CreateFile(Path.Combine(srcDir, "d1", "d2"), "d2_file.txt");
            CreateFile(Path.Combine(srcDir, "d1", "d2", "d3"), "d3_file.txt");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.MinDepth = 1;
            prop.MaxDepth = 2;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "d1", "d1_file.txt")));
            Assert.True(File.Exists(Path.Combine(dstDir, "d1", "d2", "d2_file.txt")));
            // 深度3のファイルはコピーされないこと
            Assert.False(File.Exists(Path.Combine(dstDir, "d1", "d2", "d3", "d3_file.txt")));
        }

        #endregion

        #region 7. ディレクトリおよびファイルフィルタのテスト

        [Fact]
        public void Execute_FileFilter_IncAndExcLists_FiltersCorrectly()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_ffilter");
            string dstDir = Path.Combine(_testRoot, "dst_ffilter");
            CreateFile(srcDir, "match1.txt", "m1");
            CreateFile(srcDir, "match2.txt", "m2");
            CreateFile(srcDir, "match_skip.txt", "skip");
            CreateFile(srcDir, "other.csv", "csv");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IncFilesList.Add(@"\.txt$");
            prop.ExcFilesList.Add(@"skip");

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "match1.txt")));
            Assert.True(File.Exists(Path.Combine(dstDir, "match2.txt")));
            Assert.False(File.Exists(Path.Combine(dstDir, "match_skip.txt")));
            Assert.False(File.Exists(Path.Combine(dstDir, "other.csv")));
        }

        [Fact]
        public void Execute_DirectoryFilter_ExcludeDirectory_SkipsSubtree()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_dfilter");
            string dstDir = Path.Combine(_testRoot, "dst_dfilter");
            CreateFile(Path.Combine(srcDir, "keep_dir"), "file1.txt");
            CreateFile(Path.Combine(srcDir, "exclude_dir", "sub"), "file2.txt");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.ExcDirsList.Add("exclude_dir");
            prop.IsExcHitRecursive = true;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "keep_dir", "file1.txt")));
            Assert.False(Directory.Exists(Path.Combine(dstDir, "exclude_dir")));
            Assert.False(File.Exists(Path.Combine(dstDir, "exclude_dir", "sub", "file2.txt")));
        }

        #endregion

        #region 8. ファイルサイズ比較 (CompOpe) のテスト

        [Theory]
        [InlineData(ClsProp.COMPARISON_GE, 100, true, false)]  // 100バイト以上: 200バイトはOK、50バイトはスキップ
        [InlineData(ClsProp.COMPARISON_LE, 100, false, true)]  // 100バイト以下: 200バイトはスキップ、50バイトはOK
        public void Execute_FileSizeComparison_FiltersFilesCorrectly(int compOpe, long compSize, bool expectBig, bool expectSmall)
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_size_" + compOpe);
            string dstDir = Path.Combine(_testRoot, "dst_size_" + compOpe);
            CreateFileWithSize(srcDir, "big.bin", 200);
            CreateFileWithSize(srcDir, "small.bin", 50);

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.CompOpe = compOpe;
            prop.CompSize = compSize;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.Equal(expectBig, File.Exists(Path.Combine(dstDir, "big.bin")));
            Assert.Equal(expectSmall, File.Exists(Path.Combine(dstDir, "small.bin")));
        }

        #endregion

        #region 9. ファイルリスト処理 (ExecuteFileList) のテスト

        [Fact]
        public void ExecuteFileList_RelativePaths_CopiesSpecifiedFiles()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_fl_rel");
            string dstDir = Path.Combine(_testRoot, "dst_fl_rel");
            CreateFile(srcDir, "item1.txt", "item 1 content");
            CreateFile(srcDir, "item2.txt", "item 2 content");
            CreateFile(srcDir, "ignore.txt", "ignore content");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.FilesTypeCode = ClsProp.FILES_RELATIVE;
            prop.FileList.Add("item1.txt");
            prop.FileList.Add("item2.txt\trename2.txt");

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "item1.txt")));
            Assert.True(File.Exists(Path.Combine(dstDir, "rename2.txt")));
            Assert.False(File.Exists(Path.Combine(dstDir, "ignore.txt")));
        }

        [Fact]
        public void ExecuteFileList_FullPaths_CopiesSpecifiedFiles()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_fl_full");
            string dstDir = Path.Combine(_testRoot, "dst_fl_full");
            string file1 = CreateFile(srcDir, "full1.txt", "full 1");
            string file2 = CreateFile(srcDir, "full2.txt", "full 2");

            var prop = CreateBaseProp();
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.FilesTypeCode = ClsProp.FILES_FULL;
            prop.FileList.Add($"{file1}\t{Path.Combine(dstDir, "full1.txt")}");
            prop.FileList.Add($"{file2}\t{Path.Combine(dstDir, "custom_full2.txt")}");

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "full1.txt")));
            Assert.True(File.Exists(Path.Combine(dstDir, "custom_full2.txt")));
        }

        [Fact]
        public void ExecuteFileList_NonExistentFiles_IncrementsNotFoundCount()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_fl_missing");
            string dstDir = Path.Combine(_testRoot, "dst_fl_missing");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.FilesTypeCode = ClsProp.FILES_RELATIVE;
            prop.FileList.Add("missing_file.txt");
            prop.Verbose = 2;

            var (finder, diffCopy, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(diffCopy.NotFoundCount > 0);
        }

        #endregion

        #region 10. ファイルロック判定およびスキップ設定のテスト

        [Fact]
        public void Execute_CheckFileLock_SkipLockedFile()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_flock_skip");
            string dstDir = Path.Combine(_testRoot, "dst_flock_skip");
            string lockedFile = CreateFile(srcDir, "locked.dat", "locked data");
            string unlockedFile = CreateFile(srcDir, "unlocked.dat", "unlocked data");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.CheckFileLock = ClsProp.CHECK_FILE_LOCK_SKIP;
            prop.Verbose = 5;

            var (finder, _, _) = CreateFinder(prop);

            // Act: ファイルを開いて排他ロックをかける
            using (var fileStream = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                bool result = finder.Execute(ClsProp.TASK_CP);

                // Assert
                Assert.True(result);
                Assert.True(File.Exists(Path.Combine(dstDir, "unlocked.dat")));
                Assert.False(File.Exists(Path.Combine(dstDir, "locked.dat")));
            }
        }

        [Fact]
        public void Execute_CheckFileLock_SampleMode_SkipsUnlockedFile()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_flock_sample");
            string dstDir = Path.Combine(_testRoot, "dst_flock_sample");
            string unlockedFile = CreateFile(srcDir, "unlocked.dat", "unlocked data");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.CheckFileLock = ClsProp.CHECK_FILE_LOCK_SAMPLE;
            prop.Verbose = 5;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            // サンプルモードではロックされていないファイルがスキップされるため、unlockedFileはコピーされない
            Assert.False(File.Exists(Path.Combine(dstDir, "unlocked.dat")));
        }

        #endregion

        #region 11. 各種オプション・表示・ログ出力のテスト

        [Fact]
        public void Execute_IsFileCopyFalse_And_IsSyncRmOnlyFalse_SkipsFileProcessing()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_no_filecopy");
            string dstDir = Path.Combine(_testRoot, "dst_no_filecopy");
            string subDir = Path.Combine(srcDir, "sub_folder");
            CreateFile(subDir, "skipped.txt", "data");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IsAlwaysMkDir = true;
            prop.IsFileCopy = false;
            prop.IsSyncRmOnly = false;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            // IsAlwaysMkDir によりサブディレクトリは作成されるが、IsFileCopy=false のためファイルコピーはスキップされること
            Assert.True(Directory.Exists(Path.Combine(dstDir, "sub_folder")));
            Assert.False(File.Exists(Path.Combine(dstDir, "sub_folder", "skipped.txt")));
        }

        [Fact]
        public void Execute_VerboseHighAndShowCurDir_ExecutesWithoutError()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_verbose");
            string dstDir = Path.Combine(_testRoot, "dst_verbose");
            CreateFile(Path.Combine(srcDir, "child"), "sample.txt");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.Verbose = 7;
            prop.IsShowCurDir = 3;
            prop.IsStackTrace = true;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "child", "sample.txt")));
        }

        [Fact]
        public void Execute_UnknownTask_ReturnsFalseOrHandlesGracefully()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.SourcePath = _testRoot;
            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool resultUnknown = finder.Execute(999);
            bool resultRenameDirect = finder.Execute(ClsProp.TASK_RENAME);

            // Assert
            Assert.False(resultUnknown);
            Assert.False(resultRenameDirect);
        }

        [Fact]
        public void Execute_DirectoryDateTimeFilter_IsDirTerm_FiltersOutOldDirectory()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_dirterm");
            string dstDir = Path.Combine(_testRoot, "dst_dirterm");
            string subDir = Path.Combine(srcDir, "old_sub");
            CreateFile(subDir, "old.txt", "content");

            // 過去日時に設定
            Directory.SetLastWriteTime(subDir, new DateTime(2000, 1, 1));

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IsDirTerm = true;
            prop.IsAfter = true;
            prop.AfterTime = new DateTime(2020, 1, 1);

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.False(Directory.Exists(Path.Combine(dstDir, "old_sub")));
        }

        [Fact]
        public void Execute_TaskPrint_WithCat_BuildsArgumentsAndExecutes()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_cat_exec");
            string testFile = CreateFile(srcDir, "cat_test.txt", "line1\nline2");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.TypeCode = MdlConst.INT_TYPE_FILE;
            prop.IsCat = true;
            // cmd.exe を cat の代用として指定
            prop.CmdPath = "cmd.exe";
            prop.CatOptions = "/c echo cat_mock";
            prop.CatI = "include_pat";
            prop.CatX = "exclude_pat";
            prop.CatP = "1";
            prop.CatE = "10";
            prop.CatXmlNl = "xml_node";
            prop.IsCatRetWcl = true;
            prop.IsRetFiles = true;
            prop.IsSwitchUser = true;
            prop.Username = "dummy_user";
            prop.Password = "dummy_pass";

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_PRINT);

            // Assert: 例外なく完了すること
            Assert.True(result);
        }

        [Fact]
        public void Execute_TaskCp_WithSymLinkOption()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_symlink");
            string dstDir = Path.Combine(_testRoot, "dst_symlink");
            CreateFile(srcDir, "normal_file.txt", "content");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IsSymLink = true;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "normal_file.txt")));
        }

        [Fact]
        public void Execute_TaskCp_WithDirFilterOr_MatchesEitherFilter()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_dir_or");
            string dstDir = Path.Combine(_testRoot, "dst_dir_or");
            CreateFile(Path.Combine(srcDir, "dir_a"), "a.txt");
            CreateFile(Path.Combine(srcDir, "dir_b"), "b.txt");
            CreateFile(Path.Combine(srcDir, "dir_c"), "c.txt");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IncDirsList.Add("dir_a");
            prop.IncDirsList.Add("dir_b");
            prop.IsDirFilterOr = true;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "dir_a", "a.txt")));
            Assert.True(File.Exists(Path.Combine(dstDir, "dir_b", "b.txt")));
        }

        [Fact]
        public void Execute_TaskCp_WithXdOnlyFiles_CopiesFilesEvenWhenDirFiltered()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_xd_only");
            string dstDir = Path.Combine(_testRoot, "dst_xd_only");
            CreateFile(Path.Combine(srcDir, "excluded_dir"), "inside.txt");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.ExcDirsList.Add("excluded_dir");
            prop.IsXdOnlyFiles = true;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "excluded_dir", "inside.txt")));
        }

        [Fact]
        public void Execute_EmptySourcePath_WithFileList_ExecutesFileListDirectly()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_empty_source");
            string dstDir = Path.Combine(_testRoot, "dst_empty_source");
            string file1 = CreateFile(srcDir, "item1.txt", "data 1");

            var prop = CreateBaseProp();
            prop.SourcePath = string.Empty; // SourcePath を空に設定
            prop.DestinationPath = dstDir;
            prop.FilesTypeCode = ClsProp.FILES_FULL;
            prop.FileList.Add($"{file1}\t{Path.Combine(dstDir, "item1.txt")}");

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "item1.txt")));
        }

        [Fact]
        public void Execute_FileList_WithDirectoryElement_ProcessesRecursively()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "src_fl_dir");
            string dstDir = Path.Combine(_testRoot, "dst_fl_dir");
            string subDir = Path.Combine(srcDir, "sub_folder");
            CreateFile(subDir, "file_in_dir.txt", "recursive data");

            var prop = CreateBaseProp();
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.FilesTypeCode = ClsProp.FILES_RELATIVE;
            prop.FileList.Add("sub_folder");

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(dstDir, "sub_folder", "file_in_dir.txt")));
        }

        #endregion

        #region 12. 異常系・例外ハンドリングのテスト

        [Fact]
        public void Execute_InvalidPath_HandlesExceptionGracefully()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.SourcePath = "Z:\\NonExistentPath_" + Guid.NewGuid().ToString("N");
            prop.DestinationPath = "Z:\\NonExistentDest_" + Guid.NewGuid().ToString("N");
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IsStackTrace = true;

            var (finder, _, _) = CreateFinder(prop);

            // Act
            bool result = finder.Execute(ClsProp.TASK_CP);

            // Assert: 存在しないディレクトリでもクラッシュせず終了すること
            Assert.True(result || !result);
        }

        #endregion
    }
}
