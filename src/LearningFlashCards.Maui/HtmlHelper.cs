namespace LearningFlashCards.Maui
{
    internal static class HtmlHelper
    {
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
                        background-color: #2C3340;
                        color: #ECEFF5;
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                        font-size: 16px;
                        line-height: 1.65;
                        padding: 14px 16px;
                        word-break: break-word;
                    }
                    h1, h2, h3, h4, h5, h6 {
                        color: #ECEFF5;
                        font-weight: 700;
                        margin-bottom: 8px;
                        line-height: 1.3;
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
                        border: 1px solid #4A5160;
                        padding: 8px 12px;
                        text-align: left;
                        color: #ECEFF5;
                    }
                    th { background-color: #3A4150; font-weight: 700; }
                    tr:nth-child(even) { background-color: #313844; }
                    code {
                        background-color: #1E2430;
                        color: #84AFD7;
                        padding: 2px 6px;
                        border-radius: 4px;
                        font-size: 14px;
                        font-family: 'Cascadia Code', 'Consolas', monospace;
                    }
                    pre {
                        background-color: #1E2430;
                        color: #84AFD7;
                        padding: 12px 14px;
                        border-radius: 8px;
                        overflow-x: auto;
                        font-size: 14px;
                        margin-bottom: 10px;
                        border: 1px solid #4A5160;
                    }
                    pre code { background: none; padding: 0; }
                    a { color: #84AFD7; text-decoration: underline; }
                    img { max-width: 100%; height: auto; border-radius: 6px; }
                    blockquote {
                        border-left: 3px solid #4A5160;
                        padding-left: 12px;
                        color: #B8BFCC;
                        margin: 8px 0;
                    }
                    strong { color: #ECEFF5; font-weight: 700; }
                    em { color: #D0D7E2; }
                    hr { border: none; border-top: 1px solid #4A5160; margin: 10px 0; }
                </style>
                </head>
                <body>{{content}}</body>
                </html>
                """;
        }
    }
}
