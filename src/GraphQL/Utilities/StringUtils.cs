using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GraphQL.Utilities
{
    public static class StringUtils
    {
        private static Regex reWords = new Regex(@"[A-Z\xc0-\xd6\xd8-\xde]?[a-z\xdf-\xf6\xf8-\xff]+(?:['’](?:d|ll|m|re|s|t|ve))?(?=[\xac\xb1\xd7\xf7\x00-\x2f\x3a-\x40\x5b-\x60\x7b-\xbf\u2000-\u206f \t\x0b\f\xa0\ufeff\n\r\u2028\u2029\u1680\u180e\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000]|[A-Z\xc0-\xd6\xd8-\xde]|$)|(?:[A-Z\xc0-\xd6\xd8-\xde]|[^\ud800-\udfff\xac\xb1\xd7\xf7\x00-\x2f\x3a-\x40\x5b-\x60\x7b-\xbf\u2000-\u206f \t\x0b\f\xa0\ufeff\n\r\u2028\u2029\u1680\u180e\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000\d+\u2700-\u27bfa-z\xdf-\xf6\xf8-\xffA-Z\xc0-\xd6\xd8-\xde])+(?:['’](?:D|LL|M|RE|S|T|VE))?(?=[\xac\xb1\xd7\xf7\x00-\x2f\x3a-\x40\x5b-\x60\x7b-\xbf\u2000-\u206f \t\x0b\f\xa0\ufeff\n\r\u2028\u2029\u1680\u180e\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000]|[A-Z\xc0-\xd6\xd8-\xde](?:[a-z\xdf-\xf6\xf8-\xff]|[^\ud800-\udfff\xac\xb1\xd7\xf7\x00-\x2f\x3a-\x40\x5b-\x60\x7b-\xbf\u2000-\u206f \t\x0b\f\xa0\ufeff\n\r\u2028\u2029\u1680\u180e\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000\d+\u2700-\u27bfa-z\xdf-\xf6\xf8-\xffA-Z\xc0-\xd6\xd8-\xde])|$)|[A-Z\xc0-\xd6\xd8-\xde]?(?:[a-z\xdf-\xf6\xf8-\xff]|[^\ud800-\udfff\xac\xb1\xd7\xf7\x00-\x2f\x3a-\x40\x5b-\x60\x7b-\xbf\u2000-\u206f \t\x0b\f\xa0\ufeff\n\r\u2028\u2029\u1680\u180e\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u202f\u205f\u3000\d+\u2700-\u27bfa-z\xdf-\xf6\xf8-\xffA-Z\xc0-\xd6\xd8-\xde])+(?:['’](?:d|ll|m|re|s|t|ve))?|[A-Z\xc0-\xd6\xd8-\xde]+(?:['’](?:D|LL|M|RE|S|T|VE))?|\d+|(?:[\u2700-\u27bf]|(?:\ud83c[\udde6-\uddff]){2}|[\ud800-\udbff][\udc00-\udfff])[\ufe0e\ufe0f]?(?:[\u0300-\u036f\ufe20-\ufe23\u20d0-\u20f0]|\ud83c[\udffb-\udfff])?(?:\u200d(?:[^\ud800-\udfff]|(?:\ud83c[\udde6-\uddff]){2}|[\ud800-\udbff][\udc00-\udfff])[\ufe0e\ufe0f]?(?:[\u0300-\u036f\ufe20-\ufe23\u20d0-\u20f0]|\ud83c[\udffb-\udfff])?)*");

        /// <summary>
        /// Split a cased string into a series of "words" excluding the seperator if applicable.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static IEnumerable<string> ToWords(string str)
        {
            foreach (Match match in reWords.Matches(str))
            {
                yield return match.Value;
            }
        }

        public static string Capitalize(string str)
        {
            str = str.ToLowerInvariant();
            return str.Substring(0, 1).ToUpperInvariant() + str.Substring(1);
        }

        public static string ToCamelCase(string str)
        {
            return ChangeCase(str, "", (word, index) =>
                (index == 0 ? word.ToLowerInvariant() : Capitalize(word)));
        }

        public static string ToConstantCase(string str)
        {
            return ChangeCase(str, "_", w => w.ToUpperInvariant());
        }

        public static string ToPascalCase(string str)
        {
            return ChangeCase(str, "", Capitalize);
        }


        public static string ChangeCase(string str, string sep, Func<string, string> composer)
        {
            return ChangeCase(str, sep, (w, i) => composer(w));
        }

        public static string ChangeCase(string str, string sep, Func<string, int, string> composer)
        {
            string result = "";
            int index = 0;

            foreach (string word in ToWords(str))
            {
                result += ((index == 0 ? "" : sep) + composer(word, index++));
            }

            return result;
        }


        /// <summary>
        /// Given [ A, B, C ] return '"A", "B", or "C"'.
        /// </summary>
        public static string QuotedOrList(IEnumerable<string> items, int maxLength = 5)
        {
            var itemsArray = items.Take(maxLength).ToArray();
            var index = 0;
            return itemsArray
                .Select(x => $"\"{x}\"")
                .Aggregate((list, quoted) =>
                    list +
                    (itemsArray.Length > 2 ? ", " : " ") +
                    (++index == itemsArray.Length - 1 ? "or " : "") +
                    quoted);
        }

        /// <summary>
        /// Given an invalid input string and a list of valid options, returns a filtered
        /// list of valid options sorted based on their similarity with the input.
        /// </summary>
        public static string[] SuggestionList(string input, IEnumerable<string> options)
        {
            if (options == null)
            {
                return new string[0];
            }

            var optionsByDistance = new Dictionary<string, int>();
            var inputThreshold = input.Length / 2;
            options?.Apply(t =>
            {
                var distance = DamerauLevenshteinDistance(input, t, inputThreshold);
                var threshold = Math.Max(inputThreshold, Math.Max(t.Length / 2, 1));
                if (distance <= threshold)
                {
                    optionsByDistance[t] = distance;
                }
            });

            return optionsByDistance.OrderBy(x => x.Value).Select(x => x.Key).ToArray();
        }

        public static void Swap<T>(ref T arg1, ref T arg2)
        {
            T temp = arg1;
            arg1 = arg2;
            arg2 = temp;
        }

        /// <summary>
        /// Computes the Damerau-Levenshtein Distance between two strings, represented as arrays of
        /// integers, where each integer represents the code point of a character in the source string.
        /// Includes an optional threshhold which can be used to indicate the maximum allowable distance.
        /// http://stackoverflow.com/a/9454016/279764
        /// </summary>
        /// <param name="source">An array of the code points of the first string</param>
        /// <param name="target">An array of the code points of the second string</param>
        /// <param name="threshold">Maximum allowable distance</param>
        /// <returns>Int.MaxValue if threshhold exceeded; otherwise the Damerau-Leveshteim distance between the strings</returns>
        public static int DamerauLevenshteinDistance(string source, string target, int threshold)
        {
            int length1 = source.Length;
            int length2 = target.Length;

            // Return trivial case - difference in string lengths exceeds threshhold
            if (Math.Abs(length1 - length2) > threshold) { return int.MaxValue; }

            // Ensure arrays [i] / length1 use shorter length
            if (length1 > length2)
            {
                Swap(ref target, ref source);
                Swap(ref length1, ref length2);
            }

            int maxi = length1;
            int maxj = length2;

            int[] dCurrent = new int[maxi + 1];
            int[] dMinus1 = new int[maxi + 1];
            int[] dMinus2 = new int[maxi + 1];
            int[] dSwap;

            for (int i = 0; i <= maxi; i++) { dCurrent[i] = i; }

            int jm1 = 0, im1 = 0, im2 = -1;

            for (int j = 1; j <= maxj; j++)
            {
                // Rotate
                dSwap = dMinus2;
                dMinus2 = dMinus1;
                dMinus1 = dCurrent;
                dCurrent = dSwap;

                // Initialize
                int minDistance = int.MaxValue;
                dCurrent[0] = j;
                im1 = 0;
                im2 = -1;

                for (int i = 1; i <= maxi; i++)
                {

                    int cost = source[im1] == target[jm1] ? 0 : 1;

                    int del = dCurrent[im1] + 1;
                    int ins = dMinus1[i] + 1;
                    int sub = dMinus1[im1] + cost;

                    //Fastest execution for min value of 3 integers
                    int min = (del > ins) ? (ins > sub ? sub : ins) : (del > sub ? sub : del);

                    if (i > 1 && j > 1 && source[im2] == target[jm1] && source[im1] == target[j - 2])
                        min = Math.Min(min, dMinus2[im2] + cost);

                    dCurrent[i] = min;
                    if (min < minDistance) { minDistance = min; }
                    im1++;
                    im2++;
                }
                jm1++;
                if (minDistance > threshold) { return int.MaxValue; }
            }

            int result = dCurrent[maxi];
            return (result > threshold) ? int.MaxValue : result;
        }
    }
}
