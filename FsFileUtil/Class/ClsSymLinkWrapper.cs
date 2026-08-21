using CmnClsLib.Class;
using CmnClsLib.Module;
using CmnWinLib.Class;
using System.Runtime.Versioning;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace FsFileUtil.Class
{
    /// <summary>
    /// <see cref="ClsSymLinkWrapper"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="logger">ログ出力に使用する <see cref="ClsLogger"/> インスタンス。</param>
    /// <example>
    /// <code>
    /// ClsLogger logger = new ClsLogger();
    /// ClsSymLinkWrapper symLinkWrapper = new ClsSymLinkWrapper(logger);
    /// </code>
    /// </example>
    public class ClsSymLinkWrapper(ClsLogger logger)
    {
        private readonly ClsLogger _logger = logger;
        private readonly ClsSymLink? _symLink = OperatingSystem.IsWindows() ? new(logger) : null;
        private string _message = string.Empty;
        private string _realPath = string.Empty;
        private int _verbose = 0;
        private bool _isSilent = false;

        /// <summary>
        /// 最後に実行された処理のエラーメッセージまたは処理ログメッセージを取得または設定します。
        /// </summary>
        /// <returns>処理結果メッセージ文字列。</returns>
        /// <example>
        /// <code>
        /// string lastMessage = symLink.Message;
        /// </code>
        /// </example>
        public string Message { get => _message; set => _message = value; }

        /// <summary>
        /// 最後に取得されたシンボリックリンクの実際の参照先パスを取得または設定します。
        /// </summary>
        /// <returns>実パス文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.RealPath;
        /// </code>
        /// </example>
        public string RealPath { get => _realPath; set => _realPath = value; }

        /// <summary>
        /// ログ出力の詳細レベル（0: 非出力, 1: 最小, 2: 詳細, 3: デバッグ）を取得または設定します。
        /// </summary>
        /// <returns>ログ詳細レベルの値。</returns>
        /// <example>
        /// <code>
        /// symLink.Verbose = 2;
        /// </code>
        /// </example>
        public int Verbose { get => _verbose; set => _verbose = value; }

        /// <summary>
        /// ログ出力を完全に抑制するかどうかを示す値を取得または設定します。
        /// </summary>
        /// <returns>サイレントモードの場合は <c>true</c>。それ以外は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// symLink.IsSilent = true;
        /// </code>
        /// </example>
        public bool IsSilent { get => _isSilent; set => _isSilent = value; }

        /// <summary>
        /// 設定されたプロパティ値（Verbose, IsSilent, RealPath, Message）を内部のシンボリックリンク処理オブジェクトに反映・初期化します。
        /// </summary>
        /// <returns>戻り値はありません。</returns>
        /// <example>
        /// <code>
        /// symLink.Verbose = 2;
        /// symLink.Initialize();
        /// </code>
        /// </example>
        public void Initialize()
        {
            if (_symLink != null && OperatingSystem.IsWindows())
            {
                _symLink.Verbose = _verbose;
                _symLink.IsSilent = _isSilent;
                _symLink.RealPath = _realPath;
                _symLink.Message = _message;
            }
        }

        /// <summary>
        /// 指定されたレベルでログメッセージを出力します（サイレントモード時は出力されません）。
        /// </summary>
        /// <param name="level">ログレベル。</param>
        /// <param name="message">出力するメッセージ。</param>
        /// <returns>戻り値はありません。</returns>
        /// <example>
        /// <code>
        /// symLink.WriteLine(MdlConst.LVL_NONE, "処理を開始します。");
        /// </code>
        /// </example>
        public void WriteLine(int level, string message)
        {
            if (!_isSilent) _logger.WriteLine(level, message);
        }

        /// <summary>
        /// シンボリックリンクをコピーします（相対パス指定フラグ対応）。
        /// </summary>
        /// <param name="sourcePath">コピー元となるファイルまたはディレクトリのパス。</param>
        /// <param name="destinationPath">コピー先となるファイルまたはディレクトリのパス。</param>
        /// <param name="overwrite">コピー先が存在する場合に上書きする場合は <c>true</c>。上書きしない場合は <c>false</c>。</param>
        /// <param name="isRelative">実パスの取得・設定を相対パスとして処理する場合は <c>true</c>。</param>
        /// <returns>コピーおよびリンク生成が成功した場合は <c>true</c>。それ以外は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool result = symLink.Copy(@"C:\data\link.txt", @"C:\backup\link.txt", overwrite: true, isRelative: false);
        /// </code>
        /// </example>
        public bool Copy(string sourcePath, string destinationPath, bool overwrite, bool isRelative)
        {
            if (_symLink != null && OperatingSystem.IsWindows())
            {
                return _symLink.Copy(sourcePath, destinationPath, overwrite, isRelative);
            }
            return false;
        }

        /// <summary>
        /// シンボリックリンクをコピーします（絶対パス固定）。
        /// </summary>
        /// <param name="sourcePath">コピー元となるファイルまたはディレクトリのパス。</param>
        /// <param name="destinationPath">コピー先となるファイルまたはディレクトリのパス。</param>
        /// <param name="overwrite">コピー先が存在する場合に上書きする場合は <c>true</c>。上書きしない場合は <c>false</c>。</param>
        /// <returns>コピーおよびリンク生成が成功した場合は <c>true</c>。それ以外は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool result = symLink.Copy(@"C:\data\link.txt", @"C:\backup\link.txt", overwrite: true);
        /// </code>
        /// </example>
        public bool Copy(string sourcePath, string destinationPath, bool overwrite)
        {
            return Copy(sourcePath, destinationPath, overwrite, false);
        }

        /// <summary>
        /// 指定したパスにシンボリックリンクを作成します。
        /// </summary>
        /// <param name="linkPath">作成するシンボリックリンクのパス。</param>
        /// <param name="targetPath">リンク先のターゲットファイルまたはディレクトリのパス。</param>
        /// <param name="pathType">パス種別（<see cref="MdlFile.PATH_IS_DIRECTORY"/> または <see cref="MdlFile.PATH_IS_FILE"/>）。</param>
        /// <param name="overwrite">既存の同名リンクまたはファイルを削除して作成し直す場合は <c>true</c>。</param>
        /// <returns>作成が成功した場合は <c>true</c>。失敗した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool result = symLink.CreateSymbolicLink(@"C:\link_dir", @"D:\real_dir", MdlFile.PATH_IS_DIRECTORY, overwrite: true);
        /// </code>
        /// </example>
        public bool CreateSymbolicLink(string linkPath, string targetPath, int pathType, bool overwrite)
        {
            if (_symLink != null && OperatingSystem.IsWindows())
            {
                return _symLink.CreateSymbolicLink(linkPath, targetPath, pathType, overwrite);
            }
            return false;
        }

        /// <summary>
        /// 指定されたパスのシンボリックリンクまたはファイル・ディレクトリを再帰的に削除します。
        /// </summary>
        /// <param name="linkPath">削除対象のパス。</param>
        /// <returns>削除が成功した場合は <c>true</c>。失敗した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool deleted = symLink.Delete(@"C:\link.txt");
        /// </code>
        /// </example>
        public bool Delete(string linkPath)
        {
            return MdlFile.DeleteRecursively(linkPath);
        }

        /// <summary>
        /// ファイル・ディレクトリが存在し、かつシンボリックリンクである場合にその参照先の実パスを取得します（相対パス変換指定可能）。
        /// </summary>
        /// <param name="linkPath">対象のシンボリックリンクパス。</param>
        /// <param name="isRelative">取得パスを相対パスに変換する場合は <c>true</c>。</param>
        /// <returns>実パス文字列。ファイルが存在しない場合またはシンボリックリンクでない場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.GetRealPathIfExists(@"C:\link.txt", isRelative: false);
        /// </code>
        /// </example>
        public string GetRealPathIfExists(string linkPath, bool isRelative)
        {
            bool isSuccess = true;
            _message = string.Empty;
            if (string.IsNullOrWhiteSpace(linkPath)) return string.Empty;

            switch (MdlFile.GetPathType(linkPath))
            {
                case MdlFile.PATH_IS_DIRECTORY:
                case MdlFile.PATH_IS_FILE:
                    break;
                default:
                    isSuccess = false;
                    _message = $" => ERROR : ClsSymLink.GetRealPathIfExists() : NO SUCH A FILE OR DIRECTORY : {linkPath}";
                    if (_verbose > 1) WriteLine(MdlConst.LVL_NONE, _message);
                    break;
            }
            if (!isSuccess) return string.Empty;
            if (!MdlFile.IsSymlink(linkPath)) return string.Empty;
            return GetRealPath(linkPath, isRelative);
        }

        /// <summary>
        /// ファイル・ディレクトリが存在し、かつシンボリックリンクである場合にその参照先の実パスを取得します（絶対パス固定）。
        /// </summary>
        /// <param name="linkPath">対象のシンボリックリンクパス。</param>
        /// <returns>実パス文字列。ファイルが存在しない場合またはシンボリックリンクでない場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.GetRealPathIfExists(@"C:\link.txt");
        /// </code>
        /// </example>
        public string GetRealPathIfExists(string linkPath)
        {
            return GetRealPathIfExists(linkPath, false);
        }

        /// <summary>
        /// 指定されたシンボリックリンクが参照しているターゲットの実パスを取得します（相対パス指定可能）。
        /// </summary>
        /// <param name="linkPath">対象のシンボリックリンクパス。</param>
        /// <param name="isRelative">結果を相対パスに変換する場合は <c>true</c>。</param>
        /// <returns>実パス文字列。取得に失敗した場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.GetRealPath(@"C:\link.txt", isRelative: true);
        /// </code>
        /// </example>
        public string GetRealPath(string linkPath, bool isRelative)
        {
            if (_symLink != null && OperatingSystem.IsWindows())
            {
                return _symLink.GetRealPath(linkPath, isRelative);
            }
            return string.Empty;
        }

        /// <summary>
        /// 指定されたシンボリックリンクが参照しているターゲットの実パスを取得します（絶対パス固定）。
        /// </summary>
        /// <param name="linkPath">対象のシンボリックリンクパス。</param>
        /// <returns>実パス文字列。取得に失敗した場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.GetRealPath(@"C:\link.txt");
        /// </code>
        /// </example>
        public string GetRealPath(string linkPath)
        {
            return GetRealPath(linkPath, false);
        }
    }
}
