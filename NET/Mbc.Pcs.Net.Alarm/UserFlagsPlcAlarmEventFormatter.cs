using System.Linq;
using System.Text.RegularExpressions;

namespace Mbc.Pcs.Net.Alarm
{
    /// <summary>
    /// Removes the last Argument from the Messages if the <see cref="AlarmUserFlags.IncludeSerialNo"/> is set
    /// </summary>
    public class UserFlagsPlcAlarmEventFormatter : PlcAlarmEventFormatterBase
    {
        public UserFlagsPlcAlarmEventFormatter()
        {
        }

        public UserFlagsPlcAlarmEventFormatter(IPlcAlarmEventFormatter nextFormater)
        {
            NextFormater = nextFormater;
        }

        public override PlcAlarmEvent Format(PlcAlarmEvent plcAlarmEvent)
        {
            (bool change, string message) = CleanUpMessage(plcAlarmEvent);
            if (change)
            {
                plcAlarmEvent.Message = message;
            }

            return base.Format(plcAlarmEvent);
        }

        protected (bool change, string message) CleanUpMessage(PlcAlarmEvent plcAlarmEvent)
        {
            if (!string.IsNullOrWhiteSpace(plcAlarmEvent.Message) &&
                plcAlarmEvent.UserData != null &&
                (plcAlarmEvent.UserData & (int)AlarmUserFlags.IncludeSerialNo) != 0)
            {
                if (plcAlarmEvent.ArgumentData != null && plcAlarmEvent.ArgumentData.Count >= 1)
                {
                    // Remove last () with known argument value
                    object argument = plcAlarmEvent.ArgumentData.Last();
                    string serialArgument = string.Empty;

                    if (argument != null && !string.IsNullOrEmpty(argument.ToString()))
                    {
                        serialArgument = argument.ToString();
                    }

                    string textToRemove = $" ({serialArgument})";
                    if (TryRemoveLastText(plcAlarmEvent.Message, textToRemove, out string result))
                    {
                        return (true, result);
                    }
                }

                // alternative Regex, remove last ()
                var matches = Regex.Match(plcAlarmEvent.Message, @" ?\(.*?\)", RegexOptions.RightToLeft);
                if (matches.Success && (matches.Index + matches.Length >= plcAlarmEvent.Message.Length))
                {
                    return (true, plcAlarmEvent.Message.Remove(matches.Index, matches.Length));
                }
            }

            // Nothing happens
            return (false, plcAlarmEvent.Message);
        }

        protected bool TryRemoveLastText(string input, string toRemove, out string result)
        {
            int startIdx = input.LastIndexOf(toRemove);
            if (startIdx >= 0 && !string.IsNullOrEmpty(toRemove))
            {
                result = input.Remove(startIdx, toRemove.Length);
                return true;
            }

            result = input;
            return false;
        }
    }
}
