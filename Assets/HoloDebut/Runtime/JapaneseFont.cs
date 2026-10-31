using System;
using UnityEngine;

namespace HoloDebut.Runtime
{
    public static class JapaneseFont
    {
        public const string ResourcePath = "HoloDebut/NotoSansJP-Regular";
        private static Font cached;
        public static Font Load()
        {
            if (!cached) cached = Resources.Load<Font>(ResourcePath);
            if (!cached) throw new InvalidOperationException("同梱の日本語フォントを読み込めません。NotoSansJP-Regular.otfのインポート状態を確認してください。");
            return cached;
        }
    }
}
