using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Module;
using FsFileUtil.Class;
using Xunit;

namespace TestProject1.Class
{
    /// <summary>
    /// <see cref="ClsLogon"/> クラスの単体テストを提供します。
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class UnitTest_ClsLogon : IDisposable
    {
        private readonly string _testRoot;
        private readonly ClsLogger _logger;

        public UnitTest_ClsLogon()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), @"UnitTest", @"FsFileUtil", @"ClsLogon", Guid.NewGuid().ToString("N"));
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
                // 一時ディレクトリの削除失敗は無視
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

        #region 1. 列挙型（Enum）の検証テスト

        [Fact]
        public void Enum_LogonSessionType_ValuesAreCorrect()
        {
            Assert.Equal(2, (int)ClsLogon.LogonSessionType.Interactive);
            Assert.Equal(3, (int)ClsLogon.LogonSessionType.Network);
            Assert.Equal(4, (int)ClsLogon.LogonSessionType.Batch);
            Assert.Equal(5, (int)ClsLogon.LogonSessionType.Service);
            Assert.Equal(8, (int)ClsLogon.LogonSessionType.NetworkCleartext);
            Assert.Equal(9, (int)ClsLogon.LogonSessionType.NewCredentials);
        }

        [Fact]
        public void Enum_LogonProvider_ValuesAreCorrect()
        {
            Assert.Equal(0, (int)ClsLogon.LogonProvider.Default);
            Assert.Equal(1, (int)ClsLogon.LogonProvider.WinNT35);
            Assert.Equal(2, (int)ClsLogon.LogonProvider.WinNT40);
            Assert.Equal(3, (int)ClsLogon.LogonProvider.WinNT50);
        }

        #endregion

        #region 2. コンストラクタおよびプロパティ初期値のテスト

        [Fact]
        public void Constructor_InitializesDefaultValues()
        {
            // Arrange & Act
            using var logon = new ClsLogon();

            // Assert
            Assert.Equal(0, logon.ReturnCode);
            Assert.Equal(0, logon.Verbose);
            Assert.Equal(string.Empty, logon.DomainName);
            Assert.Equal(string.Empty, logon.Username);
            Assert.Equal(string.Empty, logon.Password);
            Assert.Equal(string.Empty, logon.Message);
        }

        #endregion

        #region 3. プロパティの読み書き（Getter/Setter）テスト

        [Theory]
        [InlineData(0, "DOMAIN", "User1", "Pass1")]
        [InlineData(1, "WORKGROUP", "Admin", "Secret#123")]
        [InlineData(7, "localhost", "TestUser", "")]
        [InlineData(-1, "", "", "")]
        public void Properties_GetSet_WorkCorrectly(int verbose, string domain, string user, string pass)
        {
            // Arrange
            using var logon = new ClsLogon();

            // Act
            logon.Verbose = verbose;
            logon.DomainName = domain;
            logon.Username = user;
            logon.Password = pass;

            // Assert
            Assert.Equal(verbose, logon.Verbose);
            Assert.Equal(domain, logon.DomainName);
            Assert.Equal(user, logon.Username);
            Assert.Equal(pass, logon.Password);
        }

        [Fact]
        public void Properties_SetNullValues_StoredAsNull()
        {
            // Arrange
            using var logon = new ClsLogon();

            // Act
            logon.DomainName = null!;
            logon.Username = null!;
            logon.Password = null!;

            // Assert
            Assert.Null(logon.DomainName);
            Assert.Null(logon.Username);
            Assert.Null(logon.Password);
        }

        #endregion

        #region 4. Dispose / IDisposable パターンのテスト

        [Fact]
        public void Dispose_SingleCall_Succeeds()
        {
            // Arrange
            var logon = new ClsLogon();

            // Act & Assert (例外が発生しないこと)
            logon.Dispose();
        }

        [Fact]
        public void Dispose_MultipleCalls_DoesNotThrowException()
        {
            // Arrange
            var logon = new ClsLogon();

            // Act & Assert
            logon.Dispose();
            logon.Dispose();
            logon.Dispose();
        }

        [Fact]
        public void UsingStatement_DisposesInstanceCorrectly()
        {
            ClsLogon? capturedLogon = null;

            // Arrange & Act
            using (var logon = new ClsLogon())
            {
                capturedLogon = logon;
                Assert.NotNull(capturedLogon);
            }

            // Assert (Dispose済みのためExecuteで例外が発生することを確認)
            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_NONE;
            var actionCtrl = new ClsActionCtrl(_logger, prop);

            Assert.Throws<ObjectDisposedException>(() => capturedLogon.Execute(actionCtrl));
        }

        #endregion

        #region 5. Execute メソッドの実行テスト

        [Fact]
        public void Execute_WithActionNone_ExecutesSuccessfullyAndReturnsLvlI()
        {
            // Arrange
            using var logon = new ClsLogon();
            logon.DomainName = "localhost";
            logon.Username = "TestUser";
            logon.Password = "TestPassword";
            logon.Verbose = 0;

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_NONE;
            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            logon.Execute(actionCtrl);

            // Assert
            Assert.Equal(MdlConst.LVL_I, logon.ReturnCode);
            Assert.StartsWith("OK", logon.Message);
        }

        [Fact]
        public void Execute_WithActionExist_SuccessCase_PropagatesReturnCode()
        {
            // Arrange
            string subDir = Path.Combine(_testRoot, "ExistingDir");
            Directory.CreateDirectory(subDir);

            using var logon = new ClsLogon();
            logon.DomainName = "DOMAIN";
            logon.Username = "User";
            logon.Password = "Pass";

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_EXIST;
            prop.SourcePath = subDir;
            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            logon.Execute(actionCtrl);

            // Assert
            Assert.Equal(MdlConst.LVL_I, logon.ReturnCode);
            Assert.StartsWith("OK", logon.Message);
        }

        [Fact]
        public void Execute_WithActionExist_NotFoundCase_PropagatesErrorCode()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_testRoot, "NonExistentPath");

            using var logon = new ClsLogon();
            logon.DomainName = "DOMAIN";
            logon.Username = "User";
            logon.Password = "Pass";

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_EXIST;
            prop.SourcePath = nonExistentPath;
            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            logon.Execute(actionCtrl);

            // Assert
            Assert.Equal(MdlConst.LVL_E, logon.ReturnCode);
            Assert.StartsWith("OK", logon.Message);
        }

        [Fact]
        public void Execute_WithActionMkdir_CreatesDirectoryAndSetsReturnCode()
        {
            // Arrange
            string newDir = Path.Combine(_testRoot, "NewCreatedDir");

            using var logon = new ClsLogon();
            logon.DomainName = "DOMAIN";
            logon.Username = "User";
            logon.Password = "Pass";

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_MKDIR;
            prop.SourcePath = newDir;
            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            logon.Execute(actionCtrl);

            // Assert
            Assert.Equal(MdlConst.LVL_I, logon.ReturnCode);
            Assert.True(Directory.Exists(newDir));
            Assert.StartsWith("OK", logon.Message);
        }

        [Fact]
        public void Execute_WithVerboseAboveDebugThreshold_ExecutesAndOutputsLogs()
        {
            // Arrange
            using var logon = new ClsLogon();
            logon.DomainName = "TESTDOMAIN";
            logon.Username = "TestUser";
            logon.Password = "TestPass";
            logon.Verbose = 7; // _debugThreshold (6) より大きい

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_NONE;
            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            logon.Execute(actionCtrl);

            // Assert
            Assert.Equal(MdlConst.LVL_I, logon.ReturnCode);
            Assert.StartsWith("OK", logon.Message);
        }

        [Fact]
        public void Execute_FollowedByDispose_CleansUpSafeTokenHandleSafely()
        {
            // Arrange
            var logon = new ClsLogon();
            logon.DomainName = "DOMAIN";
            logon.Username = "User";
            logon.Password = "Pass";
            logon.Verbose = 7; // Dispose時のVerboseログ出力パスを通す

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_NONE;
            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act
            logon.Execute(actionCtrl);
            logon.Dispose();

            // Assert
            // Dispose後に再度Disposeを呼んでも安全
            logon.Dispose();
        }

        #endregion

        #region 6. 例外系のテスト

        [Fact]
        public void Execute_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            var logon = new ClsLogon();
            logon.Dispose();

            var prop = CreateBaseProp();
            prop.ActionCode = ClsProp.ACTION_NONE;
            var actionCtrl = new ClsActionCtrl(_logger, prop);

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => logon.Execute(actionCtrl));
        }

        [Fact]
        public void Execute_WithNullActionController_ThrowsNullReferenceException()
        {
            // Arrange
            using var logon = new ClsLogon();
            logon.DomainName = "DOMAIN";
            logon.Username = "User";
            logon.Password = "Pass";

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => logon.Execute(null!));
        }

        #endregion
    }
}
