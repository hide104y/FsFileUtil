using System;
using System.IO;
using CmnClsLib.Class;
using CmnClsLib.Module;
using FsFileUtil.Class;
using Xunit;

namespace TestProject1.Class
{
    /// <summary>
    /// <see cref="ClsFsAttrib"/> クラスの単体テストを提供します。
    /// </summary>
    public class UnitTest_ClsFsAttrib : IDisposable
    {
        private readonly string _testRoot;
        private readonly string _logFile;
        private readonly ClsLogger _logger;

        public UnitTest_ClsFsAttrib()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "UnitTest", "FsFileUtil", "ClsFsAttrib", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);

            _logFile = Path.Combine(_testRoot, "test.log");
            _logger = new ClsLogger();
            _logger.SetValueByKey(ClsLogger.IS_FILE, "true");
            _logger.SetValueByKey(ClsLogger.PATH, _logFile);
            _logger.SetValueByKey(ClsLogger.IS_CONSOLE, "false");
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

        /// <summary>
        /// 指定されたサイズのテストファイルを生成します。
        /// </summary>
        private string CreateFileWithSize(string dirPath, string fileName, int byteSize)
        {
            Directory.CreateDirectory(dirPath);
            string filePath = Path.Combine(dirPath, fileName);
            byte[] bytes = new byte[byteSize];
            Array.Fill(bytes, (byte)0x41); // 'A'
            File.WriteAllBytes(filePath, bytes);
            return filePath;
        }

        /// <summary>
        /// ログファイルの内容を取得します。
        /// </summary>
        private string GetLogContent()
        {
            if (File.Exists(_logFile))
            {
                return File.ReadAllText(_logFile);
            }
            return string.Empty;
        }

        #region 1. コンストラクタおよびプロパティ・カウンタリセットテスト

        [Fact]
        public void Constructor_InitializesWithDefaultValues()
        {
            // Arrange & Act
            var attrib = new ClsFsAttrib(_logger);

            // Assert
            Assert.Equal((ulong)0, attrib.DirectoryCount);
            Assert.Equal((ulong)0, attrib.FileCount);
            Assert.Equal((ulong)0, attrib.TotalSize);
            Assert.Equal((ulong)0, attrib.ErrorDirectoryCount);
            Assert.Equal((ulong)0, attrib.ErrorFileCount);
            Assert.False(attrib.IsProgressEnabled);
            Assert.Equal(0, attrib.ProgressIntervalDirectories);
            Assert.Equal(0, attrib.ProgressIntervalFiles);
        }

        [Fact]
        public void Properties_CanGetAndSetValues()
        {
            // Arrange
            var attrib = new ClsFsAttrib(_logger);

            // Act
            attrib.DirectoryCount = 10;
            attrib.FileCount = 20;
            attrib.TotalSize = 3000;
            attrib.ErrorDirectoryCount = 1;
            attrib.ErrorFileCount = 2;
            attrib.IsProgressEnabled = true;
            attrib.ProgressIntervalDirectories = 100;
            attrib.ProgressIntervalFiles = 200;

            // Assert
            Assert.Equal((ulong)10, attrib.DirectoryCount);
            Assert.Equal((ulong)20, attrib.FileCount);
            Assert.Equal((ulong)3000, attrib.TotalSize);
            Assert.Equal((ulong)1, attrib.ErrorDirectoryCount);
            Assert.Equal((ulong)2, attrib.ErrorFileCount);
            Assert.True(attrib.IsProgressEnabled);
            Assert.Equal(100, attrib.ProgressIntervalDirectories);
            Assert.Equal(200, attrib.ProgressIntervalFiles);
        }

        [Fact]
        public void ClearCounter_ResetsCountersOnly()
        {
            // Arrange
            var attrib = new ClsFsAttrib(_logger)
            {
                DirectoryCount = 15,
                FileCount = 25,
                TotalSize = 9999,
                ErrorDirectoryCount = 3,
                ErrorFileCount = 4,
                IsProgressEnabled = true,
                ProgressIntervalDirectories = 50,
                ProgressIntervalFiles = 100
            };

            // Act
            attrib.ClearCounter();

            // Assert
            Assert.Equal((ulong)0, attrib.DirectoryCount);
            Assert.Equal((ulong)0, attrib.FileCount);
            Assert.Equal((ulong)0, attrib.TotalSize);
            Assert.Equal((ulong)0, attrib.ErrorDirectoryCount);
            Assert.Equal((ulong)0, attrib.ErrorFileCount);
            // 設定値プロパティは維持されること
            Assert.True(attrib.IsProgressEnabled);
            Assert.Equal(50, attrib.ProgressIntervalDirectories);
            Assert.Equal(100, attrib.ProgressIntervalFiles);
        }

        #endregion

        #region 2. CalculateDirectorySize テスト

        [Fact]
        public void CalculateDirectorySize_EmptyDirectory_ReturnsTrueAndCountsOneDirectory()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "empty_dir");
            Directory.CreateDirectory(dir);
            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.CalculateDirectorySize(dir, checkSymlink: false, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, attrib.DirectoryCount);
            Assert.Equal((ulong)0, attrib.FileCount);
            Assert.Equal((ulong)0, attrib.TotalSize);
            Assert.Equal((ulong)0, attrib.ErrorDirectoryCount);
            Assert.Equal((ulong)0, attrib.ErrorFileCount);
        }

        [Fact]
        public void CalculateDirectorySize_SingleLevelDirectory_CalculatesFilesAndSize()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "single_level");
            CreateFileWithSize(dir, "file1.txt", 100);
            CreateFileWithSize(dir, "file2.txt", 250);
            CreateFileWithSize(dir, "file3.txt", 50);

            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.CalculateDirectorySize(dir, checkSymlink: false, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, attrib.DirectoryCount);
            Assert.Equal((ulong)3, attrib.FileCount);
            Assert.Equal((ulong)400, attrib.TotalSize);
            Assert.Equal((ulong)0, attrib.ErrorDirectoryCount);
            Assert.Equal((ulong)0, attrib.ErrorFileCount);
        }

        [Fact]
        public void CalculateDirectorySize_NestedDirectories_CalculatesRecursively()
        {
            // Arrange
            string rootDir = Path.Combine(_testRoot, "nested_root");
            string subDir1 = Path.Combine(rootDir, "sub1");
            string subDir2 = Path.Combine(subDir1, "sub2");

            CreateFileWithSize(rootDir, "root_file.txt", 100);
            CreateFileWithSize(subDir1, "sub1_file.txt", 200);
            CreateFileWithSize(subDir2, "sub2_file.txt", 300);

            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.CalculateDirectorySize(rootDir, checkSymlink: false, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)3, attrib.DirectoryCount); // root, sub1, sub2
            Assert.Equal((ulong)3, attrib.FileCount);
            Assert.Equal((ulong)600, attrib.TotalSize);
            Assert.Equal((ulong)0, attrib.ErrorDirectoryCount);
            Assert.Equal((ulong)0, attrib.ErrorFileCount);
        }

        [Fact]
        public void CalculateDirectorySize_WithDirectoryInfo_CalculatesCorrectly()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "dir_info_test");
            CreateFileWithSize(dir, "data.bin", 512);

            var attrib = new ClsFsAttrib(_logger);
            var dirInfo = new DirectoryInfo(dir);

            // Act
            bool result = attrib.CalculateDirectorySize(dirInfo, checkSymlink: false, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, attrib.DirectoryCount);
            Assert.Equal((ulong)1, attrib.FileCount);
            Assert.Equal((ulong)512, attrib.TotalSize);
        }

        [Fact]
        public void CalculateDirectorySize_NonExistentDirectory_ReturnsFalseAndIncrementsErrorCount()
        {
            // Arrange
            string nonExistentDir = Path.Combine(_testRoot, "does_not_exist_" + Guid.NewGuid().ToString("N"));
            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.CalculateDirectorySize(nonExistentDir, checkSymlink: false, verboseLevel: 1, isStackTrace: true);

            // Assert
            Assert.False(result);
            Assert.Equal((ulong)1, attrib.DirectoryCount);
            Assert.Equal((ulong)1, attrib.ErrorDirectoryCount);

            string logs = GetLogContent();
            Assert.Contains("SKIP DIR", logs);
        }

        [Fact]
        public void CalculateDirectorySize_WithProgressEnabled_FilesInterval_OutputsLogs()
        {
            // Arrange
            // ProgressIntervalFiles の判定はディレクトリ走査開始時に行われるため、
            // sub1 でファイルを加算後、sub2 の走査開始時に FileCount % Interval == 0 となる構造を作成
            string rootDir = Path.Combine(_testRoot, "progress_files_root");
            string subDir1 = Path.Combine(rootDir, "sub1");
            string subDir2 = Path.Combine(rootDir, "sub2");

            CreateFileWithSize(subDir1, "f1.txt", 10);
            CreateFileWithSize(subDir1, "f2.txt", 20);
            CreateFileWithSize(subDir2, "f3.txt", 30);

            var attrib = new ClsFsAttrib(_logger)
            {
                IsProgressEnabled = true,
                ProgressIntervalFiles = 2
            };

            // Act
            bool result = attrib.CalculateDirectorySize(rootDir, checkSymlink: false, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.True(result);
            string logs = GetLogContent();
            Assert.Contains("CURRENT STATUS", logs);
            Assert.Contains("FILES=2", logs);
        }

        [Fact]
        public void CalculateDirectorySize_WithProgressEnabled_DirectoriesInterval_OutputsLogs()
        {
            // Arrange
            string rootDir = Path.Combine(_testRoot, "progress_dirs");
            string subDir1 = Path.Combine(rootDir, "sub1");
            string subDir2 = Path.Combine(rootDir, "sub2");
            Directory.CreateDirectory(subDir1);
            Directory.CreateDirectory(subDir2);

            var attrib = new ClsFsAttrib(_logger)
            {
                IsProgressEnabled = true,
                ProgressIntervalDirectories = 1
            };

            // Act
            bool result = attrib.CalculateDirectorySize(rootDir, checkSymlink: false, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.True(result);
            string logs = GetLogContent();
            Assert.Contains("CURRENT STATUS", logs);
        }

        [Fact]
        public void CalculateDirectorySize_CheckSymlinkTrue_ProcessesNormalFilesCorrectly()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "symlink_check_dir");
            CreateFileWithSize(dir, "normal.txt", 128);

            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.CalculateDirectorySize(dir, checkSymlink: true, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, attrib.DirectoryCount);
            Assert.Equal((ulong)1, attrib.FileCount);
            Assert.Equal((ulong)128, attrib.TotalSize);
        }

        #endregion

        #region 3. CalculateFileSize テスト

        [Fact]
        public void CalculateFileSize_SingleFile_CalculatesSizeAndCount()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "file_calc");
            string file = CreateFileWithSize(dir, "test.dat", 1024);

            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.CalculateFileSize(file, checkSymlink: false, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, attrib.FileCount);
            Assert.Equal((ulong)1024, attrib.TotalSize);
            Assert.Equal((ulong)0, attrib.ErrorFileCount);
        }

        [Fact]
        public void CalculateFileSize_MultipleCalls_AccumulatesSizeAndCount()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "file_accumulate");
            string file1 = CreateFileWithSize(dir, "file1.dat", 200);
            string file2 = CreateFileWithSize(dir, "file2.dat", 300);

            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool res1 = attrib.CalculateFileSize(file1, checkSymlink: false, verboseLevel: 0, isStackTrace: false);
            bool res2 = attrib.CalculateFileSize(file2, checkSymlink: false, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.True(res1);
            Assert.True(res2);
            Assert.Equal((ulong)2, attrib.FileCount);
            Assert.Equal((ulong)500, attrib.TotalSize);
        }

        [Fact]
        public void CalculateFileSize_WithFileInfo_CalculatesCorrectly()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "file_info_test");
            string file = CreateFileWithSize(dir, "info.dat", 450);

            var attrib = new ClsFsAttrib(_logger);
            var fileInfo = new FileInfo(file);

            // Act
            bool result = attrib.CalculateFileSize(fileInfo, checkSymlink: false, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, attrib.FileCount);
            Assert.Equal((ulong)450, attrib.TotalSize);
        }

        [Fact]
        public void CalculateFileSize_NonExistentFile_ReturnsFalseAndIncrementsErrorFileCount()
        {
            // Arrange
            string nonExistentFile = Path.Combine(_testRoot, "no_file_" + Guid.NewGuid().ToString("N") + ".tmp");
            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.CalculateFileSize(nonExistentFile, checkSymlink: false, verboseLevel: 1, isStackTrace: true);

            // Assert
            Assert.False(result);
            Assert.Equal((ulong)1, attrib.ErrorFileCount);
            Assert.Equal((ulong)0, attrib.FileCount);

            string logs = GetLogContent();
            Assert.Contains("SKIP FILE", logs);
        }

        [Fact]
        public void CalculateFileSize_CheckSymlinkTrue_ProcessesNormalFileCorrectly()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "file_symlink");
            string file = CreateFileWithSize(dir, "normal_file.dat", 350);

            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.CalculateFileSize(file, checkSymlink: true, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.True(result);
            Assert.Equal((ulong)1, attrib.FileCount);
            Assert.Equal((ulong)350, attrib.TotalSize);
        }

        #endregion

        #region 4. OutputDirectoryOwner テスト

        [Fact]
        public void OutputDirectoryOwner_ExistingDirectory_ShowPathTrue_OutputsOwnerWithPath()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "owner_dir_showpath");
            Directory.CreateDirectory(dir);

            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.OutputDirectoryOwner(dir, verboseLevel: 0, showPath: true, isStackTrace: false);

            // Assert
            Assert.True(result);
            if (OperatingSystem.IsWindows())
            {
                string logs = GetLogContent();
                Assert.Contains($"{dir},OWNER,OWNER,", logs);
            }
        }

        [Fact]
        public void OutputDirectoryOwner_ExistingDirectory_ShowPathFalse_OutputsOwnerOnly()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "owner_dir_noshowpath");
            Directory.CreateDirectory(dir);

            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.OutputDirectoryOwner(dir, verboseLevel: 0, showPath: false, isStackTrace: false);

            // Assert
            Assert.True(result);
            if (OperatingSystem.IsWindows())
            {
                string logs = GetLogContent();
                Assert.False(string.IsNullOrWhiteSpace(logs));
                Assert.DoesNotContain($"{dir},OWNER,OWNER,", logs);
            }
        }

        [Fact]
        public void OutputDirectoryOwner_NonExistentDirectory_ReturnsFalseAndLogsError()
        {
            // Arrange
            string nonExistentDir = Path.Combine(_testRoot, "no_owner_dir_" + Guid.NewGuid().ToString("N"));
            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.OutputDirectoryOwner(nonExistentDir, verboseLevel: 1, showPath: true, isStackTrace: true);

            // Assert
            if (OperatingSystem.IsWindows())
            {
                Assert.False(result);
                string logs = GetLogContent();
                Assert.Contains("FAILED TO GET OWNER", logs);
                Assert.Contains(nonExistentDir, logs);
            }
            else
            {
                Assert.True(result);
            }
        }

        [Fact]
        public void OutputDirectoryOwner_NonExistentDirectory_ShowPathFalse_LogsError()
        {
            // Arrange
            string nonExistentDir = Path.Combine(_testRoot, "no_owner_dir_noshow_" + Guid.NewGuid().ToString("N"));
            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.OutputDirectoryOwner(nonExistentDir, verboseLevel: 1, showPath: false, isStackTrace: false);

            // Assert
            if (OperatingSystem.IsWindows())
            {
                Assert.False(result);
                string logs = GetLogContent();
                Assert.Contains("FAILED TO GET OWNER", logs);
            }
            else
            {
                Assert.True(result);
            }
        }

        #endregion

        #region 5. OutputDirectoryPermission テスト

        [Fact]
        public void OutputDirectoryPermission_ExistingDirectory_ShowPathTrue_OutputsPermissionWithPath()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "perm_dir_showpath");
            Directory.CreateDirectory(dir);

            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.OutputDirectoryPermission(dir, verboseLevel: 0, showPath: true, isStackTrace: false);

            // Assert
            Assert.True(result);
            if (OperatingSystem.IsWindows())
            {
                string logs = GetLogContent();
                Assert.Contains($"{dir},", logs);
            }
        }

        [Fact]
        public void OutputDirectoryPermission_ExistingDirectory_ShowPathFalse_OutputsPermissionWithoutPath()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "perm_dir_noshowpath");
            Directory.CreateDirectory(dir);

            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.OutputDirectoryPermission(dir, verboseLevel: 0, showPath: false, isStackTrace: false);

            // Assert
            Assert.True(result);
            if (OperatingSystem.IsWindows())
            {
                string logs = GetLogContent();
                Assert.False(string.IsNullOrWhiteSpace(logs));
            }
        }

        [Fact]
        public void OutputDirectoryPermission_NonExistentDirectory_ReturnsFalseAndLogsError()
        {
            // Arrange
            string nonExistentDir = Path.Combine(_testRoot, "no_perm_dir_" + Guid.NewGuid().ToString("N"));
            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.OutputDirectoryPermission(nonExistentDir, verboseLevel: 1, showPath: true, isStackTrace: true);

            // Assert
            if (OperatingSystem.IsWindows())
            {
                Assert.False(result);
                string logs = GetLogContent();
                Assert.Contains("FAILED TO GET PERMISSION", logs);
                Assert.Contains(nonExistentDir, logs);
            }
            else
            {
                Assert.True(result);
            }
        }

        [Fact]
        public void OutputDirectoryPermission_NonExistentDirectory_ShowPathFalse_LogsError()
        {
            // Arrange
            string nonExistentDir = Path.Combine(_testRoot, "no_perm_dir_noshow_" + Guid.NewGuid().ToString("N"));
            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.OutputDirectoryPermission(nonExistentDir, verboseLevel: 1, showPath: false, isStackTrace: false);

            // Assert
            if (OperatingSystem.IsWindows())
            {
                Assert.False(result);
                string logs = GetLogContent();
                Assert.Contains("FAILED TO GET PERMISSION", logs);
            }
            else
            {
                Assert.True(result);
            }
        }

        [Fact]
        public void CalculateDirectorySize_NonExistentDirectory_VerboseZero_DoesNotLogSkipDir()
        {
            // Arrange
            string nonExistentDir = Path.Combine(_testRoot, "no_dir_v0_" + Guid.NewGuid().ToString("N"));
            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.CalculateDirectorySize(nonExistentDir, checkSymlink: false, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.False(result);
            Assert.Equal((ulong)1, attrib.ErrorDirectoryCount);
            string logs = GetLogContent();
            Assert.DoesNotContain("SKIP DIR", logs);
        }

        [Fact]
        public void CalculateFileSize_NonExistentFile_VerboseZero_DoesNotLogSkipFile()
        {
            // Arrange
            string nonExistentFile = Path.Combine(_testRoot, "no_file_v0_" + Guid.NewGuid().ToString("N") + ".tmp");
            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.CalculateFileSize(nonExistentFile, checkSymlink: false, verboseLevel: 0, isStackTrace: false);

            // Assert
            Assert.False(result);
            Assert.Equal((ulong)1, attrib.ErrorFileCount);
            string logs = GetLogContent();
            Assert.DoesNotContain("SKIP FILE", logs);
        }

        [Fact]
        public void OutputDirectoryOwner_NonExistentDirectory_VerboseZero_DoesNotLogErrorMessage()
        {
            // Arrange
            string nonExistentDir = Path.Combine(_testRoot, "no_owner_v0_" + Guid.NewGuid().ToString("N"));
            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.OutputDirectoryOwner(nonExistentDir, verboseLevel: 0, showPath: true, isStackTrace: false);

            // Assert
            if (OperatingSystem.IsWindows())
            {
                Assert.False(result);
                string logs = GetLogContent();
                Assert.DoesNotContain("FAILED TO GET OWNER", logs);
            }
            else
            {
                Assert.True(result);
            }
        }

        [Fact]
        public void OutputDirectoryPermission_NonExistentDirectory_VerboseZero_DoesNotLogErrorMessage()
        {
            // Arrange
            string nonExistentDir = Path.Combine(_testRoot, "no_perm_v0_" + Guid.NewGuid().ToString("N"));
            var attrib = new ClsFsAttrib(_logger);

            // Act
            bool result = attrib.OutputDirectoryPermission(nonExistentDir, verboseLevel: 0, showPath: true, isStackTrace: false);

            // Assert
            if (OperatingSystem.IsWindows())
            {
                Assert.False(result);
                string logs = GetLogContent();
                Assert.DoesNotContain("FAILED TO GET PERMISSION", logs);
            }
            else
            {
                Assert.True(result);
            }
        }

        #endregion
    }
}
