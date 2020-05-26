using System;

namespace Seatbelt.Commands
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    class CommandOutputTypeAttribute : Attribute
    {
        public Type Type { get; set; }

        public CommandOutputTypeAttribute(Type outputDTO)
        {
            if (!typeof(CommandDTOBase).IsAssignableFrom(outputDTO))
            {
                throw new Exception($"CommandOutputTypeAttribute: the specified output DTO({outputDTO}) does not inherit from CommandDTOBase");
            }

            Type = outputDTO;
        }
    }
}
