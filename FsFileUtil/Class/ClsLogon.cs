using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using CmnClsLib.Class;
using CmnClsLib.Module;
using CmnWinLib.Class;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace FsFileUtil.Class
{
    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Windows ログオンセッションおよびユーザー成り代わり (Impersonation) 処理を提供するクラスです。
    /// </summary>
    /// <example>
    /// <code>
    /// using ClsLogon logon = new ClsLogon();
    /// logon.DomainName = "DOMAIN";
    /// logon.Username = "User";
    /// logon.Password = "Password";
    /// logon.Execute(actionController);
    /// </code>
    /// </example>
    public class ClsLogon : IDisposable
    {
        private static class NativeMethods
        {
            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool LogonUser(
                string lpszUsername,
                string lpszDomain,
                string lpszPassword,
                int dwLogonType,
                int dwLogonProvider,
                out SafeAccessTokenHandle phToken);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DuplicateToken(
                SafeAccessTokenHandle existingTokenHandle,
                int SECURITY_IMPERSONATION_LEVEL,
                ref SafeAccessTokenHandle duplicateTokenHandle);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr handle);
        }

        /// <summary>
        /// ログオンセッションの種別を表す列挙体です。
        /// </summary>
        public enum LogonSessionType : int
        {
            /// <summary>対話的ログオン セッション</summary>
            Interactive = 2,
            /// <summary>ネットワーク ログオン セッション</summary>
            Network,
            /// <summary>バッチ ログオン セッション</summary>
            Batch,
            /// <summary>サービス ログオン セッション</summary>
            Service,
            /// <summary>ネットワーク クリアテキスト ログオン セッション</summary>
            NetworkCleartext = 8,
            /// <summary>新しいクレデンシャル ログオン セッション</summary>
            NewCredentials
        }

        /// <summary>
        /// ログオンプロバイダーの種類を表す列挙体です。
        /// </summary>
        public enum LogonProvider : int
        {
            /// <summary>プラットフォームのデフォルトプロバイダー</summary>
            Default = 0,
            /// <summary>Windows NT 3.5 互換プロバイダー</summary>
            WinNT35,
            /// <summary>NTLM プロバイダー</summary>
            WinNT40,
            /// <summary>Kerberos または NTLM ネゴシエーションプロバイダー</summary>
            WinNT50
        }

        private string _domainName = string.Empty;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _message = string.Empty;
        private int _returnCode = 0;
        private int _verbose = 0;
        private readonly int _debugThreshold = 6;
        private SafeAccessTokenHandle _safeTokenHandle = SafeAccessTokenHandle.InvalidHandle;
        private bool _disposed = false;

        /// <summary>
        /// 処理の実行結果コードを取得します。
        /// </summary>
        public int ReturnCode => _returnCode;

        /// <summary>
        /// 詳細ログ出力レベルを取得または設定します。
        /// </summary>
        public int Verbose { get => _verbose; set => _verbose = value; }

        /// <summary>
        /// ログオン対象のドメイン名を取得または設定します。
        /// </summary>
        public string DomainName { get => _domainName; set => _domainName = value; }

        /// <summary>
        /// ログオン対象のユーザー名を取得または設定します。
        /// </summary>
        public string Username { get => _username; set => _username = value; }

        /// <summary>
        /// ログオン対象のパスワードを取得または設定します。
        /// </summary>
        public string Password { get => _password; set => _password = value; }

        /// <summary>
        /// 実行結果のメッセージ文字列を取得します。
        /// </summary>
        public string Message { get => _message; set => _message = value; }

        /// <summary>
        /// <see cref="ClsLogon"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <example>
        /// <code>
        /// ClsLogon logon = new ClsLogon();
        /// </code>
        /// </example>
        public ClsLogon()
        {
        }

        /// <summary>
        /// 設定された資格情報でログオン処理を行い、指定されたアクションコントローラーを成り代わりコンテキスト下で実行します。
        /// </summary>
        /// <param name="actionController">成り代わりユーザー権限で実行するアクションコントローラーオブジェクト</param>
        /// <example>
        /// <code>
        /// ClsLogon logon = new ClsLogon();
        /// logon.DomainName = "MYDOMAIN";
        /// logon.Username = "AdminUser";
        /// logon.Password = "SecretPass";
        /// logon.Execute(actionController);
        /// </code>
        /// </example>
        /// <exception cref="ObjectDisposedException">オブジェクトが既に破棄されている場合に発生します。</exception>
        /// <exception cref="Win32Exception">Win32 ログオン処理に失敗した場合に発生します。</exception>
        public void Execute(ClsActionCtrl actionController)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_verbose > _debugThreshold)
            {
                Console.Out.WriteLine($"[ClsLogon.Execute()] Domain = {_domainName} Username = {_username} password = {_password}");
            }

            // Call LogonUser to obtain a handle to an access token.
            bool isOk = NativeMethods.LogonUser(
                _username,
                _domainName,
                _password,
                (int)LogonSessionType.NewCredentials,
                (int)LogonProvider.Default,
                out _safeTokenHandle
                );

            int win32Error = Marshal.GetLastWin32Error();

            if (isOk)
            {
                _message = $"OK ({win32Error}){new Win32Exception(win32Error).Message}".Trim();
                if (_verbose > _debugThreshold)
                {
                    Console.Out.WriteLine($"[ClsLogon.Execute()] LogonUser() : {_message}");
                }
            }
            else
            {
                _returnCode = MdlConst.LVL_E;
                _message = $"NG ({win32Error}){new Win32Exception(win32Error).Message}".Trim();
                if (_verbose > _debugThreshold)
                {
                    Console.Error.WriteLine($"[ClsLogon.Execute()] LogonUser() : {_message}");
                }
                throw new Win32Exception(win32Error);
            }

            // 偽造ユーザで処理を実行
            if (_verbose > _debugThreshold)
            {
                Console.Out.WriteLine("[ClsLogon.Execute()] T R Y : WindowsIdentity.RunImpersonated()");
            }

            WindowsIdentity.RunImpersonated(
                _safeTokenHandle,
                () =>
                {
                    if (_verbose > _debugThreshold)
                    {
                        Console.Out.WriteLine("[ClsLogon.Execute()] START : WindowsIdentity.RunImpersonated() -> ClsActionCtrl.Execute()");
                    }
                    _returnCode = actionController.Execute();
                    if (_verbose > _debugThreshold)
                    {
                        Console.Out.WriteLine($"[ClsLogon.Execute()] E N D : WindowsIdentity.RunImpersonated() -> ClsActionCtrl.Execute() -> RETURN CODE = {_returnCode}");
                    }
                }
            );
        }

        /// <summary>
        /// <see cref="ClsLogon"/> クラスによって使用されているすべてのリソースを解放します。
        /// </summary>
        /// <example>
        /// <code>
        /// logon.Dispose();
        /// </code>
        /// </example>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// アンマネージド リソースを解放し、オプションでマネージド リソースも解放します。
        /// </summary>
        /// <param name="disposing">マネージド リソースとアンマネージド リソースの両方を解放する場合は <c>true</c>。アンマネージド リソースのみを解放する場合は <c>false</c>。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_safeTokenHandle != null && !_safeTokenHandle.IsInvalid && !_safeTokenHandle.IsClosed)
                {
                    _safeTokenHandle.Dispose();
                    _safeTokenHandle = SafeAccessTokenHandle.InvalidHandle;
                    if (_verbose > _debugThreshold)
                    {
                        Console.Out.WriteLine("[ClsLogon.Dispose()] OK : SafeAccessTokenHandle.Dispose()");
                    }
                }
                _disposed = true;
            }
        }
    }
}
