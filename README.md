# ホロデビュー

Unity向けのローカル2～4人用すごろくプロトタイプです。空のリポジトリから、基本仕様v0.3のA段階のコードと、それを利用する耐久配信v0.1を追加しています。

**Unity Editorはこのクラウドにないため、Unityでのコンパイル・Play・Windowsビルドは未検証です。外部の皿モデルも未取得です。完成判定には下記のUnity検証が必要です。** 実施済みの検証と制約は [docs/Validation.md](docs/Validation.md) に分けて記録しています。

## 起動

1. Unity Hubから **Unity 6.3 LTS / 6000.3.5f2** でこのフォルダーを開きます。新規プロジェクト用の指定バージョンです。この環境にEditorが導入済みという意味ではありません。
2. UPMの依存解決とC#コンパイルを待ちます。URPは17.3.0です。Editor処理が初回に `Assets/HoloDebut/Generated` 以下へURPのRenderer・Pipeline・仮Materialを用意します。必要なら `HoloDebut > Prepare URP and build scene` を実行します。
3. 保存済みの `Assets/HoloDebut/Scenes/Prototype.unity` を開き、Playします。このシーンはGameRootと設定参照を保存したブートストラップです。盤面・カメラ・Canvas・EventSystem・駒・皿はPlay時に構築します。
4. 初期はP1 Pekora、P2 Marine、P3 Flare、P4 Noelです。`Data/Session.asset` のplayers配列を2～4件に変更できます。同じキャラクターを複数指定してもplayerIdは独立します。

初回のUnityインポートは `packages.unity.com` 等への通常のUPMアクセスが必要です。WindowsビルドにはHubのWindows Build Supportが必要です。メジャーバージョンやパッケージを一括更新する必要はありません。

## パッケージの署名がInvalidになる場合

Unity公式のPackage Managerトラブルシューティングによると、**6000.3.5f2より前の6.3では正常な署名もInvalidと判定される既知不具合**があります。このプロジェクトの指定を6000.3.5f2へ変更しました。

Unity Hubで6000.3.5f2以降の6.3 LTSをインストールし、プロジェクトの「⋮」→「Unityバージョンを選択」でそのEditorを指定してください。Editorを追加しただけでは、古い6000.3.0f1で開く場合があります。起動後のタイトルバーでもバージョンを確認します。署名検証は無効にしません。

ZIPで取得した既存プロジェクトは編集済みファイルをバックアップしたうえで、`ProjectSettings/ProjectVersion.txt` の指定を更新してください。Git利用時は最新mainを取り込めます。PCへのEditorのインストール・切替はGitの更新だけでは実行されません。

## 操作

- **Space / ROLL・PROGRESS領域を新たに押す**：物理投球を開始。結果表示から移動、着地、手番終了までは自動です。
- **処理中に0.2秒長押し**：2倍速。実時間で長押し判定し、物理・表示・移動はゲーム内時間で進めます。`fixedDeltaTime` はゲーム内1/120秒のままです。
- **Use Rocket Carrot**：通常は自分の投球前に1ターン1個。次の最終移動だけ＋5です。所持上限3個、最初に1個あります。
- **企画マスへ着地**：通常ゲーム配信か耐久配信を選びます。耐久は強制発生しません。
- **耐久**：曜日を問わず1個の物理サイコロ。EXITへぴったり停止すれば成功。アイテムを使うと本来の＋5に加えて追加投球を1回得ます。同一投球での重ね掛けは禁止です。投球回数を使い切った場合だけ、アイテム使用か終了を選べます。所持品もない場合は自動失敗です。
- **Retry same physical roll**：無効投球5回で停止した場合の再試行。使用済みアイテム・ピンゾロ累積を保持します。
- **Restart NEW game**：全状態を初期化します。同じ手番の再試行とは別操作です。

ボタンを押したまま次のプレイヤーへ進んでも自動投球しません。一度離してから押します。Spaceが選択済みアイテムボタンのSubmitとして働かないよう、UIナビゲーションを無効にしています。

## 実装の範囲

通常ゲームは曜日、丁半、チンチロ、反復式ピンゾロ連鎖、接続データによる1歩移動、実移動の登録者、着地アイテム、ロケットニンジン、到着順、ゴール後投球、全員到着終了、同率順位と共催エンディングを実装しています。物理サイコロは結果を事前抽選せず、Rigidbodyの回転・支持接触・静止時間・上面の内積から読み取ります。全個体を同じ物理ステップで投げます。数値強制による救済はありません。

耐久は能力と設定の開始時スナップショット、環状盤面、専用駒、着地だけの技能／資金報酬、早期脱出報酬、アイテム継続、挑戦IDによる古い通知の拒否、元の通常位置の保持と手番終了を追加しています。

本仕様で位置が未指定の「配信企画を考えるマス」は、追加機能用の**仮配置10・40・70・100番**としました。20・50・80・110番のItemPickupと30・60・90番の将来用チェックポイントは維持しています。`Data/Board.asset` の `planningNodes` で配置変更、`enablePlanning` をOFFで元のA段階の盤面に戻せます。非企画のEmptyマスでは配信報酬を発生させません。

技能経験値の換算式は追加仕様にないため、耐久の試作では**経験値1＝技能1ポイント、上限100**としています。画面の獲得XPは上限で切り詰めた実際の成長量です。スタミナもPlayerStateの能力として成長APIを持ち、初期値は全員50です。耐久自体ではスタミナを消費・自動育成しません。

B段階全般（案件、賭け、借金、関係値、各ジャンルの配信マス）、C段階のコラボ／ミニゲーム、D段階のオンラインは未実装です。企画選択の「通常ゲーム配信」だけは機能する対案として実装し、試作用の倍率抽選・報酬・技能成長を行います。未実装機能のメニューはありません。

## 設定・差し替え

| 対象 | 参照先 |
| --- | --- |
| 人数、初期値、移動・表示時間、各設定 | `Data/Session.asset` |
| キャラクター、色、任意の駒Prefab・肖像、技能、成長係数 | `Data/pekora.asset` など |
| 仮盤面の規模、間隔、企画マス | `Data/Board.asset` |
| 投球・停止・無効条件、材質、初速・角速度 | `Data/DicePhysics.asset` |
| 到着順・ゴール後報酬 | `Data/GoalRewards.asset`、`Data/PostGoalRewards.asset` |
| ロケットニンジンの定義 | `Data/RocketCarrot.asset` |
| 通常ゲーム配信の報酬・確率・共通成長係数 | `Data/StreamPlan.asset` |
| 耐久の技能境界・マス数・着地／成功報酬・半径 | `Data/Endurance.asset` |
| 終了カード、任意画像・演出参照 | `Data/*_Ending.asset` |
| 最小の仮UI | `Presentation/PrototypePresenter.cs` |

Prefabは `Prefabs/Dice.prefab`、`Bowl.prefab`、`Pawn.prefab`、`Tile.prefab` を保存しています。Rootの専用コンポーネントが初期化時にVisualと必要な物理部品を構成します。外観の交換でルールを変更する必要はありません。

- サイコロは `DiceBody.Initialize` のVisualと面ラベルを交換します。RootのBoxCollider／Rigidbodyと `DiceFaceDetector` の法線対応を保ち、点や数字にColliderを付けません。面法線は＋Y=1、－Y=6、＋Z=2、－Z=5、＋X=3、－X=4です。
- 駒は `CharacterDefinition.pawnPrefab` で専用外観に置き換えられます。Rootのスケール1、足元原点を保ち、動的なRigidbodyは付けません。
- 皿は現在、実行時に作る静的・非Convexの凹面MeshColliderです。外部モデル導入時は `BowlSurface.Initialize` のVisual構築部をFBXの子へ交換し、Collisionと有効内側領域を実モデルへ合わせます。Root全体をConvexにはしません。外部モデルの取得状況はThirdPartyの記録を参照してください。
- 画像と演出は任意参照として保持します。現状の仮画面は英数字と色で表示し、日本語名はデータ内に保持しています。画像・ボイス表示や豪華な演出は含みません。

## テスト・ビルド

Unityなしで純粋C#部分を検証するには.NET 8 SDKを使います。クラウドでは `/workspace/.tools/dotnet/dotnet` に導入済みです。

```sh
dotnet restore Tools/DomainTests/DomainTests.csproj --locked-mode
dotnet test Tools/DomainTests/DomainTests.csproj --no-restore --logger 'trx;LogFileName=domain-results.trx' --results-directory TestResults
dotnet run --project Tools/SyntaxCheck/SyntaxCheck.csproj -- Assets
python3 Tools/check_assets.py
```

UnityではTest RunnerのEditMode／PlayModeをそれぞれ実行してください。保存済みのC#テストはUnityと.NETで同じDomainソースを使います。構文検査はUnity APIの型検査や物理検証ではありません。

PlayModeの物理バッチは、1倍速・2個20回、1倍速・3個20回、2倍速・2個20回、2倍速・3個20回です。実行すると `TestResults/physics_*dice_*x.json` へ有効率の元データ、再投球、停止時間、無効理由を保存します。5回無効で止まればテストも失敗します。未実行なので有効率を推測していません。

Windows向けは `HoloDebut > Build Windows prototype` または `-executeMethod HoloDebut.Editor.PrototypeSetup.BuildWindows` で `Builds/Windows/HoloDebut.exe` を生成します。詳細な手動確認項目は [docs/Validation.md](docs/Validation.md) にあります。

元の基本仕様は [docs/BaseSpecification-v0.3.txt](docs/BaseSpecification-v0.3.txt)、耐久の対応表は [docs/Endurance.md](docs/Endurance.md) に保存しています。
