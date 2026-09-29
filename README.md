# RPT Carrier

RPT Carrier は、**Steam Remote Play Together でうまく共有できないゲームを、[RemotePlayDetached](https://github.com/smaTc/RemotePlayDetached) と組み合わせて共有しやすくするための Windows 向け補助ツール**です。

特に、次のような症状が出るゲームを想定しています。

- RemotePlayDetached 経由では起動できるが、ゲスト側の入力が通らない
- ゲスト側に「ホストがビジーです」などと表示され、操作できない
- Steam Overlay が実際のゲーム側にうまく表示されない
- NW.js 製ゲームで映像は見えるが入力できない
- NW.js 製ゲームで映像と入力は通るが、ゲスト側だけ音が出ない

> **非公式ツールです。**  
> RPT Carrier は Valve / Steam / NW.js / RemotePlayDetached の公式ツールではなく、各プロジェクトからの公認・サポートを受けたものでもありません。

## これは何をするツール？

通常の流れは次の通りです。

1. RemotePlayDetached から `RPTCarrier.exe` を起動
2. RPT Carrier を Steam Remote Play Together の共有元として認識させる
3. ゲームの EXE を選ぶ
4. Steam Overlay からフレンドを招待
5. RPT Carrier の **「ゲーム起動」** を押す
6. 実際のゲームを起動し、前面操作をゲーム側へ引き渡す
7. ゲーム終了時に RPT Carrier も終了し、Steam の donor AppID を解放

RPT Carrier 単体で Steam Remote Play Together を置き換えるものではありません。

**通常の共有がうまくいかないゲームを RemotePlayDetached と組み合わせて共有するための補助ツール**です。

---

## 必要なもの

- Windows 10 以降
- Steam
- [RemotePlayDetached](https://github.com/smaTc/RemotePlayDetached)
- .NET Framework 4.x
- Steam Remote Play Together 対応の donor ゲーム

---

## ビルド方法

Visual Studio のプロジェクトは不要です。

`build.cmd` を実行してください。

```bat
build.cmd
```

.NET Framework に付属している C# コンパイラを使って、次の2つを生成します。

```text
RPTCarrier.exe
RPTCarrier.x86.exe
```

通常は `RPTCarrier.exe` を使えば大丈夫です。

---

## いちばん簡単な使い方

RemotePlayDetached 側で次のように設定します。

```text
Path:
C:\path\to\RPTCarrier.exe

Arguments:
（空欄）
```

RemotePlayDetached から RPT Carrier を起動すると、Windows のファイル選択画面が開きます。

そこで共有したいゲームの EXE を選択してください。

その後、

1. Steam Overlay を開く
2. Remote Play Together でフレンドを招待
3. Overlay を閉じる
4. RPT Carrier の **「ゲーム起動」** を押す

これでゲームを起動できます。

---

## EXEパスを引数で指定する

ファイル選択画面を使わず、直接 EXE を指定することもできます。

```text
RPTCarrier.exe --target C:\Games\MyGame\Game.exe
```

RemotePlayDetached 側から使う場合も、空白を含むパスに対応しています。

主なオプション：

```text
--target <path>              起動するゲームEXE
--handoff                    推奨の引き渡しモード
--controller-probe           XInput / WinMM の診断を有効化
--nw-overlay-compat          NW.js互換処理を明示的に有効化
--no-nw-overlay-compat       NW.js互換処理を明示的に無効化
--force-nw-in-process-gpu    NW.js向け実験オプション
```

**明示的に指定した引数は、自動判定より優先されます。**

---

## NW.js の自動判定

RPT Carrier は、選択された EXE が NW.js 製かどうかを自動判定します。

次のどれかに当てはまると NW.js と判断します。

- EXE 名が `nw.exe`
- ProductName / FileDescription に `NW.js` または `NWJS` が含まれる
- EXE と同じフォルダに `nw_elf.dll` が存在する

そのため、NW.js の EXE 名を変更したゲームでも判定できる場合があります。

NW.js を検出すると、開発中に必要だった互換処理を自動適用します。

対象：

- Steam Overlay のフック
- Remote Play Together の入力
- Chromium / DirectComposition の互換性
- ゲスト側の音声キャプチャ

---

## コントローラー診断

ゲーム起動前の画面下部には、XInput と WinMM の検出状態が表示されます。

例：

```text
XInput 60Hz  1:[ON] 2:[--] 3:[--] 4:[--]  WinMM:[1]
Last input: XInput 1 buttons=A ...
```

これは診断用です。

Remote Play Together 側がゲストのコントローラーをどのように割り当てるかは Steam 側の処理になります。

---

## トラブルシューティング

### ゲスト側に「ホストがビジー」と表示される

NW.js 製ゲームの場合、最新の RPT Carrier を使ってください。

通常は NW.js を自動判定して互換設定を適用します。

`--no-nw-overlay-compat` を指定している場合は外して試してください。

---

### ホスト側では音が出るのに、ゲスト側だけ無音

NW.js 製ゲームでは Chromium の音声処理が別プロセスになることがあります。

RPT Carrier は NW.js を検出した場合、Remote Play で音声を拾いやすくする互換処理を自動適用します。

---

### donor が「実行中」「Busy」のまま残る

現在の handoff モードでは、ゲーム終了時に RPT Carrier も終了して donor AppID を解放します。

古いバージョンの Carrier が残っている場合は、次で終了できます。

```bat
taskkill /IM RPTCarrier.exe /F
```

---

### 起動時に EXE を指定していない

問題ありません。

引数なしで起動すると、自動的に EXE 選択画面が開きます。

---

## 設定について

公開版では、できるだけ単純に使えるようにしています。

通常は INI ファイル不要です。

使い方は次のどちらかです。

- 引数なしで起動し、EXE をファイル選択画面から選ぶ
- `--target` などの引数を指定する

---

## ライセンス

RPT Carrier 本体のソースコードは **MIT License** で公開します。

詳細は [LICENSE](LICENSE) を参照してください。

Steam / Valve、NW.js、RemotePlayDetached、Windows / Microsoft などの権利は、それぞれの権利者に帰属します。

詳細は [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) を参照してください。

---

## クレジット

RPT Carrier は、次の仕組みと組み合わせて利用することを想定しています。

- [RemotePlayDetached](https://github.com/smaTc/RemotePlayDetached)
- Steam Remote Play Together
- NW.js など、Remote Play Together でそのまま共有すると問題が出るランタイム

RemotePlayDetached は別プロジェクトです。

RPT Carrier のリポジトリには RemotePlayDetached 本体やバイナリは同梱していません。

---

## English summary

RPT Carrier is an unofficial Windows helper intended to be used with RemotePlayDetached when Steam Remote Play Together does not share a game correctly.

It can help with guest input, host-busy states, Steam Overlay attachment, NW.js video/input compatibility, and NW.js remote audio capture.

For most users:

```text
RemotePlayDetached
Path: RPTCarrier.exe
Arguments: (empty)
```

Then select the game EXE, invite the guest through Steam Overlay, and press Game Launch.
