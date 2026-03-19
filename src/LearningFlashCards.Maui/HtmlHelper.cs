using System.Text.RegularExpressions;

namespace LearningFlashCards.Maui
{
    internal static class HtmlHelper
    {
        internal static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var text = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);
            text = System.Net.WebUtility.HtmlDecode(text);
            return text.Trim();
        }

        internal static string WrapWithDarkTheme(string content)
        {
            return $$"""
                <!DOCTYPE html>
                <html>
                <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <style>
                    * { box-sizing: border-box; margin: 0; padding: 0; }
                    html, body {
                        background-color: #F8FAFF;
                        color: #2C3148;
                        font-family: Georgia, 'Times New Roman', serif;
                        font-size: 16px;
                        line-height: 1.65;
                        padding: 14px 16px;
                        word-break: break-word;
                    }
                    h1, h2, h3, h4, h5, h6 {
                        color: #2C3148;
                        font-weight: 700;
                        margin-bottom: 8px;
                        line-height: 1.3;
                        font-style: italic;
                    }
                    p { margin-bottom: 8px; }
                    ul, ol { padding-left: 20px; margin-bottom: 8px; }
                    li { margin-bottom: 4px; }
                    table {
                        border-collapse: collapse;
                        width: 100%;
                        margin-bottom: 10px;
                    }
                    th, td {
                        border: 1px solid #B5C7F0;
                        padding: 8px 12px;
                        text-align: left;
                        color: #2C3148;
                    }
                    th { background-color: #EAF1FF; font-weight: 700; }
                    tr:nth-child(even) { background-color: #F2F6FF; }
                    code {
                        background-color: #EEF3FF;
                        color: #5D4D99;
                        padding: 2px 6px;
                        border-radius: 4px;
                        font-size: 14px;
                        font-family: 'Cascadia Code', 'Consolas', monospace;
                    }
                    pre {
                        background-color: #EEF3FF;
                        color: #4D5F99;
                        padding: 12px 14px;
                        border-radius: 8px;
                        overflow-x: auto;
                        font-size: 14px;
                        margin-bottom: 10px;
                        border: 1px solid #B5C7F0;
                    }
                    pre code { background: none; padding: 0; }
                    a { color: #5D4D99; text-decoration: underline; }
                    img { max-width: 100%; height: auto; border-radius: 6px; }
                    blockquote {
                        border-left: 3px solid #B5C7F0;
                        padding-left: 12px;
                        color: #66708A;
                        margin: 8px 0;
                    }
                    strong { color: #2C3148; font-weight: 700; }
                    em { color: #5A6382; }
                    hr { border: none; border-top: 1px solid #B5C7F0; margin: 10px 0; }
                </style>
                </head>
                <body>{{content}}</body>
                </html>
                """;
        }
    }
}
