namespace EmployeeDirectory.Core.Enums
{

    public enum Loctype
    {
        Corporate = 1,
        MetalMart = 2,
        ServiceCenter = 3,
        Plant = 4,
        Other = 5
    }

    public static class LoctypeExtensions
    {
        public static string GetDisplayName(this Loctype loctype)
        {
            return loctype switch
            {
                Loctype.Corporate => "Corporate",
                Loctype.MetalMart => "Metal Mart",
                Loctype.ServiceCenter => "Service Center",
                Loctype.Plant => "Plant",
                Loctype.Other => "Other",
                _ => "Unknown"
            };
        }
    }
}
