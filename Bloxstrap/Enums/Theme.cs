namespace Bloxstrap.Enums
{
    public enum Theme
    {
        [EnumName(FromTranslation = "Common.SystemDefault")]
        Default,
        Light,
        Dark,
        [EnumName(StaticName = "AMOLED Black")]
        AmoledBlack,
        [EnumName(StaticName = "Midnight Blue")]
        MidnightBlue,
        [EnumName(StaticName = "Rose")]
        Rose,

        [EnumName(StaticName = "Horrorstrap")]
        Horrorstrap,

        [EnumName(StaticName = "Red")]
        Red,

        [EnumName(StaticName = "Orange")]
        Orange,

        [EnumName(StaticName = "Yellow")]
        Yellow,

        [EnumName(StaticName = "Green")]
        Green,

        [EnumName(StaticName = "Blue")]
        Blue,

        [EnumName(StaticName = "Indigo")]
        Indigo,

        [EnumName(StaticName = "Violet")]
        Violet,

        [EnumName(StaticName = "Custom")]
        Custom
    }
}
