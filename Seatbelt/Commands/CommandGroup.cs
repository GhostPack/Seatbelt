using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seatbelt.Commands
{
    [Flags]
    public enum CommandGroup
    {
        All,
        User,
        System,
        Slack,
        Chrome,
        Remote,
        Misc,
    }
}
