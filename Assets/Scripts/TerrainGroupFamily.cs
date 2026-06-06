// A "family" of terrain types that score together as one group.
//
// Most terrains form a group only with their own type, so they get their own
// family. The exception is water: water bodies and rivers belong to the SAME
// family, so a river that flows into a lake counts as one continuous group.
public enum TerrainGroupFamily
{
    // Does not form scoring groups at all (plains, empty/city placeholders).
    None = 0,
    Water = 1,
    Railroad = 2,
    Forest = 3,
    Mountains = 4
}
