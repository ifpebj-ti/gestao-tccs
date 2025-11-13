using System.ComponentModel;

namespace gestaotcc.Domain.Enums;

public enum ShiftType
{
    [Description("Matutino")]
    MORNING = 1,

    [Description("Vespertino")]
    AFTERNOON = 2,

    [Description("Noturno")]
    NIGHT = 3,

    [Description("Diurno")]
    DAYTIME = 4
}
