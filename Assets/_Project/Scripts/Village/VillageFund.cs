using UnityEngine;

/// <summary>
/// The Village Fund (Bram's ledger): the players spend Motion Shards on restoration projects. Nothing extra is saved:
///   balance = shards collected - the prices of the projects already funded (never below 0),
/// so the collection record (for 100%) is never reduced. Funding a project calls RestorationProject.Restore, which
/// saves "fund_&lt;id&gt;" and switches every piece of that project in the village to its restored state.
/// Edit names, descriptions and prices here. Region 1 has 24 shards; the four projects cost 22 together.
/// No power upgrades: puzzle numbers never change (see CLAUDE.md).
/// </summary>
public static class VillageFund
{
    public struct Project
    {
        public string id;          // RestorationProject id
        public string name;
        public string description;
        public string where;       // where to see it once funded
        public int price;
    }

    public static readonly Project[] All =
    {
        new Project { id = "well",   price = 3, name = "Repair the well",          description = "A new roof, a rope and a bucket.",       where = "the well in the market square" },
        new Project { id = "lamps",  price = 5, name = "Relight the lamps",        description = "Warm light along every street.",         where = "the lamps on every street" },
        new Project { id = "garden", price = 6, name = "Mend the garden & fences", description = "Fences up, Mira's flowers in bloom.",    where = "Mira's cottage in the square" },
        new Project { id = "shrine", price = 8, name = "Restore the shard shrine", description = "Wake the great crystal in the square.",  where = "the shrine in the market square" },
    };

    public static int Collected => SaveSystem.Data.shards.Count;

    public static int Spent
    {
        get
        {
            int spent = 0;
            foreach (Project p in All) if (IsFunded(p)) spent += p.price;
            return spent;
        }
    }

    public static int Balance => Mathf.Max(0, Collected - Spent);

    public static bool IsFunded(Project p) => RestorationProject.IsRestored(p.id);

    /// <summary>Funds the project if it isn't funded yet and the balance covers it.</summary>
    public static bool Fund(Project p)
    {
        if (IsFunded(p) || Balance < p.price) return false;
        RestorationProject.Restore(p.id);
        return true;
    }
}
