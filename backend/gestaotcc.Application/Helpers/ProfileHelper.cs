namespace gestaotcc.Application.Helpers;
public static class ProfileHelper
{
    private static readonly List<string> Hierarchy = new()
    {
        "ADMIN",
        "COORDINATOR",
        "SUPERVISOR",
        "ADVISOR",
        "BANKING"
    };

    public static List<string> ExpandProfiles(List<string> receivedProfiles)
    {
        var expandedProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in receivedProfiles)
        {
            var index = Hierarchy.IndexOf(profile.ToUpper());
            if (index >= 0)
            {
                for (int i = index; i < Hierarchy.Count; i++)
                    expandedProfiles.Add(Hierarchy[i]);
            }
            else
            {
                expandedProfiles.Add(profile.ToUpper());
            }
        }

        return expandedProfiles.ToList();
    }
}
