using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CmnClsLib.Class;
using CmnClsLib.Module;
using FsFileUtil.Class;
using Xunit;

namespace TestProject1.Class
{
    public class UnitTest_ClsFsUtil : IDisposable
    {
        private readonly string _testRoot;
        private readonly ClsLogger _logger;

        public UnitTest_ClsFsUtil()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "UnitTest", "FsFileUtil", "ClsFsUtil", Guid.NewGuid().ToString("N"));
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

        private string CreateTestFile(string fileName, string content = "test content")
        {
            string filePath = Path.Combine(_testRoot, fileName);
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(filePath, content, Encoding.UTF8);
            return filePath;
        }

        private string CreateTestFileWithBytes(string fileName, byte[] bytes)
        {
            string filePath = Path.Combine(_testRoot, fileName);
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(filePath, bytes);
            return filePath;
        }

        #region 1. コンストラクタおよびプロパティのテスト

        [Fact]
        public void Constructor_WithLogger_InitializesDefaults()
        {
            // Arrange & Act
            var util = new ClsFsUtil(_logger);

            // Assert
            Assert.NotNull(util);
            Assert.Equal("", util.Message);
            Assert.Equal("", util.Result);
            Assert.False(util.IsStackTrace);
            Assert.Equal(0, util.Verbose);
            Assert.Equal(200, util.WaitMSecForRetryCopy);
            Assert.Equal(0, util.RetryMax);
        }

        [Fact]
        public void Constructor_WithNullLogger_InitializesDefaults()
        {
            // Arrange & Act
            var util = new ClsFsUtil(null!);

            // Assert
            Assert.NotNull(util);
            Assert.Equal("", util.Message);
            Assert.Equal("", util.Result);
            Assert.False(util.IsStackTrace);
            Assert.Equal(0, util.Verbose);
            Assert.Equal(200, util.WaitMSecForRetryCopy);
            Assert.Equal(0, util.RetryMax);
        }

        [Fact]
        public void Properties_SetAndGet_ReturnExpectedValues()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);

            // Act & Assert
            util.Message = "CustomMessage";
            Assert.Equal("CustomMessage", util.Message);

            util.Result = "CustomResult";
            Assert.Equal("CustomResult", util.Result);

            util.IsStackTrace = true;
            Assert.True(util.IsStackTrace);

            util.Verbose = 2;
            Assert.Equal(2, util.Verbose);

            util.WaitMSecForRetryCopy = 500;
            Assert.Equal(500, util.WaitMSecForRetryCopy);

            util.RetryMax = 3;
            Assert.Equal(3, util.RetryMax);
        }

        #endregion

        #region 2. Rotate メソッドのテスト

        [Fact]
        public void Rotate_NullPath_ThrowsArgumentNullException()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => util.Rotate(null!, 3));
        }

        [Fact]
        public void Rotate_SingleGeneration_RotatesFileCorrectly()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string baseFile = CreateTestFile("rotate_single.log", "gen0");

            // Act
            int status = util.Rotate(baseFile, 1);

            // Assert
            Assert.Equal(MdlConst.LVL_I, status);
            Assert.False(File.Exists(baseFile));
            Assert.True(File.Exists($"{baseFile}.1"));
            Assert.Equal("gen0", File.ReadAllText($"{baseFile}.1", Encoding.UTF8));
        }

        [Fact]
        public void Rotate_MultipleGenerations_RotatesAllFilesAndDeletesOldest()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string baseFile = CreateTestFile("app.log", "generation 0");
            string gen1File = CreateTestFile("app.log.1", "generation 1");
            string gen2File = CreateTestFile("app.log.2", "generation 2");
            string gen3File = CreateTestFile("app.log.3", "generation 3 (to be deleted)");

            // Act
            int status = util.Rotate(baseFile, 3);

            // Assert
            Assert.Equal(MdlConst.LVL_I, status);
            Assert.False(File.Exists(baseFile));
            Assert.True(File.Exists($"{baseFile}.1"));
            Assert.True(File.Exists($"{baseFile}.2"));
            Assert.True(File.Exists($"{baseFile}.3"));
            Assert.False(File.Exists($"{baseFile}.4"));

            // Verify contents shifted
            Assert.Equal("generation 0", File.ReadAllText($"{baseFile}.1", Encoding.UTF8));
            Assert.Equal("generation 1", File.ReadAllText($"{baseFile}.2", Encoding.UTF8));
            Assert.Equal("generation 2", File.ReadAllText($"{baseFile}.3", Encoding.UTF8));
        }

        [Fact]
        public void Rotate_KeepMaxZero_DeletesBaseZeroAndReturnsSuccess()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string baseFile = CreateTestFile("zero.log", "current");
            string zeroFile = CreateTestFile("zero.log.0", "old zero");

            // Act
            int status = util.Rotate(baseFile, 0);

            // Assert
            Assert.Equal(MdlConst.LVL_I, status);
            Assert.True(File.Exists(baseFile)); // baseFile はそのまま
            Assert.False(File.Exists(zeroFile)); // zero.log.0 は削除される
        }

        [Fact]
        public void Rotate_NonExistentFile_ReturnsSuccess()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string nonExistentPath = Path.Combine(_testRoot, "non_existent.log");

            // Act
            int status = util.Rotate(nonExistentPath, 3);

            // Assert
            Assert.Equal(MdlConst.LVL_I, status);
        }

        [Fact]
        public void Rotate_WithNullLogger_ExecutesSafely()
        {
            // Arrange
            var util = new ClsFsUtil(null!);
            string baseFile = CreateTestFile("null_logger_rotate.log", "data");

            // Act
            int status = util.Rotate(baseFile, 2);

            // Assert
            Assert.Equal(MdlConst.LVL_I, status);
            Assert.True(File.Exists($"{baseFile}.1"));
        }

        #endregion

        #region 3. WaitUntilFileExists メソッドのテスト

        [Fact]
        public void WaitUntilFileExists_NullPath_ThrowsArgumentNullException()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => util.WaitUntilFileExists(null!, 1, 0));
            Assert.Throws<ArgumentNullException>(() => util.WaitUntilFileExists(null!, 1, 0, false));
        }

        [Fact]
        public void WaitUntilFileExists_ExistingFile_ReturnsTrueImmediately()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string filePath = CreateTestFile("exists.txt", "content");

            // Act
            bool result = util.WaitUntilFileExists(filePath, 3, 0);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void WaitUntilFileExists_NonExistentFile_ReturnsFalseAfterMaxLoop()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string filePath = Path.Combine(_testRoot, "not_found.txt");

            // Act
            bool result = util.WaitUntilFileExists(filePath, 2, 0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void WaitUntilFileExists_MaxLoopLessThanOne_AdjustsToOneAndReturnsFalse()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string filePath = Path.Combine(_testRoot, "not_found_0.txt");

            // Act
            bool result = util.WaitUntilFileExists(filePath, 0, 0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void WaitUntilFileExists_CheckFileLock_UnlockedFile_ReturnsTrue()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string filePath = CreateTestFile("unlocked.txt", "content");

            // Act
            bool result = util.WaitUntilFileExists(filePath, 2, 0, true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void WaitUntilFileExists_CheckFileLock_LockedFile_ReturnsFalse()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string filePath = CreateTestFile("locked.txt", "content");

            // Open exclusively to simulate lock
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // Act
                bool result = util.WaitUntilFileExists(filePath, 2, 0, true);

                // Assert
                Assert.False(result);
            }
        }

        [Fact]
        public void WaitUntilFileExists_FileCreatedDuringWait_ReturnsTrue()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string delayedFilePath = Path.Combine(_testRoot, "delayed.txt");

            // Start a task that creates the file after 500ms
            Task.Run(() =>
            {
                Thread.Sleep(500);
                File.WriteAllText(delayedFilePath, "created");
            });

            // Act (maxLoop=3, interval=1 -> up to 3 seconds wait)
            bool result = util.WaitUntilFileExists(delayedFilePath, 3, 1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void WaitUntilFileExists_NullLogger_ExecutesSafely()
        {
            // Arrange
            var util = new ClsFsUtil(null!);
            string filePath = CreateTestFile("null_logger_wait.txt", "content");

            // Act
            bool result = util.WaitUntilFileExists(filePath, 1, 0);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region 4. Rename メソッドのテスト

        [Fact]
        public void Rename_NullArguments_ThrowsArgumentNullException()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string validPath = Path.Combine(_testRoot, "valid.txt");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => util.Rename(null!, validPath));
            Assert.Throws<ArgumentNullException>(() => util.Rename(validPath, null!));
        }

        [Fact]
        public void Rename_File_RenamesSuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string sourcePath = CreateTestFile("rename_src.txt", "hello rename");
            string destinationPath = Path.Combine(_testRoot, "rename_dst.txt");

            // Act
            bool result = util.Rename(sourcePath, destinationPath);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(destinationPath));
            Assert.Equal("hello rename", File.ReadAllText(destinationPath, Encoding.UTF8));
        }

        [Fact]
        public void Rename_Directory_RenamesSuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string sourceDir = Path.Combine(_testRoot, "src_dir");
            Directory.CreateDirectory(sourceDir);
            CreateTestFile(Path.Combine("src_dir", "sub.txt"), "sub content");
            string destinationDir = Path.Combine(_testRoot, "dst_dir");

            // Act
            bool result = util.Rename(sourceDir, destinationDir);

            // Assert
            Assert.True(result);
            Assert.False(Directory.Exists(sourceDir));
            Assert.True(Directory.Exists(destinationDir));
            Assert.True(File.Exists(Path.Combine(destinationDir, "sub.txt")));
        }

        [Fact]
        public void Rename_NonExistentPath_SkipsAndReturnsTrue()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string sourcePath = Path.Combine(_testRoot, "non_existent.txt");
            string destinationPath = Path.Combine(_testRoot, "dst.txt");

            // Act
            bool result = util.Rename(sourcePath, destinationPath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Rename_DestinationExists_ReturnsFalseAndLogsError()
        {
            // Arrange
            var util = new ClsFsUtil(_logger) { IsStackTrace = true };
            string sourcePath = CreateTestFile("src_exist.txt", "source");
            string destinationPath = CreateTestFile("dst_exist.txt", "dest");

            // Act (File.Move will fail if destination exists without overwrite option in net10.0 File.Move(src, dst))
            bool result = util.Rename(sourcePath, destinationPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Rename_WithNullLogger_ExecutesSafely()
        {
            // Arrange
            var util = new ClsFsUtil(null!);
            string sourcePath = CreateTestFile("rename_null_log.txt", "data");
            string destinationPath = Path.Combine(_testRoot, "rename_null_log_dst.txt");

            // Act
            bool result = util.Rename(sourcePath, destinationPath);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(destinationPath));
        }

        #endregion

        #region 5. ComputeSha1Hash メソッドのテスト

        [Fact]
        public void ComputeSha1Hash_NullPath_ThrowsArgumentNullException()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => util.ComputeSha1Hash(null!));
        }

        [Fact]
        public void ComputeSha1Hash_ValidFile_ReturnsCorrectSha1()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            byte[] testBytes = Encoding.UTF8.GetBytes("SHA1 Test Data 12345");
            string filePath = CreateTestFileWithBytes("sha1_test.bin", testBytes);

            using var sha1 = SHA1.Create();
            string expectedHash = BitConverter.ToString(sha1.ComputeHash(testBytes)).Replace("-", "").ToLowerInvariant();

            // Act
            string hash = util.ComputeSha1Hash(filePath);

            // Assert
            Assert.Equal(expectedHash, hash.ToLowerInvariant());
        }

        [Fact]
        public void ComputeSha1Hash_EmptyFile_ReturnsEmptyFileHash()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string filePath = CreateTestFileWithBytes("empty.bin", Array.Empty<byte>());

            // SHA-1 of empty content is da39a3ee5e6b4b0d3255bfef95601890afd80709
            const string expectedSha1 = "da39a3ee5e6b4b0d3255bfef95601890afd80709";

            // Act
            string hash = util.ComputeSha1Hash(filePath);

            // Assert
            Assert.Equal(expectedSha1, hash.ToLowerInvariant());
        }

        [Fact]
        public void ComputeSha1Hash_NonExistentFile_ReturnsEmptyStringAndLogsError()
        {
            // Arrange
            var util = new ClsFsUtil(_logger) { IsStackTrace = true };
            string nonExistentPath = Path.Combine(_testRoot, "not_found.bin");

            // Act
            string hash = util.ComputeSha1Hash(nonExistentPath);

            // Assert
            Assert.Equal("", hash);
        }

        [Fact]
        public void ComputeSha1Hash_WithNullLogger_ExecutesSafely()
        {
            // Arrange
            var util = new ClsFsUtil(null!);
            string filePath = CreateTestFile("null_logger_sha1.txt", "test");

            // Act
            string hash = util.ComputeSha1Hash(filePath);

            // Assert
            Assert.NotEmpty(hash);
        }

        #endregion

        #region 6. SetCursorVisible & WhoIsLocking のテスト

        [Fact]
        public void SetCursorVisible_TrueAndFalse_DoesNotThrow()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);

            // Act & Assert (Should not throw any exception)
            var ex1 = Record.Exception(() => util.SetCursorVisible(true));
            var ex2 = Record.Exception(() => util.SetCursorVisible(false));

            Assert.Null(ex1);
            Assert.Null(ex2);
        }

        [Fact]
        public void WhoIsLocking_ReturnsErrorLevel()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string dummyPath = Path.Combine(_testRoot, "dummy.txt");

            // Act
            int result = util.WhoIsLocking(dummyPath);

            // Assert
            Assert.Equal(MdlConst.LVL_E, result);
        }

        [Fact]
        public void WhoIsLocking_WithNullLogger_ReturnsErrorLevel()
        {
            // Arrange
            var util = new ClsFsUtil(null!);
            string dummyPath = Path.Combine(_testRoot, "dummy.txt");

            // Act
            int result = util.WhoIsLocking(dummyPath);

            // Assert
            Assert.Equal(MdlConst.LVL_E, result);
        }

        #endregion

        #region 7. CopyFileWithRetry メソッドのテスト

        [Fact]
        public void CopyFileWithRetry_NullArguments_ThrowsArgumentNullException()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string validPath = Path.Combine(_testRoot, "valid.txt");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => util.CopyFileWithRetry(null!, validPath));
            Assert.Throws<ArgumentNullException>(() => util.CopyFileWithRetry(validPath, null!));
        }

        [Fact]
        public void CopyFileWithRetry_NormalFile_CopiesSuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(_logger) { RetryMax = 2, WaitMSecForRetryCopy = 10 };
            string sourcePath = CreateTestFile("retry_src.txt", "content to copy");
            string destinationPath = Path.Combine(_testRoot, "retry_dst.txt");

            // Act
            util.CopyFileWithRetry(sourcePath, destinationPath);

            // Assert
            Assert.True(File.Exists(destinationPath));
            Assert.Equal("content to copy", File.ReadAllText(destinationPath, Encoding.UTF8));
        }

        [Fact]
        public void CopyFileWithRetry_OverwriteExistingFile_OverwritesSuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string sourcePath = CreateTestFile("overwrite_src.txt", "new version");
            string destinationPath = CreateTestFile("overwrite_dst.txt", "old version");

            // Act
            util.CopyFileWithRetry(sourcePath, destinationPath);

            // Assert
            Assert.Equal("new version", File.ReadAllText(destinationPath, Encoding.UTF8));
        }

        [Fact]
        public void CopyFileWithRetry_NonExistentSource_ThrowsExceptionAfterRetries()
        {
            // Arrange
            var util = new ClsFsUtil(_logger) { RetryMax = 2, WaitMSecForRetryCopy = 10 };
            string nonExistentSource = Path.Combine(_testRoot, "not_exist_source.txt");
            string destinationPath = Path.Combine(_testRoot, "dst.txt");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => util.CopyFileWithRetry(nonExistentSource, destinationPath));
        }

        #endregion

        #region 8. BinaryCopy & BinaryCopyWithProgress メソッドのテスト

        [Fact]
        public void BinaryCopy_NullArguments_ThrowsArgumentNullException()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string validPath = Path.Combine(_testRoot, "valid.bin");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => util.BinaryCopy(null!, validPath, false));
            Assert.Throws<ArgumentNullException>(() => util.BinaryCopy(validPath, null!, false));
            Assert.Throws<ArgumentNullException>(() => util.BinaryCopy(null!, validPath, false, FileShare.Read));
            Assert.Throws<ArgumentNullException>(() => util.BinaryCopy(validPath, null!, false, FileShare.Read));
        }

        [Fact]
        public void BinaryCopyWithProgress_NullArguments_ThrowsArgumentNullException()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string validPath = Path.Combine(_testRoot, "valid.bin");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => util.BinaryCopyWithProgress(null!, validPath));
            Assert.Throws<ArgumentNullException>(() => util.BinaryCopyWithProgress(validPath, null!));
            Assert.Throws<ArgumentNullException>(() => util.BinaryCopyWithProgress(null!, validPath, FileShare.Read));
            Assert.Throws<ArgumentNullException>(() => util.BinaryCopyWithProgress(validPath, null!, FileShare.Read));
        }

        [Fact]
        public void BinaryCopy_SmallFile_CopiesSuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(_logger) { Verbose = 2, IsStackTrace = true };
            byte[] data = Encoding.UTF8.GetBytes("Small Binary Data String 1234567890");
            string sourcePath = CreateTestFileWithBytes("small.bin", data);
            string destinationPath = Path.Combine(_testRoot, "small_dst.bin");

            // Act
            util.BinaryCopy(sourcePath, destinationPath, false);

            // Assert
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(data, File.ReadAllBytes(destinationPath));
        }

        [Fact]
        public void BinaryCopy_LargeFile_WithProgress_CopiesSuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            // 256KB of repeating data to span multiple buffers
            byte[] data = new byte[256 * 1024];
            new Random(42).NextBytes(data);
            string sourcePath = CreateTestFileWithBytes("large.bin", data);
            string destinationPath = Path.Combine(_testRoot, "large_dst.bin");

            // Act
            util.BinaryCopy(sourcePath, destinationPath, true, FileShare.Read);

            // Assert
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(data, File.ReadAllBytes(destinationPath));
            Assert.NotEmpty(util.Result);
        }

        [Fact]
        public void BinaryCopy_OverloadWithoutFileShare_CopiesSuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            byte[] data = Encoding.UTF8.GetBytes("BinaryCopy overload without FileShare");
            string sourcePath = CreateTestFileWithBytes("bin_overload.bin", data);
            string destinationPath = Path.Combine(_testRoot, "bin_overload_dst.bin");

            // Act
            util.BinaryCopy(sourcePath, destinationPath, true);

            // Assert
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(data, File.ReadAllBytes(destinationPath));
        }

        [Fact]
        public void BinaryCopyWithProgress_Overloads_CopySuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            byte[] data1 = Encoding.UTF8.GetBytes("BinaryCopyWithProgress Test 1");
            string src1 = CreateTestFileWithBytes("bw_src1.bin", data1);
            string dst1 = Path.Combine(_testRoot, "bw_dst1.bin");

            byte[] data2 = Encoding.UTF8.GetBytes("BinaryCopyWithProgress Test 2");
            string src2 = CreateTestFileWithBytes("bw_src2.bin", data2);
            string dst2 = Path.Combine(_testRoot, "bw_dst2.bin");

            // Act
            util.BinaryCopyWithProgress(src1, dst1);
            util.BinaryCopyWithProgress(src2, dst2, FileShare.ReadWrite);

            // Assert
            Assert.Equal(data1, File.ReadAllBytes(dst1));
            Assert.Equal(data2, File.ReadAllBytes(dst2));
        }

        [Fact]
        public void BinaryCopy_NonExistentSource_FallsBackAndThrows()
        {
            // Arrange
            var util = new ClsFsUtil(_logger)
            {
                Verbose = 2,
                IsStackTrace = true,
                WaitMSecForRetryCopy = 10,
                RetryMax = 1
            };
            string nonExistentSource = Path.Combine(_testRoot, "non_existent_binary.bin");
            string destinationPath = Path.Combine(_testRoot, "dst.bin");

            // Act & Assert (Fails in asyncCpStatus and falls back to CopyFileWithRetry which throws)
            Assert.Throws<FileNotFoundException>(() => util.BinaryCopy(nonExistentSource, destinationPath, false));
        }

        #endregion

        #region 9. AsyncCopy メソッドのテスト

        [Fact]
        public void AsyncCopy_NullArguments_ThrowsArgumentNullException()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            string validPath = Path.Combine(_testRoot, "valid.bin");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => util.AsyncCopy(null!, validPath, false));
            Assert.Throws<ArgumentNullException>(() => util.AsyncCopy(validPath, null!, false));
            Assert.Throws<ArgumentNullException>(() => util.AsyncCopy(null!, validPath, false, FileShare.Read));
            Assert.Throws<ArgumentNullException>(() => util.AsyncCopy(validPath, null!, false, FileShare.Read));
        }

        [Fact]
        public void AsyncCopy_SmallFile_CopiesSuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            byte[] data = Encoding.UTF8.GetBytes("Async Copy Test Content");
            string sourcePath = CreateTestFileWithBytes("async_small.bin", data);
            string destinationPath = Path.Combine(_testRoot, "async_small_dst.bin");

            // Act
            util.AsyncCopy(sourcePath, destinationPath, false);

            // Assert
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(data, File.ReadAllBytes(destinationPath));
            Assert.NotNull(util.Result);
        }

        [Fact]
        public void AsyncCopy_LargeFile_WithProgressAndFileShare_CopiesSuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(_logger) { Verbose = 2, IsStackTrace = true };
            // 256KB of data
            byte[] data = new byte[256 * 1024];
            new Random(123).NextBytes(data);
            string sourcePath = CreateTestFileWithBytes("async_large.bin", data);
            string destinationPath = Path.Combine(_testRoot, "async_large_dst.bin");

            // Act
            util.AsyncCopy(sourcePath, destinationPath, true, FileShare.Read);

            // Assert
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(data, File.ReadAllBytes(destinationPath));
            Assert.NotEmpty(util.Result);
        }

        [Fact]
        public void AsyncCopy_OverloadWithoutFileShare_CopiesSuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(_logger);
            byte[] data = Encoding.UTF8.GetBytes("Async Copy Overload Test Content");
            string sourcePath = CreateTestFileWithBytes("async_overload.bin", data);
            string destinationPath = Path.Combine(_testRoot, "async_overload_dst.bin");

            // Act
            util.AsyncCopy(sourcePath, destinationPath, true);

            // Assert
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(data, File.ReadAllBytes(destinationPath));
        }

        [Fact]
        public void AsyncCopy_NonExistentSource_FallsBackAndThrows()
        {
            // Arrange
            var util = new ClsFsUtil(_logger)
            {
                Verbose = 2,
                IsStackTrace = true,
                WaitMSecForRetryCopy = 10,
                RetryMax = 1
            };
            string nonExistentSource = Path.Combine(_testRoot, "non_existent_async.bin");
            string destinationPath = Path.Combine(_testRoot, "dst_async.bin");

            // Act & Assert (asyncCpStatus fails and falls back to CopyFileWithRetry which throws)
            Assert.Throws<FileNotFoundException>(() => util.AsyncCopy(nonExistentSource, destinationPath, false));
        }

        [Fact]
        public void AsyncCopy_WithNullLogger_CopiesSuccessfully()
        {
            // Arrange
            var util = new ClsFsUtil(null!);
            byte[] data = Encoding.UTF8.GetBytes("Async Copy with Null Logger");
            string sourcePath = CreateTestFileWithBytes("async_null_logger.bin", data);
            string destinationPath = Path.Combine(_testRoot, "async_null_logger_dst.bin");

            // Act
            util.AsyncCopy(sourcePath, destinationPath, false);

            // Assert
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(data, File.ReadAllBytes(destinationPath));
        }

        #endregion
    }
}
