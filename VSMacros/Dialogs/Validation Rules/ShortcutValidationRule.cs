﻿//-----------------------------------------------------------------------
// <copyright file="ShortcutValidationRule.cs" company="Microsoft Corporation">
//     Copyright Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace VSMacros.Dialogs.Validation_Rules
{
    internal class ShortcutValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string shortcut = value as string;

            Regex rgx = new Regex(@"(ALT+Q, [1-9])");
            MatchCollection matches = rgx.Matches(shortcut);

            if (matches.Count != 1)
            {
                string msg = Resources.ShortcutIsInvalid;
                return new ValidationResult(false, msg);
            }

            return new ValidationResult(true, null);
        }
    }
}
