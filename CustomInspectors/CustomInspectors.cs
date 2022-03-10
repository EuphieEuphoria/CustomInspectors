using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.UIX;
using BaseX;
using FrooxEngine.LogiX.Color;
using FrooxEngine.LogiX.Input;
using FrooxEngine.LogiX.Operators;

namespace CustomInspectors
{
    public class CustomInspector : NeosMod
    {
        public override string Name => "Custom Scene Inspector";
        public override string Author => "EuphieEuphoria";
        public override string Version => "1.0";


        //Defining constant asset Uris
        static Uri backPanelAlbedoMapUri = new Uri("neosdb:///ffc927b7d2d7d63a4ce888e3aabea5ac7e746f0abe1b4cb69051b01278046a3e.png");
        static Uri panelNormalMapUri = new Uri("neosdb:///95ef1fd8a153ad3d4c2588563274f961da94b812f90ffb4a235e624684c8e332");
        static Uri panelMSMapUri = new Uri("neosdb:///a38400e37e4e6b96d2e49557e0c7f614475edef637b2474f4c017f7e3f4971dc");
        static Uri fontUri = new Uri("neosdb:///0b82ab5fdba8e0147e38e89237ea4a430f0d7017c313d9b8e56a309acde756c0.ttf");

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("net.euphie.customInspector");
            harmony.PatchAll();

        }
        //This patch will patch the "OnAttach" function of the Scene Inspector, allowing us to do some setup whenever the inspector is spawned
        [HarmonyPatch(typeof(InspectorPanel),"OnAttach")]
        class SceneInspectorPatch
        {
            static void Prefix(SceneInspector __instance)
            {
                __instance.Slot.Name = "Fancy Metal Inspector";

                Slot Assets = __instance.Slot.AddSlot("Assets");
                Assets.Tag = "MetalInspector.Assets";

                StaticFont staticFont = Assets.AttachFont(fontUri);
                staticFont.GlyphEmSize.Value = 32;



            }

        }

        [HarmonyPatch(typeof(InspectorPanel), "Setup")]
        class InspectorPanelPatch
        {

            static void Postfix( InspectorPanel __instance, NeosPanel __result)
            {
                if (__instance.Slot.Name != "Fancy Metal Inspector")
                    return;

                Slot slot = __instance.Slot.FindChild(ch => ch.Name.Equals("Panel"), 1);
                Slot HandleSlot = __instance.Slot.FindChild(ch => ch.Name.Equals("Handle"), 1);
                Slot TitleSlot = __instance.Slot.FindChild(ch => ch.Name.Equals("Title Mesh"), 2);
                Slot TitleText = __instance.Slot.FindChild(ch => ch.Name.Equals("Title"), 2);
                Slot AssetsSlot = __instance.Slot.FindChild(ch => ch.Tag.Equals("MetalInspector.Assets"));
                Slot ContentSlot = __instance.Slot.FindChild(ch => ch.Name.Equals("Image"),1);
                


                PBS_TriplanarMetallic NewPanelMat = AssetsSlot.AttachComponent<PBS_TriplanarMetallic>(true, null);
                //Get our awesome font
                StaticFont staticFont = AssetsSlot.GetComponent<StaticFont>();
                //Making the static texture for the normal map
                StaticTexture2D panelNormalMap = AssetsSlot.AttachComponent<StaticTexture2D>(true, null);
                panelNormalMap.URL.Value = panelNormalMapUri;
                panelNormalMap.IsNormalMap.Value = true;
                panelNormalMap.CrunchCompressed.Value = false;
                panelNormalMap.PreferredFormat.Value = CodeX.TextureCompression.RawRGBA;
                panelNormalMap.FilterMode.Value = TextureFilterMode.Anisotropic;
                panelNormalMap.AnisotropicLevel.Value = 16;
                //Making the static texture for the MetallicSmoothness map
                StaticTexture2D panelMSMap = AssetsSlot.AttachComponent<StaticTexture2D>(true, null);
                panelMSMap.URL.Value = panelMSMapUri;
                panelMSMap.FilterMode.Value = TextureFilterMode.Anisotropic;
                panelMSMap.AnisotropicLevel.Value = 16;

                NewPanelMat.AlbedoColor.Value = new color(0.7686275f, 0.7803922f, 0.7803922f, 1f);
                NewPanelMat.ObjectSpace.Value = true;
                NewPanelMat.NormalMap.Target = panelNormalMap;
                NewPanelMat.MetallicMap.Target = panelMSMap;
                NewPanelMat.TextureScale.Value = new float2(4f, 4f);

                PBS_TriplanarMetallic gold = AssetsSlot.DuplicateComponent<PBS_TriplanarMetallic>(NewPanelMat, false);
                gold.AlbedoColor.Value = new color(1f, 0.89f, 0.61f, 1f);

                slot.GetComponents<MeshRenderer>(null, false)[0].Material.Target = NewPanelMat;
                slot.GetComponents<MeshRenderer>(null, false)[1].Destroy();

                HandleSlot.GetComponents<MeshRenderer>(null, false)[0].Material.Target = gold;
                HandleSlot.GetComponents<MeshRenderer>(null, false)[1].Destroy();

                TitleSlot.GetComponents<MeshRenderer>(null, false)[0].Material.Target = gold;
                TextRenderer textRenderer = TitleText.GetComponents<TextRenderer>(null, false)[0];
                textRenderer.Font.Target = staticFont;
                textRenderer.Color.Value = new color(1f, 1f, 1f, 1f);
                textRenderer.Size.Value = .9f;

                __instance.RunInUpdates(3, () =>
                 {
                     SceneInspector inspector = __instance as SceneInspector;

                     Slot hierarchy = (AccessTools.Field(typeof(SceneInspector), "_hierarchyContentRoot").GetValue(inspector) as SyncRef<Slot>).Target;
                     Slot components = (AccessTools.Field(typeof(SceneInspector), "_componentsContentRoot").GetValue(inspector) as SyncRef<Slot>).Target;

                     hierarchy.GetComponentsInParents<Image>()[1].Tint.Value = new color(.58f, .51f, .38f, .76f);
                     components.GetComponentsInParents<Image>()[1].Tint.Value = new color(.36f, .52f, .52f, .76f);
                 });

                StaticTexture2D backSpriteTexture = AssetsSlot.AttachComponent<StaticTexture2D>(true, null);
                backSpriteTexture.URL.Value = backPanelAlbedoMapUri;
                backSpriteTexture.FilterMode.Value = TextureFilterMode.Anisotropic;
                backSpriteTexture.AnisotropicLevel.Value = 16;
                UnlitMaterial backSpriteUnlit = AssetsSlot.AttachComponent<UnlitMaterial>(true, null);
                backSpriteUnlit.Texture.Target = backSpriteTexture;
                backSpriteUnlit.TintColor.Value = new color(1.25f, 1.25f, 1.25f, 1f);
                backSpriteUnlit.BlendMode.Value = BlendMode.Alpha;


                Slot backSprite = slot.AddSlot("Back Panel Sprite");
                QuadMesh coolBackMesh = backSprite.AttachMesh<QuadMesh>(backSpriteUnlit, false, 0);
                coolBackMesh.Size.Value = new float2(.4f, .4f);
                backSprite.LocalPosition = new float3(0f, 0f, .0053f);
                backSprite.LocalRotation = floatQ.Euler(0f, 180f, 0f);

                Slot colorDriver = backSprite.AddSlot("Color Driver");

                List<IField<color>> colorTargets = new List<IField<color>>();
                colorTargets.Add(coolBackMesh.UpperLeftColor);
                colorTargets.Add(coolBackMesh.LowerLeftColor);
                colorTargets.Add(coolBackMesh.LowerRightColor);
                colorTargets.Add(coolBackMesh.UpperRightColor);

                var T = colorDriver.AttachComponent<TimeNode>();
                var TMulti = colorDriver.AttachComponent<Mul_Float>();
                var TMultiValue = colorDriver.AttachComponent<ValueNode<float>>();
                TMultiValue.Value.Value = .25f;

                var colorRot = .25f;

                var colorSaturation = colorDriver.AttachComponent<ValueNode<float>>();
                colorSaturation.Value.Value = .75f;

                var colorValue = colorDriver.AttachComponent<ValueNode<float>>();
                colorValue.Value.Value = 1f;


                TMulti.A.Target = T;
                TMulti.B.Target = TMultiValue;

                for (int i = 0; i < colorTargets.Count; i++)
                {
                    var colorRotHolder = colorDriver.AttachComponent<ValueNode<float>>();
                    colorRotHolder.Value.Value = colorRot * i;
                    var addition = colorDriver.AttachComponent<Add_Float>();
                    addition.A.Target = TMulti;
                    addition.B.Target = colorRotHolder;
                    var hsv = colorDriver.AttachComponent<HSV_ToColor>();
                    hsv.H.Target = addition;
                    hsv.S.Target = colorSaturation;
                    hsv.V.Target = colorValue;
                    var driver = colorDriver.AttachComponent<DriverNode<color>>();
                    driver.Source.Target = hsv;
                    driver.DriveTarget.Target = colorTargets[i];

                }

            }

        }
    }
}
