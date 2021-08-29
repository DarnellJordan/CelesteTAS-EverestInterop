﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using TAS.EverestInterop.Hitboxes;
using TAS.EverestInterop.InfoHUD;
using TAS.Input;
using TAS.Utils;

namespace TAS.EverestInterop {
    internal static class Menu {
        private static readonly MethodInfo CreateKeyboardConfigUi = typeof(EverestModule).GetMethodInfo("CreateKeyboardConfigUI");
        private static readonly MethodInfo CreateButtonConfigUI = typeof(EverestModule).GetMethodInfo("CreateButtonConfigUI");
        private static List<EaseInSubMenu> options;
        private static TextMenu.Item hotkeysSubMenu;
        private static CelesteTasModuleSettings Settings => CelesteTasModule.Settings;

        internal static string ToDialogText(this string input) => Dialog.Clean("TAS_" + input.Replace(" ", "_"));

        private static void CreateOptions(EverestModule everestModule, TextMenu menu, bool inGame) {
            options = new List<EaseInSubMenu> {
                HitboxTweak.CreateSubMenu(menu, inGame),
                SimplifiedGraphicsFeature.CreateSubMenu(),
                InfoHud.CreateSubMenu(),

                new EaseInSubMenu("Round Values".ToDialogText(), false).Apply(subMenu => {
                    subMenu.Add(new TextMenu.OnOff("Round Position".ToDialogText(), Settings.RoundPosition).Change(value =>
                        Settings.RoundPosition = value));
                    subMenu.Add(new TextMenu.OnOff("Round Speed".ToDialogText(), Settings.RoundSpeed).Change(value =>
                        Settings.RoundSpeed = value));
                    subMenu.Add(new TextMenu.OnOff("Round Velocity".ToDialogText(), Settings.RoundVelocity).Change(value =>
                        Settings.RoundVelocity = value));
                    subMenu.Add(new TextMenu.OnOff("Round Custom Info".ToDialogText(), Settings.RoundCustomInfo).Change(value =>
                        Settings.RoundCustomInfo = value));
                }),

                new EaseInSubMenu("Relaunch Required".ToDialogText(), false).Apply(subMenu => {
                    subMenu.Add(new TextMenu.OnOff("Launch Studio At Boot".ToDialogText(), Settings.LaunchStudioAtBoot).Change(value =>
                        Settings.LaunchStudioAtBoot = value));
                    subMenu.Add(new TextMenu.OnOff("Auto Extract New Studio".ToDialogText(), Settings.AutoExtractNewStudio).Change(value =>
                        Settings.AutoExtractNewStudio = value));
                }),
                
                new EaseInSubMenu("Hotkeys".ToDialogText(), false).Apply(subMenu => {
                    subMenu.Add(new TextMenu.Button(Dialog.Clean("options_keyconfig")).Pressed(() => {
                        menu.Focused = false;
                        Entity keyboardConfig;
                        if (CreateKeyboardConfigUi != null) {
                            keyboardConfig = CreateKeyboardConfigUi.Invoke(everestModule, new object[] { menu }) as Entity;
                        } else {
                            keyboardConfig = new ModuleSettingsKeyboardConfigUI(everestModule) { OnClose = () => menu.Focused = true };
                        }

                        Engine.Scene.Add(keyboardConfig);
                        Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
                    }));

                    subMenu.Add(new TextMenu.Button(Dialog.Clean("options_btnconfig")).Pressed(() => {
                        menu.Focused = false;
                        Entity keyboardConfig;
                        if (CreateButtonConfigUI != null) {
                            keyboardConfig = CreateButtonConfigUI.Invoke(everestModule, new object[] { menu }) as Entity;
                        } else {
                            keyboardConfig = new ModuleSettingsButtonConfigUI(everestModule) { OnClose = () => menu.Focused = true };
                        }

                        Engine.Scene.Add(keyboardConfig);
                        Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
                    }));
                }).Apply(subMenu => hotkeysSubMenu = subMenu),

                new EaseInSubMenu("More Options".ToDialogText(), false).Apply(subMenu => {
                    subMenu.Add(new TextMenu.OnOff("Center Camera".ToDialogText(), Settings.CenterCamera).Change(value =>
                        Settings.CenterCamera = value));
                    subMenu.Add(new TextMenu.OnOff("Pause After Load State".ToDialogText(), Settings.PauseAfterLoadState).Change(value =>
                        Settings.PauseAfterLoadState = value));
                    subMenu.Add(new TextMenu.OnOff("Restore Settings".ToDialogText(), Settings.RestoreSettings).Change(value =>
                        Settings.RestoreSettings = value));
                    subMenu.Add(
                        new TextMenu.OnOff("Disable Achievements".ToDialogText(), Settings.DisableAchievements).Change(value =>
                            Settings.DisableAchievements = value));
                    subMenu.Add(new TextMenu.OnOff("Mod 9D Lighting".ToDialogText(), Settings.Mod9DLighting).Change(value =>
                        Settings.Mod9DLighting = value));
                }),
            };
        }

        public static void CreateMenu(EverestModule everestModule, TextMenu menu, bool inGame) {
            List<TextMenuExt.EaseInSubHeaderExt> enabledDescriptions = new();
            
            TextMenuExt.EaseInSubHeaderExt AddEnabledDescription(TextMenu.Item enabledItem, TextMenu containingMenu, string description) {
                TextMenuExt.EaseInSubHeaderExt descriptionText = new(description, false, containingMenu) {
                    TextColor = Color.Gray,
                    HeightExtra = 0f
                };

                List<TextMenu.Item> items = containingMenu.GetItems();
                if (items.Contains(enabledItem)) {
                    containingMenu.Insert(items.IndexOf(enabledItem) + 1, descriptionText);
                }

                enabledItem.OnEnter += () => descriptionText.FadeVisible = Settings.Enabled;
                enabledItem.OnLeave += () => descriptionText.FadeVisible = false;

                return descriptionText;
            }

            TextMenu.Item enabledItem = new TextMenu.OnOff("Enabled".ToDialogText(), Settings.Enabled).Change((value) => {
                Settings.Enabled = value;
                foreach (EaseInSubMenu easeInSubMenu in options) {
                    easeInSubMenu.FadeVisible = value;
                }

                foreach (TextMenuExt.EaseInSubHeaderExt easeInSubHeader in enabledDescriptions) {
                    easeInSubHeader.FadeVisible = value;
                }
            });
            menu.Add(enabledItem);
            CreateOptions(everestModule, menu, inGame);
            foreach (EaseInSubMenu easeInSubMenu in options) {
                menu.Add(easeInSubMenu);
            }

            foreach (string text in Split(InputController.TasFilePath, 60).Reverse()) {
                enabledDescriptions.Add(AddEnabledDescription(enabledItem, menu, text));
            }

            enabledDescriptions.Add(AddEnabledDescription(enabledItem, menu, "Enabled Description".ToDialogText()));

            HitboxTweak.AddSubMenuDescription(menu, inGame);
            InfoHud.AddSubMenuDescription(menu);
            hotkeysSubMenu.AddDescription(menu, "Hotkeys Description".ToDialogText());
        }

        private static IEnumerable<string> Split(string str, int n) {
            if (String.IsNullOrEmpty(str) || n < 1) {
                throw new ArgumentException();
            }

            for (int i = 0; i < str.Length; i += n) {
                yield return str.Substring(i, Math.Min(n, str.Length - i));
            }
        }

        public static IEnumerable<KeyValuePair<int?, string>> CreateSliderOptions(int start, int end, Func<int, string> formatter = null) {
            if (formatter == null) {
                formatter = i => i.ToString();
            }

            List<KeyValuePair<int?, string>> result = new();

            if (start <= end) {
                for (int current = start; current <= end; current++) {
                    result.Add(new KeyValuePair<int?, string>(current, formatter(current)));
                }

                result.Insert(0, new KeyValuePair<int?, string>(null, "Default".ToDialogText()));
            } else {
                for (int current = start; current >= end; current--) {
                    result.Add(new KeyValuePair<int?, string>(current, formatter(current)));
                }

                result.Insert(0, new KeyValuePair<int?, string>(null, "Default".ToDialogText()));
            }

            return result;
        }

        public static IEnumerable<KeyValuePair<bool, string>> CreateDefaultHideOptions() {
            return new List<KeyValuePair<bool, string>> {
                new(false, "Default".ToDialogText()),
                new(true, "Hide".ToDialogText()),
            };
        }

        public static IEnumerable<KeyValuePair<bool, string>> CreateSimplifyOptions() {
            return new List<KeyValuePair<bool, string>> {
                new(false, "Default".ToDialogText()),
                new(true, "Simplify".ToDialogText()),
            };
        }
    }

    public class EaseInSubMenu : TextMenuExt.SubMenu {
        public bool FadeVisible { get; set; }
        private float alpha;
        private float unEasedAlpha;
        private float ease;
        private readonly MTexture icon;

        public EaseInSubMenu(string label, bool enterOnSelect) : base(label, enterOnSelect) {
            alpha = unEasedAlpha = CelesteTasModule.Settings.Enabled ? 1f : 0f;
            FadeVisible = Visible = CelesteTasModule.Settings.Enabled;
            icon = GFX.Gui["downarrow"];
        }

        public override float Height() => MathHelper.Lerp(-Container.ItemSpacing, base.Height(), alpha);

        public override void Update() {
            ease = Calc.Approach(ease, Focused ? 1f : 0f, Engine.RawDeltaTime * 4f);
            base.Update();

            float targetAlpha = FadeVisible ? 1 : 0;
            if (Math.Abs(unEasedAlpha - targetAlpha) > 0.001f) {
                unEasedAlpha = Calc.Approach(unEasedAlpha, targetAlpha, Engine.RawDeltaTime * 3f);
                alpha = FadeVisible ? Ease.SineOut(unEasedAlpha) : Ease.SineIn(unEasedAlpha);
            }

            Visible = alpha != 0;
        }

        public override void Render(Vector2 position, bool highlighted) {
            Vector2 top = new(position.X, position.Y - (Height() / 2));

            float currentAlpha = Container.Alpha * alpha;
            Color color = Disabled ? Color.DarkSlateGray : ((highlighted ? Container.HighlightColor : Color.White) * currentAlpha);
            Color strokeColor = Color.Black * (currentAlpha * currentAlpha * currentAlpha);

            bool unCentered = Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;

            Vector2 titlePosition = top + (Vector2.UnitY * TitleHeight / 2) + (unCentered ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0f));
            Vector2 justify = unCentered ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
            Vector2 iconJustify = unCentered
                ? new Vector2(ActiveFont.Measure(Label).X + icon.Width, 5f)
                : new Vector2(ActiveFont.Measure(Label).X / 2 + icon.Width, 5f);
            DrawIcon(titlePosition, iconJustify, true, Items.Count < 1 ? Color.DarkSlateGray : color, alpha);
            ActiveFont.DrawOutline(Label, titlePosition, justify, Vector2.One, color, 2f, strokeColor);

            if (Focused && ease > 0.9f) {
                Vector2 menuPosition = new(top.X + ItemIndent, top.Y + TitleHeight + ItemSpacing);
                RecalculateSize();
                foreach (TextMenu.Item item in Items) {
                    if (item.Visible) {
                        float height = item.Height();
                        Vector2 itemPosition = menuPosition + new Vector2(0f, height * 0.5f + item.SelectWiggler.Value * 8f);
                        if (itemPosition.Y + height * 0.5f > 0f && itemPosition.Y - height * 0.5f < Engine.Height) {
                            item.Render(itemPosition, Focused && Current == item);
                        }

                        menuPosition.Y += height + ItemSpacing;
                    }
                }
            }
        }

        private void DrawIcon(Vector2 position, Vector2 justify, bool outline, Color color, float scale) {
            if (outline) {
                icon.DrawOutlineCentered(position + justify, color, scale);
            } else {
                icon.DrawCentered(position + justify, color, scale);
            }
        }
    }
}