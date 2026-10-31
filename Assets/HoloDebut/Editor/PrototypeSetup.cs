using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HoloDebut.Editor
{
    [InitializeOnLoad]
    public static class PrototypeSetup
    {
        private const string Generated="Assets/HoloDebut/Generated";
        private const string Scene="Assets/HoloDebut/Scenes/Prototype.unity";
        static PrototypeSetup(){EditorApplication.delayCall+=EnsureRendering;}
        [MenuItem("ホロデビュー/描画設定と起動シーンを準備")]
        public static void EnsureRendering()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            Directory.CreateDirectory(Generated+"/Resources");AssetDatabase.Refresh();
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Generated+"/Renderer.asset");
            if(!renderer){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,Generated+"/Renderer.asset");}
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Generated+"/Pipeline.asset");
            if(!pipeline){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,Generated+"/Pipeline.asset");}
            if(!GraphicsSettings.defaultRenderPipeline)GraphicsSettings.defaultRenderPipeline=pipeline;
            if(!QualitySettings.renderPipeline)QualitySettings.renderPipeline=GraphicsSettings.defaultRenderPipeline;
            string mat=Generated+"/Resources/HoloDebutPrototypeMaterial.mat";
            if(!AssetDatabase.LoadAssetAtPath<Material>(mat))
            {
                var shader=Shader.Find("Universal Render Pipeline/Lit");
                if(!shader){Debug.LogError("URPのLitシェーダーがありません。パッケージの読み込み完了後に実行してください。");return;}
                AssetDatabase.CreateAsset(new Material(shader),mat);
            }
            if(EditorBuildSettings.scenes.Length==0)EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            AssetDatabase.SaveAssets();
        }
        [MenuItem("ホロデビュー/Windows向けにビルド")]
        public static void BuildWindows()
        {
            EnsureRendering();Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/Windows/HoloDebut.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Windowsビルドに失敗しました："+report.summary.result);
            Directory.CreateDirectory("Builds/Windows/ThirdPartyLicenses");
            File.Copy("Assets/ThirdParty/Noto/LICENSE.txt","Builds/Windows/ThirdPartyLicenses/NotoSansJP-LICENSE.txt",true);
            File.Copy("Assets/ThirdParty/Noto/README.md","Builds/Windows/ThirdPartyLicenses/NotoSansJP-NOTICE.md",true);
        }
    }
}
