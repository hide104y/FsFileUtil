# FsFileUtil

## 事前作業
1. .NET SDKがインストールされていない場合はインストール：winget install -e --id Microsoft.DotNet.SDK.10
1. Github CLIがインストールされていない場合はインストール：winget install -e --id GitHub.cli
1. Powershellプロンプトを開く

## リポジトリ作成（未作成の場合）
```shell
# サインイン状態の確認
gh auth status
# 初回サインインしていない場合はサインイン
gh auth login
# 削除権限付与
gh auth refresh -h github.com -s delete_repo
# 作成
gh repo create FsFileUtil --private
# 確認
gh repo list | Select-String FsFileUtil
```

## リモートリポジトリ（mainブランチ）の取得
```shell
# CD
cd D:\Github\Projects
# フォルダが存在する場合は削除
if (-Not (Test-Path -Path .\FsFileUtil)){rmdir .\FsFileUtil}
# クローン実行
git clone https://github.com/hide104y/FsFileUtil.git
```

## リモートリポジトリ（mainブランチ）にREADME.mdが存在しない場合
```shell
# CD
cd D:\Github\Projects\FsFileUtil
# ファイル作成
ruby -e "File.write('README.md', '# FsFileUtil', encoding: 'UTF-8')"
# コミット
git add README.md
git commit -m "add README.md"
# プッシュ
git push -u origin main
# ブランチの一覧表示
git branch -a
```

## ブランチの作成
```shell
# ブランチをmainに切り替え・復元
git checkout main
# ブランチ作成
git checkout -b dotnet10
# 作成したブランチをリモートにプッシュ
git push -u origin dotnet10
```

## プロジェクトの作成
```shell
# コンソールアプリ：.net 10.0
cd D:\Github\Projects\FsFileUtil
dotnet new console --framework net10.0 -o FsFileUtil
```

## ソリューションファイルの作成
.\FsFileUtil\FsFileUtil.slnx
```xml
<Solution>
  <Project Path="FsFileUtil/FsFileUtil.csproj" />
  <Project Path="TestProject1/TestProject1.csproj" />
</Solution>
```

## プロジェクトファイルの修正
.\FsFileUtil\FsFileUtil\FsFileUtil.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <InvariantGlobalization>false</InvariantGlobalization>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="CmnClsLib">
      <HintPath>..\..\CmnClsLib\CmnClsLib\bin\Release\net10.0\CmnClsLib.dll</HintPath>
    </Reference>
    <Reference Include="CmnWinLib">
      <HintPath>..\..\CmnWinLib\CmnWinLib\bin\Release\net10.0\CmnWinLib.dll</HintPath>
    </Reference>
  </ItemGroup>

</Project>
```

## 依存パッケージ
```shell
# CD
cd D:\Github\Projects
# 依存プロジェクト参照の追加
dotnet add .\FsFileUtil\FsFileUtil\FsFileUtil.csproj reference .\CmnClsLib\CmnClsLib\CmnClsLib.csproj
dotnet add .\FsFileUtil\FsFileUtil\FsFileUtil.csproj reference .\CmnWinLib\CmnWinLib\CmnWinLib.csproj
# 依存パッケージのインストール
(なし)
```

## コーディング
(省略)

## AIレビュー
```shell
# CD
cd D:\Github\Projects
agy
.\FsFileUtil\FsFileUtil\Class\ClsAppArg.csに対して、スキル「source-review」を実行して
/clear
.\FsFileUtil\FsFileUtil\Program.csに対して、スキル「source-review」を実行して
/exit
```

## ビルド
```shell
# CD
cd D:\Github\Projects
# パッケージの最新化
dotnet package update --file .\FsFileUtil\FsFileUtil.slnx
# ビルド
dotnet build .\FsFileUtil\FsFileUtil.slnx -c Release -p:InvariantGlobalization=false
dotnet build .\FsFileUtil\TestProject1\TestProject1.csproj
# 単体テスト
dotnet test .\FsFileUtil\TestProject1\TestProject1.csproj
# Usage
FsFileUtil\FsFileUtil\bin\Release\net10.0\FsFileUtil.exe -h
```

## リポジトリにコミット
```shell
cd D:\Github\Projects\FsFileUtil
git switch dotnet10
git add .
git commit -m "README.mdの修正"
git push -u origin dotnet10
```

## デプロイ
```shell
dotnet publish .\FsFileUtil\FsFileUtil\FsFileUtil.csproj -c Release -o D:\Github\bin.n10 -r win-x64 --self-contained=false -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:PublishTrimmed=false -p:PublishAot=false -p:InvariantGlobalization=false
```

## リモートリポジトリの確認
- https://github.com/hide104y/FsFileUtil/tree/dotnet10
<br>※GitHubの画面で「Compare & pull request」が表示されるが放置

## リモートリポジトリ（dotnet10ブランチ）の取得
```shell
# CD
cd D:\Github\Projects
# フォルダが存在する場合は削除
if (-Not (Test-Path -Path .\FsFileUtil)){rmdir .\FsFileUtil}
# クローン実行
git clone -b dotnet10 https://github.com/hide104y/FsFileUtil.git
```

## License
- These codes are licensed under CC0.
- http://creativecommons.org/publicdomain/zero/1.0/deed.ja
