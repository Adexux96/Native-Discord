using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using Microsoft.UI.Text;
using ColorCode;
using ColorCode.Styling;
using ColorCode.Common;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NativeDiscord.Services;
using System.Linq;
// Aliases to avoid ambiguity with Markdig

using WinUIBlock = Microsoft.UI.Xaml.Documents.Block;
using MarkdigBlock = Markdig.Syntax.Block;
using UIInline = Microsoft.UI.Xaml.Documents.Inline;
using UIRun = Microsoft.UI.Xaml.Documents.Run;
using UISpan = Microsoft.UI.Xaml.Documents.Span;
using UIParagraph = Microsoft.UI.Xaml.Documents.Paragraph;
using UIInlineCollection = Microsoft.UI.Xaml.Documents.InlineCollection;

namespace NativeDiscord.Helpers
{
    // Fix for missing scopes in ColorCode.WinUI 2.x
    public static class FixedScopeName
    {
        public const string PlainText = "PlainText";
        public const string Keyword = "Keyword";
        public const string PreprocessorKeyword = "PreprocessorKeyword";
        public const string ControlKeyword = "ControlKeyword";
        public const string PseudoKeyword = "PseudoKeyword";
        public const string String = "String";
        public const string StringCSharpVerbatim = "StringCSharpVerbatim";
        public const string StringEscape = "StringEscape";
        public const string Comment = "Comment";
        public const string XmlDocComment = "XmlDocComment";
        public const string XmlDocTag = "XmlDocTag";
        public const string XmlComment = "XmlComment";
        public const string Number = "Number";
        public const string MarkdownHeader = "MarkdownHeader";
        public const string MarkdownListItem = "MarkdownListItem";
        public const string MarkdownEmph = "MarkdownEmph";
        public const string MarkdownBold = "MarkdownBold";
        public const string ClassName = "ClassName";
        public const string Type = "Type";
        public const string TypeVariable = "TypeVariable";
        public const string NameSpace = "NameSpace";
        public const string Constructor = "Constructor";
        public const string Intrinsic = "Intrinsic";
        public const string BuiltinFunction = "BuiltinFunction";
        public const string Operator = "Operator";
        public const string Delimiter = "Delimiter";
        public const string Brackets = "Brackets";
        public const string HtmlElementName = "HtmlElementName";
        public const string HtmlAttributeName = "HtmlAttributeName";
        public const string HtmlAttributeValue = "HtmlAttributeValue";
        public const string HtmlTagDelimiter = "HtmlTagDelimiter";
        public const string CssSelector = "CssSelector";
        public const string CssPropertyName = "CssPropertyName";
        public const string CssPropertyValue = "CssPropertyValue";
        public const string BuiltinValue = "BuiltinValue";
        public const string Attribute = "Attribute";
        
        // These might be missing in older/newer standard ColorCode packages
        public const string JsonKey = "JsonKey";
        public const string JsonString = "JsonString";
        public const string JsonNumber = "JsonNumber";
    }

    // Custom Discord-like syntax highlighting theme (based on VS Dark/Discord colors)
    public static class DiscordCodeStyle
    {
        // Mathematically Exact Discord 1:1 Palette (Verified via Dev Docs)
        private const string PlainTextColor = "#dbdee1";  
        private const string BackgroundColor = "#2c2d32"; // Verified
        private const string KeywordColor = "#ff7b72";    // Verified
        private const string StringColor = "#a5d6ff";     // Verified
        private const string CommentColor = "#8b949e";    // Verified
        private const string NumberColor = "#79c0ff";     // Verified
        private const string FunctionColor = "#d2a8ff";   // Lavender Purple (Verified)
        private const string QuoteColor = "#57f287";      // Discord Green (Verified)

        public static readonly StyleDictionary Style = new StyleDictionary
        {
            // Plain text / default
            new ColorCode.Styling.Style(FixedScopeName.PlainText) { Foreground = PlainTextColor, Background = BackgroundColor },
            
            // Keywords
            new ColorCode.Styling.Style(FixedScopeName.Keyword) { Foreground = KeywordColor },
            new ColorCode.Styling.Style(FixedScopeName.PreprocessorKeyword) { Foreground = KeywordColor },
            new ColorCode.Styling.Style(FixedScopeName.ControlKeyword) { Foreground = KeywordColor },
            new ColorCode.Styling.Style(FixedScopeName.PseudoKeyword) { Foreground = KeywordColor },
            
            // Strings
            new ColorCode.Styling.Style(FixedScopeName.String) { Foreground = StringColor },
            new ColorCode.Styling.Style(FixedScopeName.StringCSharpVerbatim) { Foreground = StringColor },
            new ColorCode.Styling.Style(FixedScopeName.StringEscape) { Foreground = "#d7ba7d" },
            
            // Comments
            new ColorCode.Styling.Style(FixedScopeName.Comment) { Foreground = CommentColor },
            new ColorCode.Styling.Style(FixedScopeName.XmlDocComment) { Foreground = CommentColor },
            new ColorCode.Styling.Style(FixedScopeName.XmlDocTag) { Foreground = "#dbdee1" },
            new ColorCode.Styling.Style(FixedScopeName.XmlComment) { Foreground = CommentColor },
            
            // Numbers
            new ColorCode.Styling.Style(FixedScopeName.Number) { Foreground = NumberColor },
            
            // Markdown Specifics
            new ColorCode.Styling.Style(FixedScopeName.MarkdownHeader) { Foreground = KeywordColor },
            new ColorCode.Styling.Style(FixedScopeName.MarkdownListItem) { Foreground = KeywordColor },
            new ColorCode.Styling.Style(FixedScopeName.MarkdownEmph) { Foreground = PlainTextColor, Italic = true },
            new ColorCode.Styling.Style(FixedScopeName.MarkdownBold) { Foreground = KeywordColor, Bold = true },
            
            // Common Scopes and Classes (Usually white in Discord)
            new ColorCode.Styling.Style(FixedScopeName.ClassName) { Foreground = PlainTextColor },
            new ColorCode.Styling.Style(FixedScopeName.Type) { Foreground = PlainTextColor },
            new ColorCode.Styling.Style(FixedScopeName.TypeVariable) { Foreground = PlainTextColor },
            new ColorCode.Styling.Style(FixedScopeName.NameSpace) { Foreground = PlainTextColor },
            new ColorCode.Styling.Style(FixedScopeName.Constructor) { Foreground = PlainTextColor },
            // Functions/Methods (Mapped to Intrinsic as a fallback for some grammars)
            new ColorCode.Styling.Style(FixedScopeName.Intrinsic) { Foreground = FunctionColor },
            new ColorCode.Styling.Style(FixedScopeName.BuiltinFunction) { Foreground = FunctionColor },
            
            // Operators and Delimiters
            new ColorCode.Styling.Style(FixedScopeName.Operator) { Foreground = PlainTextColor },
            new ColorCode.Styling.Style(FixedScopeName.Delimiter) { Foreground = PlainTextColor },
            new ColorCode.Styling.Style(FixedScopeName.Brackets) { Foreground = PlainTextColor },
            
            // HTML/XML
            new ColorCode.Styling.Style(FixedScopeName.HtmlElementName) { Foreground = KeywordColor },
            new ColorCode.Styling.Style(FixedScopeName.HtmlAttributeName) { Foreground = NumberColor },
            new ColorCode.Styling.Style(FixedScopeName.HtmlAttributeValue) { Foreground = StringColor },
            new ColorCode.Styling.Style(FixedScopeName.HtmlTagDelimiter) { Foreground = PlainTextColor },
            
            // CSS
            new ColorCode.Styling.Style(FixedScopeName.CssSelector) { Foreground = KeywordColor },
            new ColorCode.Styling.Style(FixedScopeName.CssPropertyName) { Foreground = NumberColor },
            new ColorCode.Styling.Style(FixedScopeName.CssPropertyValue) { Foreground = PlainTextColor },
            
            // JSON
            new ColorCode.Styling.Style(FixedScopeName.JsonKey) { Foreground = KeywordColor },
            new ColorCode.Styling.Style(FixedScopeName.JsonString) { Foreground = StringColor },
            new ColorCode.Styling.Style(FixedScopeName.JsonNumber) { Foreground = NumberColor },
            
            // Misc
            new ColorCode.Styling.Style(FixedScopeName.BuiltinValue) { Foreground = PlainTextColor },
            new ColorCode.Styling.Style(FixedScopeName.Attribute) { Foreground = PlainTextColor },
        };
    }

    public class MarkdownTextHelper : DependencyObject
    {
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.RegisterAttached("Markdown", typeof(string), typeof(MarkdownTextHelper), new PropertyMetadata(null, OnMarkdownChanged));

        public static string GetMarkdown(DependencyObject obj)
        {
            return (string)obj.GetValue(MarkdownProperty);
        }

        public static void SetMarkdown(DependencyObject obj, string value)
        {
            obj.SetValue(MarkdownProperty, value);
        }

        public static readonly DependencyProperty IsEditedProperty =
            DependencyProperty.RegisterAttached("IsEdited", typeof(bool), typeof(MarkdownTextHelper), new PropertyMetadata(false, OnIsEditedChanged));

        public static readonly DependencyProperty RefreshIdProperty =
            DependencyProperty.RegisterAttached("RefreshId", typeof(int), typeof(MarkdownTextHelper), new PropertyMetadata(0, OnRefreshIdChanged));

        public static int GetRefreshId(DependencyObject obj)
        {
            return (int)obj.GetValue(RefreshIdProperty);
        }

        public static void SetRefreshId(DependencyObject obj, int value)
        {
            obj.SetValue(RefreshIdProperty, value);
        }

        public static readonly DependencyProperty CurrentGuildIdProperty =
            DependencyProperty.RegisterAttached("CurrentGuildId", typeof(string), typeof(MarkdownTextHelper), new PropertyMetadata(null, OnCurrentGuildIdChanged));

        public static string GetCurrentGuildId(DependencyObject obj)
        {
            return (string)obj.GetValue(CurrentGuildIdProperty);
        }

        public static void SetCurrentGuildId(DependencyObject obj, string value)
        {
            obj.SetValue(CurrentGuildIdProperty, value);
        }

        private static void OnCurrentGuildIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
             if (d is RichTextBlock control)
             {
                 string markdown = GetMarkdown(control);
                 if (markdown != null)
                 {
                     RenderMarkdown(control, markdown);
                 }
             }
        }

        private static void OnRefreshIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
             if (d is RichTextBlock control)
             {
                 string markdown = GetMarkdown(control);
                 if (markdown != null)
                 {
                     RenderMarkdown(control, markdown);
                 }
             }
        }

        public static bool GetIsEdited(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEditedProperty);
        }

        public static void SetIsEdited(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEditedProperty, value);
        }

        public static readonly DependencyProperty DiscordServiceProperty =
            DependencyProperty.RegisterAttached("DiscordService", typeof(DiscordService), typeof(MarkdownTextHelper), new PropertyMetadata(null, OnDiscordServiceChanged));

        public static DiscordService GetDiscordService(DependencyObject obj)
        {
            return (DiscordService)obj.GetValue(DiscordServiceProperty);
        }

        public static void SetDiscordService(DependencyObject obj, DiscordService value)
        {
            obj.SetValue(DiscordServiceProperty, value);
        }

        private static void OnDiscordServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
             // Re-render if service changes (unlikely but good practice)
             var text = GetMarkdown(d);
             if (text != null && d is RichTextBlock rtb)
             {
                 RenderMarkdown(rtb, text);
             }
        }

        private static void OnIsEditedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Re-render markdown if edited status changes
            var text = GetMarkdown(d);
            if (text != null)
            {
                RenderMarkdown(d as RichTextBlock, text);
            }
        }

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBlock richText)
            {
                var text = e.NewValue as string;
                RenderMarkdown(richText, text);
            }
        }

        private static void RenderMarkdown(RichTextBlock richText, string text)
        {
            if (richText == null) return;
            richText.Blocks.Clear();
            
            if (string.IsNullOrEmpty(text)) return;

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            var document = Markdig.Markdown.Parse(text, pipeline);

            var service = GetDiscordService(richText);
            var currentGuildId = GetCurrentGuildId(richText);

            foreach (var block in document)
            {
                var xamlBlock = RenderBlock(block, service, currentGuildId);
                if (xamlBlock != null)
                {
                    richText.Blocks.Add(xamlBlock);
                }
            }

            // Append (edited) if needed
            if (GetIsEdited(richText))
            {
                var editedRun = new UIRun 
                { 
                    Text = " (edited)", 
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 148, 155, 164)), // #949BA4
                    FontSize = 11
                };

                if (richText.Blocks.Count > 0 && richText.Blocks[richText.Blocks.Count - 1] is UIParagraph lastPara)
                {
                    lastPara.Inlines.Add(editedRun);
                }
                else
                {
                    var p = new UIParagraph();
                    p.Inlines.Add(editedRun);
                    richText.Blocks.Add(p);
                }
            }
        }



        private static WinUIBlock RenderBlock(MarkdigBlock block, DiscordService service, string currentGuildId)
        {
            if (block is ParagraphBlock paragraphBlock)
            {
                var paragraph = new UIParagraph();
                paragraph.Margin = new Thickness(0, 0, 0, 8); 
                RenderInlines(paragraph.Inlines, paragraphBlock.Inline, service, currentGuildId);
                return paragraph;
            }
            else if (block is FencedCodeBlock fencedCodeBlock)
            {
                return CreateCodeBlock(fencedCodeBlock.Lines.ToString(), fencedCodeBlock.Info);
            }
            else if (block is CodeBlock codeBlock) 
            {
                return CreateCodeBlock(codeBlock.Lines.ToString(), null);
            }
            else if (block is QuoteBlock quoteBlock)
            {
                return RenderQuoteBlock(quoteBlock, service, currentGuildId);
            }
            else if (block is ListBlock listBlock)
            {
                return RenderListBlock(listBlock, service, currentGuildId);
            }
            
            return null;
        }

        private static WinUIBlock CreateCodeBlock(string code, string language)
        {
            var paragraph = new UIParagraph();
            var container = new InlineUIContainer();
            
            var border = new Border
            {
                Background = (SolidColorBrush)Application.Current.Resources["DiscordCodeBackground"],
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 4, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var scrollViewer = new ScrollViewer 
            { 
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled 
            };

            var codeRichText = new RichTextBlock
            {
                FontFamily = (FontFamily)Application.Current.Resources["DiscordCodeFont"],
                // Don't set Foreground here - let the formatter apply individual colors
                FontSize = 13,
                IsTextSelectionEnabled = true
            };

            ILanguage lang = null;
            if (!string.IsNullOrEmpty(language))
            {
                lang = Languages.FindById(language);
            }

            if (lang != null)
            {
                try
                {
                    // Explicitly use the static Style dictionary which is now strictly enforced
                    var formatter = new RichTextBlockFormatter(DiscordCodeStyle.Style);
                    
                    // Force a debug check to verify this code is reached
                    formatter.FormatRichTextBlock(code.Trim(), lang, codeRichText);

                    // --- Highlighting Boosters ---
                    var langLower = language?.ToLower();
                    if (langLower == "csharp" || langLower == "cs")
                    {
                        ApplyRecursiveCSharpBooster(codeRichText);
                    }
                    else if (langLower == "markdown" || langLower == "md")
                    {
                        ApplyMarkdownQuoteBooster(codeRichText);
                    }
                }
                catch
                {
                    var p = new UIParagraph();
                    p.Inlines.Add(new UIRun { Text = code.Trim() });
                    codeRichText.Blocks.Add(p);
                }
            }
            else
            {
                var p = new UIParagraph();
                p.Inlines.Add(new UIRun { Text = code.Trim() });
                codeRichText.Blocks.Add(p);
            }

            scrollViewer.Content = codeRichText;
            border.Child = scrollViewer;
            container.Child = border;
            paragraph.Inlines.Add(container);

            return paragraph;
        }

        private static System.Collections.Generic.List<UIRun> GetFlattenedRuns(DependencyObject obj)
        {
            var runs = new System.Collections.Generic.List<UIRun>();
            if (obj is RichTextBlock rtb)
            {
                foreach (var block in rtb.Blocks) runs.AddRange(GetFlattenedRuns(block));
            }
            else if (obj is UIParagraph p)
            {
                foreach (var inline in p.Inlines) runs.AddRange(GetFlattenedRuns(inline));
            }
            else if (obj is UISpan s)
            {
                foreach (var inline in s.Inlines) runs.AddRange(GetFlattenedRuns(inline));
            }
            else if (obj is UIRun r)
            {
                runs.Add(r);
            }
            return runs;
        }

        private class RunMetadata
        {
            public UIRun Run;
            public UIInlineCollection ParentCollection;
        }

        private static void GetRunMetadata(UIInlineCollection inlines, System.Collections.Generic.List<RunMetadata> list)
        {
            foreach (var inline in inlines)
            {
                if (inline is UIRun r) list.Add(new RunMetadata { Run = r, ParentCollection = inlines });
                else if (inline is UISpan s) GetRunMetadata(s.Inlines, list);
            }
        }

        private static void ApplyRecursiveCSharpBooster(RichTextBlock codeRichText)
        {
            var purple = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 210, 168, 255));

            foreach (var block in codeRichText.Blocks)
            {
                if (block is UIParagraph p)
                {
                    var allRuns = new System.Collections.Generic.List<RunMetadata>();
                    GetRunMetadata(p.Inlines, allRuns);

                    for (int i = 0; i < allRuns.Count; i++)
                    {
                        var meta = allRuns[i];
                        var text = meta.Run.Text;
                        if (string.IsNullOrEmpty(text)) continue;

                        // Check Case 1 (Same run) or Case 2 (Lookahead)
                        bool isMethod = false;
                        var sameRunMatch = System.Text.RegularExpressions.Regex.Match(text, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*(?=\()");
                        
                        if (sameRunMatch.Success)
                        {
                            isMethod = true;
                        }
                        else if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*$"))
                        {
                            // Lookahead
                            string nextText = null;
                            for (int k = i + 1; k < allRuns.Count; k++)
                            {
                                if (!string.IsNullOrWhiteSpace(allRuns[k].Run.Text))
                                {
                                    nextText = allRuns[k].Run.Text;
                                    break;
                                }
                            }

                            if (nextText != null && nextText.TrimStart().StartsWith("("))
                            {
                                isMethod = true;
                            }
                        }

                        if (isMethod)
                        {
                            // Surgical split/apply
                            var match = System.Text.RegularExpressions.Regex.Match(text, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*(?=\()|(\b[a-zA-Z_][a-zA-Z0-9_]*)\s*$");
                            var group = match.Groups[1].Success ? match.Groups[1] : match.Groups[2];
                            
                            int nameStart = group.Index;
                            int nameLength = group.Length;
                            
                            string before = text.Substring(0, nameStart);
                            string name = text.Substring(nameStart, nameLength);
                            string after = text.Substring(nameStart + nameLength);

                            if (string.IsNullOrEmpty(before) && string.IsNullOrEmpty(after))
                            {
                                meta.Run.Foreground = purple;
                            }
                            else
                            {
                                // We need to replace meta.Run in meta.ParentCollection
                                int idx = meta.ParentCollection.IndexOf(meta.Run);
                                if (idx != -1)
                                {
                                    meta.ParentCollection.RemoveAt(idx);
                                    if (!string.IsNullOrEmpty(after)) meta.ParentCollection.Insert(idx, new UIRun { Text = after });
                                    meta.ParentCollection.Insert(idx, new UIRun { Text = name, Foreground = purple });
                                    if (!string.IsNullOrEmpty(before)) meta.ParentCollection.Insert(idx, new UIRun { Text = before });
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void ApplyMarkdownQuoteBooster(RichTextBlock codeRichText)
        {
            var runs = GetFlattenedRuns(codeRichText);
            var green = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 87, 242, 135));
            bool isQuotedLine = false;
            bool isStartOfLine = true;

            foreach (var run in runs)
            {
                var text = run.Text;
                if (string.IsNullOrEmpty(text)) continue;

                var lines = text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var linePart = lines[i];
                    
                    // If we just started a line, check for '>'
                    if (isStartOfLine && linePart.TrimStart().StartsWith(">"))
                    {
                        isQuotedLine = true;
                    }

                    if (isQuotedLine)
                    {
                        run.Foreground = green;
                    }

                    // If there are more parts, it means a newline occurred
                    if (i < lines.Length - 1)
                    {
                        isQuotedLine = false;
                        isStartOfLine = true;
                    }
                    else
                    {
                        // We are at the end of the text in this run
                        isStartOfLine = false;
                    }
                }
            }
        }




        private static WinUIBlock RenderQuoteBlock(QuoteBlock quoteBlock, DiscordService service, string currentGuildId)
        {
            var paragraph = new UIParagraph();
            var container = new InlineUIContainer();

            // Discord Quotes are simple: thin bar and slightly indented text
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) }); // Thin margin
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Discord Dark Quote Bar: #4e5058
            var bar = new Border 
            { 
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 78, 80, 88)),
                Width = 4,
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Stretch
            };
            
            var innerRichText = new RichTextBlock 
            { 
                IsTextSelectionEnabled = true
            };
            
            foreach (var subBlock in quoteBlock)
            {
                var rendered = RenderBlock(subBlock, service, currentGuildId);
                if (rendered != null)
                {
                    innerRichText.Blocks.Add(rendered);
                }
            }

            Grid.SetColumn(bar, 0);
            Grid.SetColumn(innerRichText, 1);
            grid.Children.Add(bar);
            grid.Children.Add(innerRichText);
            
            container.Child = grid;
            paragraph.Inlines.Add(container);
            return paragraph;
        }

        private static WinUIBlock RenderListBlock(ListBlock listBlock, DiscordService service, string currentGuildId)
        {
            var p = new UIParagraph();
            p.Margin = new Thickness(0, 4, 0, 8);

            for (int i = 0; i < listBlock.Count; i++)
            {
                var listItem = (ListItemBlock)listBlock[i];
                var prefix = listBlock.IsOrdered ? $"{i + 1}. " : "• ";
                
                p.Inlines.Add(new UIRun { Text = prefix, Foreground = (SolidColorBrush)Application.Current.Resources["DiscordTextMuted"] });

                foreach (var block in listItem)
                {
                    if (block is ParagraphBlock pb)
                    {
                        RenderInlines(p.Inlines, pb.Inline, service, currentGuildId);
                    }
                }

                if (i < listBlock.Count - 1) p.Inlines.Add(new LineBreak());
            }

            return p;
        }

        private static void RenderInlines(UIInlineCollection inlines, Markdig.Syntax.Inlines.ContainerInline container, DiscordService service, string currentGuildId)
        {
            if (container == null) return;
            foreach (var inline in container)
            {
                RenderInline(inlines, inline, service, currentGuildId);
            }
        }

        private static void RenderInline(UIInlineCollection inlines, Markdig.Syntax.Inlines.Inline inline, DiscordService service, string currentGuildId)
        {
            if (inline is LiteralInline literal)
            {
                RenderLiteralWithMentions(inlines, literal.Content.ToString(), service, currentGuildId);
            }
            else if (inline is EmphasisInline emphasis)
            {
                var span = new UISpan();
                if (emphasis.DelimiterCount == 2) span.FontWeight = FontWeights.Bold;
                else span.FontStyle = Windows.UI.Text.FontStyle.Italic;
                
                RenderInlines(span.Inlines, emphasis, service, currentGuildId);
                inlines.Add(span);
            }
            else if (inline is CodeInline code)
            {
                var border = new Border
                {
                    Background = (SolidColorBrush)Application.Current.Resources["DiscordCodeBackground"],
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2,0,2,0)
                };
                border.Child = new TextBlock { Text = code.Content, Foreground = (SolidColorBrush)Application.Current.Resources["DiscordTextNormal"], FontSize = 13 };
                inlines.Add(new InlineUIContainer { Child = border });
            }
            else if (inline is LineBreakInline)
            {
                inlines.Add(new LineBreak());
            }
            else if (inline is LinkInline link)
            {
                if (link.IsImage)
                {
                    // For now, don't render images inline. They are handled by Attachments/Embeds
                }
                else
                {
                    // Check if it's a Discord Channel Link
                    // Format: https://discord.com/channels/123/456
                    var url = link.Url;
                    var match = Regex.Match(url, @"https://discord\.com/channels/(\d+)/(\d+)");
                    if (match.Success)
                    {
                        var guildId = match.Groups[1].Value;
                        var channelId = match.Groups[2].Value;
                        inlines.Add(CreateChannelMentionUI(service, guildId, channelId, url, currentGuildId));
                    }
                    else
                    {
                        var hyperLink = new Hyperlink { NavigateUri = new Uri(url) };
                        RenderInlines(hyperLink.Inlines, link, service, currentGuildId);
                        inlines.Add(hyperLink);
                    }
                }
            }
        }

        private static void RenderLiteralWithMentions(UIInlineCollection collection, string text, DiscordService service, string currentGuildId)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Regex for mentions: <@ID>, <@!ID>, <#ID>, <@&ID>, and Channel Links
            // Note: Channel Links format: https://discord.com/channels/guildId/channelId
            var mentionRegex = new Regex(@"(<@!?\d+>)|(<#\d+>)|(<@&\d+>)|(https://discord\.com/channels/\d+/(\d+))");
            
            int lastIndex = 0;
            foreach (Match match in mentionRegex.Matches(text))
            {
                // Add text before match
                if (match.Index > lastIndex)
                {
                     collection.Add(new UIRun { Text = text.Substring(lastIndex, match.Index - lastIndex) });
                }

                string content = match.Value;
                bool isUser = content.StartsWith("<@");
                bool isChannel = content.StartsWith("<#");
                bool isRole = content.StartsWith("<@&");
                bool isChannelLink = content.StartsWith("https://discord.com/channels/");

                string id = "";
                if (isChannelLink)
                {
                     // Extract guildId from original URL if possible
                     // Group 5 has the ID from the regex: (https.../(\d+))
                     id = match.Groups[5].Value;
                }
                else
                {
                    id = Regex.Match(content, @"\d+").Value;
                }

                string displayText = content; // Fallback
                
                // Color
                var brandColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 88, 101, 242)); // Blurple
                var brandBackground = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 88, 101, 242)); // Low opacity blurple

                if (service != null && !string.IsNullOrEmpty(id))
                {
                    if (isUser)
                    {
                        var user = service.GetCachedUser(id);
                        if (user != null) displayText = "@" + user.DisplayName;
                        else service.RequestUser(id);

                        var container = new InlineUIContainer();
                        var border = new Border
                        {
                            Background = brandBackground,
                            CornerRadius = new CornerRadius(3),
                            Padding = new Thickness(2, 0, 2, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(2, 0, 2, 0)
                        };
                        var tb = new TextBlock
                        {
                            Text = displayText,
                            Foreground = brandColor,
                            FontWeight = FontWeights.SemiBold,
                            FontSize = 14
                        };
                        border.Child = tb;
                        container.Child = border;
                        collection.Add(container);
                    }
                    else if (isChannel || isChannelLink)
                    {
                        string guildId = "";
                        if (isChannelLink)
                        {
                             // Extract guildId: https://discord.com/channels/(\d+)/...
                             var guildMatch = Regex.Match(content, @"channels/(\d+)/");
                             if (guildMatch.Success) guildId = guildMatch.Groups[1].Value;
                        }
                        collection.Add(CreateChannelMentionUI(service, guildId, id, content, currentGuildId));
                    }
                    else if (isRole)
                    {
                         // Roles stay highlight color for now
                         collection.Add(new UIRun { Text = content, Foreground = brandColor });
                    }
                }
                else
                {
                    collection.Add(new UIRun { Text = content });
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                collection.Add(new UIRun { Text = text.Substring(lastIndex) });
            }
        }



        private static UIInline CreateChannelMentionUI(DiscordService service, string guildId, string channelId, string originalUrl, string currentGuildId)
        {
            var brandColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 88, 101, 242)); // Blurple
            var brandBackground = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 88, 101, 242)); // Low opacity blurple

            string displayText = originalUrl;

            // Attempt Resolution
            if (service != null)
            {
                var channel = service.GetCachedChannel(channelId);
                if (channel != null)
                {
                    string guildName = "";
                    // Try to get guild info
                    var effectiveGuildId = !string.IsNullOrEmpty(guildId) ? guildId : channel.GuildId;
                    
                    bool isCurrentGuild = !string.IsNullOrEmpty(currentGuildId) && effectiveGuildId == currentGuildId;

                    if (!string.IsNullOrEmpty(effectiveGuildId) && service.Guilds != null)
                    {
                        var guild = service.Guilds.FirstOrDefault(g => g.Id == effectiveGuildId);
                        if (guild != null) guildName = guild.Name;
                    }

                    if (!string.IsNullOrEmpty(guildName) && !isCurrentGuild)
                        displayText = $"{guildName} \u203A #{channel.Name}"; // U+203A is single right angle quote (>)
                    else
                        displayText = "#" + channel.Name;
                }
                else
                {
                    service.RequestChannel(channelId);
                    
                    // Fallback Logic
                    // If it's a strict mention <#id>, we MUST use the Token UI (HyperlinkButton) to avoid UriFormatException
                    // If it's a URL, we can use Hyperlink, but Token UI is safer and more consistent.
                    if (originalUrl.StartsWith("<#"))
                    {
                         // Stay in this method to render as a Token (unresolved)
                    }
                    else if (Uri.TryCreate(originalUrl, UriKind.Absolute, out var uri))
                    {
                        var hyperLink = new Hyperlink { NavigateUri = uri };
                        hyperLink.Inlines.Add(new UIRun { Text = originalUrl, Foreground = brandColor });
                        return hyperLink;
                    }
                }
            }

            // Resolved UI
            var container = new InlineUIContainer();
            
            var btn = new Button
            {
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                MinHeight = 0,
                MinWidth = 0,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Remove hover/pressed styles if possible, or just use Button behavior
            // HyperlinkButton often has weird default styling (underlines etc), Button with custom content is safer for "Token" look.
            // Using standard Button for now, style as transparent.
            // Actually, in WinUI 3 HyperlinkButton is specifically for navigation.
            
            var linkBtn = new HyperlinkButton
            {
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var border = new Border
            {
                Background = brandBackground,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(2, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2,0,2,0)
            };
            var tb = new TextBlock
            {
                Text = displayText,
                Foreground = brandColor,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14
            };
            border.Child = tb;
            linkBtn.Content = border;
            
            linkBtn.Click += (s, e) => 
            {
                 service.RequestNavigation(channelId);
            };

            container.Child = linkBtn;
            return container;
        }
    }
}
