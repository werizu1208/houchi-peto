# 最初の試作：タスクバーの上を歩く

透明・最前面のウィンドウで、ドットのペットがタスクバーの上を歩くだけの試作。
Unity 側で一番リスクの高い「透明ウィンドウ」が動くかを確かめるのが目的。

## できること

- 枠なし・背景透明・最前面の 128×128 ピクセルのウィンドウ（タスクバーと Alt+Tab には出ない）
- ペトが止まったり歩いたりしながら、タスクバーの上を左右に行き来する
- ペトのドットの上以外では、クリックが下のアプリへ素通りする
- ペトをクリックすると跳ねる
- 30fps に制限（他のゲームの裏で動いても重くならないように）

ペトの絵は仮（`PlaceholderPetSprite.cs` がコードで作る 16×16 のドット絵）。
シーンに何も置かなくても、起動時に `Bootstrap.cs` がカメラとペトを用意する。

## 動かし方

1. Unity Hub で、このリポジトリのフォルダを「追加」→「ディスクから追加」で開く
   - 開けない場合は、別の場所に **2D（Built-In Render Pipeline）** で新規プロジェクトを作り、
     その `Packages/` と `ProjectSettings/` をこのフォルダにコピーしてから開く
2. メニュー **放置ペト > 試作用の Player Settings を適用** を実行する
   - flip model スワップチェーンのオフ、Direct3D11、ウィンドウモード 128×128、バックグラウンド実行 など
3. **File > Build Profiles**（古い版は Build Settings）で Windows を選んでビルドし、できた exe を起動する

エディタの再生でも、ゲームビューの中でペトが歩く。ただし透明化とタスクバーの上を歩く動きは、ビルドした exe でしか確かめられない。

## 確かめたいこと

- [ ] 背景が透明になるか（黒くなったら下の「うまくいかないとき」）
- [ ] タスクバーの上にちょうど立っているか
- [ ] ペト以外の場所をクリックしたとき、下のアプリが操作できるか
- [ ] ペトをクリックすると跳ねるか
- [ ] 他のゲームを遊んでいるとき、CPU や GPU の使用率が目に見えて上がらないか

## うまくいかないとき

- **背景が黒い**：Player Settings の「Use DXGI flip model swapchain」がオフになっているか、Graphics API が Direct3D11 だけになっているかを確認する。URP のプロジェクトなら、カメラの背景色のアルファが 0 か、HDR とポストプロセスがオフかも確認する
- **ウィンドウが消える**：クリックの素通り（WS_EX_LAYERED）が原因の可能性がある。`DesktopWindow.SetClickThrough` を一時的に呼ばないようにして切り分ける
- **自前の実装で詰まったら**：UniWindowController（kirurobo、MIT）に置き換える

## ファイル

| ファイル | 役割 |
| --- | --- |
| `Assets/Scripts/Bootstrap.cs` | 起動時にカメラ・ウィンドウ・ペトを用意する |
| `Assets/Scripts/Desktop/DesktopWindow.cs` | 透明・最前面・クリック素通りのウィンドウにする、ウィンドウを動かす |
| `Assets/Scripts/Desktop/Taskbar.cs` | タスクバーの位置から、ペトが歩く床を求める |
| `Assets/Scripts/Desktop/Win32.cs` | Windows API の宣言 |
| `Assets/Scripts/Pet/PetWalker.cs` | 止まる・歩く・跳ねる、クリックの当たり判定 |
| `Assets/Scripts/Pet/PlaceholderPetSprite.cs` | 仮のドット絵 |
| `Assets/Editor/PrototypeSetup.cs` | 試作用の Player Settings をまとめて設定するメニュー |

## 試作で割り切ったこと

- メインモニターのみ。タスクバーが上・左・右にあるときは画面の下端を歩く
- ペトをクリックするとウィンドウがアクティブになる（前面のアプリからフォーカスが移る）
