using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Eto;
using Eto.Drawing;
using Eto.Forms;

namespace CelesteStudio.Util;

public static class DialogUtil
{
    public static T ShowNumberInputDialog<T>(string title, T input, T minValue, T maxValue, T step) where T : INumber<T> {
        var stepper = new NumericStepper {
            Value = double.CreateChecked(input),
            MinValue = double.CreateChecked(minValue),
            MaxValue = double.CreateChecked(maxValue),
            Increment = double.CreateChecked(step),
            Width = 200,
        };

        if (input is int)
            stepper.DecimalPlaces = stepper.MaximumDecimalPlaces = 0;
        else
            stepper.DecimalPlaces = 2;

        var dialog = new Dialog<T> {
            Title = title,
            Content = new StackLayout {
                Padding = 10,
                Items = { stepper },
            }
        };

        dialog.DefaultButton = new Button((_, _) => dialog.Close(T.CreateChecked(stepper.Value))) { Text = "&OK" };
        dialog.AbortButton = new Button((_, _) => dialog.Close()) { Text = "&Cancel" };
        
        dialog.PositiveButtons.Add(dialog.DefaultButton);
        dialog.NegativeButtons.Add(dialog.AbortButton);
        dialog.Result = input;
            
        return dialog.ShowModal();
    }
    
    private class FontPreview : Drawable {
        private Font font = null!;
        private SyntaxHighlighter? highlighter;
        
        public void SetFont(string fontFamily, float size) {
            font = FontManager.CreateFont(fontFamily, size);
            highlighter = new SyntaxHighlighter(font,
                FontManager.CreateFont(fontFamily, size, FontStyle.Bold),
                FontManager.CreateFont(fontFamily, size, FontStyle.Italic),
                FontManager.CreateFont(fontFamily, size, FontStyle.Bold | FontStyle.Italic));
            
            Invalidate();
        }
        
        public FontPreview() {
            BackgroundColor = Settings.Instance.Theme.Background;
            Settings.ThemeChanged += () => BackgroundColor = Settings.Instance.Theme.Background;
        }
        
        protected override void OnPaint(PaintEventArgs e) {
            if (highlighter == null)
                return;
            
            string[] previewText = [
                "#Start",
                "   7,L,Z",
                "***S",
                "  38,R,J,AD",
                "  17,F,192.59",
                "Set, Player.Speed.X, 200",
                " 145,R,D",
            ];
            
            e.Graphics.AntiAlias = true;
            
            float yPos = 0.0f;
            float maxWidth = 0.0f;
            foreach (var line in previewText) {
                highlighter.DrawLine(e.Graphics, 0.0f, yPos, line);
                yPos += font.LineHeight();
                maxWidth = Math.Max(maxWidth, font.MeasureString(line).Width);
            }
            
            Size = new((int)maxWidth, (int)yPos);
            
            base.OnPaint(e);
        }
    }
    
    private static string[]? cachedFontFamilys;
    
    public static void ShowFontDialog() {
        var preview = new FontPreview();
        preview.SetFont(Settings.Instance.FontFamily, Settings.Instance.EditorFontSize);
        string fontFamily = Settings.Instance.FontFamily;
        
        var editorFontSize = new NumericStepper {
            Value = Settings.Instance.EditorFontSize,
            MinValue = 1.0f,
            Increment = 1.0f,
            DecimalPlaces = 1,
            Width = 100,
        };
        var statusFontSize = new NumericStepper {
            Value = Settings.Instance.StatusFontSize,
            MinValue = 1.0f,
            Increment = 1.0f,
            DecimalPlaces = 1,
            Width = 100,
        };
        
        var dialog = new Dialog<bool> {
            Title = "Font",
            Content = CreateDialogContent(),
        };
        
        dialog.DefaultButton = new Button((_, _) => dialog.Close(true)) { Text = "&OK" };
        dialog.AbortButton = new Button((_, _) => dialog.Close(false)) { Text = "&Cancel" };
        
        dialog.PositiveButtons.Add(dialog.DefaultButton);
        dialog.NegativeButtons.Add(dialog.AbortButton);
        
        // Fetch font list asynchronously
        if (cachedFontFamilys == null) {
            Task.Run(() => {
                var fonts = new List<string>();
                foreach (var family in Fonts.AvailableFontFamilies) {
                    // Check if the font is monospaced
                    var font = new Font(family, 12.0f);
                    if (Math.Abs(font.MeasureString("I").Width - font.MeasureString("X").Width) > 0.01f)
                        continue;
                    
                    fonts.Add(family.Name);
                }
                cachedFontFamilys = fonts.ToArray();
                dialog.Content = CreateDialogContent();
            });
        }
        
        if (!dialog.ShowModal())
            return;
        
        Settings.Instance.FontFamily = fontFamily;
        Settings.Instance.EditorFontSize = (float)editorFontSize.Value;
        Settings.Instance.StatusFontSize = (float)statusFontSize.Value;
        Settings.Instance.OnFontChanged();
        Settings.Save();
        
        return;
        
        Control CreateDialogContent() {
            var settingsPanel = new StackLayout {
                Padding = 10,
                Spacing = 10,
                Items = {
                    new StackLayout {
                        Spacing = 10,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Orientation = Orientation.Horizontal,
                        Items = { new Label { Text = "Editor Font Size" }, editorFontSize },
                    },
                    new StackLayout {
                        Spacing = 10,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Orientation = Orientation.Horizontal,
                        Items = { new Label { Text = "Status Font Size" }, statusFontSize },
                    },
                    new Label { Text = "  Preview:" }.WithFontStyle(FontStyle.Bold),
                    new Scrollable {
                        Content = preview,
                        Width = 250,
                        Height = 200,
                    }
                }
            };
            
            if (cachedFontFamilys != null) {
                var list = new ListBox { Width = 250, Height = 330 };
                list.SelectedValue = Settings.Instance.FontFamily;
                
                // Add built-in font
                list.Items.Add(new ListItem {
                    Text = FontManager.FontFamilyBuiltinDisplayName,
                    Key = FontManager.FontFamilyBuiltin,
                });
                foreach (var family in cachedFontFamilys) {
                    list.Items.Add(new ListItem {
                        Text = family,
                        Key = family,
                    });
                }
                
                // Select current font
                foreach (var item in list.Items) {
                    if (item.Key == Settings.Instance.FontFamily) {
                        list.SelectedValue = item;
                        break;
                    }
                        
                }
                
                editorFontSize.ValueChanged += (_, _) => UpdateFont();
                list.SelectedValueChanged += (_, _) => UpdateFont();
                
                return new StackLayout {
                    Padding = 10,
                    Orientation = Orientation.Horizontal,
                    Items = { list, settingsPanel }
                };
                
                void UpdateFont() {
                    if (list.SelectedValue is not ListItem item)
                        return;
                    
                    fontFamily = item.Key;
                    preview.SetFont(item.Key, (float)editorFontSize.Value);
                }
            } else {
                var loadingPanel = new StackLayout {
                    Spacing = 10,
                    Width = 250, 
                    Height = 330,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Items = {
                        new Spinner { Enabled = true },
                        new Label { Text = "Loading font list..." }.WithFontStyle(FontStyle.Italic),
                    },
                };
                
                return new StackLayout {
                    Padding = 10,
                    Orientation = Orientation.Horizontal,
                    Items = { loadingPanel, settingsPanel }
                };
            }
        }
    }
    
    public static void ShowRecordDialog() {
        var textBox = new TextBox { Text = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}", Width = 200 };
        
        var dialog = new Dialog<bool> {
            Title = "Record TAS",
            Content = new StackLayout {
                Padding = 10,
                Spacing = 10,
                VerticalContentAlignment = VerticalAlignment.Center, 
                Orientation = Orientation.Horizontal,
                Items = { new Label { Text = "File Name" }, textBox },
            }
        };
        
        dialog.DefaultButton = new Button((_, _) => dialog.Close(true)) { Text = "&Record" };
        dialog.AbortButton = new Button((_, _) => dialog.Close(false)) { Text = "&Cancel" };
        
        dialog.PositiveButtons.Add(dialog.DefaultButton);
        dialog.NegativeButtons.Add(dialog.AbortButton);
        
        if (!dialog.ShowModal())
            return;
        
        if (string.IsNullOrWhiteSpace(textBox.Text)) {
            MessageBox.Show("An empty file name is not valid!", MessageBoxButtons.OK, MessageBoxType.Error);
            return;
        }

        Studio.CommunicationWrapper.Server.RecordTAS(textBox.Text);
    }
}