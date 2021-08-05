using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets.Syntax;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal static class StyleFieldConstants
    {
        // Units
        public static readonly string UnitPixel = "px";
        public static readonly string UnitPercent = "%";
        public static readonly string UnitDegree = "deg";
        public static readonly string UnitGrad = "grad";
        public static readonly string UnitRad = "rad";
        public static readonly string UnitTurn = "turn";

        public static readonly Dictionary<string, Dimension.Unit> StringToDimensionUnitMap = new Dictionary<string, Dimension.Unit>()
        {
            { UnitPixel, Dimension.Unit.Pixel },
            { UnitPercent, Dimension.Unit.Percent },
#if !UI_BUILDER_PACKAGE || UNITY_2021_2_OR_NEWER
            { UnitDegree, Dimension.Unit.Degree },
            { UnitGrad, Dimension.Unit.Gradian },
            { UnitRad, Dimension.Unit.Radian },
            { UnitTurn, Dimension.Unit.Turn }
#endif
        };

        public static readonly Dictionary<Dimension.Unit, string> DimensionUnitToStringMap = new Dictionary<Dimension.Unit, string>()
        {
            { Dimension.Unit.Pixel, UnitPixel },
            { Dimension.Unit.Percent, UnitPercent },
#if !UI_BUILDER_PACKAGE || UNITY_2021_2_OR_NEWER
            { Dimension.Unit.Degree, UnitDegree },
            { Dimension.Unit.Gradian, UnitGrad },
            { Dimension.Unit.Radian, UnitRad },
            { Dimension.Unit.Turn, UnitTurn }
#endif
        };

        // Keywords
        public static readonly string KeywordInitial = "initial";
        public static readonly string KeywordAuto = "auto";
        public static readonly string KeywordNone = "none";

        public static readonly Dictionary<string, StyleValueKeyword> StringToStyleValueKeywordMap = new Dictionary<string, StyleValueKeyword>()
        {
            { "initial", StyleValueKeyword.Initial },
            { "auto", StyleValueKeyword.Auto },
            { "none", StyleValueKeyword.None }
        };

        public static readonly Dictionary<StyleValueKeyword, string> StyleValueKeywordToStringMap = new Dictionary<StyleValueKeyword, string>()
        {
            { StyleValueKeyword.Initial, "initial" },
            { StyleValueKeyword.Auto, "auto" },
            { StyleValueKeyword.None, "none" }
        };

        // Keyword Lists
        public static readonly List<string> KLEmpty = new List<string>() {};
        public static readonly List<string> KLDefault = new List<string>() { KeywordInitial };
        public static readonly List<string> KLAuto = new List<string>() { KeywordAuto, KeywordInitial };
        public static readonly List<string> KLNone = new List<string>() { KeywordNone, KeywordInitial };

        public static List<string> GetStyleKeywords(string binding)
        {
            if (string.IsNullOrEmpty(binding))
                return StyleFieldConstants.KLEmpty;

            var syntaxParser = new StyleSyntaxParser();
            var syntaxFound = StylePropertyCache.TryGetSyntax(binding, out var syntax);

            if (!syntaxFound)
                return StyleFieldConstants.KLDefault;

            var expression = syntaxParser.Parse(syntax);
            if (expression == null)
                return StyleFieldConstants.KLDefault;

            var hasAuto = FindKeywordInExpression(expression, StyleFieldConstants.KeywordAuto);
            var hasNone = FindKeywordInExpression(expression, StyleFieldConstants.KeywordNone);

            if (hasAuto)
                return StyleFieldConstants.KLAuto;
            else if (hasNone)
                return StyleFieldConstants.KLNone;

            return StyleFieldConstants.KLDefault;
        }

        static bool FindKeywordInExpression(Expression expression, string keyword)
        {
            if (expression.type == ExpressionType.Keyword && expression.keyword == keyword)
                return true;

            if (expression.subExpressions == null)
                return false;

            foreach (var subExp in expression.subExpressions)
                if (FindKeywordInExpression(subExp, keyword))
                    return true;

            return false;
        }
    }
}
