using HoloDebut.Domain;

namespace HoloDebut.Presentation
{
    public static class JapaneseText
    {
        public static string PlayerName(int number, string japaneseName) =>
            "P" + number + " " + (string.IsNullOrWhiteSpace(japaneseName) ? "プレイヤー" + number : japaneseName);

        public static string Day(int day) => new[] { "月", "火", "水", "木", "金", "土", "日" }[day] + "曜日";

        public static string Phase(SessionPhase phase)
        {
            switch (phase)
            {
                case SessionPhase.AwaitRoll: return "投球待ち";
                case SessionPhase.Rolling: return "投球中";
                case SessionPhase.PreparingPinzoroReroll: return "ピンゾロ・再投球";
                case SessionPhase.Moving: return "移動中";
                case SessionPhase.EndingTurn: return "手番終了処理";
                case SessionPhase.AwaitPlan: return "配信企画を選択";
                case SessionPhase.RunningSubgame: return "耐久配信中";
                case SessionPhase.Finished: return "ゲーム終了";
                case SessionPhase.Cancelled: return "中断";
                default: return "状態を確認中";
            }
        }

        public static string Role(DiceRole role)
        {
            switch (role)
            {
                case DiceRole.Cho: return "丁";
                case DiceRole.Han: return "半";
                case DiceRole.Pinzoro: return "ピンゾロ";
                case DiceRole.Triple: return "ゾロ目";
                case DiceRole.Shigoro: return "シゴロ";
                case DiceRole.Hifumi: return "ヒフミ";
                case DiceRole.Pair: return "通常の目";
                case DiceRole.NoRole: return "役なし";
                default: return "役を確認中";
            }
        }

        public static string RollResult(DiceResult result, long bonus, long item, long planned, bool postGoal, long gained)
        {
            string faces = "出目 " + string.Join("・", result.RawFaces);
            if (result.RequiresReroll)
                return faces + "：ピンゾロ\n" + (postGoal ? "登録者 " + bonus + "人を累積。" : "移動量は未確定、累積＋" + bonus + "マス。") + "3個を再投球します。";
            if (postGoal) return faces + "：" + Role(result.Role) + "\nゴール後の登録者報酬 ＋" + gained + "人";
            return faces + "：" + Role(result.Role) + "\n基本 " + result.BaseMoveSteps + "＋累積 " + bonus + "＋道具 " + item + "＝" + planned + "マス";
        }

        public static string InvalidRollReason(string reason)
        {
            // Keep the raw reason in the diagnostic history; localize only its player-facing display.
            switch (reason)
            {
                case "Overlapping spawn points": return "サイコロの開始位置が重なっています";
                case "Spawn intersects bowl": return "サイコロの開始位置が皿と重なっています";
                case "Non-finite physics state": return "物理状態の数値が不正です";
                case "Out of flight bounds": return "サイコロが場外へ出ました";
                case "Stopped outside bowl interior": return "皿の有効範囲外で停止しました";
                case "Ambiguous upper face": return "斜めに停止し、上面を確定できません";
                case "Timeout (unsupported/stacked/unsettled die)": return "制限時間内に有効な停止を確認できません";
                case "Stacked on another die": return "サイコロが別のサイコロに乗っています";
                case "Landed on table/floor": return "サイコロが台・床へ着地しました";
                default: return "投球条件を確認してください（" + reason + "）";
            }
        }
    }
}
