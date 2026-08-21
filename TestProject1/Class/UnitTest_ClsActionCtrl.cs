using System;
using System.Collections.Generic;
using System.IO;
using CmnClsLib.Class;
using CmnClsLib.Module;
using FsFileUtil.Class;
using Xunit;

namespace TestProject1.Class
{
    public class UnitTest_ClsActionCtrl : IDisposable
    {
        private readonly string _testRoot;
        private readonly ClsLogger _logger;

        public UnitTest_ClsActionCtrl()
        {
            _testRoot = Path.Combine(System.IO.Path.GetTempPath(), @"UnitTest", @"FsFileUtil", @"ClsActionCtrl", Guid.NewGuid().ToString("N"));
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
                IsFrPathCheck = false,
                IsSourceCheck = false
            };
        }

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.Verbose = 2;
            prop.IsStackTrace = true;
            prop.WaitMSecForRetryCopy = 150;
            prop.RetrySystemCopyMax = 3;

            // Act
            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Assert
            Assert.NotNull(actionCtrl);
        }

        [Fact]
        public void Execute_WithSymLinkFlag_ExecutesWithoutError()
        {
            // Arrange
            string dir = Path.Combine(_testRoot, "symlink_test_dir");
            Directory.CreateDirectory(dir);
            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_EXIST;
            prop.SourcePath = dir;
            prop.IsSymLink = true;
            prop.Verbose = 1;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);
        }

        [Theory]
        [InlineData(true, MdlConst.LVL_E)]
        [InlineData(false, MdlConst.LVL_I)]
        public void Execute_SourcePathNotExists_HandlesSourceCheckFlag(bool isSourceCheck, int expectedResult)
        {
            // Arrange
            string nonExistentPath = Path.Combine(_testRoot, "non_existent_folder_" + Guid.NewGuid().ToString("N"));
            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_COPY;
            prop.SourcePath = nonExistentPath;
            prop.DestinationPath = Path.Combine(_testRoot, "dest");
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IsFrPathCheck = true;
            prop.IsSourceCheck = isSourceCheck;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void Execute_ActionFind_ReturnsSuccess()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "find_src");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "test.txt"), "hello find");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_FIND;
            prop.SourcePath = srcDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.Verbose = 2;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);
        }

        [Fact]
        public void Execute_ActionCopy_CopiesFilesAndUpdatesFilesCount()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "copy_src");
            string dstDir = Path.Combine(_testRoot, "copy_dst");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "sample.txt"), "hello copy");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_COPY;
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.TypeCode = MdlConst.INT_TYPE_ALL;
            prop.Verbose = 2;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(File.Exists(Path.Combine(dstDir, "sample.txt")));
            Assert.True(prop.Files > 0);
        }

        [Fact]
        public void Execute_ActionMove_MovesFilesAndUpdatesFilesCount()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "move_src");
            string dstDir = Path.Combine(_testRoot, "move_dst");
            Directory.CreateDirectory(srcDir);
            string srcFile = Path.Combine(srcDir, "file_to_move.txt");
            File.WriteAllText(srcFile, "hello move");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_MOVE;
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.TypeCode = MdlConst.INT_TYPE_ALL;
            prop.Verbose = 2;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(File.Exists(Path.Combine(dstDir, "file_to_move.txt")));
            Assert.False(File.Exists(srcFile));
            Assert.True(prop.Files > 0);
        }

        [Fact]
        public void Execute_ActionSync_CopiesAndDeletesAppropriately()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "sync_src");
            string dstDir = Path.Combine(_testRoot, "sync_dst");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(dstDir);

            File.WriteAllText(Path.Combine(srcDir, "keep.txt"), "keep content");
            File.WriteAllText(Path.Combine(dstDir, "orphan.txt"), "delete content");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_SYNC;
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.TypeCode = MdlConst.INT_TYPE_ALL;
            prop.Verbose = 2;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(File.Exists(Path.Combine(dstDir, "keep.txt")));
            Assert.False(File.Exists(Path.Combine(dstDir, "orphan.txt")));
        }

        [Fact]
        public void Execute_ActionSync_SyncRmOnly_SkipsCopy()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "sync_rm_src");
            string dstDir = Path.Combine(_testRoot, "sync_rm_dst");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(dstDir);

            File.WriteAllText(Path.Combine(srcDir, "new_src.txt"), "new file");
            File.WriteAllText(Path.Combine(dstDir, "orphan.txt"), "orphan file");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_SYNC;
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.IsSyncRmOnly = true;
            prop.Verbose = 2;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);
            Assert.False(File.Exists(Path.Combine(dstDir, "orphan.txt")));
            Assert.False(File.Exists(Path.Combine(dstDir, "new_src.txt")));
        }

        [Fact]
        public void Execute_ActionMkdir_CreatesDirectory()
        {
            // Arrange
            string dirToCreate = Path.Combine(_testRoot, "mkdir_test");
            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_MKDIR;
            prop.SourcePath = dirToCreate;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act - 新規作成
            int resultNew = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, resultNew);
            Assert.True(Directory.Exists(dirToCreate));

            // Act - 既存ディレクトリに対して再実行
            int resultExisting = actionCtrl.Execute();
            Assert.Equal(MdlConst.LVL_I, resultExisting);
        }

        [Fact]
        public void Execute_ActionMkdir_ReturnsErrorIfSameNameFileExists()
        {
            // Arrange
            string filePath = Path.Combine(_testRoot, "existing_file_for_mkdir");
            File.WriteAllText(filePath, "file");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_MKDIR;
            prop.SourcePath = filePath;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_E, result);
        }

        [Fact]
        public void Execute_ActionTouch_CreatesAndUpdatesFile()
        {
            // Arrange
            string touchFile = Path.Combine(_testRoot, "touch_test.txt");
            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_TOUCH;
            prop.SourcePath = touchFile;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act - 新規作成
            int resultNew = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, resultNew);
            Assert.True(File.Exists(touchFile));

            // Act - 既存ファイルのタイムスタンプ更新
            int resultExisting = actionCtrl.Execute();
            Assert.Equal(MdlConst.LVL_I, resultExisting);
        }

        [Fact]
        public void Execute_ActionTouch_ReturnsErrorIfSameNameDirExists()
        {
            // Arrange
            string dirPath = Path.Combine(_testRoot, "existing_dir_for_touch");
            Directory.CreateDirectory(dirPath);

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_TOUCH;
            prop.SourcePath = dirPath;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_E, result);
        }

        [Fact]
        public void Execute_ActionDelete_DeletesDirectoryAndFile()
        {
            // Arrange - ディレクトリ削除
            string delDir = Path.Combine(_testRoot, "del_dir");
            Directory.CreateDirectory(delDir);
            File.WriteAllText(Path.Combine(delDir, "f.txt"), "content");

            var propDir = CreateBaseProp();
            propDir.ActionCode = ClsProp.ACTION_DELETE;
            propDir.SourcePath = delDir;
            propDir.TypeCode = MdlConst.INT_TYPE_DIRECTORY;

            var actionCtrlDir = new ClsActionCtrl(_logger, propDir);
            int resDir = actionCtrlDir.Execute();

            Assert.Equal(MdlConst.LVL_I, resDir);
            Assert.False(Directory.Exists(delDir));

            // Arrange - ファイル削除
            string delFile = Path.Combine(_testRoot, "del_file.txt");
            File.WriteAllText(delFile, "content");

            var propFile = CreateBaseProp();
            propFile.ActionCode = ClsProp.ACTION_DELETE;
            propFile.SourcePath = delFile;
            propFile.TypeCode = MdlConst.INT_TYPE_FILE;

            var actionCtrlFile = new ClsActionCtrl(_logger, propFile);
            int resFile = actionCtrlFile.Execute();

            Assert.Equal(MdlConst.LVL_I, resFile);
            Assert.False(File.Exists(delFile));
        }

        [Fact]
        public void Execute_ActionDelete_MismatchType_HandlesCorrectly()
        {
            // Arrange - ディレクトリに対して TypeCode=INT_TYPE_FILE で削除試行
            string testDir = Path.Combine(_testRoot, "mismatch_dir");
            Directory.CreateDirectory(testDir);

            var propDirMismatch = CreateBaseProp();
            propDirMismatch.ActionCode = ClsProp.ACTION_DELETE;
            propDirMismatch.SourcePath = testDir;
            propDirMismatch.TypeCode = MdlConst.INT_TYPE_FILE;

            var actionCtrlDirMismatch = new ClsActionCtrl(_logger, propDirMismatch);
            int resDir = actionCtrlDirMismatch.Execute();
            Assert.True(Directory.Exists(testDir)); // 削除されずにディレクトリが維持される

            // Arrange - ファイルに対して TypeCode=INT_TYPE_DIRECTORY で削除試行
            string testFile = Path.Combine(_testRoot, "mismatch_file.txt");
            File.WriteAllText(testFile, "test");

            var propFileMismatch = CreateBaseProp();
            propFileMismatch.ActionCode = ClsProp.ACTION_DELETE;
            propFileMismatch.SourcePath = testFile;
            propFileMismatch.TypeCode = MdlConst.INT_TYPE_DIRECTORY;

            var actionCtrlFileMismatch = new ClsActionCtrl(_logger, propFileMismatch);
            int resFile = actionCtrlFileMismatch.Execute();
            Assert.True(File.Exists(testFile)); // 削除されずにファイルが維持される
        }

        [Theory]
        [InlineData(true, MdlConst.LVL_E)]
        [InlineData(false, MdlConst.LVL_I)]
        public void Execute_ActionDelete_NonExistentPath_HandlesSourceCheckFlag(bool isSourceCheck, int expectedResult)
        {
            // Arrange
            string nonExistent = Path.Combine(_testRoot, "not_found_" + Guid.NewGuid().ToString("N"));
            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_DELETE;
            prop.SourcePath = nonExistent;
            prop.IsSourceCheck = isSourceCheck;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void Execute_ActionExist_ChecksPathExistence()
        {
            // Arrange - 存在する場合
            string existFile = Path.Combine(_testRoot, "exist.txt");
            File.WriteAllText(existFile, "exists");

            var propExist = CreateBaseProp();
            propExist.ActionCode = ClsProp.ACTION_EXIST;
            propExist.SourcePath = existFile;

            var actionCtrlExist = new ClsActionCtrl(_logger, propExist);
            int resExist = actionCtrlExist.Execute();
            Assert.Equal(MdlConst.LVL_I, resExist);

            // Arrange - 存在しない場合
            var propNotExist = CreateBaseProp();
            propNotExist.ActionCode = ClsProp.ACTION_EXIST;
            propNotExist.SourcePath = Path.Combine(_testRoot, "non_existent.txt");

            var actionCtrlNotExist = new ClsActionCtrl(_logger, propNotExist);
            int resNotExist = actionCtrlNotExist.Execute();
            Assert.Equal(MdlConst.LVL_E, resNotExist);
        }

        [Fact]
        public void Execute_ActionExistDir_ChecksDirectoryType()
        {
            // Arrange - ディレクトリ指定
            var propDir = CreateBaseProp();
            propDir.ActionCode = ClsProp.ACTION_EXIST_DIR;
            propDir.PathType = MdlFile.PATH_IS_DIRECTORY;
            propDir.SourcePath = _testRoot;

            var actionCtrlDir = new ClsActionCtrl(_logger, propDir);
            Assert.Equal(MdlConst.LVL_I, actionCtrlDir.Execute());

            // Arrange - ファイル指定
            var propFile = CreateBaseProp();
            propFile.ActionCode = ClsProp.ACTION_EXIST_DIR;
            propFile.PathType = MdlFile.PATH_IS_FILE;
            propFile.SourcePath = _testRoot;

            var actionCtrlFile = new ClsActionCtrl(_logger, propFile);
            Assert.Equal(MdlConst.LVL_E, actionCtrlFile.Execute());

            // Arrange - 無効指定
            var propNull = CreateBaseProp();
            propNull.ActionCode = ClsProp.ACTION_EXIST_DIR;
            propNull.PathType = MdlFile.PATH_IS_NULL;

            var actionCtrlNull = new ClsActionCtrl(_logger, propNull);
            Assert.Equal(MdlConst.LVL_E, actionCtrlNull.Execute());
        }

        [Fact]
        public void Execute_ActionExistFile_ChecksFileType()
        {
            // Arrange - ファイル指定
            var propFile = CreateBaseProp();
            propFile.ActionCode = ClsProp.ACTION_EXIST_FILE;
            propFile.PathType = MdlFile.PATH_IS_FILE;
            propFile.SourcePath = _testRoot;

            var actionCtrlFile = new ClsActionCtrl(_logger, propFile);
            Assert.Equal(MdlConst.LVL_I, actionCtrlFile.Execute());

            // Arrange - ディレクトリ指定
            var propDir = CreateBaseProp();
            propDir.ActionCode = ClsProp.ACTION_EXIST_FILE;
            propDir.PathType = MdlFile.PATH_IS_DIRECTORY;
            propDir.SourcePath = _testRoot;

            var actionCtrlDir = new ClsActionCtrl(_logger, propDir);
            Assert.Equal(MdlConst.LVL_E, actionCtrlDir.Execute());

            // Arrange - 無効指定
            var propNull = CreateBaseProp();
            propNull.ActionCode = ClsProp.ACTION_EXIST_FILE;
            propNull.PathType = MdlFile.PATH_IS_NULL;

            var actionCtrlNull = new ClsActionCtrl(_logger, propNull);
            Assert.Equal(MdlConst.LVL_E, actionCtrlNull.Execute());
        }

        [Fact]
        public void Execute_ActionWait_WaitsForFile()
        {
            // Arrange - 存在するファイル
            string existingFile = Path.Combine(_testRoot, "wait_file.txt");
            File.WriteAllText(existingFile, "wait test");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_WAIT;
            prop.SourcePath = existingFile;
            prop.MaxLoop = 1;
            prop.Interval = 1;

            var actionCtrl = new ClsActionCtrl(_logger, prop);
            Assert.Equal(MdlConst.LVL_I, actionCtrl.Execute());

            // Arrange - 存在しないファイル (タイムアウト)
            prop.SourcePath = Path.Combine(_testRoot, "wait_non_existent.txt");
            var actionCtrlFail = new ClsActionCtrl(_logger, prop);
            Assert.Equal(MdlConst.LVL_E, actionCtrlFail.Execute());
        }

        [Fact]
        public void Execute_ActionFileLocked_DetectsLockStatus()
        {
            // Arrange - ロックされていないファイル
            string testFile = Path.Combine(_testRoot, "lock_test.txt");
            File.WriteAllText(testFile, "lock content");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_FILE_LOCKED;
            prop.SourcePath = testFile;

            var actionCtrl = new ClsActionCtrl(_logger, prop);
            Assert.Equal(MdlConst.LVL_I, actionCtrl.Execute());

            // Arrange - 排他ロック中
            using (var fs = new FileStream(testFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var actionCtrlLocked = new ClsActionCtrl(_logger, prop);
                Assert.Equal(MdlConst.LVL_W, actionCtrlLocked.Execute());
            }

            // Arrange - 存在しないファイル
            prop.SourcePath = Path.Combine(_testRoot, "lock_non_existent.txt");
            var actionCtrlNotExist = new ClsActionCtrl(_logger, prop);
            Assert.Equal(MdlConst.LVL_E, actionCtrlNotExist.Execute());
        }

        [Fact]
        public void Execute_ActionListLockProc_ReturnsError()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_LIST_LOCK_PROC;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_E, result);
        }

        [Fact]
        public void Execute_ActionRename_RenamesSingleFile()
        {
            // Arrange
            string srcFile = Path.Combine(_testRoot, "old_name.txt");
            string dstFile = Path.Combine(_testRoot, "new_name.txt");
            File.WriteAllText(srcFile, "rename content");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_RENAME;
            prop.SourcePath = srcFile;
            prop.DestinationPath = dstFile;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);
            Assert.False(File.Exists(srcFile));
            Assert.True(File.Exists(dstFile));
        }

        [Fact]
        public void Execute_ActionRename_WithFileList_RenamesFiles()
        {
            // Arrange
            string srcDir = Path.Combine(_testRoot, "rename_fl_src");
            string dstDir = Path.Combine(_testRoot, "rename_fl_dst");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(dstDir);

            File.WriteAllText(Path.Combine(srcDir, "file_a.txt"), "content a");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_RENAME;
            prop.SourcePath = srcDir;
            prop.DestinationPath = dstDir;
            prop.PathType = MdlFile.PATH_IS_DIRECTORY;
            prop.FileList = new List<string> { "file_a.txt" };
            prop.Verbose = 2;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);
        }

        [Fact]
        public void Execute_ActionRotate_RotatesFile()
        {
            // Arrange
            string targetFile = Path.Combine(_testRoot, "rotate_target.log");
            File.WriteAllText(targetFile, "log 1");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_ROTATE;
            prop.SourcePath = targetFile;
            prop.MaxKeep = 3;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);
        }

        [Fact]
        public void Execute_ActionMklink_InvalidPathType_ReturnsError20()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_MKLINK;
            prop.PathType = MdlFile.PATH_IS_NULL;
            prop.SourcePath = Path.Combine(_testRoot, "invalid_link");

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(20, result);
        }

        [Fact]
        public void Execute_ActionGetRealPath_ReturnsPathForValidTarget()
        {
            // Arrange
            string testFile = Path.Combine(_testRoot, "real_path_test.txt");
            File.WriteAllText(testFile, "real path");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_GET_REAL_PATH;
            prop.SourcePath = testFile;
            prop.PathType = MdlFile.PATH_IS_FILE;
            prop.IsDq = true;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);

            // Arrange - 無効な PathType の場合
            prop.PathType = MdlFile.PATH_IS_NULL;
            var actionCtrlFail = new ClsActionCtrl(_logger, prop);
            Assert.Equal(MdlConst.LVL_E, actionCtrlFail.Execute());
        }

        [Fact]
        public void Execute_ActionLs_ListsFilesAndDirectories()
        {
            // Arrange
            string lsDir = Path.Combine(_testRoot, "ls_dir");
            Directory.CreateDirectory(lsDir);
            Directory.CreateDirectory(Path.Combine(lsDir, "sub_dir"));
            File.WriteAllText(Path.Combine(lsDir, "file1.txt"), "f1");
            File.WriteAllText(Path.Combine(lsDir, "file2.txt"), "f2");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_LS;
            prop.SourcePath = lsDir;
            prop.TypeCode = MdlConst.INT_TYPE_ALL;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);
            Assert.True(prop.Files >= 3);

            // Arrange - 存在しないディレクトリ
            prop.SourcePath = Path.Combine(_testRoot, "ls_non_existent");
            var actionCtrlFail = new ClsActionCtrl(_logger, prop);
            Assert.Equal(MdlConst.LVL_E, actionCtrlFail.Execute());
        }

        [Fact]
        public void Execute_ActionLs_WithFileLockFilter_FiltersProperly()
        {
            // Arrange
            string lsDir = Path.Combine(_testRoot, "ls_lock_dir");
            Directory.CreateDirectory(lsDir);
            string normalFile = Path.Combine(lsDir, "normal.txt");
            string lockedFile = Path.Combine(lsDir, "locked.txt");
            File.WriteAllText(normalFile, "normal");
            File.WriteAllText(lockedFile, "locked");

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_LS;
            prop.SourcePath = lsDir;
            prop.TypeCode = MdlConst.INT_TYPE_FILE;
            prop.CheckFileLock = ClsProp.CHECK_FILE_LOCK_SAMPLE;

            using (var fs = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var actionCtrl = new ClsActionCtrl(_logger, prop);
                int result = actionCtrl.Execute();
                Assert.Equal(MdlConst.LVL_I, result);
            }
        }

        [Fact]
        public void Execute_ActionGetSize_CalculatesDirectoryAndFileSize()
        {
            // Arrange - ディレクトリ
            string sizeDir = Path.Combine(_testRoot, "size_dir");
            Directory.CreateDirectory(sizeDir);
            string file1 = Path.Combine(sizeDir, "file1.txt");
            File.WriteAllText(file1, "12345");

            var propDir = CreateBaseProp();
            propDir.ActionCode = ClsProp.ACTION_GET_SIZE;
            propDir.SourcePath = sizeDir;
            propDir.IsShowSize = true;
            propDir.IsShowFileNum = true;
            propDir.IsShowDirNum = true;
            propDir.IsShowPath = true;
            propDir.IsProgress = true;
            propDir.ProgressIntervalDirs = 1;
            propDir.ProgressIntervalFiles = 1;

            var actionCtrlDir = new ClsActionCtrl(_logger, propDir);
            Assert.Equal(MdlConst.LVL_I, actionCtrlDir.Execute());

            // Arrange - 単一ファイル
            var propFile = CreateBaseProp();
            propFile.ActionCode = ClsProp.ACTION_GET_SIZE;
            propFile.SourcePath = file1;
            propFile.IsShowSize = true;

            var actionCtrlFile = new ClsActionCtrl(_logger, propFile);
            Assert.Equal(MdlConst.LVL_I, actionCtrlFile.Execute());

            // Arrange - 存在しないパス
            var propNotExist = CreateBaseProp();
            propNotExist.ActionCode = ClsProp.ACTION_GET_SIZE;
            propNotExist.SourcePath = Path.Combine(_testRoot, "size_non_existent");

            var actionCtrlNotExist = new ClsActionCtrl(_logger, propNotExist);
            Assert.Equal(MdlConst.LVL_E, actionCtrlNotExist.Execute());
        }

        [Fact]
        public void Execute_ActionGetPermAndOwner_ExecutesWithoutError()
        {
            // Arrange
            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_GET_PERM;
            prop.SourcePath = _testRoot;
            prop.IsShowPerm = true;
            prop.IsShowOwner = true;

            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            int result = actionCtrl.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, result);
        }

        [Fact]
        public void Execute_ActionExec_ExecutesCommand()
        {
            // Arrange - CMD モード
            var propCmd = CreateBaseProp();
            propCmd.ActionCode = ClsProp.ACTION_EXEC;
            propCmd.ExecModeCode = ClsProp.EXEC_MODE_CMD;
            propCmd.CmdPath = "cmd.exe";
            propCmd.CmdArgs = "/c echo test";
            propCmd.SourcePath = _testRoot;
            propCmd.IsShowCmd = false;
            propCmd.IsShowOutput = false;

            var actionCtrlCmd = new ClsActionCtrl(_logger, propCmd);
            int resultCmd = actionCtrlCmd.Execute();
            Assert.Equal(MdlConst.LVL_I, resultCmd);
            Assert.Equal(1UL, propCmd.Files);

            // Arrange - PowerShell モード
            var propPs = CreateBaseProp();
            propPs.ActionCode = ClsProp.ACTION_EXEC;
            propPs.ExecModeCode = ClsProp.EXEC_MODE_PS;
            propPs.CmdPath = "powershell";
            propPs.CmdArgs = "Write-Output 'hello'";
            propPs.SourcePath = _testRoot;

            var actionCtrlPs = new ClsActionCtrl(_logger, propPs);
            int resultPs = actionCtrlPs.Execute();
            Assert.Equal(MdlConst.LVL_I, resultPs);
        }
    }
}
