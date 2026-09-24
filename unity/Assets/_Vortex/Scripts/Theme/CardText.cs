using System;
using System.Text;

namespace Vortex.Client.Theme
{
    /// <summary>
    /// Turns the text printed on a card (content files: <c>**bold**</c> and icon tags such as <c>&lt;ATQ&gt;</c>) into
    /// TextMeshPro rich text. Any other markup is shown as plain characters, so content text can never restyle the
    /// screen.
    /// </summary>
    public static class CardText
    {
        private const string LiteralLessThan = "<noparse><</noparse>";

        /// <summary>Converts a card text.</summary>
        /// <param name="text">Text from the content files.</param>
        /// <param name="hasIcon">Whether the theme has a sprite for an icon name. Without one, the icon is left out:
        /// in the content an icon always precedes the word it illustrates, so the sentence still reads.</param>
        public static string ToRichText(string text, Func<string, bool> hasIcon)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (hasIcon is null)
            {
                throw new ArgumentNullException(nameof(hasIcon));
            }

            var result = new StringBuilder(text.Length + 32);
            bool bold = false;
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '*' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    result.Append(bold ? "</b>" : "<b>");
                    bold = !bold;
                    i += 2;
                }
                else if (c == '<')
                {
                    int length = IconTagLength(text, i);
                    if (length == 0)
                    {
                        result.Append(LiteralLessThan);
                        i++;
                        continue;
                    }

                    string icon = text.Substring(i + 1, length - 2);
                    i += length;
                    if (hasIcon(icon))
                    {
                        result.Append("<sprite name=\"").Append(icon).Append("\">");
                    }
                    else if (i < text.Length && text[i] == ' ' && (result.Length == 0 || result[result.Length - 1] == ' '))
                    {
                        // "de <BOU> bouclier" becomes "de bouclier", not "de  bouclier".
                        i++;
                    }
                }
                else
                {
                    result.Append(c);
                    i++;
                }
            }

            if (bold)
            {
                result.Append("</b>");
            }

            return result.ToString();
        }

        // Length of an icon tag starting at `start` ('<', 2 to 5 capital letters, '>'), or 0 if there is none.
        private static int IconTagLength(string text, int start)
        {
            int i = start + 1;
            while (i < text.Length && i - start <= 5 && text[i] >= 'A' && text[i] <= 'Z')
            {
                i++;
            }

            int letters = i - start - 1;
            return letters >= 2 && letters <= 5 && i < text.Length && text[i] == '>' ? letters + 2 : 0;
        }
    }
}
