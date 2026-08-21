using CmnClsLib.Class;
using CmnClsLib.Module;
using CmnWinLib.Class;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace FsFileUtil.Class
{
    /// <summary>
    /// ファイルシステム上の各種操作（コピー、移動、リネーム、ローテーション、待機など）を行うユーティリティクラス。
    /// </summary>
    public class ClsFsUtil
    {
        // 変数
        private ClsLogger _logger;
        private string _message = "";               // メッセージ
        private string _result = "";                // 最終表示メッセージ
        private bool _isStackTrace = false;         // スタックトレースフラグ
        private int _verbose = 0;                   // 冗長出力レベル
        private int _waitMSecForRetryCopy = 200;    // 待ち
        private int _retryMax = 0;                  // リトライ

        /// <summary>
        /// <see cref="ClsFsUtil"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="logger">ログ出力用のロガーインスタンス</param>
        /// <example>
        /// <code>
        /// var logger = new ClsLogger(@"C:\logs\app.log");
        /// var fsUtil = new ClsFsUtil(logger);
        /// </code>
        /// </example>
        public ClsFsUtil(ClsLogger logger)
        {
            _logger = logger;
        }

        // プロパティ
        /// <summary>
        /// 内部で発生した処理メッセージを取得または設定します。
        /// </summary>
        /// <value>処理状況メッセージ文字列</value>
        /// <example>
        /// <code>
        /// string msg = fsUtil.Message;
        /// </code>
        /// </example>
        public string Message { get => _message; set => _message = value; }

        /// <summary>
        /// 処理の最終実行結果メッセージを取得または設定します。
        /// </summary>
        /// <value>実行結果文字列</value>
        /// <example>
        /// <code>
        /// string result = fsUtil.Result;
        /// </code>
        /// </example>
        public string Result { get => _result; set => _result = value; }

        /// <summary>
        /// 例外発生時にスタックトレースをログ出力するかどうかを取得または設定します。
        /// </summary>
        /// <value>スタックトレースを出力する場合は true、それ以外は false</value>
        /// <example>
        /// <code>
        /// fsUtil.IsStackTrace = true;
        /// </code>
        /// </example>
        public bool IsStackTrace { get => _isStackTrace; set => _isStackTrace = value; }

        /// <summary>
        /// ログ出力の冗長レベルを取得または設定します。
        /// </summary>
        /// <value>冗長レベルを示す数値</value>
        /// <example>
        /// <code>
        /// fsUtil.Verbose = 2;
        /// </code>
        /// </example>
        public int Verbose { get => _verbose; set => _verbose = value; }

        /// <summary>
        /// ファイルコピー時のリトライ待機時間（ミリ秒）を取得または設定します。
        /// </summary>
        /// <value>待機時間（ミリ秒）</value>
        /// <example>
        /// <code>
        /// fsUtil.WaitMSecForRetryCopy = 500;
        /// </code>
        /// </example>
        public int WaitMSecForRetryCopy { get => _waitMSecForRetryCopy; set => _waitMSecForRetryCopy = value; }

        /// <summary>
        /// ファイルコピー時の最大リトライ回数を取得または設定します。
        /// </summary>
        /// <value>最大リトライ回数</value>
        /// <example>
        /// <code>
        /// fsUtil.RetryMax = 3;
        /// </code>
        /// </example>
        public int RetryMax { get => _retryMax; set => _retryMax = value; }

        /// <summary>
        /// 指定されたパスのファイルをローテーションします。
        /// </summary>
        /// <param name="path">ローテーションするファイルのパス</param>
        /// <param name="keepMax">保持するファイルの最大世代数</param>
        /// <returns>操作結果を示すステータス値（MdlConst.LVL_I: 成功、MdlConst.LVL_E: 失敗）</returns>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger);
        /// int status = fsUtil.Rotate(@"C:\logs\app.log", 5);
        /// </code>
        /// </example>
        public int Rotate(string path, int keepMax)
        {
            ArgumentNullException.ThrowIfNull(path);
            _message = "";
            int returnCode = MdlFile.DeleteRecursively($"{path}.{keepMax}") ? MdlConst.LVL_I : MdlConst.LVL_E;
            if (returnCode == MdlConst.LVL_I)
            {
                for (int i = 0; i < keepMax; i++)
                {
                    int suffixNo = keepMax - i;
                    string sourcePath = suffixNo == 1 ? path : $"{path}.{suffixNo - 1}";
                    string destinationPath = $"{path}.{suffixNo}";
                    if (!Rename(sourcePath, destinationPath))
                    {
                        returnCode = MdlConst.LVL_E;
                        break;
                    }
                }
            }
            else
            {
                _logger?.WriteLine(MdlConst.LVL_NONE, $"NG : DELETE {path}.{keepMax}");
                returnCode = MdlConst.LVL_E;
            }
            return returnCode;
        }

        /// <summary>
        /// 指定されたファイルが存在するまで待機します。
        /// </summary>
        /// <param name="path">確認対象のファイルパス</param>
        /// <param name="maxLoop">最大確認回数</param>
        /// <param name="interval">ループ間の待機秒数</param>
        /// <returns>ファイルが存在する場合は true、存在しない場合は false</returns>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger);
        /// bool found = fsUtil.WaitUntilFileExists(@"C:\temp\target.txt", 5, 1);
        /// </code>
        /// </example>
        public bool WaitUntilFileExists(string path, int maxLoop, int interval)
        {
            return WaitUntilFileExists(path, maxLoop, interval, false);
        }

        /// <summary>
        /// 指定されたファイルが存在するまで待機します。ファイルロックの判定フラグも指定可能です。
        /// </summary>
        /// <param name="path">確認対象のファイルパス</param>
        /// <param name="maxLoop">最大確認回数</param>
        /// <param name="interval">ループ間の待機秒数</param>
        /// <param name="checkFileLock">ファイルロック状態も判定する場合は true</param>
        /// <returns>ファイルが存在し、かつロックされていない場合は true、それ以外は false</returns>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger);
        /// bool foundAndUnlocked = fsUtil.WaitUntilFileExists(@"C:\temp\target.txt", 5, 1, true);
        /// </code>
        /// </example>
        public bool WaitUntilFileExists(string path, int maxLoop, int interval, bool checkFileLock)
        {
            ArgumentNullException.ThrowIfNull(path);
            bool isOk = false;
            if (maxLoop < 1) maxLoop = 1;
            for (int i = 0; i < maxLoop; i++)
            {
                if (MdlFile.PathExists(path))
                {
                    if (checkFileLock && MdlFile.IsFileLocked(path))
                    {
                        _logger?.WriteLine(MdlConst.LVL_NONE, $" => [{i + 1}][--] LOCKED    : {path}");
                    }
                    else
                    {
                        _logger?.WriteLine(MdlConst.LVL_NONE, $" => [{i + 1}][OK] FOUND     : {path}");
                        isOk = true;
                        break;
                    }
                }
                else
                {
                    _logger?.WriteLine(MdlConst.LVL_NONE, $" => [{i + 1}][--] NOT FOUND");
                }
                if (i < maxLoop - 1) Thread.Sleep(interval * 1000);
            }
            return isOk;
        }

        /// <summary>
        /// ディレクトリまたはファイルの名前を変更（移動）します。
        /// </summary>
        /// <param name="sourcePath">移動元のディレクトリまたはファイルパス</param>
        /// <param name="destinationPath">移動先のディレクトリまたはファイルパス</param>
        /// <returns>移動処理が正常に完了した場合は true、例外等が発生した場合は false</returns>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger);
        /// bool success = fsUtil.Rename(@"C:\temp\old.txt", @"C:\temp\new.txt");
        /// </code>
        /// </example>
        public bool Rename(string sourcePath, string destinationPath)
        {
            ArgumentNullException.ThrowIfNull(sourcePath);
            ArgumentNullException.ThrowIfNull(destinationPath);
            bool isOk = true;
            try
            {
                _logger?.WriteLine(MdlConst.LVL_NONE, $"TRY : MOVE : {sourcePath} => {destinationPath}");
                int check = MdlFile.GetPathType(sourcePath);
                switch (check)
                {
                    case MdlFile.PATH_IS_DIRECTORY:
                        Directory.Move(sourcePath, destinationPath);
                        _logger?.WriteLine(MdlConst.LVL_NONE, " -> OK : MOVED THE DIRECTORY");
                        break;
                    case MdlFile.PATH_IS_FILE:
                        File.Move(sourcePath, destinationPath);
                        _logger?.WriteLine(MdlConst.LVL_NONE, " -> OK : MOVED THE FILE");
                        break;
                    default:
                        _logger?.WriteLine(MdlConst.LVL_NONE, $" -> SKIP : NOT FOUND({check})");
                        break;
                }
            }
            catch (Exception ex)
            {
                isOk = false;
                if (_logger is not null)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, $" -> EXCEPTION : {ex.Message}");
                    if (_isStackTrace)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                        _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                    }
                }
            }
            return isOk;
        }

        /// <summary>
        /// 指定されたファイルの SHA-1 ハッシュ値を取得します。
        /// </summary>
        /// <param name="path">ハッシュ値を計算する対象ファイルのパス</param>
        /// <returns>SHA-1 ハッシュ文字列（例外発生時は空文字列）</returns>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger);
        /// string hash = fsUtil.ComputeSha1Hash(@"C:\temp\data.bin");
        /// </code>
        /// </example>
        public string ComputeSha1Hash(string path)
        {
            ArgumentNullException.ThrowIfNull(path);
            string result = "";
            try
            {
                result = MdlFile.ComputeSha1Hash(path);
            }
            catch (Exception ex)
            {
                if (_logger is not null)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, $"NG : FAILED TO GET SHA1 : {path} => {ex.Message}");
                    if (_isStackTrace)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                        _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// コンソールに進捗表示を行いつつ、バイナリファイルをコピーします。ファイル共有モードの指定が可能です。
        /// </summary>
        /// <param name="sourcePath">コピー元のファイルパス</param>
        /// <param name="destinationPath">コピー先のファイルパス</param>
        /// <param name="fileShare">コピー元ストリームのファイル共有モード</param>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger);
        /// fsUtil.BinaryCopyWithProgress(@"C:\src.iso", @"D:\dst.iso", FileShare.Read);
        /// </code>
        /// </example>
        public void BinaryCopyWithProgress(string sourcePath, string destinationPath, FileShare fileShare)
        {
            BinaryCopy(sourcePath, destinationPath, true, fileShare);
        }

        /// <summary>
        /// コンソールに進捗表示を行いつつ、バイナリファイルをコピーします。
        /// </summary>
        /// <param name="sourcePath">コピー元のファイルパス</param>
        /// <param name="destinationPath">コピー先のファイルパス</param>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger);
        /// fsUtil.BinaryCopyWithProgress(@"C:\src.iso", @"D:\dst.iso");
        /// </code>
        /// </example>
        public void BinaryCopyWithProgress(string sourcePath, string destinationPath)
        {
            BinaryCopyWithProgress(sourcePath, destinationPath, FileShare.ReadWrite);
        }

        /// <summary>
        /// コンソールのカーソルの表示／非表示を設定します。
        /// </summary>
        /// <param name="isVisible">カーソルを表示する場合は true、非表示にする場合は false</param>
        /// <example>
        /// <code>
        /// fsUtil.SetCursorVisible(false);
        /// </code>
        /// </example>
        public void SetCursorVisible(bool isVisible)
        {
            try
            {
                Console.CursorVisible = isVisible;
            }
            catch { }
        }

        /// <summary>
        /// 指定されたファイルをロックしているプロセス一覧を出力します。（非推奨／未サポート）
        /// </summary>
        /// <param name="path">確認対象のファイルパス</param>
        /// <returns>エラーレベルの定数（MdlConst.LVL_E）</returns>
        /// <example>
        /// <code>
        /// int locks = fsUtil.WhoIsLocking(@"C:\temp\locked.txt");
        /// </code>
        /// </example>
        public int WhoIsLocking(string path)
        {
            _logger?.WriteLine(MdlConst.LVL_E, "This feature is no longer supported.");
            return MdlConst.LVL_E;
        }

        /// <summary>
        /// 設定されたリトライ回数および待機時間に基づいて、ファイルをコピーします。
        /// </summary>
        /// <param name="sourcePath">コピー元のファイルパス</param>
        /// <param name="destinationPath">コピー先のファイルパス</param>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger) { RetryMax = 3, WaitMSecForRetryCopy = 500 };
        /// fsUtil.CopyFileWithRetry(@"C:\src.txt", @"D:\dst.txt");
        /// </code>
        /// </example>
        public void CopyFileWithRetry(string sourcePath, string destinationPath)
        {
            ArgumentNullException.ThrowIfNull(sourcePath);
            ArgumentNullException.ThrowIfNull(destinationPath);
            bool isBreak = false;
            for (int i = 0; i < _retryMax + 1; i++)
            {
                if (isBreak) break;
                try
                {
                    _logger?.WriteLine(MdlConst.LVL_NONE, $" -> TRY {i}/{_retryMax} System.IO.File.Copy({sourcePath}, {destinationPath}, true)");
                    File.Copy(sourcePath, destinationPath, overwrite: true);
                    break;
                }
                catch
                {
                    if (i < _retryMax)
                    {
                        _logger?.WriteLine(MdlConst.LVL_NONE, $" -> RETRY SLEEP({_waitMSecForRetryCopy})");
                        Thread.Sleep(_waitMSecForRetryCopy);
                    }
                    else
                    {
                        isBreak = true;
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// バイナリファイルをコピーします。進捗表示有無や共有モードのカスタマイズが可能です。
        /// </summary>
        /// <param name="sourcePath">コピー元のファイルパス</param>
        /// <param name="destinationPath">コピー先のファイルパス</param>
        /// <param name="showProgress">進捗表示を表示する場合は true</param>
        /// <param name="fileShare">コピー元のファイル共有モード</param>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger);
        /// fsUtil.BinaryCopy(@"C:\large.bin", @"D:\large.bin", true, FileShare.Read);
        /// </code>
        /// </example>
        public void BinaryCopy(string sourcePath, string destinationPath, bool showProgress, FileShare fileShare)
        {
            ArgumentNullException.ThrowIfNull(sourcePath);
            ArgumentNullException.ThrowIfNull(destinationPath);
            ClsFsAsyncCopyStatus asyncCpStatus = new ClsFsAsyncCopyStatus(sourcePath, destinationPath, false, fileShare);
            long totalCount = 0;
            bool isException = false;
            _message = "[ClsFsUtil.BinaryCopy()] Called";
            _result = "";
            BinaryReader? br = null;
            BinaryWriter? bw = null;
            if (showProgress) SetCursorVisible(false);                     // カーソル表示OFF
            if (asyncCpStatus.IsOk && asyncCpStatus.SourceStream != null && asyncCpStatus.DestinationStream != null)
            {
                try
                {
                    asyncCpStatus.IsShowProgress = showProgress;
                    _message = "[ClsFsUtil.BinaryCopy()] BinaryReader r = new BinaryReader(asyncCpStatus.SourceStream)";
                    br = new BinaryReader(asyncCpStatus.SourceStream);
                    _message = "[ClsFsUtil.BinaryCopy()] BinaryWriter w = new BinaryWriter(asyncCpStatus.DestinationStream)";
                    bw = new BinaryWriter(asyncCpStatus.DestinationStream);
                    _message = $"[ClsFsUtil.BinaryCopy()][LN={asyncCpStatus.FileSize}][CNT={totalCount}] r.ReadBytes({asyncCpStatus.Buffer.Length})";
                    byte[] b = br.ReadBytes(asyncCpStatus.Buffer.Length);
                    // ループ処理：指定バイト数毎のコピー処理
                    while (b.Length > 0)
                    {
                        bw.Write(b);
                        asyncCpStatus.CurrentCount++;
                        totalCount++;
                        _message = $"[ClsFsUtil.BinaryCopy()][LN={asyncCpStatus.FileSize}][CNT={totalCount}] r.ReadBytes({asyncCpStatus.Buffer.Length})";
                        b = br.ReadBytes(asyncCpStatus.Buffer.Length);
                        if (showProgress && asyncCpStatus.CheckCount > 0 && asyncCpStatus.CurrentCount >= asyncCpStatus.CheckCount) asyncCpStatus.ShowProgress();
                    }
                    if (showProgress) asyncCpStatus.ShowProgress();
                }
                catch (Exception objExcptn)
                {
                    isException = true;
                    if (_verbose > 1 && _logger is not null)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, $" -> EXCEPTION 2 : ClsFsUtil.BinaryCopy({sourcePath}, {destinationPath}) : {objExcptn.Message}");
                    }
                    if (_isStackTrace && _logger is not null)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                        _logger.WriteLine(MdlConst.LVL_NONE, "---[STACKTRACE]---");
                        _logger.WriteLine(MdlConst.LVL_NONE, asyncCpStatus.StackTrace);
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                    }
                }
                finally
                {
                    asyncCpStatus.Close();
                    if (showProgress) SetCursorVisible(true);
                    _result = asyncCpStatus.ProgressLine;
                    br?.Close();
                    bw?.Close();
                }
                if (isException)
                {
                    Thread.Sleep(_waitMSecForRetryCopy);
                    CopyFileWithRetry(sourcePath, destinationPath);
                }
            }
            else
            {
                asyncCpStatus.Close();
                if (showProgress) SetCursorVisible(true);
                _result = asyncCpStatus.ProgressLine;
                if (_verbose > 1 && _logger is not null)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, $" -> EXCEPTION 1 : ClsFsUtil.BinaryCopy({sourcePath}, {destinationPath}) : {asyncCpStatus.Message}");
                }
                if (_isStackTrace && _logger is not null)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "---[STACKTRACE]---");
                    _logger.WriteLine(MdlConst.LVL_NONE, asyncCpStatus.StackTrace);
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
                Thread.Sleep(_waitMSecForRetryCopy);
                CopyFileWithRetry(sourcePath, destinationPath);
            }
        }

        /// <summary>
        /// バイナリファイルをコピーします。
        /// </summary>
        /// <param name="sourcePath">コピー元のファイルパス</param>
        /// <param name="destinationPath">コピー先のファイルパス</param>
        /// <param name="showProgress">進捗表示を表示する場合は true</param>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger);
        /// fsUtil.BinaryCopy(@"C:\large.bin", @"D:\large.bin", true);
        /// </code>
        /// </example>
        public void BinaryCopy(string sourcePath, string destinationPath, bool showProgress)
        {
            BinaryCopy(sourcePath, destinationPath, showProgress, FileShare.ReadWrite);
        }

        /// <summary>
        /// 非同期でファイルをコピーします。
        /// </summary>
        /// <param name="sourcePath">コピー元のファイルパス</param>
        /// <param name="destinationPath">コピー先のファイルパス</param>
        /// <param name="showProgress">進捗表示を表示する場合は true</param>
        /// <param name="fileShare">コピー元のファイル共有モード</param>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger);
        /// fsUtil.AsyncCopy(@"C:\src.dat", @"D:\dst.dat", true, FileShare.ReadWrite);
        /// </code>
        /// </example>
        public void AsyncCopy(string sourcePath, string destinationPath, bool showProgress, FileShare fileShare)
        {
            ArgumentNullException.ThrowIfNull(sourcePath);
            ArgumentNullException.ThrowIfNull(destinationPath);
            ClsFsAsyncCopyStatus? asyncCpStatus = null;
            bool isException = false;
            if (showProgress) SetCursorVisible(false);
            asyncCpStatus = new ClsFsAsyncCopyStatus(sourcePath, destinationPath, true, fileShare);
            if (asyncCpStatus.IsOk)
            {
                try
                {
                    asyncCpStatus.IsShowProgress = showProgress;
                    if (asyncCpStatus.SourceStream != null && asyncCpStatus.SourceStream.CanRead)
                    {
                        asyncCpStatus.SourceStream.BeginRead(
                            asyncCpStatus.Buffer,
                            0,
                            asyncCpStatus.Buffer.Length,
                            AsyncCopyReadCallback,
                            asyncCpStatus);
                    }
                    while (!asyncCpStatus.IsDone)
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception objExcptn)
                {
                    isException = true;
                    if (_verbose > 1 && _logger is not null)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, $" -> EXCEPTION 2 : ClsFsUtil.AsyncCopy({sourcePath}, {destinationPath}) : {objExcptn.Message}");
                    }
                    if (_isStackTrace && _logger is not null)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                        _logger.WriteLine(MdlConst.LVL_NONE, "---[STACKTRACE]---");
                        _logger.WriteLine(MdlConst.LVL_NONE, asyncCpStatus.StackTrace);
                        _logger.WriteLine(MdlConst.LVL_NONE, "");
                    }
                }
                finally
                {
                    asyncCpStatus.Close();
                    if (showProgress) SetCursorVisible(true);
                    _result = asyncCpStatus.ProgressLine;
                }
                if (isException)
                {
                    Thread.Sleep(_waitMSecForRetryCopy);
                    CopyFileWithRetry(sourcePath, destinationPath);
                }
            }
            else
            {
                asyncCpStatus.Close();
                if (showProgress) SetCursorVisible(true);
                _result = asyncCpStatus.ProgressLine;
                if (_verbose > 1 && _logger is not null)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, $" -> EXCEPTION 1 : ClsFsUtil.AsyncCopy({sourcePath}, {destinationPath}) : {asyncCpStatus.Message}");
                }
                if (_isStackTrace && _logger is not null)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "---[STACKTRACE]---");
                    _logger.WriteLine(MdlConst.LVL_NONE, asyncCpStatus.StackTrace);
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, $"---[LOCK FILE LIST : {sourcePath}]---");
                    int numOfLocks = WhoIsLocking(sourcePath);
                    _logger.WriteLine(MdlConst.LVL_NONE, $"ロックファイル数 = {numOfLocks}");
                    _logger.WriteLine(MdlConst.LVL_NONE, $"---[LOCK FILE LIST : {destinationPath}]---");
                    numOfLocks = WhoIsLocking(destinationPath);
                    _logger.WriteLine(MdlConst.LVL_NONE, $"ロックファイル数 = {numOfLocks}");
                    _logger.WriteLine(MdlConst.LVL_NONE, "----------");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
                Thread.Sleep(_waitMSecForRetryCopy);
                CopyFileWithRetry(sourcePath, destinationPath);
            }
        }

        /// <summary>
        /// 非同期でファイルをコピーします。
        /// </summary>
        /// <param name="sourcePath">コピー元のファイルパス</param>
        /// <param name="destinationPath">コピー先のファイルパス</param>
        /// <param name="showProgress">進捗表示を表示する場合は true</param>
        /// <example>
        /// <code>
        /// var fsUtil = new ClsFsUtil(logger);
        /// fsUtil.AsyncCopy(@"C:\src.dat", @"D:\dst.dat", true);
        /// </code>
        /// </example>
        public void AsyncCopy(string sourcePath, string destinationPath, bool showProgress)
        {
            AsyncCopy(sourcePath, destinationPath, showProgress, FileShare.ReadWrite);
        }

        /// <summary>
        /// 非同期コピーの読み取り処理用コールバックメソッド。
        /// </summary>
        /// <param name="objResult">非同期操作の結果状態情報</param>
        /// <example>
        /// <code>
        /// // 内部非同期コールバックとして自動呼出しされます。
        /// </code>
        /// </example>
        private static void AsyncCopyReadCallback(IAsyncResult objResult)
        {
            if (objResult.IsCompleted)
            {
                if (objResult.AsyncState is ClsFsAsyncCopyStatus asyncCpStatus)
                {
                    try
                    {
                        if (asyncCpStatus.IsShowProgress)
                        {
                            asyncCpStatus.CurrentCount++;
                            if (asyncCpStatus.CurrentCount >= asyncCpStatus.CheckCount) asyncCpStatus.ShowProgress();
                        }
                        if (asyncCpStatus.SourceStream != null && asyncCpStatus.DestinationStream != null)
                        {
                            long curCpLength = asyncCpStatus.Buffer.Length;
                            if (asyncCpStatus.SourceStream.Length - asyncCpStatus.Buffer.Length < asyncCpStatus.DestinationStream.Length)
                            {
                                curCpLength = asyncCpStatus.SourceStream.Length - asyncCpStatus.DestinationStream.Length;
                            }
                            asyncCpStatus.DestinationStream.BeginWrite(
                                asyncCpStatus.Buffer,
                                0,
                                (int)curCpLength,
                                AsyncCopyWriteCallback,
                                asyncCpStatus);
                        }
                    }
                    catch
                    {
                        asyncCpStatus.Close();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 非同期コピーの書き込み処理用コールバックメソッド。
        /// </summary>
        /// <param name="objResult">非同期操作の結果状態情報</param>
        /// <example>
        /// <code>
        /// // 内部非同期コールバックとして自動呼出しされます。
        /// </code>
        /// </example>
        private static void AsyncCopyWriteCallback(IAsyncResult objResult)
        {
            if (objResult.IsCompleted)
            {
                if (objResult.AsyncState is ClsFsAsyncCopyStatus asyncCpStatus)
                {
                    try
                    {
                        if (asyncCpStatus.DestinationStream != null && asyncCpStatus.SourceStream != null)
                        {
                            asyncCpStatus.DestinationStream.Flush();
                            if (asyncCpStatus.SourceStream.Length > asyncCpStatus.SourceStream.Position)
                            {
                                asyncCpStatus.SourceStream.BeginRead(
                                    asyncCpStatus.Buffer,
                                    0,
                                    asyncCpStatus.Buffer.Length,
                                    AsyncCopyReadCallback, asyncCpStatus);
                            }
                            else
                            {
                                asyncCpStatus.Close();
                            }
                        }
                        else
                        {
                            asyncCpStatus.Close();
                        }
                    }
                    catch
                    {
                        asyncCpStatus.Close();
                        throw;
                    }
                }
            }
        }

    }
}

