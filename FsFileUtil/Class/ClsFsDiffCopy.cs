
using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Module;
using CmnWinLib.Class;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace FsFileUtil.Class
{
    public class ClsFsDiffCopy
    {
        private ClsLogger _logger;
        private ClsProp _prop;
        private ClsFsUtil _fsUtil;
        private ClsSymLinkWrapper _symLink;
        private ulong _copyNewCount = 0;
        private ulong _copyUpdateCount = 0;
        private ulong _copySkipCount = 0;
        private ulong _copyErrorCount = 0;
        private ulong _copyTotalCount = 0;
        private ulong _rmOkCount = 0;
        private ulong _rmNgCount = 0;
        private ulong _rmSkipCount = 0;
        private ulong _rmTotalCount = 0;
        private ulong _mkdirOkCount = 0;
        private ulong _mkdirNgCount = 0;
        private ulong _notFoundCount = 0;

        /// <summary>
        /// <see cref="ClsFsDiffCopy"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="logger">ログ出力を行うロガーオブジェクト</param>
        /// <param name="prop">動作設定を保持するプロパティオブジェクト</param>
        /// <param name="fsUtil">ファイルシステム操作ユーティリティ</param>
        /// <param name="symLink">シンボリックリンク操作オブジェクト</param>
        /// <example>
        /// <code>
        /// var diffCopy = new ClsFsDiffCopy(logger, prop, fsUtil, symLink);
        /// </code>
        /// </example>
        public ClsFsDiffCopy(ClsLogger logger, ClsProp prop, ClsFsUtil fsUtil, ClsSymLinkWrapper symLink)
        {
            _logger = logger;
            _prop = prop;
            _fsUtil = fsUtil;
            _symLink = symLink;
        }

        public ClsProp Properties { get => _prop; set => _prop = value; }
        public ulong CopyNewCount { get => _copyNewCount; set => _copyNewCount = value; }
        public ulong CopyUpdateCount { get => _copyUpdateCount; set => _copyUpdateCount = value; }
        public ulong CopySkipCount { get => _copySkipCount; set => _copySkipCount = value; }
        public ulong CopyErrorCount { get => _copyErrorCount; set => _copyErrorCount = value; }
        public ulong CopyTotalCount { get => _copyTotalCount; set => _copyTotalCount = value; }
        public ulong RmOkCount { get => _rmOkCount; set => _rmOkCount = value; }
        public ulong RmNgCount { get => _rmNgCount; set => _rmNgCount = value; }
        public ulong RmSkipCount { get => _rmSkipCount; set => _rmSkipCount = value; }
        public ulong RmTotalCount { get => _rmTotalCount; set => _rmTotalCount = value; }
        public ulong MkdirOkCount { get => _mkdirOkCount; set => _mkdirOkCount = value; }
        public ulong MkdirNgCount { get => _mkdirNgCount; set => _mkdirNgCount = value; }
        public ulong NotFoundCount { get => _notFoundCount; set => _notFoundCount = value; }

        /// <summary>
        /// 指定されたソースパスおよび宛先パスに基づいて、ファイルまたはディレクトリのコピーを実行します。
        /// </summary>
        /// <param name="sourcePath">コピー元の絶対パス</param>
        /// <param name="destinationPath">コピー先の絶対パス</param>
        /// <param name="relativePath">対象の相対パス</param>
        /// <param name="pathType">パス種別（<see cref="MdlFile.PATH_IS_DIRECTORY"/> または <see cref="MdlFile.PATH_IS_FILE"/>）</param>
        /// <param name="isSymLink">シンボリックリンク処理判定（-1: 自動検出, 0: 対象外, 1: シンボリックリンクとして処理）</param>
        /// <returns>コピー処理が成功した場合は <c>true</c>。失敗した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool isSuccess = diffCopy.Copy(@"C:\Source\file.txt", @"D:\Dest\file.txt", "file.txt", MdlFile.PATH_IS_FILE, -1);
        /// </code>
        /// </example>
        public bool Copy(String sourcePath, String destinationPath, String relativePath, int pathType, int isSymLink)
        {
            bool isOk = true;
            bool isSymLinkFlag = false;
            if (_prop.IsSymLink)
            {
                isSymLinkFlag = isSymLink switch
                {
                    -1 => MdlFile.IsSymlink(sourcePath),
                    0 => false,
                    _ => true
                };
            }
            switch (pathType)
            {
                case MdlFile.PATH_IS_DIRECTORY:
                    if (_prop.IsAlwaysMkDir)
                    {
                        if (isSymLinkFlag)
                        {
                            _copyTotalCount++;
                            if (_prop.Verbose > 4) _logger.WriteLine(MdlConst.LVL_NONE, $"[TRY] MkLink({sourcePath}, {destinationPath}, {relativePath}, MdlFile.PATH_IS_DIRECTORY)");
                            isOk = MkLink(sourcePath, destinationPath, relativePath, MdlFile.PATH_IS_DIRECTORY);
                        }
                        else
                        {
                            if (_prop.Verbose > 5) _logger.WriteLine(MdlConst.LVL_NONE, $"[TRY] Mkdir({sourcePath}, {destinationPath}, {relativePath}, MdlFile.PATH_IS_DIRECTORY)");
                            isOk = Mkdir(sourcePath, destinationPath, relativePath);
                        }
                    }
                    break;
                case MdlFile.PATH_IS_FILE:
                    if (_prop.IsFileCopy)
                    {
                        _copyTotalCount++;
                        if (isSymLinkFlag)
                        {
                            if (_prop.Verbose > 4) _logger.WriteLine(MdlConst.LVL_NONE, $"[TRY] MkLink({sourcePath}, {destinationPath}, {relativePath}, MdlFile.PATH_IS_FILE)");
                            isOk = MkLink(sourcePath, destinationPath, relativePath, MdlFile.PATH_IS_FILE);
                        }
                        else
                        {
                            if (_prop.Verbose > 5) _logger.WriteLine(MdlConst.LVL_NONE, $"[TRY] DiffCopyFileMain({sourcePath}, {destinationPath}, {relativePath}, MdlFile.PATH_IS_FILE)");
                            isOk = DiffCopyFileMain(sourcePath, destinationPath, relativePath);
                        }
                    }
                    break;
            }
            return isOk;
        }

        /// <summary>
        /// 指定された宛先パスにディレクトリを作成します。
        /// </summary>
        /// <param name="sourcePath">ソースディレクトリのパス</param>
        /// <param name="destinationPath">作成対象のディレクトリパス</param>
        /// <param name="relativePath">相対パス</param>
        /// <returns>ディレクトリ作成（または既存確認）が正常に完了した場合は <c>true</c>。失敗した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool created = diffCopy.Mkdir(@"C:\Source\Dir", @"D:\Dest\Dir", "Dir");
        /// </code>
        /// </example>
        public bool Mkdir(String sourcePath, String destinationPath, String relativePath)
        {
            bool isOk = true;
            if (!_prop.IsList)
            {
                switch (MdlFile.CreateDirectory(destinationPath))
                {
                    case MdlFile.OK_MKDIR_ALREADY_EXIST:
                        SetDateToDir(sourcePath, destinationPath, relativePath, "---");
                        break;
                    case MdlFile.OK_MKDIR_CREATE:
                        SetDateToDir(sourcePath, destinationPath, relativePath, "NEW");
                        _mkdirOkCount++;
                        break;
                    default:
                        isOk = false;
                        _mkdirNgCount++;
                        _logger.WriteLine(MdlConst.LVL_NONE, $"NG : FAILED TO MKDIR : {destinationPath}");
                        break;
                }
            }
            return isOk;
        }

        /// <summary>
        /// 指定されたパスに対してシンボリックリンクの作成および複製を行います。
        /// </summary>
        /// <param name="sourcePath">リンク元のソースパス</param>
        /// <param name="destinationPath">作成先となるリンクパス</param>
        /// <param name="relativePath">相対パス</param>
        /// <param name="pathType">パス種別（<see cref="MdlFile.PATH_IS_DIRECTORY"/> または <see cref="MdlFile.PATH_IS_FILE"/>）</param>
        /// <returns>シンボリックリンクの作成・処理が成功した場合は <c>true</c>。失敗した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool result = diffCopy.MkLink(@"C:\Source\Link", @"D:\Dest\Link", "Link", MdlFile.PATH_IS_DIRECTORY);
        /// </code>
        /// </example>
        public bool MkLink(String sourcePath, String destinationPath, String relativePath, int pathType)
        {
            bool isOk = true;
            bool isExistTo = false;
            bool isExistResult = false;
            bool isNew = false;
            bool isUpdate = false;
            String srcLastWriteTimeStr = "";
            String dstLastWriteTimeStr = "";
            DateTime srcTime = DateTime.Now;
            DateTime dstTime = DateTime.Now;
            String action = "---";
            String realPath = "";
            _symLink.Verbose = 0;
            realPath = _symLink.GetRealPath(sourcePath, _prop.IsRelative);
            _symLink.Verbose = _prop.Verbose;
            if (String.IsNullOrEmpty(realPath)) realPath = "FAILED TO GET REALPATH";
            String outputPath = _prop.OutputPathCode switch
            {
                ClsProp.FROM => $"{sourcePath} [{realPath}]",
                ClsProp.TO => $"{destinationPath} [{realPath}]",
                ClsProp.BOTH => $"{sourcePath} => {destinationPath} [{realPath}]",
                _ => $"{GetOutputRelativePath(relativePath)} [{realPath}]"
            };
            switch (pathType)
            {
                case MdlFile.PATH_IS_DIRECTORY:
                    var srcDirInfo = new DirectoryInfo(sourcePath);
                    srcTime = srcDirInfo.LastWriteTime;
                    break;
                case MdlFile.PATH_IS_FILE:
                    var srcFileInfo = new FileInfo(sourcePath);
                    srcTime = srcFileInfo.LastWriteTime;
                    break;
            }
            srcLastWriteTimeStr = MdlDate.GetFormattedDate(srcTime, "yyyy/MM/dd HH:mm:ss");
            switch (MdlFile.GetPathType(destinationPath))
            {
                case MdlFile.PATH_IS_DIRECTORY:
                    if (MdlFile.PATH_IS_DIRECTORY != pathType) isUpdate = true;
                    if (!MdlFile.IsSymlink(destinationPath)) isUpdate = true;
                    isExistTo = true;
                    var dstDirInfo = new DirectoryInfo(destinationPath);
                    dstTime = dstDirInfo.LastWriteTime;
                    break;
                case MdlFile.PATH_IS_FILE:
                    if (MdlFile.PATH_IS_FILE != pathType) isUpdate = true;
                    if (!MdlFile.IsSymlink(destinationPath)) isUpdate = true;
                    isExistTo = true;
                    var dstFileInfo = new FileInfo(sourcePath);
                    dstTime = dstFileInfo.LastWriteTime;
                    break;
                default:
                    isNew = true;
                    break;
            }
            dstLastWriteTimeStr = MdlDate.GetFormattedDate(dstTime, "yyyy/MM/dd HH:mm:ss");
            if (isExistTo)
            {
                if (MdlFile.PATH_IS_FILE == pathType)
                {
                    switch (_prop.CheckLogic)
                    {
                        case ClsProp.CHECK_MTIME:
                            if (0 != MdlDate.CompareDateTime(srcTime, dstTime, _prop.SecRange)) isUpdate = true;
                            break;
                        case ClsProp.CHECK_MTIME_NEW:
                            if (MdlDate.CompareDateTime(srcTime, dstTime, _prop.SecRange) > 0) isUpdate = true;
                            break;
                        case ClsProp.CHECK_MTIME_OLD:
                            if (MdlDate.CompareDateTime(dstTime, srcTime, _prop.SecRange) > 0) isUpdate = true;
                            break;
                    }
                }
                if (_prop.IntIsOverWrite > 0)
                {
                    _symLink.Verbose = 0;
                    String realPathTo = _symLink.GetRealPath(destinationPath, _prop.IsRelative);
                    if (!String.IsNullOrEmpty(realPath) && !String.IsNullOrEmpty(realPathTo))
                    {
                        isUpdate = !realPath.Equals(realPathTo);
                    }
                    _symLink.Verbose = _prop.Verbose;
                }
                if (_prop.IntIsOverWrite > 1) isUpdate = true;
            }
            if (isNew)
            {
                if (_prop.IsShowNewFile)
                {
                    if (_prop.Verbose >= 0)
                    {
                        action = (_prop.IsList ? "-N-" : "NEW");
                        EchoTitle($"[{action}][{srcLastWriteTimeStr}] {outputPath}");
                    }
                    else if (_prop.Verbose == -1)
                    {
                        EchoTitle($"[C P] {outputPath}");
                    }
                    else
                    {
                        EchoTitle(outputPath);
                    }
                }
            }
            else if (isUpdate)
            {
                if (_prop.IsShowUpdatedFile)
                {
                    if (_prop.Verbose >= 0)
                    {
                        action = (_prop.IsList ? "-U-" : "UPD");
                        EchoTitle($"[{action}][{dstLastWriteTimeStr}=>{srcLastWriteTimeStr}] {outputPath}");
                    }
                    else if (_prop.Verbose == -1)
                    {
                        EchoTitle($"[C P] {outputPath}");
                    }
                    else
                    {
                        EchoTitle(outputPath);
                    }
                }
            }
            else
            {
                if (_prop.IsShowSameFile)
                {
                    if (_prop.Verbose >= 0)
                    {
                        action = "---";
                        EchoTitle($"[{action}][{srcLastWriteTimeStr}] {outputPath}");
                    }
                    else if (_prop.Verbose == -1)
                    {
                        EchoTitle($"[C P] {outputPath}");
                    }
                    else
                    {
                        EchoTitle(outputPath);
                    }
                }
            }
            if (!_prop.IsList)
            {
                if (isNew || isUpdate)
                {
                    if (isNew)
                    {
                        MkParentDir(destinationPath, true);
                    }
                    if (_symLink.Copy(sourcePath, destinationPath, isUpdate, _prop.IsRelative))
                    {
                        if (MdlFile.PathExists(destinationPath)) isExistResult = true;
                        switch (pathType)
                        {
                            case MdlFile.PATH_IS_DIRECTORY:
                                if (isExistResult)
                                {
                                    SetDateToDir(sourcePath, destinationPath, relativePath, "");
                                    if (isNew) _copyNewCount++;
                                    if (isUpdate) _copyUpdateCount++;
                                }
                                else
                                {
                                    isOk = false;
                                    _logger.WriteLine(MdlConst.LVL_NONE, $" -> NG : FAILED TO MKLINK : {relativePath}{_symLink?.Message}");
                                }
                                break;
                            case MdlFile.PATH_IS_FILE:
                                if (isExistResult)
                                {
                                    SetDateToFile(sourcePath, destinationPath, relativePath, "");
                                    if (isNew) _copyNewCount++;
                                    if (isUpdate) _copyUpdateCount++;
                                }
                                else
                                {
                                    isOk = false;
                                    _logger.WriteLine(MdlConst.LVL_NONE, $" -> NG : FAILED TO MKLINK : {relativePath}{_symLink?.Message}");
                                }
                                break;
                        }
                    }
                    else
                    {
                        isOk = false;
                        _logger.WriteLine(MdlConst.LVL_NONE, $" -> NG : FAILED TO MKLINK : {relativePath}{_symLink?.Message}");
                    }
                }
                else
                {
                    _copySkipCount++;
                }
            }
            else
            {
                if (isNew) _copyNewCount++;
                if (isUpdate) _copyUpdateCount++;
            }
            if (!isOk) _copyErrorCount++;
            return isOk;
        }

        /// <summary>
        /// ファイルの差分比較およびメインのコピー処理を実行します。
        /// </summary>
        /// <param name="sourceFilePath">コピー元のファイルパス</param>
        /// <param name="destFilePath">コピー先のファイルパス</param>
        /// <param name="relativePath">相対パス</param>
        /// <returns>ファイルコピーが正常に完了した場合は <c>true</c>。失敗した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool copyResult = diffCopy.DiffCopyFileMain(@"C:\src\data.bin", @"D:\dst\data.bin", "data.bin");
        /// </code>
        /// </example>
        public bool DiffCopyFileMain(String sourceFilePath, String destFilePath, String relativePath)
        {
            if (_prop.IsProgress && _fsUtil is null)
            {
                _logger.WriteLine(MdlConst.LVL_E, "[ClsFsDiffCopy.DiffCopyFileMain()] null == _objFile");
                _copyErrorCount++;
                return false;
            }
            bool isOk = true;
            bool isCopy = false;
            bool isNew = false;
            bool isShowCksum = false;
            String srcCheckStr = "";
            String dstCheckStr = "";
            var srcFileInfo = new FileInfo(sourceFilePath);
            var dstFileInfo = new FileInfo(destFilePath);
            String srcLastWriteTimeStr = MdlDate.GetFormattedDate(srcFileInfo.LastWriteTime, "yyyy/MM/dd HH:mm:ss");
            String dstLastWriteTimeStr = "";
            String outputPath = _prop.OutputPathCode switch
            {
                ClsProp.FROM => sourceFilePath,
                ClsProp.TO => destFilePath,
                ClsProp.BOTH => $"{sourceFilePath} => {destFilePath}",
                _ => GetOutputRelativePath(relativePath)
            };
            // コピー先のパスの存在をチェックします
            switch (MdlFile.GetPathType(destFilePath))
            {
                // コピー先がディレクトリの場合は、そのディレクトリを強制的に削除して、ファイルをコピーする
                case MdlFile.PATH_IS_DIRECTORY:
                    RemoveRecursive(destFilePath, relativePath, MdlFile.IsSymlink(destFilePath));
                    isCopy = true;
                    isNew = true;
                    break;
                // コピー先がファイルの場合は、更新の必要性をチェックする
                case MdlFile.PATH_IS_FILE:
                    bool isCheckUpdates = false;
                    dstLastWriteTimeStr = MdlDate.GetFormattedDate(dstFileInfo.LastWriteTime, "yyyy/MM/dd HH:mm:ss");
                    // チェックしない場合は強制的にコピーする
                    if (ClsProp.CHECK_NONE == _prop.CheckLogic)
                    {
                        isCopy = true;
                    }
                    // 存在チェックのみの場合は、既にコピー先にファイルが存在するのでコピーしない
                    else if (ClsProp.CHECK_EXIST == _prop.CheckLogic)
                    {
                        isCopy = false;
                    }
                    // その他のチェックロジックの場合は、詳細な比較を行う
                    else
                    {
                        if (_prop.IsSizeCheck)
                        {
                            if (File.Exists(dstFileInfo.FullName) && srcFileInfo.Length == dstFileInfo.Length)
                            {
                                isCheckUpdates = true;
                            }
                            else
                            {
                                isCopy = true;
                                if (_prop.Verbose > 1)
                                {
                                    srcCheckStr = "size:" + srcFileInfo.Length;
                                    dstCheckStr = "size:" + (File.Exists(dstFileInfo.FullName) ? dstFileInfo.Length : 0);
                                    isShowCksum = true;
                                }
                            }
                        }
                        else
                        {
                            isCheckUpdates = true;
                        }
                        if (isCheckUpdates)
                        {
                            switch (_prop.CheckLogic)
                            {
                                case ClsProp.CHECK_ADLER32:
                                    switch (CheckIsSkipBySize(srcFileInfo.Length))
                                    {
                                        case 0:
                                            ClsAdler32 objAdler32 = new ClsAdler32();
                                            srcCheckStr = "adler:" + objAdler32.ComputeChecksum(sourceFilePath);
                                            if (_prop.Verbose > 6) _logger.WriteLine(MdlConst.LVL_NONE, "[ADLER32] " + srcCheckStr + " : " + sourceFilePath);
                                            dstCheckStr = "adler:" + objAdler32.ComputeChecksum(destFilePath);
                                            if (_prop.Verbose > 6) _logger.WriteLine(MdlConst.LVL_NONE, "[ADLER32] " + dstCheckStr + " : " + destFilePath);
                                            if (!srcCheckStr.Equals(dstCheckStr)) isCopy = true;
                                            if (_prop.Verbose > 1) isShowCksum = true;
                                            break;
                                        case 1:
                                            isCopy = true;
                                            if (_prop.Verbose > 1)
                                            {
                                                srcCheckStr = "filesize:" + (srcFileInfo.Length / 1024 / 1024).ToString("F0") + "MB";
                                                dstCheckStr = "copysize:" + (_prop.CopySize / 1024 / 1024).ToString("F0") + "MB";
                                                isShowCksum = true;
                                            }
                                            break;
                                        case 10:
                                            if (_prop.Verbose > 1)
                                            {
                                                srcCheckStr = "skipsize:" + (_prop.SkipSize / 1024 / 1024).ToString("F0") + "MB=>filesize:" + (srcFileInfo.Length / 1024 / 1024).ToString("F0") + "MB";
                                                dstCheckStr = "";
                                                isShowCksum = true;
                                            }
                                            break;
                                    }
                                    break;
                                case ClsProp.CHECK_CKSUM:
                                    switch (CheckIsSkipBySize(srcFileInfo.Length))
                                    {
                                        case 0:
                                            isCopy = false;
                                            ClsCksum objCksum = new ClsCksum();
                                            srcCheckStr = "cksum:" + objCksum.GetChecksum(sourceFilePath);
                                            dstCheckStr = "cksum:" + objCksum.GetChecksum(destFilePath);
                                            if (!srcCheckStr.Equals(dstCheckStr)) isCopy = true;
                                            if (_prop.Verbose > 1) isShowCksum = true;
                                            break;
                                        case 1:
                                            isCopy = true;
                                            if (_prop.Verbose > 1)
                                            {
                                                srcCheckStr = "filesize:" + (srcFileInfo.Length / 1024 / 1024).ToString("F0") + "MB";
                                                dstCheckStr = "copysize:" + (_prop.CopySize / 1024 / 1024).ToString("F0") + "MB";
                                                isShowCksum = true;
                                            }
                                            break;
                                        case 10:
                                            if (_prop.Verbose > 1)
                                            {
                                                srcCheckStr = "skipsize:" + (_prop.SkipSize / 1024 / 1024).ToString("F0") + "MB=>filesize:" + (srcFileInfo.Length / 1024 / 1024).ToString("F0") + "MB";
                                                dstCheckStr = "";
                                                isShowCksum = true;
                                            }
                                            break;
                                    }
                                    break;
                                case ClsProp.CHECK_SHA1:
                                    switch (CheckIsSkipBySize(srcFileInfo.Length))
                                    {
                                        case 0:
                                            srcCheckStr = "sha1:" + _fsUtil.ComputeSha1Hash(sourceFilePath);
                                            dstCheckStr = "sha1:" + _fsUtil.ComputeSha1Hash(destFilePath);
                                            if (!srcCheckStr.Equals(dstCheckStr)) isCopy = true;
                                            if (_prop.Verbose > 1) isShowCksum = true;
                                            break;
                                        case 1:
                                            isCopy = true;
                                            if (_prop.Verbose > 1)
                                            {
                                                srcCheckStr = "filesize:" + (srcFileInfo.Length / 1024 / 1024).ToString("F0") + "MB";
                                                dstCheckStr = "copysize:" + (_prop.CopySize / 1024 / 1024).ToString("F0") + "MB";
                                                isShowCksum = true;
                                            }
                                            break;
                                        case 10:
                                            if (_prop.Verbose > 1)
                                            {
                                                srcCheckStr = "skipsize:" + (_prop.SkipSize / 1024 / 1024).ToString("F0") + "MB=>filesize:" + (srcFileInfo.Length / 1024 / 1024).ToString("F0") + "MB";
                                                dstCheckStr = "";
                                                isShowCksum = true;
                                            }
                                            break;
                                    }
                                    break;
                                case ClsProp.CHECK_MTIME:
                                    if (0 != MdlDate.CompareDateTime(srcFileInfo.LastWriteTime, dstFileInfo.LastWriteTime, _prop.SecRange)) isCopy = true;
                                    break;
                                case ClsProp.CHECK_MTIME_NEW:
                                    if (MdlDate.CompareDateTime(srcFileInfo.LastWriteTime, dstFileInfo.LastWriteTime, _prop.SecRange) > 0) isCopy = true;
                                    break;
                                case ClsProp.CHECK_MTIME_OLD:
                                    if (MdlDate.CompareDateTime(dstFileInfo.LastWriteTime, srcFileInfo.LastWriteTime, _prop.SecRange) > 0) isCopy = true;
                                    break;
                            }
                        }
                    }
                    break;
                default:
                    isCopy = true;
                    isNew = true;
                    break;
            }
            if (isCopy)
            {
                if (_prop.Verbose > 5)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "---[DEBUG]--------------------------------------------------");
                    _logger.WriteLine(MdlConst.LVL_NONE, "DiffCopyFileMain(" + sourceFilePath + ", " + destFilePath + ", " + relativePath + ")");
                    _logger.WriteLine(MdlConst.LVL_NONE, "isCopy = " + isCopy.ToString() + " / isNew = " + isNew.ToString());
                    _logger.WriteLine(MdlConst.LVL_NONE, "MdlFile.GetPathType(" + destFilePath + ") = " + MdlFile.GetPathType(destFilePath));
                    if (null != dstFileInfo && File.Exists(dstFileInfo.FullName))
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, "[dstFileInfo] Exists=" + dstFileInfo.Exists + " / LastWriteTime = " + dstFileInfo.LastWriteTime + " / Length = " + dstFileInfo.Length);
                        _logger.WriteLine(MdlConst.LVL_NONE, "[dstFileInfo] FullName=" + dstFileInfo.FullName + " / Extension=" + dstFileInfo.Extension);
                    }
                    _logger.WriteLine(MdlConst.LVL_NONE, "------------------------------------------------------------");
                }
                if (isNew)
                {
                    _copyNewCount++;
                    if (_prop.IsShowNewFile)
                    {
                        if (_prop.Verbose >= 0)
                        {
                            String action = (_prop.IsList ? "-N-" : "NEW");
                            EchoTitle("[" + action + "][" + srcLastWriteTimeStr + "] " + outputPath);
                        }
                        else
                        {
                            if (_prop.Verbose == -1)
                            {
                                EchoTitle("[C P] " + outputPath);
                            }
                            else
                            {
                                EchoTitle(outputPath);
                            }
                        }
                    }
                }
                else
                {
                    _copyUpdateCount++;
                    if (_prop.IsShowUpdatedFile)
                    {
                        if (_prop.Verbose >= 0)
                        {
                            String action = (_prop.IsList ? "-U-" : "UPD");
                            if (isShowCksum)
                            {
                                EchoTitle("[" + action + "][" + dstLastWriteTimeStr + "=>" + srcLastWriteTimeStr + "][" + dstCheckStr + "=>" + srcCheckStr + "] " + outputPath);
                            }
                            else
                            {
                                EchoTitle("[" + action + "][" + dstLastWriteTimeStr + "=>" + srcLastWriteTimeStr + "] " + outputPath);
                            }
                        }
                        else
                        {
                            if (_prop.Verbose == -1)
                            {
                                EchoTitle("[C P] " + outputPath);
                            }
                            else
                            {
                                EchoTitle(outputPath);
                            }
                        }
                    }
                }
                isOk = CopyFile(sourceFilePath, destFilePath, relativePath, isNew);
            }
            else
            {
                _copySkipCount++;
                String modeStr = "---";
                if (!_prop.IsShowSameFile) modeStr = "";
                SetDateToFile(srcFileInfo, dstFileInfo, relativePath, modeStr, isShowCksum, srcCheckStr);
            }
            return isOk;
        }

        /// <summary>
        /// ファイルサイズに基づいて、コピー処理をスキップするかどうかを判定します。
        /// </summary>
        /// <param name="fileSize">判定対象のファイルサイズ（バイト）</param>
        /// <returns>スキップする場合は <c>10</c>、強制コピーする場合は <c>1</c>、通常判定の場合は <c>0</c>。</returns>
        /// <example>
        /// <code>
        /// int status = diffCopy.CheckIsSkipBySize(1024 * 1024 * 50);
        /// </code>
        /// </example>
        public int CheckIsSkipBySize(long fileSize)
        {
            bool isSkip = _prop.SkipSize > 0 && fileSize > _prop.SkipSize;
            bool isCopy = _prop.CopySize > 0 && fileSize > _prop.CopySize;
            int result = 0;

            if (_prop.SkipSize >= _prop.CopySize)
            {
                if (isSkip) result = 10;
                if (isCopy) result = 1;
                if (isSkip && isCopy) result = 10;
            }
            else
            {
                if (isSkip) result = 10;
                if (isCopy) result = 1;
                if (isSkip && isCopy) result = 1;
            }
            return result;
        }

        /// <summary>
        /// 設定されているプレフィックスを付加した出力用相対パスを取得します。
        /// </summary>
        /// <param name="relativePath">元となる相対パス</param>
        /// <returns>プレフィックスが付加された相対パス</returns>
        /// <example>
        /// <code>
        /// string displayPath = diffCopy.GetOutputRelativePath("subfolder/file.txt");
        /// </code>
        /// </example>
        public String GetOutputRelativePath(String relativePath)
        {
            return string.IsNullOrEmpty(_prop.OutputPathPrefix)
                ? relativePath
                : _prop.OutputPathPrefix + relativePath;
        }

        /// <summary>
        /// 設定されたログレベルや進捗表示オプションに従ってタイトルメッセージを出力します。
        /// </summary>
        /// <param name="message">出力するメッセージ文字列</param>
        /// <example>
        /// <code>
        /// diffCopy.EchoTitle("[NEW] C:\Source\file.txt");
        /// </code>
        /// </example>
        public void EchoTitle(String message)
        {
            switch (_prop.Task)
            {
                case ClsProp.TASK_CP:
                case ClsProp.TASK_MV:
                    if (_prop.IsProgress)
                    {
                        // 0        1         2         3         4         5         6         7         8
                        // 12345678901234567890123456789012345678901234567890123456789012345678901234567890
                        // [NEW][2015/06/06 00:00:00] 00.TEST\2015\[テスト] 20150606.iso00/ Remain=00:00:00
                        if (null != _fsUtil && !String.IsNullOrEmpty(_fsUtil.Result))
                        {
                            int intProgressSize = 86;
                            int intLength = MdlUtil.GetShiftJisByteCount(_fsUtil.Result);
                            if (intLength < intProgressSize) intLength = intProgressSize;
                            if (MdlUtil.GetShiftJisByteCount(message) < intLength)
                            {
                                message = message.PadRight(intLength);
                                _logger.SetValueByKey(ClsLogger.IS_TRIM_CONSOLE, "false");
                            }
                            else
                            {
                                _logger.SetValueByKey(ClsLogger.IS_TRIM_CONSOLE, "true");
                            }
                        }
                    }
                    break;
            }
            _logger.WriteLine(MdlConst.LVL_NONE, message);
            if (!_logger.GetValueByKey(ClsLogger.IS_TRIM_CONSOLE, false)) _logger.SetValueByKey(ClsLogger.IS_TRIM_CONSOLE, "true");
        }

        /// <summary>
        /// バックアップ処理、タイムスタンプ制御を含めてファイルの実コピーを実行します。
        /// </summary>
        /// <param name="sourceFilePath">コピー元のファイルパス</param>
        /// <param name="destFilePath">コピー先のファイルパス</param>
        /// <param name="relativePath">相対パス</param>
        /// <param name="isNew">新規ファイル作成であるかどうかのフラグ</param>
        /// <returns>ファイルコピーが成功した場合は <c>true</c>。失敗した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool success = diffCopy.CopyFile(@"C:\src\file.txt", @"D:\dst\file.txt", "file.txt", true);
        /// </code>
        /// </example>
        public bool CopyFile(String sourceFilePath, String destFilePath, String relativePath, bool isNew)
        {
            bool isSuccess = true;
            if (_prop.IsList) return true;
            bool isSymLink = false;
            if (_prop.IsSymLink) isSymLink = MdlFile.IsSymlink(sourceFilePath);
            try
            {
                DateTime creationTime = System.IO.File.GetCreationTime(sourceFilePath);
                DateTime lastWriteTime = System.IO.File.GetLastWriteTime(sourceFilePath);
                if (isNew)
                {
                    MkParentDir(destFilePath, true);
                }
                else
                {
                    MdlFile.ChangeFileAttributes(destFilePath, "w");
                    if (_prop.IsBackup)
                    {
                        String backupPath = _prop.BackupDir + "\\" + relativePath;
                        MkParentDir(backupPath, false);
                        try
                        {
                            System.IO.File.Move(destFilePath, backupPath);
                            try
                            {
                                MdlFile.SetDateToFile(backupPath, System.IO.File.GetCreationTime(destFilePath), 1);
                            }
                            catch { }
                            try
                            {
                                MdlFile.SetDateToFile(backupPath, System.IO.File.GetLastWriteTime(destFilePath), 2);
                            }
                            catch { }
                        }
                        catch (Exception backupException)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, "[ERR] FAILED TO BACKUP(" + destFilePath + " => " + backupPath + ") : " + backupException.Message);
                            if (_prop.IsStackTrace)
                            {
                                _logger.WriteLine(MdlConst.LVL_NONE, "");
                                _logger.WriteLine(MdlConst.LVL_NONE, backupException.StackTrace ?? "");
                                _logger.WriteLine(MdlConst.LVL_NONE, "");
                            }
                            if (_prop.IsErrorIfBackupFailed)
                            {
                                throw new System.FieldAccessException("上書ファイルの退避に失敗しました。");
                            }
                        }
                    }
                }
                switch (_prop.Task)
                {
                    case ClsProp.TASK_CP:
                        try
                        {
                            switch(_prop.CopyCmdType)
                            {
                                case ClsProp.COPY_BINARY:
                                    if (_prop.Verbose > 5) _logger.WriteLine(MdlConst.LVL_NONE, "[TRY] _objFile.BinaryCopy(" + sourceFilePath + ", " + destFilePath + ")");
                                    _fsUtil.BinaryCopy(sourceFilePath, destFilePath, _prop.IsProgress, _prop.ObjFileShare);
                                    break;
                                case ClsProp.COPY_ASYNC:
                                    if (_prop.Verbose > 5) _logger.WriteLine(MdlConst.LVL_NONE, "[TRY] _objFile.AsyncCopy(" + sourceFilePath + ", " + destFilePath + ")");
                                    _fsUtil.AsyncCopy(sourceFilePath, destFilePath, _prop.IsProgress, _prop.ObjFileShare);
                                    break;
                                default:
                                    _fsUtil.CopyFileWithRetry(sourceFilePath, destFilePath);
                                    break;
                            }
                        }
                        catch (Exception copyException)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, _fsUtil.Message.ToString());
                            throw new System.FieldAccessException(copyException.Message);
                        }
                        break;
                    case ClsProp.TASK_MV:
                        if (!isNew) RemoveRecursive(destFilePath, relativePath, isSymLink);
                        if (_prop.IsProgress)
                        {
                            try
                            {
                                switch (_prop.CopyCmdType)
                                {
                                    case ClsProp.COPY_BINARY:
                                        if (_prop.Verbose > 5) _logger.WriteLine(MdlConst.LVL_NONE, "[TRY] _objFile.BinaryCopy(" + sourceFilePath + ", " + destFilePath + ")");
                                        _fsUtil.BinaryCopy(sourceFilePath, destFilePath, _prop.IsProgress, _prop.ObjFileShare);
                                        break;
                                    case ClsProp.COPY_ASYNC:
                                        if (_prop.Verbose > 5) _logger.WriteLine(MdlConst.LVL_NONE, "[TRY] _objFile.AsyncCopy(" + sourceFilePath + ", " + destFilePath + ")");
                                        _fsUtil.AsyncCopy(sourceFilePath, destFilePath, _prop.IsProgress, _prop.ObjFileShare);
                                        break;
                                    default:
                                        _fsUtil.Rename(sourceFilePath, destFilePath);
                                        break;
                                }
                            }
                            catch (Exception copyException)
                            {
                                _logger.WriteLine(MdlConst.LVL_NONE, _fsUtil.Message.ToString());
                                throw new System.FieldAccessException(copyException.Message);
                            }
                            if (!RemoveRecursive(sourceFilePath, relativePath, isSymLink))
                            {
                                throw new System.FieldAccessException("[ERR] CopyFile() : CAN NOT DELETE FILE : " + sourceFilePath);
                            }
                        }
                        else
                        {
                            if (_prop.Verbose > 5) _logger.WriteLine(MdlConst.LVL_NONE, "[TRY] System.IO.File.Move(" + sourceFilePath + ", " + destFilePath + ")");
                            _fsUtil.Rename(sourceFilePath, destFilePath);
                        }
                        break;
                    case ClsProp.TASK_RENAME:
                        try
                        {
                            if (_prop.Verbose > 5) _logger.WriteLine(MdlConst.LVL_NONE, "[TRY] _objFile.Rename(" + sourceFilePath + ", " + destFilePath + ")");
                            _fsUtil.Rename(sourceFilePath, destFilePath);
                        }
                        catch (Exception renameException)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, _fsUtil.Message.ToString());
                            throw new System.FieldAccessException(renameException.Message);
                        }
                        break;
                }
                // Created
                try
                {
                    MdlFile.SetDateToFile(destFilePath, creationTime, 1);
                }
                catch { }
                // Modified
                try
                {
                    MdlFile.SetDateToFile(destFilePath, lastWriteTime, 2);
                }
                catch { }
            }
            catch (Exception exception)
            {
                isSuccess = false;
                _copyErrorCount++;
                _logger.WriteLine(MdlConst.LVL_NONE, "[ERR] CopyFile(" + sourceFilePath + ", " + destFilePath + ") : " + exception.Message);
                if (_prop.IsStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, exception.StackTrace ?? "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 指定されたパスの親ディレクトリが存在しない場合に親ディレクトリを作成します。
        /// </summary>
        /// <param name="path">対象となるファイルのパス</param>
        /// <param name="count">作成成功/失敗のカウントを更新するかどうか</param>
        /// <returns>親ディレクトリの確認・作成が成功した場合は <c>true</c>。失敗した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool ok = diffCopy.MkParentDir(@"D:\Dest\Sub\file.txt", true);
        /// </code>
        /// </example>
        public bool MkParentDir(String path, bool count)
        {
            bool success = true;
            String parentDir = MdlFile.GetDirectoryPath(path);
            if (!String.IsNullOrEmpty(parentDir))
            {
                switch (MdlFile.CreateDirectory(parentDir))
                {
                    case MdlFile.OK_MKDIR_ALREADY_EXIST:
                        break;
                    case MdlFile.OK_MKDIR_CREATE:
                        if (count) _mkdirOkCount++;
                        break;
                    default:
                        success = false;
                        if (count) _mkdirNgCount++;
                        _logger.WriteLine(MdlConst.LVL_NONE, "[ERR] ClsFsDiffCopy.MkParentDir() : FAILED TO MKDIR : " + parentDir);
                        break;
                }
            }
            return success;
        }

        /// <summary>
        /// 指定されたパスのファイルまたはディレクトリを再帰的に削除します。
        /// </summary>
        /// <param name="path">削除対象のファイルまたはディレクトリのパス</param>
        /// <param name="relativePath">相対パス</param>
        /// <param name="isSymLink">シンボリックリンクかどうかを示すフラグ</param>
        /// <returns>削除が正常に成功した場合は <c>true</c>。失敗した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool ok = diffCopy.RemoveRecursive(@"C:\Target\Path", "Path", false);
        /// </code>
        /// </example>
        public bool RemoveRecursive(String path, String relativePath, bool isSymLink)
        {
            bool isSuccess = true;
            String outputPath = GetOutputRelativePath(relativePath);
            switch (_prop.OutputPathCode)
            {
                case ClsProp.FROM:
                case ClsProp.TO:
                case ClsProp.BOTH:
                    outputPath = path;
                    break;
            }
            switch (MdlFile.GetPathType(path))
            {
                case MdlFile.PATH_IS_DIRECTORY:
                    isSuccess = RemoveRecursive(new System.IO.DirectoryInfo(path), relativePath, isSymLink);
                    break;
                case MdlFile.PATH_IS_FILE:
                    try
                    {
                        if (String.IsNullOrEmpty(relativePath)) relativePath = path;
                        System.IO.FileInfo fileInfo = new System.IO.FileInfo(path);
                        _rmTotalCount++;
                        if (!_prop.IsList)
                        {
                            try
                            {
                                MdlFile.ChangeFileAttributes(fileInfo, "w");
                            }
                            catch { }
                            if (_prop.Verbose >= 0)
                            {
                                try
                                {
                                    _logger.WriteLine(MdlConst.LVL_NONE, "[DEL][" + MdlDate.GetFormattedDate(fileInfo.LastWriteTime, "yyyy/MM/dd HH:mm:ss") + "] " + outputPath);
                                }
                                catch
                                {
                                    _logger.WriteLine(MdlConst.LVL_NONE, "[DEL][更新日時の取得失敗] " + outputPath);
                                }
                            }
                            else
                            {
                                if (_prop.Verbose == -1)
                                {
                                    EchoTitle("[DEL] " + outputPath);
                                }
                                else
                                {
                                    EchoTitle(outputPath);
                                }
                            }
                            fileInfo.Delete();
                            _rmOkCount++;
                        }
                        else
                        {
                            if (_prop.Verbose >= 0)
                            {
                                _logger.WriteLine(MdlConst.LVL_NONE, "[-D-][" + MdlDate.GetFormattedDate(fileInfo.LastWriteTime, "yyyy/MM/dd HH:mm:ss") + "] " + outputPath);
                            }
                            else
                            {
                                if (_prop.Verbose == -1)
                                {
                                    EchoTitle("[-D-] " + outputPath);
                                }
                                else
                                {
                                    EchoTitle(outputPath);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        isSuccess = false;
                        _rmNgCount++;
                        _logger.WriteLine(MdlConst.LVL_NONE, "[ERR] RemoveRecursive(" + path + ") 1 : " + ex.Message + " : " + path);
                        if (_prop.IsStackTrace)
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, "");
                            _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                            _logger.WriteLine(MdlConst.LVL_NONE, "");
                        }
                    }
                    break;
            }
            return isSuccess;
        }

        /// <summary>
        /// 指定されたディレクトリとその内容を再帰的に削除します。
        /// </summary>
        /// <param name="dirInfo">削除するディレクトリの情報。</param>
        /// <param name="relativePath">相対パス。</param>
        /// <param name="isSymLink">シンボリックリンクかどうかを示すフラグ。</param>
        /// <returns>削除が成功したかどうかを示すブール値。</returns>
        /// <example>
        /// <code>
        /// var dirInfo = new DirectoryInfo(@"C:\Target\Dir");
        /// bool ok = diffCopy.RemoveRecursive(dirInfo, "Dir", false);
        /// </code>
        /// </example>
        public bool RemoveRecursive(System.IO.DirectoryInfo dirInfo, String relativePath, bool isSymLink)
        {
            bool isSuccess = true;
            ulong fileCount = 0;
            String outputPath = GetOutputRelativePath(relativePath);
            switch (_prop.OutputPathCode)
            {
                case ClsProp.FROM:
                case ClsProp.TO:
                case ClsProp.BOTH:
                    outputPath = dirInfo.FullName;
                    break;
            }
            if (!_prop.IsList)
            {
                try
                {
                    if (!isSymLink)
                    {
                        foreach (System.IO.FileInfo fileInfo in dirInfo.GetFiles())
                        {
                            fileCount++;
                            MdlFile.ChangeFileAttributes(fileInfo, "W");
                        }
                        foreach (System.IO.DirectoryInfo subDirInfo in dirInfo.GetDirectories())
                        {
                            RemoveRecursive(subDirInfo, System.IO.Path.Combine(relativePath, System.IO.Path.GetFileName(subDirInfo.FullName)), isSymLink);
                        }
                    }
                    MdlFile.ChangeDirectoryAttributes(dirInfo, "W");
                }
                catch (Exception ex)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "[ERR] RemoveRecursive(" + dirInfo.FullName + ") 2-RW : " + ex.Message);
                }
            }
            try
            {
                if (fileCount == 0) fileCount = 1;
                if (String.IsNullOrEmpty(relativePath)) relativePath = dirInfo.FullName;
                _rmTotalCount += fileCount;
                if (!_prop.IsList)
                {
                    if (_prop.Verbose >= 0)
                    {
                        try
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, "[DEL][" + MdlDate.GetFormattedDate(dirInfo.LastWriteTime, "yyyy/MM/dd HH:mm:ss") + "] " + outputPath);
                        }
                        catch
                        {
                            _logger.WriteLine(MdlConst.LVL_NONE, "[DEL][更新日時の取得失敗] " + outputPath);
                        }
                    }
                    else
                    {
                        if (_prop.Verbose == -1)
                        {
                            EchoTitle("[DEL] " + outputPath);
                        }
                        else
                        {
                            EchoTitle(outputPath);
                        }
                    }
                    if (isSymLink)
                    {
                        dirInfo.Delete(false);
                    }
                    else
                    {
                        dirInfo.Delete(true);
                    }
                    _rmOkCount += fileCount;
                }
                else
                {
                    if (_prop.Verbose >= 0)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, "[-D-][" + MdlDate.GetFormattedDate(dirInfo.LastWriteTime, "yyyy/MM/dd HH:mm:ss") + "] " + outputPath);
                    }
                    else
                    {
                        if (_prop.Verbose == -1)
                        {
                            EchoTitle("[-D-] " + outputPath);
                        }
                        else
                        {
                            EchoTitle(outputPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                _rmNgCount += fileCount;
                _logger.WriteLine(MdlConst.LVL_NONE, "[ERR] RemoveRecursive(" + dirInfo.FullName + ") 2-RM : " + ex.Message);
                if (_prop.IsStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 指定されたパスのファイルまたはディレクトリを削除します。
        /// </summary>
        /// <param name="path">削除するパス</param>
        /// <returns>削除が成功したかどうか</returns>
        /// <example>
        /// <code>
        /// bool ok = diffCopy.RemoveRecursive(@"C:\Target\Path");
        /// </code>
        /// </example>
        public bool RemoveRecursive(String path)
        {
            bool isOk = true;
            if (!_prop.IsList)
            {
                if (_prop.Verbose >= 0)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "[DEL] " + path);
                }
                else
                {
                    if (_prop.Verbose == -1)
                    {
                        EchoTitle("[DEL] " + path);
                    }
                    else
                    {
                        EchoTitle(path);
                    }
                }
                isOk = MdlFile.DeleteRecursively(path, _prop.Verbose);
            }
            else
            {
                if (_prop.Verbose >= 0)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "[-D-] " + path);
                }
                else
                {
                    if (_prop.Verbose == -1)
                    {
                        EchoTitle("[-D-] " + path);
                    }
                    else
                    {
                        EchoTitle(path);
                    }
                }
            }
            return isOk;
        }

        /// <summary>
        /// ディレクトリのタイムスタンプを設定・比較します。
        /// </summary>
        /// <param name="sourcePath">ソースディレクトリのパス</param>
        /// <param name="destinationPath">宛先ディレクトリのパス</param>
        /// <param name="relativePath">相対パス</param>
        /// <param name="modeStr">モード文字列</param>
        /// <example>
        /// <code>
        /// diffCopy.SetDateToDir(@"C:\src\dir", @"D:\dst\dir", "dir", "NEW");
        /// </code>
        /// </example>
        public void SetDateToDir(String sourcePath, String destinationPath, String relativePath, String modeStr)
        {
            System.IO.DirectoryInfo sourceDirInfo = new System.IO.DirectoryInfo(sourcePath);
            System.IO.DirectoryInfo destinationDirInfo = new System.IO.DirectoryInfo(destinationPath);
            SetDateToDir(sourceDirInfo, destinationDirInfo, relativePath, modeStr);
        }

        /// <summary>
        /// ディレクトリの情報オブジェクトを用いてタイムスタンプを設定・比較します。
        /// </summary>
        /// <param name="sourceDirInfo">ソースディレクトリの情報</param>
        /// <param name="destinationDirInfo">宛先ディレクトリの情報</param>
        /// <param name="relativePath">相対パス</param>
        /// <param name="modeStr">モード文字列</param>
        /// <example>
        /// <code>
        /// diffCopy.SetDateToDir(new DirectoryInfo(@"C:\src\dir"), new DirectoryInfo(@"D:\dst\dir"), "dir", "NEW");
        /// </code>
        /// </example>
        public void SetDateToDir(System.IO.DirectoryInfo sourceDirInfo, System.IO.DirectoryInfo destinationDirInfo, String relativePath, String modeStr)
        {
            bool isSetTimestamp = false;
            String sourceLastWriteTimeStr = MdlDate.GetFormattedDate(sourceDirInfo.LastWriteTime, "yyyy/MM/dd HH:mm:ss");
            String destinationLastWriteTimeStr = MdlDate.GetFormattedDate(destinationDirInfo.LastWriteTime, "yyyy/MM/dd HH:mm:ss");
            String result = ">";
            String outputPath = GetOutputRelativePath(relativePath);
            switch (_prop.OutputPathCode)
            {
                case ClsProp.FROM:
                    outputPath = sourceDirInfo.FullName;
                    break;
                case ClsProp.TO:
                    outputPath = destinationDirInfo.FullName;
                    break;
                case ClsProp.BOTH:
                    outputPath = sourceDirInfo.FullName + " => " + destinationDirInfo.FullName;
                    break;
            }
            if (_prop.IsCpTimestamp > 0 && null != destinationDirInfo)
            {
                isSetTimestamp = MdlFile.IsDirectoryTimestampDifferent(sourceDirInfo, destinationDirInfo, _prop.SecRange, _prop.IsCpTimestamp);
                if (isSetTimestamp)
                {
                    try
                    {
                        if (!_prop.IsList)
                        {
                            if (_prop.IsCpTimestamp == 1 || _prop.IsCpTimestamp == 3) MdlFile.SetDateToDir(destinationDirInfo.FullName, sourceDirInfo.CreationTime, 1);
                            if (_prop.IsCpTimestamp == 2 || _prop.IsCpTimestamp == 3) MdlFile.SetDateToDir(destinationDirInfo.FullName, sourceDirInfo.LastWriteTime, 2);
                        }
                    }
                    catch
                    {
                        result = "X";
                    }
                }
            }
            if (!String.IsNullOrEmpty(modeStr))
            {
                if (!_prop.IsFileCopy)
                {
                    if (_prop.Verbose >= 0)
                    {
                        if (isSetTimestamp)
                        {
                            EchoTitle("[" + modeStr + "][" + destinationLastWriteTimeStr + "=" + result + sourceLastWriteTimeStr + "] " + outputPath);
                        }
                        else
                        {
                            EchoTitle("[" + modeStr + "] " + outputPath);
                        }
                    }
                    else
                    {
                        if (_prop.Verbose == -1)
                        {
                            EchoTitle("[" + modeStr + "] " + outputPath);
                        }
                        else
                        {
                            EchoTitle(outputPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ファイルのタイムスタンプを設定・比較します。
        /// </summary>
        /// <param name="sourcePath">ソースファイルのパス</param>
        /// <param name="destinationPath">宛先ファイルのパス</param>
        /// <param name="relativePath">相対パス</param>
        /// <param name="modeStr">モード文字列</param>
        /// <example>
        /// <code>
        /// diffCopy.SetDateToFile(@"C:\src\file.txt", @"D:\dst\file.txt", "file.txt", "UPD");
        /// </code>
        /// </example>
        public void SetDateToFile(String sourcePath, String destinationPath, String relativePath, String modeStr)
        {
            SetDateToFile(sourcePath, destinationPath, relativePath, modeStr, false, "");
        }

        /// <summary>
        /// ファイルのタイムスタンプおよびチェックサム比較結果を設定・表示します。
        /// </summary>
        /// <param name="sourcePath">ソースファイルのパス</param>
        /// <param name="destinationPath">宛先ファイルのパス</param>
        /// <param name="relativePath">相対パス</param>
        /// <param name="modeStr">モード文字列</param>
        /// <param name="isShowCksum">チェックサムを表示するかどうか</param>
        /// <param name="srcCheckStr">チェック元の文字列</param>
        /// <example>
        /// <code>
        /// diffCopy.SetDateToFile(@"C:\src\file.txt", @"D:\dst\file.txt", "file.txt", "UPD", true, "cksum:12345");
        /// </code>
        /// </example>
        public void SetDateToFile(String sourcePath, String destinationPath, String relativePath, String modeStr, bool isShowCksum, String srcCheckStr)
        {
            System.IO.FileInfo sourceFileInfo = new System.IO.FileInfo(sourcePath);
            System.IO.FileInfo destinationFileInfo = new System.IO.FileInfo(destinationPath);
            SetDateToFile(sourceFileInfo, destinationFileInfo, relativePath, modeStr, isShowCksum, srcCheckStr);
        }

        /// <summary>
        /// FileInfoオブジェクトを使用してファイルのタイムスタンプを設定・比較します。
        /// </summary>
        /// <param name="sourceFileInfo">ソースファイルのFileInfoオブジェクト</param>
        /// <param name="destinationFileInfo">宛先ファイルのFileInfoオブジェクト</param>
        /// <param name="relativePath">相対パス</param>
        /// <param name="modeStr">モード文字列</param>
        /// <example>
        /// <code>
        /// diffCopy.SetDateToFile(new FileInfo(@"C:\src\file.txt"), new FileInfo(@"D:\dst\file.txt"), "file.txt", "UPD");
        /// </code>
        /// </example>
        public void SetDateToFile(System.IO.FileInfo sourceFileInfo, System.IO.FileInfo destinationFileInfo, String relativePath, String modeStr)
        {
            SetDateToFile(sourceFileInfo, destinationFileInfo, relativePath, modeStr, false, "");
        }

        /// <summary>
        /// FileInfoオブジェクトを使用してファイルのタイムスタンプおよびチェックサム表示を設定します。
        /// </summary>
        /// <param name="sourceFileInfo">ソースファイルのFileInfoオブジェクト</param>
        /// <param name="destinationFileInfo">宛先ファイルのFileInfoオブジェクト</param>
        /// <param name="relativePath">相対パス</param>
        /// <param name="modeStr">モード文字列</param>
        /// <param name="isShowCksum">チェックサムを表示するかどうか</param>
        /// <param name="srcCheckStr">チェック元の文字列</param>
        /// <example>
        /// <code>
        /// diffCopy.SetDateToFile(new FileInfo(@"C:\src\file.txt"), new FileInfo(@"D:\dst\file.txt"), "file.txt", "UPD", true, "sha1:abc");
        /// </code>
        /// </example>
        public void SetDateToFile(System.IO.FileInfo sourceFileInfo, System.IO.FileInfo destinationFileInfo, String relativePath, String modeStr, bool isShowCksum, String srcCheckStr)
        {
            bool isSetTimestamp = false;
            String sourceLastWriteTimeStr = MdlDate.GetFormattedDate(sourceFileInfo.LastWriteTime, "yyyy/MM/dd HH:mm:ss");
            String destinationLastWriteTimeStr = MdlDate.GetFormattedDate(destinationFileInfo.LastWriteTime, "yyyy/MM/dd HH:mm:ss");
            String resultStr = ">";
            if (_prop.IsCpTimestamp > 0)
            {
                isSetTimestamp = MdlFile.IsFileTimestampDifferent(sourceFileInfo, destinationFileInfo, _prop.SecRange, _prop.IsCpTimestamp);
                if (isSetTimestamp)
                {
                    try
                    {
                        if (!_prop.IsList)
                        {
                            if (_prop.IsCpTimestamp == 1 || _prop.IsCpTimestamp == 3) MdlFile.SetDateToFile(destinationFileInfo.FullName, sourceFileInfo.CreationTime, 1);
                            if (_prop.IsCpTimestamp == 2 || _prop.IsCpTimestamp == 3) MdlFile.SetDateToFile(destinationFileInfo.FullName, sourceFileInfo.LastWriteTime, 2);
                        }
                    }
                    catch { resultStr = "X"; }
                }
            }
            if (!String.IsNullOrEmpty(modeStr))
            {
                String outputPath = GetOutputRelativePath(relativePath);
                switch (_prop.OutputPathCode)
                {
                    case ClsProp.FROM:
                        outputPath = sourceFileInfo.FullName;
                        break;
                    case ClsProp.TO:
                        outputPath = destinationFileInfo.FullName;
                        break;
                    case ClsProp.BOTH:
                        outputPath = sourceFileInfo.FullName + " => " + destinationFileInfo.FullName;
                        break;
                }
                if (_prop.Verbose >= 0)
                {
                    if (isSetTimestamp)
                    {
                        if (isShowCksum)
                        {
                            EchoTitle("[" + modeStr + "][" + destinationLastWriteTimeStr + "=" + resultStr + sourceLastWriteTimeStr + "][" + srcCheckStr + "] " + outputPath);
                        }
                        else
                        {
                            EchoTitle("[" + modeStr + "][" + destinationLastWriteTimeStr + "=" + resultStr + sourceLastWriteTimeStr + "] " + outputPath);
                        }
                    }
                    else
                    {
                        if (isShowCksum)
                        {
                            EchoTitle("[" + modeStr + "][" + sourceLastWriteTimeStr + "][" + srcCheckStr + "] " + outputPath);
                        }
                        else
                        {
                            EchoTitle("[" + modeStr + "][" + sourceLastWriteTimeStr + "] " + outputPath);
                        }
                    }
                }
                else
                {
                    if (_prop.Verbose == -1)
                    {
                        EchoTitle("[" + modeStr + "] " + outputPath);
                    }
                    else
                    {
                        EchoTitle(outputPath);
                    }
                }
            }
        }

    }
}
